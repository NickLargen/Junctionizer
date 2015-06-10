using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Forms;
using GameMover.Model;
using Microsoft.VisualBasic.FileIO;
using Monitor.Core.Utilities;
using static GameMover.StaticMethods;
using DataGrid = System.Windows.Controls.DataGrid;

namespace GameMover {

    public class FoldersPane {

        public string Location => FolderCollection.Location;
        public Collection<GameFolder> Folders => FolderCollection.Folders;

        public string SteamCommonFolderGuess {
            get { return FolderCollection.SteamCommonFolderGuess; }
            set { FolderCollection.SteamCommonFolderGuess = value; }
        }

        private FolderCollection FolderCollection { get; } = new FolderCollection();

        public DataGrid GridDisplay { get; set; }
        public TextBlock TextFolderPath { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>True if  the location changed </returns>
        public bool SelectLocation() {
            var folderBrowserDialog = CreateFolderBrowserDialog(SteamCommonFolderGuess);
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK) {
                var selectedPath = folderBrowserDialog.SelectedPath;
                var isNewLocation = !selectedPath.Equals(Location, StringComparison.OrdinalIgnoreCase);
                SetLocation(selectedPath);
                return isNewLocation;
            }
            return false;
        }

        public void SetLocation(string selectedPath) {
            FolderCollection.SetLocation(selectedPath);

            TextFolderPath.Text = selectedPath;

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