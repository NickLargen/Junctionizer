using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GameMover.External_Code;
using Microsoft.VisualBasic.FileIO;
using Prism.Commands;
using Prism.Mvvm;
using static GameMover.StaticMethods;

namespace GameMover.Model
{

    public sealed class FolderCollection : BindableBase, IDisposable
    {

        [PropertyChanged.DoNotNotify]
        public FolderCollection OtherPane { get; set; }
        [PropertyChanged.DoNotNotify]
        public string SteamCommonFolderGuess { get; set; }

        public ObservableCollection<GameFolder> Folders { get; } = new AsyncObservableCollection<GameFolder>();

        public ObservableCollection<object> SelectedItems { get; set; }

        private FileSystemWatcher FileSystemWatcher { get; } = new FileSystemWatcher();

        private string _location;

        public string Location
        {
            get { return _location; }
            set {
                if (value == Location) return;

                _location = value;
                OnPropertyChanged();
                FileSystemWatcher.Path = Location;
                if (FileSystemWatcher.EnableRaisingEvents == false) InitFileSystemWatcher();
            }
        }



        public bool IsLocationInvalid()
        {
            if (Location != null) return false;

            ShowMessage($"Must select VisibleName!! location first.");
            return true;
        }



        [AutoLazy.Lazy]
        public DelegateCommand SelectLocationCommand => new DelegateCommand(() => {
            var folderBrowserDialog = CreateFolderBrowserDialog(SteamCommonFolderGuess);
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                SetLocation(folderBrowserDialog.SelectedPath);
            }
        }, () => true);

        [AutoLazy.Lazy]
        public DelegateCommand<IEnumerable<GameFolder>> SelectionChangedCommand => new DelegateCommand<IEnumerable<GameFolder>>(folders => {

            SelectedItems.Clear();
            foreach (var folder in folders)
            {
                SelectedItems.Add(folder);
            }
        }, _ => true);

        [AutoLazy.Lazy]
        public DelegateCommand CopyCommand => new DelegateCommand(() => {
            foreach (var folder in SelectedItems)
            {
                OtherPane.CopyFolder(folder as GameFolder);
            }
        }, () => true);

        [AutoLazy.Lazy]
        public DelegateCommand DeleteFoldersCommand => new DelegateCommand(() => {
            SelectedItems.TraverseBackwards(folder => DeleteFolder((GameFolder) folder));
        }, () => true);

        [AutoLazy.Lazy]
        public DelegateCommand DeleteJuctionsCommand => new DelegateCommand(() => {
            SelectedItems.TraverseBackwards(folder => DeleteJunction((DirectoryInfo) folder));
        }, () => true);

        [AutoLazy.Lazy]
        public DelegateCommand CreateJunctionCommand => new DelegateCommand(() => {
            SelectedItems.TraverseBackwards(folder => OtherPane.CreateJunctionTo((GameFolder) folder));
        }, () => true);

        //TODO test
        [AutoLazy.Lazy]
        public DelegateCommand ArchiveToStorageCommand => new DelegateCommand(() => {
            SelectedItems.TraverseBackwards(gameFolder => {
                var folder = gameFolder as GameFolder;
                var createdFolder = OtherPane.CopyFolder(folder);
                var folderDeleted = DeleteFolder(folder);
                if (createdFolder != null && folderDeleted) CreateJunctionTo(createdFolder);
            });
        }, () => true);

        [AutoLazy.Lazy]
        public DelegateCommand SelectFoldersNotInOtherPaneCommand => new DelegateCommand(() => {
            if (IsLocationInvalid() || OtherPane.IsLocationInvalid()) return;

            SelectionChangedCommand.Execute(Folders.Except(OtherPane.Folders));
        }, () => true);







        private void InitFileSystemWatcher()
        {
            FileSystemWatcher.EnableRaisingEvents = true;
            FileSystemWatcher.NotifyFilter = NotifyFilters.DirectoryName;
            //Known issue: size doesn't update if they change folder contents while the program is running, could not find a way to do it without listening to subdirectory changes which is not desired. 
            FileSystemWatcher.Created += (sender, args) => {
                Folders.Add(new GameFolder(args.FullPath));
                //TODO test changed what is being watched middle of running after setlocation called a second time
            };
            FileSystemWatcher.Deleted += (sender, args) => { Folders.Remove(FolderByName(args.Name)); };
            FileSystemWatcher.Renamed += (sender, args) => {
                var folder = FolderByName(args.OldName);
                folder.Rename(args.Name);

                // Refresh sort order
                Folders.Remove(folder);
                Folders.Add(folder);
            };
        }

        private GameFolder FolderByName(string name)
        {
            return Folders.FirstOrDefault(folder => folder.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public void SetLocation(string selectedPath)
        {
            if (Location == selectedPath) return;

            Location = selectedPath;

            DirectoryInfo[] directories = new DirectoryInfo(Location).GetDirectories();

            Folders.Clear();
            foreach (var directoryInfo in directories)
            {
                //Skip folders that we don't have access to
                var attributes = directoryInfo.Attributes;
                if (attributes.HasFlag(FileAttributes.System) || attributes.HasFlag(FileAttributes.Hidden)) continue;

                Folders.Add(new GameFolder(directoryInfo));
            }
        }

        /// <exception cref="UnauthorizedAccessException">Junction creation failed.</exception>
        public void CreateJunctionTo(GameFolder junctionTarget)
        {
            try
            {
                var junctionDirectory = new DirectoryInfo(Location + @"\" + junctionTarget.Name);
                if (junctionDirectory.Exists == false)
                {
                    JunctionPoint.Create(junctionDirectory, junctionTarget, false);
                }
            }
            catch (UnauthorizedAccessException e)
            {
                Debug.WriteLine(e);
                ShowMessage(InvalidPermission);
            }
        }

        /// <summary>
        ///     Returns the created/overwritten folder on success, null otherwise (if operation is canceled)
        /// </summary>
        /// <param name="folderToCopy"></param>
        /// <returns></returns>
        public GameFolder CopyFolder(GameFolder folderToCopy)
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
        public bool DeleteFolder(GameFolder folderToDelete)
        {
            try
            {
                FileSystem.DeleteDirectory(folderToDelete.DirectoryInfo.FullName, UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
            }
            catch (OperationCanceledException)
            {
                //Do nothing if they cancel
                return false;
            }

            //Delete junctions pointing to the deleted folder
            OtherPane.Folders.TraverseBackwards(folder => {
                if (folder.IsJunction && folder.JunctionTarget.Equals(folderToDelete.DirectoryInfo.FullName))
                {
                    OtherPane.DeleteJunction(folder);
                }
            });

            return true;
        }

        /// <exception cref="IOException">If it is not a junction path.</exception>
        public void DeleteJunction(DirectoryInfo junctionDirectory)
        {
            try
            {
                JunctionPoint.Delete(junctionDirectory);
            }
            catch (IOException e)
            {
                Debug.WriteLine(e);
                ShowMessage($"Failed to delete junction at '{junctionDirectory.FullName}'. Please verify it is a junction.");
            }
        }

        public void Dispose() => FileSystemWatcher.Dispose();

    }

}
