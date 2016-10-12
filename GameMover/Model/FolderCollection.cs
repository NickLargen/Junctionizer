using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

using GameMover.Code;
using GameMover.External_Code;
using GameMover.Properties;

using Microsoft.VisualBasic.FileIO;
using Microsoft.WindowsAPICodePack.Dialogs;

using Prism.Commands;
using Prism.Mvvm;

using static GameMover.Code.StaticMethods;

namespace GameMover.Model
{

    public sealed class FolderCollection : BindableBase, IDisposable
    {

        public FolderCollection CorrespondingCollection { get; set; }


        public string FolderBrowserDefaultLocation { get; set; }

        public AsyncObservableCollection<GameFolder> Folders { get; } = new AsyncObservableCollection<GameFolder>();

        public ObservableCollection<object> SelectedItems
        {
            get { return _selectedItems; }
            set {
                _selectedItems = value;

                SelectedItems.CollectionChanged += (sender, args) => {
                    ArchiveToStorageCommand.RaiseCanExecuteChanged();
                    CopyCommand.RaiseCanExecuteChanged();
                    CreateJunctionCommand.RaiseCanExecuteChanged();
                    DeleteFoldersCommand.RaiseCanExecuteChanged();
                    DeleteJunctionsCommand.RaiseCanExecuteChanged();
                };
//                ArchiveToStorageCommand.ObservesCollection(() => SelectedItems);
//                CopyCommand.ObservesCollection(() => SelectedItems);
//                CreateJunctionCommand.ObservesCollection(() => SelectedItems);
//                DeleteFoldersCommand.ObservesCollection(() => SelectedItems);
//                DeleteJunctionsCommand.ObservesCollection(() => SelectedItems);
            }
        }
        private IEnumerable<GameFolder> SelectedFolders
            => SelectedItems?.Reverse().Cast<GameFolder>() ?? Enumerable.Empty<GameFolder>();

        private FileSystemWatcher DirectoryWatcher { get; } = new FileSystemWatcher();

        private bool BothCollectionsInitialized { get; set; }
        private bool LocationSelected { get; set; }

        private string _location;
        private ObservableCollection<object> _selectedItems;
        public string Location
        {
            get { return _location; }
            set {
                // If the location that doesn't exist (ie a saved location that has since been deleted) just ignore it
                if (!Directory.Exists(value)) return;

                _location = value;
                LocationSelected = true;

                ShowLoadingSpinnerDuring(() => {
                    CorrespondingCollection.BothCollectionsInitialized =
                        BothCollectionsInitialized |= LocationSelected && CorrespondingCollection.LocationSelected;

                    DirectoryWatcher.Path = Location;
                    if (DirectoryWatcher.EnableRaisingEvents == false) InitFileSystemWatcher();

                    Folders.Clear();
                    try
                    {
                        foreach (var directoryInfo in new DirectoryInfo(Location)
                            .EnumerateDirectories()
                            .Where(info => (info.Attributes & (FileAttributes.System | FileAttributes.Hidden)) == 0))
                        {
                            Folders.Add(new GameFolder(directoryInfo));
                        }
                    }
                    catch (IOException e)
                    {
                        HandleError(e.Message, e);
                    }
                });
            }
        }

        #region Commands
        //TODO test
        [AutoLazy.Lazy]
        public DelegateCommand ArchiveToStorageCommand => new DelegateCommand(() => {
            foreach (var folder in SelectedFolders)
            {
                var createdFolder = CorrespondingCollection.CopyFolder(folder);
                if (createdFolder != null)
                {
                    var isFolderDeleted = DeleteFolder(folder);
                    if (isFolderDeleted) CreateJunctionTo(createdFolder);
                }
            }
        }, () => SelectedFolders.Any());

