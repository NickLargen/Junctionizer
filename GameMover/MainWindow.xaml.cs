using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using static GameMover.StaticMethods;

[assembly: CLSCompliant(false)]

namespace GameMover
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {

        //Delete on a junction gives recycle bin prompt but it's just for the junction
        //BUG: double clicking column to resize introduces the empty column on the right

        //todo save locations between runs

        //todo invalid input handling
        //todo check for permissions everywhere

        //performance: sorting by size on hdd hangs ui
        //performance: test opening giant folder

        //feature: select all corresponding elements

        //todo fix this pathsinstallandstorage
        private readonly ObservableCollection<string> _pathsInstallAndStorage = new ObservableCollection<string>();
        private const string ArrowedPathSeparator = " -> ";

        /// Allows you to set the selection without trigger selection change (so that when saving a control you don't reload)
        private bool _ignorePathsSelectionChange;

        public MainWindow()
        {
            InitializeComponent();

            RegistryKey regKey = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");

            var installSteamCommon = regKey == null ? @"C:" : regKey.GetValue("SteamPath").ToString().Replace(@"/", @"\") + @"\steamapps\common";

            InstallPane.SteamCommonFolderGuess = installSteamCommon;
            InstallPane.OtherPane = StoragePane;

            StoragePane.SteamCommonFolderGuess = @"E:\Steam\SteamApps\common";
            StoragePane.OtherPane = InstallPane;

            boxPaths.ItemsSource = _pathsInstallAndStorage;
            _pathsInstallAndStorage.Add(InstallPane.SteamCommonFolderGuess + ArrowedPathSeparator + StoragePane.SteamCommonFolderGuess);
        }

        private void SaveCurrentLocations(object sender, RoutedEventArgs e)
        {
            string arrowedPath = InstallPane.FolderCollection.Location + ArrowedPathSeparator + StoragePane.FolderCollection.Location;

            if (_pathsInstallAndStorage.Contains(arrowedPath) == false)
            {
                _pathsInstallAndStorage.Add(arrowedPath);

                _ignorePathsSelectionChange = true;
                boxPaths.SelectedItem = arrowedPath;
            }
        }

        private void DeleteCurrentLocations(object sender, RoutedEventArgs e)
        {
            if (boxPaths.SelectedIndex != -1) _pathsInstallAndStorage.RemoveAt(boxPaths.SelectedIndex);
        }

        private void DataGridRow_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            var dataGridRow = sender as DataGridRow;
            var folder = dataGridRow?.Item as GameFolder;
            if (folder != null) Process.Start(folder.DirectoryInfo.FullName);
        }

        private void SelectLocation(object sender, RoutedEventArgs e)
        {
            if (SenderPane(sender).SelectLocation()) boxPaths.SelectedIndex = -1;
        }

        #region Actions on selected items

        private void CreateJunctionsForSelected(object sender, RoutedEventArgs e)
        {
            SenderPane(sender).SelectedItems.TraverseBackwards<GameFolder>(gameFolder =>
                SenderPane(sender).OtherPane.CreateJunctionTo(gameFolder));
            //How to read the previous line:
            //            foreach (GameFolder folder in SenderPane(sender).SelectedItems) {
            //                SenderPane(sender).OtherPane.CreateJunctionTo(folder);
            //            }
        }

        private void CopySelectedFolders(object sender, RoutedEventArgs e)
        {
            SenderPane(sender).SelectedItems.TraverseBackwards<GameFolder>(gameFolder =>
                SenderPane(sender).OtherPane.CopyFolder(gameFolder));
        }

        private void DeleteSelectedFolders(object sender, RoutedEventArgs e)
        {
            SenderPane(sender).SelectedItems.TraverseBackwards<GameFolder>(gameFolder =>
                SenderPane(sender).DeleteFolder(gameFolder));
        }

        private void DeleteSelectedJunctions(object sender, RoutedEventArgs e)
        {
            SenderPane(sender).SelectedItems.TraverseBackwards<GameFolder>(gameFolder =>
                SenderPane(sender).DeleteJunction(gameFolder));
        }



        //todo test
        private void ArchiveToStorage(object sender, RoutedEventArgs e)
        {
            InstallPane.SelectedItems.TraverseBackwards<GameFolder>(gameFolder =>
            {
                var createdFolder = StoragePane.CopyFolder(gameFolder);
                var folderDeleted = InstallPane.DeleteFolder(gameFolder);
                if (folderDeleted && createdFolder != null) InstallPane.CreateJunctionTo(createdFolder);
            });
        }

        #endregion

        private void SelectFoldersNotInOtherPane(object sender, RoutedEventArgs e)
        {
            var senderPane = SenderPane(sender);
            var otherPane = senderPane.OtherPane;

            if (senderPane.IsLocationInvalid() || otherPane.IsLocationInvalid()) return;

            var foldersNotInStorage = senderPane.FolderCollection - otherPane.FolderCollection;
            senderPane.SelectedItems.Clear();

            foreach (var folder in foldersNotInStorage)
            {
                senderPane.SelectedItems.Add(folder);
            }
        }

        private FoldersPane SenderPane(object sender)
        {
            return (FoldersPane)((FrameworkElement)sender).DataContext;
        }

        private void HideStorage(object sender, RoutedEventArgs e)
        {
            storageColumnDefinition.Width = new GridLength(0);
        }

        private void ShowStorage(object sender, RoutedEventArgs e)
        {
            storageColumnDefinition.Width = new GridLength(.5, GridUnitType.Star);
        }

        private void BoxPaths_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_ignorePathsSelectionChange)
            {
                _ignorePathsSelectionChange = false;
                return;
            }

            string arrowedPaths = boxPaths.SelectedItem as string;
            string[] paths = arrowedPaths?.Split(new[] { ArrowedPathSeparator }, StringSplitOptions.RemoveEmptyEntries);

            if (paths?.Length != 2) return;

            InstallPane.SetLocation(paths[0]);
            StoragePane.SetLocation(paths[1]);
        }

    }

}