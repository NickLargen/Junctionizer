using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GameMover.Annotations;
using Microsoft.Win32;
using static GameMover.StaticMethods;

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

        private readonly ObservableCollection<string> _pathsInstallAndStorage = new ObservableCollection<string>();
        private const string ArrowedPathSeparator = " -> ";

        /// Allows you to set the selection without trigger selection change (so that when saving a control you don't reload)
        private bool _ignorePathsSelectionChange;

        public MainWindow() {
            InitializeComponent();

            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");

            var installSteamCommon = regKey == null ? @"C:" : regKey.GetValue("SteamPath").ToString().Replace(@"/", @"\") + @"\steamapps\common";

            InstallPane.VisibleName = "install";
            InstallPane.SteamCommonFolderGuess = installSteamCommon;
            InstallPane.OtherPane = StoragePane;

            StoragePane.VisibleName = "storage";
            StoragePane.SteamCommonFolderGuess = @"E:\Steam\SteamApps\common";
            StoragePane.OtherPane = InstallPane;

            InstallPane.MouseDoubleClick += DataGrid_OnMouseDoubleClick;
            StoragePane.MouseDoubleClick += DataGrid_OnMouseDoubleClick;

            boxPaths.ItemsSource = _pathsInstallAndStorage;
            _pathsInstallAndStorage.Add(InstallPane.SteamCommonFolderGuess + ArrowedPathSeparator + StoragePane.SteamCommonFolderGuess);
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

        private void CreateJunctionsForSelected(object sender, RoutedEventArgs e) {
            TraverseBackwards<GameFolder>(SenderPane(sender).SelectedItems, gameFolder =>
                SenderPane(sender).OtherPane.CreateJunctionTo(gameFolder));
            //How to read the previous line:
//            foreach (GameFolder folder in SenderPane(sender).SelectedItems) {
//                SenderPane(sender).OtherPane.CreateJunctionTo(folder);
//            }
        }

        private void CopySelectedFolders(object sender, RoutedEventArgs e) {
            TraverseBackwards<GameFolder>(SenderPane(sender).SelectedItems, gameFolder =>
                SenderPane(sender).OtherPane.CopyFolder(gameFolder));
        }

        private void DeleteSelectedFolders(object sender, RoutedEventArgs e) {
            TraverseBackwards<GameFolder>(SenderPane(sender).SelectedItems, gameFolder =>
                SenderPane(sender).DeleteFolder(gameFolder));
        }

        private void DeleteSelectedJunctions(object sender, RoutedEventArgs e) {
            TraverseBackwards<GameFolder>(SenderPane(sender).SelectedItems, folder =>
                SenderPane(sender).DeleteJunction(folder));
        }



        //todo test
        //Todo: handle if archiving is cancelled because target folder already exists
        private void ArchiveToStorage(object sender, RoutedEventArgs e) {
            TraverseBackwards<GameFolder>(InstallPane.SelectedItems, gameFolder => {
                var createdFolder = StoragePane.CopyFolder(gameFolder);
                InstallPane.DeleteFolder(gameFolder);
                InstallPane.CreateJunctionTo(createdFolder);
            });
        }

        #endregion

        private void SelectFoldersNotInOtherPane(object sender, RoutedEventArgs e) {
            var senderPane = SenderPane(sender);
            var otherPane = senderPane.OtherPane;

            if (senderPane.IsLocationInvalid() || otherPane.IsLocationInvalid()) return;

            var foldersNotInStorage = senderPane.FolderCollection - otherPane.FolderCollection;
            senderPane.SelectedItems.Clear();

            foreach (var folder in foldersNotInStorage) {
                senderPane.SelectedItems.Add(folder);
            }
        }

        private FoldersPane SenderPane(object sender) {
            return (FoldersPane) ((FrameworkElement) sender).DataContext;
        }

        private void HideStorage(object sender, RoutedEventArgs e) {
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