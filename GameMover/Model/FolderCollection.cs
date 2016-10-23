using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using GameMover.Code;
using GameMover.Properties;

using Microsoft.VisualBasic.FileIO;
using Microsoft.VisualStudio.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;

using Prism.Commands;
using Prism.Mvvm;

using static GameMover.Code.StaticMethods;
using static GameMover.Code.ErrorHandling;

namespace GameMover.Model
{
    public sealed class FolderCollection : BindableBase, IDisposable
    {
        public FolderCollection()
        {
            SelectedItems = new ObservableCollection<object>();
            InitDirectoryWatcher();
        }

        private FolderCollection _correspondingCollection;
        public FolderCollection CorrespondingCollection
        {
            get { return _correspondingCollection; }
            set {
                if (_correspondingCollection != null) _correspondingCollection._correspondingCollection = null;
                _correspondingCollection = value;
                if (_correspondingCollection != null) _correspondingCollection._correspondingCollection = this;
            }
        }

        public string FolderBrowserDefaultLocation { get; set; }

        public AsyncObservableCollection<GameFolder> Folders { get; } = new AsyncObservableCollection<GameFolder>();

        private ObservableCollection<object> _selectedItems;
        public ObservableCollection<object> SelectedItems
        {
            get { return _selectedItems; }
            set {
                _selectedItems = value;

                _selectedItems.CollectionChanged += (sender, args) => {
                    ArchiveCommand.RaiseCanExecuteChanged();
                    CopyCommand.RaiseCanExecuteChanged();
                    CreateJunctionCommand.RaiseCanExecuteChanged();
                    DeleteFoldersCommand.RaiseCanExecuteChanged();
                    DeleteJunctionsCommand.RaiseCanExecuteChanged();
                };
            }
        }

        public IEnumerable<GameFolder> SelectedFolders
            => SelectedItems?.Reverse().Cast<GameFolder>().Where(folder => !folder.IsBeingDeleted) ?? Enumerable.Empty<GameFolder>();

        private FileSystemWatcher DirectoryWatcher { get; } = new FileSystemWatcher();

        private bool BothCollectionsInitialized { get; set; }

        private FileStream _directoryLockFileStream;

        private string _location;
        public string Location
        {
            get { return _location; }
            set {
                _location = Directory.Exists(value) ? value : null;

                DisplayBusyDuring(() => {
                    CorrespondingCollection.BothCollectionsInitialized =
                        BothCollectionsInitialized = Location != null && CorrespondingCollection.Location != null;

                    DirectoryWatcher.EnableRaisingEvents = false;
                    _directoryLockFileStream?.Close();

                    foreach (var folder in Folders)
                    {
                        folder.CancelSubdirectorySearch();
                    }

                    Folders.Clear();

                    // If the location doesn't exist (ie a saved location that has since been deleted) just ignore it
                    if (Directory.Exists(Location)) SetNewLocationImpl(Location);
                });
            }
        }

        private void SetNewLocationImpl(string loc)
        {
            try
            {
                // Attempt to create a hidden file that will prevent the user from renaming the directory currently being observed
                var directoryLockFilePath = Path.Combine(loc, $"{nameof(GameMover)}DirectoryLock.tmp");
                _directoryLockFileStream = new FileStream(directoryLockFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 4096,
                    FileOptions.DeleteOnClose);
                File.SetAttributes(directoryLockFilePath, FileAttributes.Hidden);
            }
            catch (Exception)
            {
                // This does not need to succeed for the application to function
            }

            DirectoryWatcher.Path = Location;
            DirectoryWatcher.EnableRaisingEvents = true;

            try
            {
                foreach (var directoryInfo in new DirectoryInfo(loc)
                    .EnumerateDirectories()
                    .Where(info => (info.Attributes & (FileAttributes.System | FileAttributes.Hidden)) == 0))
                {
                    Folders.Add(new GameFolder(directoryInfo));
                }
            }
            catch (IOException e)
            {
                HandleException(e);
            }
        }

        #region Commands
        //TODO test
        [AutoLazy.Lazy]
        public DelegateCommand ArchiveCommand => new DelegateCommand(async () => {
            foreach (var folder in SelectedFolders)
            {
                GameFolder createdFolder = await CorrespondingCollection.CopyFolder(folder);
                if (createdFolder != null)
                {
                    var isFolderDeleted = await DeleteFolder(folder);
                    if (isFolderDeleted) CreateJunctionTo(createdFolder);
                }
            }
        }, () => SelectedFolders.Any());

        [AutoLazy.Lazy]
        public DelegateCommand CopyCommand => new DelegateCommand(async () => {
            await Task.WhenAll(SelectedFolders.Select(CorrespondingCollection.CopyFolder));
        }, () => SelectedFolders.Any(folder => !folder.IsJunction));

        [AutoLazy.Lazy]
        public DelegateCommand CreateJunctionCommand => new DelegateCommand(() => {
            foreach (var folder in SelectedFolders)
            {
                CorrespondingCollection.CreateJunctionTo(folder);
            }
        }, () => SelectedFolders.Any(folder => !folder.IsJunction));

