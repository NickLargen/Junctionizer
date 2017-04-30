using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;

using Junctionizer.ViewModels;

using WpfBindingErrors;

[assembly: CLSCompliant(false)]

namespace Junctionizer.UI
{
    public partial class MainWindow
    {
        private ExtendedContentPage ExtendedContentPage { get; set; }
        private CompactContentPage CompactContentPage { get; set; }

        public MainWindow()
        {
            //Silence freezable trace warnings since they don't seem to represent an actual problem
            PresentationTraceSources.FreezableSource.Switch.Level = SourceLevels.Error;
            BindingExceptionThrower.Attach();
            InitializeComponent();

            // Persist window position and size when opening and closing the application
            SourceInitialized += (sender, args) => Settings.StateTracker.Configure(this).IdentifyAs("WindowPosition").Apply();

            ContentRendered += (sender, args) => {
                var mainWindowViewModel = (MainWindowViewModel) DataContext;
                mainWindowViewModel.Initialize();

                EnableLiveSorting(mainWindowViewModel.DisplayedMappings);

                // This maintains state (eg selecteditems) and allows fast navigation but creates extra work keeping them both in sync
                ExtendedContentPage = new ExtendedContentPage(mainWindowViewModel);
                CompactContentPage = new CompactContentPage(mainWindowViewModel);

                SwitchInterfaces();
            };
        }

        /// Update sort order when properties on items within the collection change
        private static void EnableLiveSorting(IEnumerable observableCollection)
        {
            ((ListCollectionView) CollectionViewSource.GetDefaultView(observableCollection)).IsLiveSorting = true;
        }

        private void SwitchInterfaces(object sender, RoutedEventArgs e)
        {
            // Can't navigate before the panes have been created
            if (ExtendedContentPage == null) return;

            SwitchInterfaces();
        }

        private void SwitchInterfaces()
        {
            if (UISettings.Instance.IsCompactInterface) frame.Navigate(CompactContentPage);
            else frame.Navigate(ExtendedContentPage);
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
