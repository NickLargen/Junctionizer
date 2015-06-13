using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Forms;
using GameMover;
using GameMover.Annotations;
using GameMover.Model;
using Microsoft.VisualBasic.FileIO;
using Monitor.Core.Utilities;
using static GameMover.StaticMethods;
using DataGrid = System.Windows.Controls.DataGrid;

namespace GameMover {

    public class FoldersPane {

        public string Name { get; set; }

        public string SteamCommonFolderGuess { get; set; }
        public FolderCollection FolderCollection { get; } = new FolderCollection();
        public DataGrid GridDisplay { get; set; }

        public bool IsLocationInvalid() {
            if (FolderCollection?.Folders != null) return false;
            StaticMethods.ShowMessage($"Must select {Name} location first.");
            return true;
        }

        public void SelectFoldersNotIn(FolderCollection collection) {
            
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

            GridDisplay.ItemsSource = FolderCollection.Folders;

            //Set initial sorting
            var firstCol = GridDisplay.Columns.First();
            firstCol.SortDirection = ListSortDirection.Ascending;
            GridDisplay.Items.SortDescriptions.Add(new SortDescription(firstCol.SortMemberPath, ListSortDirection.Ascending));
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

        public void CopySelectedItems(IList selectedItems) {
            FolderCollection.CopySelectedItems(selectedItems);
        }

        public GameFolder CopyFolder(GameFolder gameFolderToCopy) {
            return FolderCollection.CopyFolder(gameFolderToCopy);
        }

        public bool DeleteFolder(GameFolder gameFolderToDelete) {
            return FolderCollection.DeleteFolder(gameFolderToDelete);
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