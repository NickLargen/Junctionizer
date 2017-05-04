using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Data;

using Junctionizer.Model;
using Junctionizer.ViewModels;

using WpfBindingErrors;

[assembly: CLSCompliant(false)]

namespace Junctionizer.UI
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            //Silence freezable trace warnings since they don't seem to represent an actual problem
            PresentationTraceSources.FreezableSource.Switch.Level = SourceLevels.Error;
            BindingExceptionThrower.Attach();
            InitializeComponent();

            // Persist window position and size when opening and closing the application
            SourceInitialized += (sender, args) => Settings.StateTracker.Configure(this).IdentifyAs("WindowPosition").Apply();

            var mainWindowViewModel = (MainWindowViewModel) DataContext;

            var mappingsCollectionView = (ListCollectionView) CollectionViewSource.GetDefaultView(mainWindowViewModel.DisplayedMappings);
            mappingsCollectionView.SortDescriptions.Add(new SortDescription(nameof(DirectoryMapping.StringRepresentation), ListSortDirection.Ascending));

            ContentRendered += (sender, args) => {
                if (!Directory.Exists(Settings.AppDataDirectoryPath)) mainWindowViewModel.NewUserSetup();
            };
        }

        private void CloseRightDrawer(object sender, RoutedEventArgs e) => UISettings.Instance.IsRightDrawerOpen = false;

        private void MainWindow_OnSizeChanged(object sender, SizeChangedEventArgs e) => UISettings.Instance.CheckWindowSize();

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (UISettings.Instance.IsModifyingFileSystem)
            {
                e.Cancel = true;
                Dialogs.DisplayMessageBox("Cannot exit until all queued file system operations have been completed or cancelled.");
            }
        }
    }
}
