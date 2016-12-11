using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

using GameMover.Code;
using GameMover.Properties;

using JetBrains.Annotations;

using Microsoft.VisualBasic.FileIO;
using Microsoft.VisualStudio.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;

using Prism.Commands;
using Prism.Mvvm;

using Utilities.Collections;
using Utilities.Tasks;

using static GameMover.Code.StaticMethods;
using static GameMover.Code.ErrorHandling;

namespace GameMover.Model
{
    public sealed class FolderCollection : BindableBase, IDisposable
    {
        public static class Factory
        {
            public static (FolderCollection source, FolderCollection destination) CreatePair(Func<bool> isDesiredThread = null)
            {
                var source = new FolderCollection(isDesiredThread);
                var destination = new FolderCollection(isDesiredThread);
                source.CorrespondingCollection = destination;
                destination.CorrespondingCollection = source;

                return (source, destination);
            }
        }

        // ReSharper disable once NotNullMemberIsNotInitialized - CorrespondingCollection can't be initialized here because it doesn't exist yet
        private FolderCollection(Func<bool> isDesiredThread)
        {
            Folders = new AsyncObservableKeyedSet<string, GameFolder>(folder => folder.Name, isDesiredThread,
                comparer: StringComparer.OrdinalIgnoreCase);

            // This is used when running headlessly, WPF bindings should replace it with a SelectedItemsCollection
            SelectedItems = new ObservableCollection<object>();
            InitDirectoryWatcher();

            Folders.CollectionChanged += (sender, args) => {
                if (args.Action == NotifyCollectionChangedAction.Add ||
                    args.Action == NotifyCollectionChangedAction.Remove ||
                    args.Action == NotifyCollectionChangedAction.Replace)
                {
                    args.OldItems?.OfType<GameFolder>().Where(folder => folder.IsJunction).ForEach(folder => {
                        JunctionTargetsDictionary.Remove(folder.JunctionTarget, folder);
                    });

                    args.NewItems?.OfType<GameFolder>().Where(folder => folder.IsJunction).ForEach(folder => {
                        JunctionTargetsDictionary.Add(folder.JunctionTarget, folder);
                    });
                }
            };
        }

        [NotNull]
        public FolderCollection CorrespondingCollection { get; private set; }

        [NotNull]
        public string FolderBrowserDefaultLocation { get; set; } = string.Empty;

        /// <summary>A dictionary where the keys are junction targets within this collection and the values are a list of the folders with that target (simply the opposite of the normal Folder -> junction target relationship).</summary>
        [NotNull]
        private MultiValueDictionary<string, GameFolder> JunctionTargetsDictionary { get; } =
            new MultiValueDictionary<string, GameFolder>(StringComparer.OrdinalIgnoreCase);

        [NotNull]
        public AsyncObservableKeyedSet<string, GameFolder> Folders { get; }

        private ObservableCollection<object> _selectedItems;
        [NotNull]
        public ObservableCollection<object> SelectedItems
        {
            get { return _selectedItems; }
            set {
                _selectedItems = value;

                Observable.FromEventPattern(_selectedItems, nameof(_selectedItems.CollectionChanged))
                          .Throttle(TimeSpan.FromMilliseconds(1))
                          .Subscribe(pattern => {
                              ArchiveSelectedCommand.RaiseCanExecuteChanged();
                              CopySelectedCommand.RaiseCanExecuteChanged();
                              CreateSelectedJunctionCommand.RaiseCanExecuteChanged();
                              DeleteSelectedFoldersCommand.RaiseCanExecuteChanged();
                              DeleteSelectedJunctionsCommand.RaiseCanExecuteChanged();
                          });
            }
        }

        [NotNull]
        public IEnumerable<GameFolder> SelectedFolders =>
            SelectedItems.Reverse().Cast<GameFolder>().Where(folder => !folder.IsBeingDeleted);

        private bool BothCollectionsInitialized => Location != null && CorrespondingCollection.Location != null;

        [NotNull] private readonly FileSystemWatcher _directoryWatcher = new FileSystemWatcher();

        [CanBeNull] private FileStream _directoryLockFileStream;