        [AutoLazy.Lazy]
        public DelegateCommand CopyCommand => new DelegateCommand(() => {
            foreach (var folder in SelectedFolders)
            {
                CorrespondingCollection.CopyFolder(folder);
            }
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

        private void SelectFolders(IEnumerable<GameFolder> folders)
        {
            SelectedItems.Clear();
            foreach (var folder in folders)
            {
                SelectedItems.Add(folder);
            }
        }

        private void InitFileSystemWatcher()
        {
            DirectoryWatcher.EnableRaisingEvents = true;
            DirectoryWatcher.NotifyFilter = NotifyFilters.DirectoryName;
            DirectoryWatcher.InternalBufferSize = 40960;
            DirectoryWatcher.Created += (sender, args) => {
                Folders.Add(new GameFolder(args.FullPath));
            };
            DirectoryWatcher.Deleted += (sender, args) => {
                Folders.Remove(FolderByName(args.Name));
            };
            DirectoryWatcher.Renamed += (sender, args) => {
                Folders.First(folder => folder.Name.Equals(args.OldName, StringComparison.OrdinalIgnoreCase)).Rename(args.Name);
                //Performance: A full reset isn't necessary here, but trying to use replace on the single element produced inconsistent results - renaming the same directory multiple times would frequently result in failure to refresh sort order. May be able to solve it by setting the synchronizing object? https://msdn.microsoft.com/en-us/library/system.io.filesystemwatcher.synchronizingobject(v=vs.110).aspx
//                Folders.RaiseCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            };
        }

        public void Dispose() => DirectoryWatcher.Dispose();

        private GameFolder FolderByName(string name)
        {
            return Folders.FirstOrDefault(folder => folder.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private void CreateJunctionTo(GameFolder junctionTarget)
        {
            try
            {
                var junctionDirectory = new DirectoryInfo(Location + @"\" + junctionTarget.Name);
                if (junctionDirectory.Exists == false)
                {
                    JunctionPoint.Create(junctionDirectory, junctionTarget.DirectoryInfo, false);
                }
            }
            catch (UnauthorizedAccessException e)
            {
                HandleError(InvalidPermission, e);
            }
        }

        /// <summary>
        ///     Returns the created/overwritten folder on success, null otherwise (if operation is canceled)
        /// </summary>
        /// <param name="folderToCopy"></param>
        /// <returns></returns>
        private GameFolder CopyFolder(GameFolder folderToCopy)
        {
            string targetDirectory = $"{Location}\\{folderToCopy.Name}";

            var targetDirectoryInfo = new DirectoryInfo(targetDirectory);
            var isOverwrite = targetDirectoryInfo.Exists;

            try
            {
                if (isOverwrite)
                {
                    var overwrittenFolder = FolderByName(targetDirectoryInfo.Name);
                    if (overwrittenFolder.IsJunction == false)
                    {
                        FileSystem.CopyDirectory(folderToCopy.DirectoryInfo.FullName, targetDirectory, UIOption.AllDialogs);
                        overwrittenFolder.RecalculateSize();
                        return overwrittenFolder;
                    }

                    //If the target is a junction, delete it and proceed normally
                    DeleteJunction(targetDirectoryInfo);
                }

                FileSystem.CopyDirectory(folderToCopy.DirectoryInfo.FullName, targetDirectory, UIOption.AllDialogs);
                var createdFolder = new GameFolder(targetDirectoryInfo);
                return createdFolder;
            }
            catch (OperationCanceledException e)
            {
                Debug.WriteLine(e);
                if (isOverwrite) return new GameFolder(targetDirectoryInfo);
            }

            return null;
        }

        /// <summary>Returns true on successful delete, false if user cancels operation</summary>
        private bool DeleteFolder(GameFolder folderToDelete)
        {
            try
            {
                FileSystem.DeleteDirectory(folderToDelete.DirectoryInfo.FullName, UIOption.AllDialogs,
                    RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
            }
            catch (OperationCanceledException)
            {
                //Do nothing if they cancel
                return false;
            }

            //Delete junctions pointing to the deleted folder
            CorrespondingCollection.Folders.Where(folder => folder.IsJunction &&
                                                            folder.JunctionTarget.Equals(folderToDelete.DirectoryInfo.FullName))
                                   .ForEach(DeleteJunction);

            return true;
        }

        /// <exception cref="IOException">If it is not a junction path.</exception>
        private static void DeleteJunction(GameFolder folder) => DeleteJunction(folder.DirectoryInfo);

        /// <exception cref="IOException">If it is not a junction path.</exception>
        private static void DeleteJunction(DirectoryInfo junctionDirectory) => JunctionPoint.Delete(junctionDirectory);

    }

}
