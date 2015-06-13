using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using GameMover.Annotations;
using GameMover;
using Microsoft.Win32;
using Monitor.Core.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GameMover {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged {

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //Delete on a junction gives recycle bin prompt but it's just for the junction
        //BUG: double clicking column to resize introduces the empty column on the right
        //bug double clicking datagrid non-row while something is selected opens folder

        //todo save locations between runs

        //todo invalid input handling
        //todo check for permissions everywhere

        //performance: sorting by size on hdd hangs ui
        //performance: test opening giant folder

        [NotNull]
        public FoldersPane StoragePane { get; }

        [NotNull]
        public FoldersPane InstallPane { get; }

        private readonly ObservableCollection<string> _pathsInstallAndStorage = new ObservableCollection<string>();
        private const string ArrowedPathSeparator = " -> ";

        /// Allows you to set the selection without trigger selection change (so that when saving a control you don't reload)
        private bool _ignorePathsSelectionChange;

        public MainWindow() {
            InitializeComponent();

            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");

            var installSteamCommon = regKey == null ? @"C:" : regKey.GetValue("SteamPath").ToString().Replace(@"/", @"\") + @"\steamapps\common";

            InstallPane = new FoldersPane {
                GridDisplay = dagInstall,
                Name = "install",
                SteamCommonFolderGuess = new DirectoryInfo(installSteamCommon)?.FullName
            };

            StoragePane = new FoldersPane {
                GridDisplay = dagStorage,
                Name = "storage",
                SteamCommonFolderGuess = @"E:\Steam\SteamApps\common", 
                OtherPane = InstallPane
            };
            InstallPane.OtherPane = StoragePane;

            //Todo: figure out why this works
            //Without these calls the text boxes showing the locations do not update
            OnPropertyChanged(nameof(InstallPane));
            OnPropertyChanged(nameof(StoragePane));

            dagInstall.MouseDoubleClick += DataGrid_OnMouseDoubleClick;
            dagStorage.MouseDoubleClick += DataGrid_OnMouseDoubleClick;


            boxPaths.ItemsSource = _pathsInstallAndStorage;
            _pathsInstallAndStorage.Add(InstallPane.SteamCommonFolderGuess + ArrowedPathSeparator + StoragePane.SteamCommonFolderGuess);

//            dagInstall.MouseRightButtonUp += (sender, args) => Console.WriteLine(((sender as DataGrid).SelectedItem as Folder).Name);
        }

        private void SaveCurrentLocations(object sender, RoutedEventArgs e) {
            string arrowedPath = InstallPane.FolderCollection.Location + ArrowedPathSeparator + StoragePane.FolderCollection.Location;

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

        private void SelectLocation(object sender, RoutedEventArgs e) {
            if (SenderPane(sender).SelectLocation()) boxPaths.SelectedIndex = -1;
        }

        #region Actions on selected items

        private void btnCreateJunction_Click(object sender, RoutedEventArgs e) {
            foreach (GameFolder folder in dagStorage.SelectedItems) {
                InstallPane.CreateJunctionTo(folder);
            }
        }

        private void CopySelectedFolders(object sender, RoutedEventArgs e) {
            SenderPane(sender).OtherPane.FolderCollection.CopySelectedItems(SenderDataGrid(sender).SelectedItems);
        }

        private void DeleteFromStorage(object sender, RoutedEventArgs e) {
            TraverseBackwards<GameFolder>(dagInstall.SelectedItems, gameFolder => {
                if (StoragePane.DeleteFolder(gameFolder)) {
                    var junctionDirectory = new DirectoryInfo(InstallPane.FolderCollection.Location + @"\" + gameFolder.Name);
                    if (JunctionPoint.Exists(junctionDirectory)) InstallPane.DeleteJunction(junctionDirectory);
                }
            });
//            var selectedItems = dagStorage.SelectedItems;
//            for (int i = selectedItems.Count - 1; i >= 0; i--) {
//                GameFolder gameFolder = (GameFolder) selectedItems[i];
//                if (StoragePane.DeleteFolder(gameFolder)) {
//                    var junctionDirectory = new DirectoryInfo(InstallPane.FolderCollection.Location + @"\" + gameFolder.Name);
//                    if (JunctionPoint.Exists(junctionDirectory)) InstallPane.DeleteJunction(junctionDirectory);
//                }
//            }
        }

        private void DeleteFromInstall(object sender, RoutedEventArgs e) {
            TraverseBackwards<GameFolder>(dagInstall.SelectedItems, gameFolder => InstallPane.DeleteFolder(gameFolder));
//            var selectedItems = dagInstall.SelectedItems;
//            for (int i = selectedItems.Count - 1; i >= 0; i--) {
//                InstallPane.DeleteFolder((GameFolder) selectedItems[i]);
//            }
        }

        private void DeleteJunctionFromInstall(object sender, RoutedEventArgs e) {
            TraverseBackwards<GameFolder>(dagInstall.SelectedItems, folder => InstallPane.DeleteJunction(folder));

//            var selectedItems = dagInstall.SelectedItems;
//            for (int i = selectedItems.Count - 1; i >= 0; i--) {
//                InstallPane.DeleteJunction((GameFolder) selectedItems[i]);
//            }
        }

        private void TraverseBackwards<T>(IList list, Action<T> action) {
            for (int i = list.Count - 1; i >= 0; i--) {
                action((T) list[i]);
            }
        }


        //todo test
        //Todo: handle if archiving is cancelled because target folder already exists
        private void ArchiveToStorage(object sender, RoutedEventArgs e) {
            var selectedItems = dagInstall.SelectedItems;
            for (int i = selectedItems.Count - 1; i >= 0; i--) {
                GameFolder gameFolder = (GameFolder) selectedItems[i];

                var createdFolder = StoragePane.CopyFolder(gameFolder);
                InstallPane.DeleteFolder(gameFolder);
                InstallPane.CreateJunctionTo(createdFolder);
            }
        }

        #endregion

        private void SelectFoldersNotInOtherPane(object sender, RoutedEventArgs e) {
            var pane = SenderPane(sender);
            var otherPane = OtherPane(pane);

            if (pane.IsLocationInvalid() || otherPane.IsLocationInvalid()) return;

            var foldersNotInStorage = pane.FolderCollection - otherPane.FolderCollection;
            pane.GridDisplay.SelectedItems.Clear();

            foreach (var folder in foldersNotInStorage) {
                pane.GridDisplay.SelectedItems.Add(folder);
            }
        }

        private DataGrid SenderDataGrid(object sender) {
            return (DataGrid)((FrameworkElement)sender).DataContext;
        }

        private FoldersPane SenderPane(object sender) {
            var dataContext = (sender as FrameworkElement)?.DataContext;

            if (Equals(dataContext, dagInstall)) {
                return InstallPane;
            }
            else if (Equals(dataContext, dagStorage)) {
                return StoragePane;
            }
            else throw new Exception("Usage error, data context should be the relevant data grid.");
        }

        private FoldersPane OtherPane(FoldersPane pane) {
            return pane == InstallPane ? StoragePane : InstallPane;
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

            InstallPane.SetLocation(paths[0]);
            StoragePane.SetLocation(paths[1]);
        }

    }

}