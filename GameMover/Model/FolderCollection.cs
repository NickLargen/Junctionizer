using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using GameMover.Annotations;
using Microsoft.VisualBasic.FileIO;
using Monitor.Core.Utilities;

namespace GameMover.Model {

    public class FolderCollection : INotifyPropertyChanged {

        public static IEnumerable<GameFolder> operator -(FolderCollection first, FolderCollection second) {
            return first.Folders.Except(second.Folders);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _location;

        public string Location {
            get { return _location; }
            set {
                _location = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<GameFolder> Folders { get; set; }

        public void SetLocation(string selectedPath) {
            Location = selectedPath;

            DirectoryInfo[] directories = new DirectoryInfo(Location).GetDirectories();

            Folders = new ObservableCollection<GameFolder>();
            foreach (var directoryInfo in directories) {
                //Skip folders that we don't have access to
                var attributes = directoryInfo.Attributes;
                if (attributes.HasFlag(FileAttributes.System) || attributes.HasFlag(FileAttributes.Hidden))
                    continue;

                Folders.Add(new GameFolder(directoryInfo));
            }
        }

        /// <exception cref="UnauthorizedAccessException">Junction creation failed.</exception>
        public void CreateJunctionTo(GameFolder junctionTarget) {
            var junctionDirectory = new DirectoryInfo(Location + @"\" + junctionTarget.Name);
            if (junctionDirectory.Exists == false) {
                JunctionPoint.Create(junctionDirectory, junctionTarget, false);
                Folders.Add(new GameFolder(junctionDirectory));
            }
        }


        /// <summary>
        /// Returns the created/overwritten folder on success, null otherwise (if operation is cancelled)
        /// </summary>
        /// <param name="folderToCopy"></param>
        /// <returns></returns>
        public GameFolder CopyFolder(GameFolder folderToCopy) {
            string targetDirectory = $"{Location}\\{folderToCopy.Name}";

            var targetDirectoryInfo = new DirectoryInfo(targetDirectory);
            var isOverwrite = targetDirectoryInfo.Exists;

            try {
                if (isOverwrite) {
                    var overwrittenFolder =
                        Folders.First(folder => folder.Name.Equals(targetDirectoryInfo.Name, StringComparison.OrdinalIgnoreCase));
                    if (overwrittenFolder.IsJunction == false) {
                        FileSystem.CopyDirectory(folderToCopy.DirectoryInfo.FullName, targetDirectory, UIOption.AllDialogs);
                        overwrittenFolder.RefreshSize();
                        return overwrittenFolder;
                    }

                    //If the target is a junction, delete it and proceed normally
                    DeleteJunction(targetDirectoryInfo);
                }

                FileSystem.CopyDirectory(folderToCopy.DirectoryInfo.FullName, targetDirectory, UIOption.AllDialogs);
                var createdFolder = new GameFolder(targetDirectoryInfo);
                Folders.Add(createdFolder);
                return createdFolder;
            }
            catch (OperationCanceledException e) {
                Debug.WriteLine(e);
            }
            return null;
        }

        /// <summary>Returns true on successful delete, false if user cancels operation</summary>
        public bool DeleteFolder(GameFolder folderToDelete) {
            try {
                FileSystem.DeleteDirectory(folderToDelete.DirectoryInfo.FullName, UIOption.AllDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
                Folders.Remove(folderToDelete);
            }
            catch (OperationCanceledException) {
                //Do nothing if they cancel
                return false;
            }
            return true;
        }

        /// <exception cref="IOException">If it is not a junction path.</exception>
        public void DeleteJunction(DirectoryInfo junctionDirectory) {
            if (JunctionPoint.Delete(junctionDirectory)) Folders.Remove(Folders.First(folder => folder.Name.Equals(junctionDirectory.Name, StringComparison.OrdinalIgnoreCase)));
        }

    }

}