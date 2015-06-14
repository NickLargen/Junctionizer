using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using GameMover.Annotations;
using GameMover.Model;
using Monitor.Core.Utilities;
using static GameMover.StaticMethods;
using DataGrid = System.Windows.Controls.DataGrid;

namespace GameMover {

    [UsedImplicitly]
    public class FoldersPane : DataGrid {

        public string VisibleName { get; set; }

        public FoldersPane OtherPane { get; set; }
        public string SteamCommonFolderGuess { get; set; }
        public FolderCollection FolderCollection { get; } = new FolderCollection();

        public bool IsLocationInvalid() {
            if (FolderCollection?.Folders != null) return false;
            ShowMessage($"Must select {VisibleName} location first.");
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if  the location changed </returns>
        public bool SelectLocation() {
            var folderBrowserDialog = CreateFolderBrowserDialog(SteamCommonFolderGuess);
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK) {
                var selectedPath = folderBrowserDialog.SelectedPath;
                var isNewLocation = !selectedPath.Equals(FolderCollection.Location, StringComparison.OrdinalIgnoreCase);
                SetLocation(selectedPath);
                return isNewLocation;
            }
            return false;
        }

        public void SetLocation(string selectedPath) {
            FolderCollection.SetLocation(selectedPath);

            ItemsSource = FolderCollection.Folders;

            //Set initial sorting
            var firstCol = Columns.First();
            firstCol.SortDirection = ListSortDirection.Ascending;
            Items.SortDescriptions.Add(new SortDescription(firstCol.SortMemberPath, ListSortDirection.Ascending));
        }

        public void CreateJunctionTo(GameFolder junctionTarget) {
            try {
                FolderCollection.CreateJunctionTo(junctionTarget);
            }
            catch (UnauthorizedAccessException e) {
                Debug.WriteLine(e);
                ShowMessage(InvalidPermission);
            }
        }
        
        public GameFolder CopyFolder(GameFolder gameFolderToCopy) {
            return FolderCollection.CopyFolder(gameFolderToCopy);
        }

        public bool DeleteFolder(GameFolder gameFolderToDelete) {
            bool folderDeleted = FolderCollection.DeleteFolder(gameFolderToDelete);
            if (folderDeleted) {
                //Delete junctions pointing to the deleted folder
                var junctionDirectory = new DirectoryInfo(OtherPane.FolderCollection.Location + @"\" + gameFolderToDelete.Name);
                if (JunctionPoint.Exists(junctionDirectory)) OtherPane.DeleteJunction(junctionDirectory);
            }

            return folderDeleted;
        }

        public void DeleteJunction(DirectoryInfo junctionDirectory) {
            try {
                FolderCollection.DeleteJunction(junctionDirectory);
            }
            catch (IOException e) {
                Debug.WriteLine(e);
                ShowMessage($"Failed to delete junction at '{junctionDirectory.FullName}'. Please verify it is a junction.");
            }
        }

    }

}