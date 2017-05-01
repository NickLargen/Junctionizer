using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Junctionizer.CustomWpfComponents;
using Junctionizer.Properties;

using Microsoft.VisualBasic.FileIO;

using Prism.Commands;
using Prism.Mvvm;

using Utilities;
using Utilities.Collections;

using static Junctionizer.StaticMethods;
using static Junctionizer.ErrorHandling;

namespace Junctionizer.Model
{
    public sealed class FolderCollection : BindableBase, IDisposable
    {
        public static class Factory
        {
            public static (FolderCollection source, FolderCollection destination) CreatePair(Func<bool> isDesiredThread = null, PauseTokenSource pauseTokenSource = default(PauseTokenSource))
            {
                var source = new FolderCollection(isDesiredThread, pauseTokenSource);
                var destination = new FolderCollection(isDesiredThread, pauseTokenSource);
                source.CorrespondingCollection = destination;
                destination.CorrespondingCollection = source;

                return (source, destination);
            }
        }

        // ReSharper disable once NotNullMemberIsNotInitialized - CorrespondingCollection can't be initialized here because it doesn't exist yet
        private FolderCollection(Func<bool> isDesiredThread, PauseTokenSource pauseTokenSource)
        {
            PauseTokenSource = pauseTokenSource;
            if (PauseTokenSource != null) PauseToken = pauseTokenSource.Token;

            Folders = new AsyncObservableKeyedSet<string, GameFolder>(folder => folder.Name, isDesiredThread,
                comparer: StringComparer.OrdinalIgnoreCase);

            InitDirectoryWatcher();

            Folders.CollectionChanged += (sender, args) => {
                switch (args.Action) {
                    case NotifyCollectionChangedAction.Add:
                    case NotifyCollectionChangedAction.Remove:
                    case NotifyCollectionChangedAction.Replace:
                        args.OldItems?.OfType<GameFolder>().Where(folder => folder.IsJunction).ForEach(folder => {
                            JunctionTargetsDictionary.Remove(folder.JunctionTarget, folder);
                        });

                        args.NewItems?.OfType<GameFolder>().Where(folder => folder.IsJunction).ForEach(folder => {
                            JunctionTargetsDictionary.Add(folder.JunctionTarget, folder);
                        });
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        JunctionTargetsDictionary.Clear();
                        break;
                    case NotifyCollectionChangedAction.Move:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            };
        }

        [NotNull]
        public FolderCollection CorrespondingCollection { get; private set; }

        [NotNull]
        public string FolderBrowserInitialLocation { get; set; } = string.Empty;

        /// <summary>A dictionary where the keys are junction targets within this collection and the values are a list of the folders with that target (simply the opposite of the normal Folder -> junction target relationship).</summary>
        [NotNull]
        private MultiValueDictionary<string, GameFolder> JunctionTargetsDictionary { get; } =
            new MultiValueDictionary<string, GameFolder>(StringComparer.OrdinalIgnoreCase);

        [NotNull]
        public AsyncObservableKeyedSet<string, GameFolder> Folders { get; }
        
        // The initial value is used when running headlessly, WPF bindings should replace it with a SelectedItemsCollection
        [NotNull]
        public ObservableCollection<object> SelectedItems { get; set; } = new ObservableCollection<object>();

        [NotNull]
        public IEnumerable<GameFolder> AllSelectedGameFolders =>
            SelectedItems.Reverse().Cast<GameFolder>().Where(folder => !folder.IsBeingAccessed);
        [NotNull]
        public IEnumerable<GameFolder> SelectedFolders => AllSelectedGameFolders.Where(folder => !folder.IsJunction);
        [NotNull]
        public IEnumerable<GameFolder> SelectedJunctions => AllSelectedGameFolders.Where(folder => folder.IsJunction);

        public bool BothCollectionsInitialized => Location != null && CorrespondingCollection.Location != null;

        [NotNull] private readonly FileSystemWatcher _directoryWatcher = new FileSystemWatcher();

        [CanBeNull] private PauseTokenSource PauseTokenSource { get; }
        private PauseToken PauseToken { get; }

        [CanBeNull] private FileStream _directoryLockFileStream;

        private string _location;
        [CanBeNull]
        public string Location
        {
            get => _location;
            set {
                _location = Directory.Exists(value) && !string.Equals(CorrespondingCollection.Location, value, StringComparison.OrdinalIgnoreCase) ? value : null;

                DisplayBusyDuring(() => {
                    // Fody PropertyChanged handles raising a change event for this collections BothCollectionsInitialized
                    CorrespondingCollection.RaisePropertyChanged(nameof(BothCollectionsInitialized));

                    _directoryWatcher.EnableRaisingEvents = false;
                    _directoryLockFileStream?.Dispose();

                    foreach (var folder in Folders)
                    {
                        folder.CancelSubdirectorySearch();
                    }

                    Folders.ClearAsync().RunTaskSynchronously();

                    // If the location doesn't exist (ie a saved location that has since been deleted) just ignore it
                    if (Directory.Exists(Location)) SetNewLocationImpl(Location);
                });
            }
        }

        private void SetNewLocationImpl(string loc)
        {
            if (LockActiveDirectory)
            {
                try
                {
                    // Attempt to create a hidden file that will prevent the user from renaming the directory currently being observed
                    var directoryLockFilePath = Path.Combine(loc, $"{nameof(Junctionizer)}DirectoryLock.tmp");
                    _directoryLockFileStream = new FileStream(directoryLockFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None,
                        4096, FileOptions.DeleteOnClose);
                    File.SetAttributes(directoryLockFilePath, FileAttributes.Hidden);
                }
                catch (Exception)
                {
                    // This does not need to succeed for the application to function
                }
            }

            _directoryWatcher.Path = Location;
            _directoryWatcher.EnableRaisingEvents = true;

            try
            {
                var newFolders = new DirectoryInfo(loc)
                    .EnumerateDirectories()
                    .Where(info => (info.Attributes & (FileAttributes.System | FileAttributes.Hidden)) == 0)
                    .Select(info => new GameFolder(info, PauseToken));

                Folders.AddAllAsync(newFolders).RunTaskSynchronously();
            }
            catch (IOException e)
            {
                HandleException(e);
            }
        }


        #region Commands

        [AutoLazy.Lazy]
        public IListCommand ArchiveSelectedCommand => new PausingListCommand<GameFolder>(
            () => BothCollectionsInitialized ? SelectedFolders : Enumerable.Empty<GameFolder>(),
            ArchiveAsync, PauseTokenSource);
        
        [AutoLazy.Lazy]
        public IListCommand CopySelectedCommand => new PausingListCommand<GameFolder>(
            () => BothCollectionsInitialized ? SelectedFolders : Enumerable.Empty<GameFolder>(),
            folder => CorrespondingCollection.CopyFolderAsync(folder), PauseTokenSource);
        
        [AutoLazy.Lazy]
        public IListCommand CreateSelectedJunctionCommand => new PausingListCommand<GameFolder>(
            () => BothCollectionsInitialized ? SelectedFolders : Enumerable.Empty<GameFolder>(),
            folder => {
                CorrespondingCollection.CreateJunctionTo(folder);
                return Task.CompletedTask;
            }, PauseTokenSource);

        [AutoLazy.Lazy]
        public IListCommand DeleteSelectedFoldersCommand => new PausingListCommand<GameFolder>(
            () => SelectedFolders, 
            DeleteFolderOrJunctionAsync, PauseTokenSource);

        [AutoLazy.Lazy]
        public IListCommand DeleteSelectedJunctionsCommand => new PausingListCommand<GameFolder>(
            () => SelectedJunctions,
            folder => {
                DeleteJunction(folder);
                return Task.CompletedTask;
            }, PauseTokenSource);

        [AutoLazy.Lazy]
        public DelegateCommand SelectFoldersNotInOtherPaneCommand => new DelegateCommand(SelectUniqueFolders)
            .ObservesCanExecute(() => BothCollectionsInitialized);

        /// <summary>Selects all folders that do not exist in the corresponding collection.</summary>
        public void SelectUniqueFolders()
        {
            var foldersToSelect = Folders.Where(folder =>
                // Same name
                    !CorrespondingCollection.Folders.ContainsKey(folder.Name) &&

                    // Other pane has a junction pointing to this folder
                    !CorrespondingCollection.GetFoldersByJunctionTarget(folder.DirectoryInfo).Any() &&

                    // This folder is a junction pointing to a folder in the other pane
                    !(folder.JunctionTarget != null &&
                      CorrespondingCollection.Folders.TryGetValue(Path.GetFileName(folder.JunctionTarget), out var junctionTargetFolder) &&
                      string.Equals(junctionTargetFolder.DirectoryInfo.FullName, folder.JunctionTarget, StringComparison.OrdinalIgnoreCase)));

            SelectFolders(foldersToSelect);
        }

        [AutoLazy.Lazy]
        public DelegateCommand SelectLocationCommand => new DelegateCommand(() => SelectLocationAsync().Forget());

        public async Task SelectLocationAsync()
        {
            var directoryInfo = await Dialogs.PromptForDirectory(Resources.SelectLocationCommand_Select_directory_containing_folders, FolderBrowserInitialLocation);
            if (directoryInfo != null) FolderBrowserInitialLocation = Location = directoryInfo.FullName;
        }

        #endregion


        public void SelectFolders(IEnumerable<GameFolder> folders) => SelectedItems.ReplaceSelectedItems(folders);

        public void RefreshSizes()
        {
            if (!Directory.Exists(Location)) Location = null;

            foreach (var folder in Folders)
            {
                folder.RecalculateSizeAsync();
            }
        }

        private void InitDirectoryWatcher()
        {
            _directoryWatcher.NotifyFilter = NotifyFilters.DirectoryName;
            _directoryWatcher.InternalBufferSize = 40960;
            _directoryWatcher.Created += (sender, args) => {
                var newFolder = new GameFolder(args.FullPath, PauseToken);
                Folders.AddAsync(newFolder).Forget();
            };
            _directoryWatcher.Deleted += (sender, args) => {
                Folders.RemoveKeyAsync(args.Name).Forget();
            };
            _directoryWatcher.Renamed += (sender, args) => {
                var folder = GetFolderByName(args.OldName);
                Folders.RemoveKeyAsync(args.OldName).ContinueWith(task => {
                    folder.Rename(args.Name);
                    Folders.AddAsync(folder);
                });
            };
        }

        [NotNull]
        [Pure]
        private GameFolder GetFolderByName([NotNull] string name) => Folders[name];

        [ItemNotNull]
        [NotNull]
        [Pure]
        public IEnumerable<GameFolder> GetFoldersByJunctionTarget([NotNull] DirectoryInfo info)
        {
            return JunctionTargetsDictionary.TryGetValue(info.FullName, out var enumerable) ? enumerable : Enumerable.Empty<GameFolder>();
        }

        public async Task ArchiveAsync([NotNull] GameFolder folder)
        {
            var createdFolder = await CorrespondingCollection.CopyFolderAsync(folder).ConfigureAwait(false);
            if (createdFolder != null)
            {
                var isFolderDeleted = await DeleteFolderOrJunctionAsync(folder).ConfigureAwait(false);
                if (isFolderDeleted) CreateJunctionTo(createdFolder);
            }
        }

        public void CreateJunctionTo([NotNull] GameFolder junctionTarget)
        {
            try
            {
                ThrowIfDirectoryNotFound(Location);

                var junctionDirectory = new DirectoryInfo(Location + @"\" + junctionTarget.Name);
                if (junctionDirectory.Exists == false)
                {
                    JunctionPoint.Create(junctionDirectory, junctionTarget.DirectoryInfo, false);
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                HandleException(e);
            }
        }

        /// <summary>Copies the provided folder to the current directory. Returns the created/overwritten folder only if the copy successfully ran to completion, null otherwise.</summary>
        public Task<GameFolder> CopyFolderAsync([NotNull] GameFolder folderToCopy)
        {
            folderToCopy.IsBeingAccessed = true;
            return Task.Run(async () => {
                string targetDirectory = $@"{Location}\{folderToCopy.Name}";

                var targetDirectoryInfo = new DirectoryInfo(targetDirectory);
                var isOverwrite = targetDirectoryInfo.Exists;

                try
                {
                    ThrowIfDirectoryNotFound(Location);

                    if (isOverwrite)
                    {
                        var overwrittenFolder = GetFolderByName(targetDirectoryInfo.Name);
                        if (overwrittenFolder.IsJunction)
                        {
                            // If the target is a junction, delete it and proceed normally
                            DeleteJunction(targetDirectoryInfo);
                        }
                        else
                        {
                            // Since a new folder isn't being created the file system watcher will not trigger size recalculation, so we do it here
                            overwrittenFolder.RecalculateSizeAsync().Forget();
                        }
                    }

                    FileSystem.CopyDirectory(folderToCopy.DirectoryInfo.FullName, targetDirectory, UIOption.AllDialogs);

                    var createdFolder = await Folders.GetValueAsync(targetDirectoryInfo.Name).ConfigureAwait(false);

                    return createdFolder;
                }
                catch (OperationCanceledException e)
                {
                    return null;
                }
                catch (IOException e)
                {
                    HandleException(e);
                    return null;
                }
                finally
                {
                    folderToCopy.IsBeingAccessed = false;
                }
            });
        }

        /// <summary>Returns true on successful delete, false if user cancels operation or there is an error.</summary>
        public Task<bool> DeleteFolderOrJunctionAsync([NotNull] GameFolder folderToDelete)
        {
            folderToDelete.IsBeingAccessed = true;
            folderToDelete.IsBeingDeleted = true;
            return Task.Run(() => {
                try
                {
                    ThrowIfDirectoryNotFound(Location);

                    FileSystem.DeleteDirectory(folderToDelete.DirectoryInfo.FullName, UIOption.OnlyErrorDialogs,
                        RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
                }
                catch (Exception e) when (e is OperationCanceledException || e is IOException)
                {
                    folderToDelete.IsBeingDeleted = false;
                    //Do nothing if they cancel
                    if (e is OperationCanceledException) return false;

                    HandleException(e);
                }
                finally
                {
                    folderToDelete.IsBeingAccessed = false;
                }

                //Delete junctions pointing to the deleted folder
                CorrespondingCollection.GetFoldersByJunctionTarget(folderToDelete.DirectoryInfo).ToList().ForEach(DeleteJunction);

                return true;
            });
        }

        public void DeleteJunction([NotNull] GameFolder folder) => DeleteJunction(folder.DirectoryInfo);

        private void DeleteJunction([NotNull] DirectoryInfo junctionDirectory)
        {
            try
            {
                ThrowIfDirectoryNotFound(Location);

                JunctionPoint.Delete(junctionDirectory);
            }
            catch (IOException e)
            {
                HandleException(e);
            }
        }

        public void Dispose()
        {
            // ReSharper disable once UseNullPropagation because Microsoft's code analysis doesn't detect it as properly disposing...
            if (_directoryLockFileStream != null) _directoryLockFileStream.Dispose();
            _directoryWatcher.Dispose();
        }
    }
}