        private string _location;
        [CanBeNull]
        public string Location
        {
            get { return _location; }
            set {
                _location = Directory.Exists(value) ? value : null;

                DisplayBusyDuring(() => {
                    // Fody PropertyChanged handless raising a change event for this collections BothCollectionsInitialized
                    CorrespondingCollection?.OnPropertyChanged(nameof(BothCollectionsInitialized));

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
                    var directoryLockFilePath = Path.Combine(loc, $"{nameof(GameMover)}DirectoryLock.tmp");
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
                    .Select(info => new GameFolder(info));

                Folders.AddAllAsync(newFolders).RunTaskSynchronously();
            }
            catch (IOException e)
            {
                HandleException(e);
            }
        }


        #region Commands

        [AutoLazy.Lazy]
        public DelegateCommand ArchiveSelectedCommand => new DelegateCommand(() => ArchiveSelected(),
            () => BothCollectionsInitialized && SelectedFolders.Any(folder => !folder.IsJunction));

        public Task ArchiveSelected() => Task.WhenAll(SelectedFolders.Where(folder => !folder.IsJunction).Select(Archive));

        [AutoLazy.Lazy]
        public DelegateCommand CopySelectedCommand => new DelegateCommand(() => CopySelectedFolders(),
            () => BothCollectionsInitialized && SelectedFolders.Any(folder => !folder.IsJunction));

        public Task CopySelectedFolders() => Task.WhenAll(SelectedFolders.Where(folder => !folder.IsJunction).Select(CorrespondingCollection.CopyFolder));

        [AutoLazy.Lazy]
        public DelegateCommand CreateSelectedJunctionCommand => new DelegateCommand(CreateSelectedJunctions,
            () => BothCollectionsInitialized && SelectedFolders.Any(folder => !folder.IsJunction));

        public void CreateSelectedJunctions() => SelectedFolders.Where(folder => !folder.IsJunction).ForEach(CorrespondingCollection.CreateJunctionTo);

        [AutoLazy.Lazy]
        public DelegateCommand DeleteSelectedFoldersCommand => new DelegateCommand(() => DeleteSelectedFolders(),
            () => SelectedFolders.Any(folder => !folder.IsJunction));

        public Task DeleteSelectedFolders() => Task.WhenAll(SelectedFolders.Where(folder => !folder.IsJunction).Select(DeleteFolder));

        [AutoLazy.Lazy]
        public DelegateCommand DeleteSelectedJunctionsCommand => new DelegateCommand(DeleteSelectedJunctions,
            () => SelectedFolders.Any(folder => folder.IsJunction));

        public void DeleteSelectedJunctions() => SelectedFolders.Where(folder => folder.IsJunction).ForEach(DeleteJunction);

        [AutoLazy.Lazy]
        public DelegateCommand SelectFoldersNotInOtherPaneCommand => new DelegateCommand(SelectFoldersNotInOtherPane)
            .ObservesCanExecute(_ => BothCollectionsInitialized);

        public void SelectFoldersNotInOtherPane()
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
        public DelegateCommand SelectLocationCommand => new DelegateCommand(SelectLocation);

        public void SelectLocation()
        {
            var folderDialog = NewFolderDialog(Resources.SelectLocationCommand_Select_directory_containing_folders);
            folderDialog.DefaultDirectory = FolderBrowserDefaultLocation;
            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Location = folderDialog.FileName;
            }
        }

        #endregion


        public void SelectFolders(IEnumerable<GameFolder> folders) => SelectedItems.ReplaceSelectedItems(folders);

        public void Refresh()
        {
            if (!Directory.Exists(Location)) Location = null;

            foreach (var folder in Folders)
            {
                folder.RecalculateSize();
            }
        }

        private void InitDirectoryWatcher()
        {
            _directoryWatcher.NotifyFilter = NotifyFilters.DirectoryName;
            _directoryWatcher.InternalBufferSize = 40960;
            _directoryWatcher.Created += (sender, args) => {
                var newFolder = new GameFolder(args.FullPath);
                newFolder.ContinuoslyRecalculateSize().Forget();
                Folders.AddAsync(newFolder).Forget();
            };
            _directoryWatcher.Deleted += (sender, args) => {
                Folders.RemoveKeyAsync(args.Name).Forget();
            };
            _directoryWatcher.Renamed += (sender, args) => {
                var folder = GetFolderByName(args.OldName);
                Folders.UpdateKeyAsync(folder, () => folder.Rename(args.Name));
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

        private async Task Archive([NotNull] GameFolder folder)
        {
            var createdFolder = await CorrespondingCollection.CopyFolder(folder);
            if (createdFolder != null)
            {
                var isFolderDeleted = await DeleteFolder(folder);
                if (isFolderDeleted) CreateJunctionTo(createdFolder);
            }
        }

        private void CreateJunctionTo([NotNull] GameFolder junctionTarget)
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
        private Task<GameFolder> CopyFolder([NotNull] GameFolder folderToCopy)
        {
            return Task.Run(() => {
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
                            overwrittenFolder.ContinuoslyRecalculateSize().Forget();
                        }
                    }

                    FileSystem.CopyDirectory(folderToCopy.DirectoryInfo.FullName, targetDirectory, UIOption.AllDialogs);
                    var createdFolder = GetFolderByName(targetDirectoryInfo.Name);
                    // Send a final recalculation request in case the user had previously paused the operation, or if there was a pause while answering the prompt for replacing vs skipping duplicate files.
                    createdFolder.RecalculateSize();
                    return createdFolder;
                }
                catch (OperationCanceledException e)
                {
                    Debug.WriteLine(e);
                    // If the user cancels the folder will still be partially copied
                    var createdFolder = GetFolderByName(targetDirectoryInfo.Name);
                    createdFolder.RecalculateSize();
                    return null;
                }
                catch (IOException e)
                {
                    HandleException(e);
                    return null;
                }
            });
        }

        /// <summary>Returns true on successful delete, false if user cancels operation or there is an error</summary>
        private Task<bool> DeleteFolder([NotNull] GameFolder folderToDelete)
        {
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

                //Delete junctions pointing to the deleted folder
                CorrespondingCollection.GetFoldersByJunctionTarget(folderToDelete.DirectoryInfo).ForEach(DeleteJunction);

                return true;
            });
        }

        private void DeleteJunction([NotNull] GameFolder folder) => DeleteJunction(folder.DirectoryInfo);

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
