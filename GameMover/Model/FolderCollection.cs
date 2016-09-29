using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using GameMover.External_Code;
using Microsoft.VisualBasic.FileIO;

namespace GameMover.Model
{

    public sealed class FolderCollection : INotifyPropertyChanged, IDisposable
    {

        public static IEnumerable<GameFolder> operator -(FolderCollection first, FolderCollection second)
        {
            return first.Folders.Except(second.Folders);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<GameFolder> Folders { get; private set; }

        private FileSystemWatcher FileSystemWatcher { get; set; } = new FileSystemWatcher();

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
            Location = selectedPath;

            DirectoryInfo[] directories = new DirectoryInfo(Location).GetDirectories();

            Folders = new AsyncObservableCollection<GameFolder>();
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
            var junctionDirectory = new DirectoryInfo(Location + @"\" + junctionTarget.Name);
            if (junctionDirectory.Exists == false)
            {
                JunctionPoint.Create(junctionDirectory, junctionTarget, false);
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
            return true;
        }

        /// <exception cref="IOException">If it is not a junction path.</exception>
        public void DeleteJunction(DirectoryInfo junctionDirectory)
        {
            JunctionPoint.Delete(junctionDirectory);
        }

        public void Dispose() => FileSystemWatcher.Dispose();

    }

}
