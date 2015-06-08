using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using Microsoft.VisualBasic.FileIO;
using Monitor.Core.Utilities;
using DataGrid = System.Windows.Controls.DataGrid;

namespace GameMover.Model {

    internal class FolderCollection {

        public string Location { set; get; }
        public ObservableCollection<GameFolder> Folders { set; get; }
        public string SteamCommonFolderGuess { set; get; }

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
            if ( junctionDirectory.Exists == false) {
                JunctionPoint.Create(junctionDirectory, junctionTarget, false);
                Folders.Add(new GameFolder(junctionDirectory));
            }
        }

        public void CopySelectedItems(IList selectedItems) {
            foreach (GameFolder folder in selectedItems) {
                CopyFolder(folder);
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
                FileSystem.DeleteDirectory(folderToDelete.DirectoryInfo.FullName, UIOption.AllDialogs, RecycleOption.SendToRecycleBin,
                    UICancelOption.ThrowException);
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