        [AutoLazy.Lazy]
        public DelegateCommand DeleteFoldersCommand => new DelegateCommand(() => {
            foreach (var folder in SelectedFolders)
            {
                DeleteFolder(folder);
            }
        }, () => SelectedFolders.Any(folder => !folder.IsJunction));

        [AutoLazy.Lazy]
        public DelegateCommand DeleteJunctionsCommand => new DelegateCommand(() => {
            foreach (var folder in SelectedFolders)
            {
                DeleteJunction(folder);
            }
        }, () => SelectedFolders.Any(folder => folder.IsJunction));

        [AutoLazy.Lazy]
        public DelegateCommand SelectFoldersNotInOtherPaneCommand => new DelegateCommand(() => {
            //TODO: does not work correctly with junctions with a different name than their target
            SelectFolders(Folders.Except(CorrespondingCollection.Folders));
        }).ObservesCanExecute(_ => BothCollectionsInitialized);

        [AutoLazy.Lazy]
        public DelegateCommand SelectLocationCommand => new DelegateCommand(() => {
            var folderDialog = NewFolderDialog(Resources.SelectLocationCommand_Select_directory_containing_folders);
            folderDialog.DefaultDirectory = FolderBrowserDefaultLocation;
            if (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Location = folderDialog.FileName;
            }
        });
        #endregion

        public void SelectFolders(IEnumerable<GameFolder> folders) => SelectedItems.ReplaceWithRange(folders);

        public void Refresh()
        {
            if (!Directory.Exists(Location)) Location = null;

            foreach (var folder in Folders)
            {
                folder.RecalculateSize();
            }
        }

        public void Dispose() => DirectoryWatcher.Dispose();

        private void InitDirectoryWatcher()
        {
            DirectoryWatcher.NotifyFilter = NotifyFilters.DirectoryName;
            DirectoryWatcher.InternalBufferSize = 40960;
            DirectoryWatcher.Created += (sender, args) => {
                var newFolder = new GameFolder(args.FullPath);
                Folders.Add(newFolder);
                newFolder.ContinuoslyRecalculateSize().Forget();
            };
            DirectoryWatcher.Deleted += (sender, args) => {
                Folders.Remove(FolderByName(args.Name));
            };
            DirectoryWatcher.Renamed += (sender, args) => {
                FolderByName(args.OldName).Rename(args.Name);
            };
        }

        private GameFolder FolderByName(string name)
        {
            return Folders.FirstOrDefault(folder => folder.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private void CreateJunctionTo(GameFolder junctionTarget)
        {
            try
            {
                CheckLocationExists(Location);

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

        /// <summary>
        ///     Copies the provided folder to the current directory. Returns the created/overwritten folder on success, null on
        ///     error.
        /// </summary>
        private Task<GameFolder> CopyFolder(GameFolder folderToCopy)
        {
            return Task.Run(() => {
                string targetDirectory = $@"{Location}\{folderToCopy.Name}";

                var targetDirectoryInfo = new DirectoryInfo(targetDirectory);
                var isOverwrite = targetDirectoryInfo.Exists;

                try
                {
                    CheckLocationExists(Location);

                    if (isOverwrite)
                    {
                        var overwrittenFolder = FolderByName(targetDirectoryInfo.Name);
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
                    var createdFolder = FolderByName(targetDirectoryInfo.Name);
                    // Send a final recalculation request in case the user had previously paused the operation, or if there was a pause while answering the fprompt for replacing vs skipping duplicate files.
                    createdFolder.RecalculateSize();
                    return createdFolder;
                }
                catch (OperationCanceledException e)
                {
                    Debug.WriteLine(e);
                    // If the user cancels the folder will still be partially copied
                    var createdFolder = FolderByName(targetDirectoryInfo.Name);
                    createdFolder.RecalculateSize();
                    return createdFolder;
                }
                catch (IOException e)
                {
                    HandleException(e);
                    return null;
                }
            });
        }

        /// <summary>Returns true on successful delete, false if user cancels operation or there is an error</summary>
        private Task<bool> DeleteFolder(GameFolder folderToDelete)
        {
            folderToDelete.IsBeingDeleted = true;
            return Task.Run(() => {
                try
                {
                    CheckLocationExists(Location);

                    FileSystem.DeleteDirectory(folderToDelete.DirectoryInfo.FullName, UIOption.OnlyErrorDialogs,
                        RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
                }
                catch (Exception e) when (e is OperationCanceledException || e is IOException)
                {
                    folderToDelete.IsBeingDeleted = false;
                    if (e is IOException) HandleException(e);
                    //Do nothing if they cancel
                    return false;
                }

                //Delete junctions pointing to the deleted folder
                CorrespondingCollection.Folders.Where(folder => folder.IsJunction &&
                                                                folder.JunctionTarget.Equals(folderToDelete.DirectoryInfo.FullName))
                                       .ForEach(DeleteJunction);
                return true;
            });
        }

        private void DeleteJunction(GameFolder folder) => DeleteJunction(folder.DirectoryInfo);

        private void DeleteJunction(DirectoryInfo junctionDirectory)
        {
            try
            {
                CheckLocationExists(Location);

                JunctionPoint.Delete(junctionDirectory);
            }
            catch (IOException e)
            {
                HandleException(e);
            }
        }
    }
}
