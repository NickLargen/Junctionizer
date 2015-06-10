using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using GameMover.Annotations;
using GameMover.Model;
using Microsoft.Win32;
using Monitor.Core.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GameMover {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow {

        //Delete on a junction gives recycle bin prompt but it's just for the junction
        //BUG: double clicking column to resize introduces the empty column on the right
        //bug double clicking datagrid non-row while something is selected opens folder

        //todo save locations between runs

        //todo invalid input handling
        //todo check for permissions everywhere

        //performance: sorting by size on hdd hangs ui
        //performance: test opening giant folder

        private readonly FoldersPane _storagePane;
        private readonly FoldersPane _installPane;

        private readonly ObservableCollection<string> _pathsInstallAndStorage = new ObservableCollection<string>();
        private const string ArrowedPathSeparator = " -> ";

        private bool _ignorePathsSelectionChange;

        public MainWindow() {
            InitializeComponent();

            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");

            var installSteamCommon = regKey == null ? @"C:" : regKey.GetValue("SteamPath").ToString().Replace(@"/", @"\") + @"\steamapps\common";

            _installPane = new FoldersPane {
                GridDisplay = dagInstall,
                TextFolderPath = txtInstallFolder,
                SteamCommonFolderGuess = installSteamCommon
            };

            _storagePane = new FoldersPane {
                GridDisplay = dagStorage,
                TextFolderPath = txtStorageFolder,
                SteamCommonFolderGuess = @"E:\Steam\SteamApps\common"
            };

            dagInstall.MouseDoubleClick += DataGrid_OnMouseDoubleClick;
            dagStorage.MouseDoubleClick += DataGrid_OnMouseDoubleClick;


            boxPaths.ItemsSource = _pathsInstallAndStorage;
            _pathsInstallAndStorage.Add(_installPane.SteamCommonFolderGuess + ArrowedPathSeparator + _storagePane.SteamCommonFolderGuess);

//            dagInstall.MouseRightButtonUp += (sender, args) => Console.WriteLine(((sender as DataGrid).SelectedItem as Folder).Name);
        }

        private void SaveCurrentLocations(object sender, RoutedEventArgs e) {
            string arrowedPath = _installPane.TextFolderPath.Text + ArrowedPathSeparator + _storagePane.TextFolderPath.Text;

            if (_pathsInstallAndStorage.Contains(arrowedPath) == false) {
                _pathsInstallAndStorage.Add(arrowedPath);

                _ignorePathsSelectionChange = true;
                boxPaths.SelectedItem = arrowedPath;
            }
        }

        private void DeleteCurrentLocations(object sender, RoutedEventArgs e) {
            if (boxPaths.SelectedIndex != -1)
                _pathsInstallAndStorage.RemoveAt(boxPaths.SelectedIndex);
        }

        private void DataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e) {
//            if (mouseButtonEventArgs.ButtonState != MouseButtonState.Pressed) return; //only react on pressed
            if (e.ChangedButton != MouseButton.Left) return;

            var dataGrid = sender as DataGrid;

            if (dataGrid?.SelectedItems?.Count == 1) {
                var folder = dataGrid.SelectedItem as GameFolder;
                if (folder != null)
                    Process.Start(folder.DirectoryInfo.FullName);
            }
        }

        private void SelectInstallLocation(object sender, RoutedEventArgs e) {
            if (_installPane.SelectLocation()) boxPaths.SelectedIndex = -1;
        }

        private void SelectStorageLocation(object sender, RoutedEventArgs e) {
            if (_storagePane.SelectLocation()) boxPaths.SelectedIndex = -1;
        }

        #region Actions on selected items

        private void btnCreateJunction_Click(object sender, RoutedEventArgs e) {
            foreach (GameFolder folder in dagStorage.SelectedItems) {
                _installPane.CreateJunctionTo(folder);
            }
        }

        private void CopyToStorage(object sender, RoutedEventArgs e) {
            _storagePane.CopySelectedItems(dagInstall.SelectedItems);
        }

        private void CopyToInstall(object sender, RoutedEventArgs e) {
            _installPane.CopySelectedItems(dagStorage.SelectedItems);
        }

        private void DeleteFromStorage(object sender, RoutedEventArgs e) {
            var selectedItems = dagStorage.SelectedItems;
            for (int i = selectedItems.Count - 1; i >= 0; i--) {
                GameFolder gameFolder = (GameFolder) selectedItems[i];
                if (_storagePane.DeleteFolder(gameFolder)) {
                    var junctionDirectory = new DirectoryInfo(_installPane.Location + @"\" + gameFolder.Name);
                    if (JunctionPoint.Exists(junctionDirectory)) _installPane.DeleteJunction(junctionDirectory);
                }
            }
        }

        private void DeleteFromInstall(object sender, RoutedEventArgs e) {
            var selectedItems = dagInstall.SelectedItems;
            for (int i = selectedItems.Count - 1; i >= 0; i--) {
                GameFolder gameFolder = (GameFolder) selectedItems[i];
                _installPane.DeleteFolder(gameFolder);
            }
        }

        private void DeleteJunctionFromInstall(object sender, RoutedEventArgs e) {
            var selectedItems = dagInstall.SelectedItems;
            for (int i = selectedItems.Count - 1; i >= 0; i--) {
                GameFolder gameFolder = selectedItems[i] as GameFolder;
                _installPane.DeleteJunction(gameFolder);
            }
        }

        //todo test
        //Todo: handle if archiving is cancelled because target folder already exists
        private void ArchiveToStorage(object sender, RoutedEventArgs e) {
            var selectedItems = dagInstall.SelectedItems;
            for (int i = selectedItems.Count - 1; i >= 0; i--) {
                GameFolder gameFolder = (GameFolder) selectedItems[i];

                var createdFolder = _storagePane.CopyFolder(gameFolder);
                _installPane.DeleteFolder(gameFolder);
                _installPane.CreateJunctionTo(createdFolder);
            }
        }

        #endregion

        //todo refactor for duplication
        private void SelectNotInstalled(object sender, RoutedEventArgs e) {
            if (IsStorageLocationInvalid() || IsInstallLocationInvalid()) return;

            var foldersNotInstalled = _storagePane.Folders.Except(_installPane.Folders);
            dagStorage.SelectedItems.Clear();

            foreach (var folder in foldersNotInstalled) {
                dagStorage.SelectedItems.Add(folder);
            }
        }

        private void SelectNotInStorage(object sender, RoutedEventArgs e) {
            if (IsInstallLocationInvalid() || IsStorageLocationInvalid()) return;

            var foldersNotInStorage = _installPane.Folders.Except(_storagePane.Folders);
            dagInstall.SelectedItems.Clear();

            foreach (var folder in foldersNotInStorage) {
                dagInstall.SelectedItems.Add(folder);
            }
        }

        private bool IsStorageLocationInvalid() {
            if (_storagePane.Folders != null) return false;
            StaticMethods.ShowMessage("Must select storage location first.");
            return true;
        }

        private bool IsInstallLocationInvalid() {
            if (_installPane.Folders != null) return false;
            StaticMethods.ShowMessage("Must select installation location first.");
            return true;
        }

        private void HideStorage(object sender, RoutedEventArgs e) {
//            if ((bool) boxHideStorage.IsChecked) storageColumnDefinition.Width = new GridLength(0);
//            else storageColumnDefinition.Width = new GridLength(.5, GridUnitType.Star);
            storageColumnDefinition.Width = new GridLength(0);
        }

        private void ShowStorage(object sender, RoutedEventArgs e) {
            storageColumnDefinition.Width = new GridLength(.5, GridUnitType.Star);
        }


        private void BoxPaths_OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (_ignorePathsSelectionChange) {
                _ignorePathsSelectionChange = false;
                return;
            }

            string arrowedPaths = boxPaths.SelectedItem as string;
            string[] paths = arrowedPaths?.Split(new[] {ArrowedPathSeparator}, StringSplitOptions.RemoveEmptyEntries);


            if (paths?.Length != 2) return;

            _installPane.SetLocation(paths[0]);
            _storagePane.SetLocation(paths[1]);
        }
    }

}