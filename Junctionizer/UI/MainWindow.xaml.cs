using System;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

using Junctionizer.ViewModels;

using MaterialDesignThemes.Wpf;

using Prism.Commands;

using WpfBindingErrors;

[assembly: CLSCompliant(false)]

namespace Junctionizer.UI
{
    public partial class MainWindow
    {
        public DualPane DualPane { get; }
        public MergedSinglePane SinglePane { get; }

        public MainWindow()
        {
            //Silence freezable trace warnings since they don't seem to represent an actual problem
            PresentationTraceSources.FreezableSource.Switch.Level = SourceLevels.Error;
            BindingExceptionThrower.Attach();
            InitializeComponent();

            var mainWindowViewModel = (MainWindowViewModel) DataContext;
            mainWindowViewModel.Initialize();

            EnableLiveSorting(mainWindowViewModel.DisplayedMappings);

            // This maintains state (eg selecteditems) and allows fast navigation but creates extra work keeping them both in sync
            DualPane = new DualPane(mainWindowViewModel);
            SinglePane = new MergedSinglePane(mainWindowViewModel);

            SwitchInterfaces();
        }

        public static DelegateCommand<object> OpenDialogCommand { get; } = new DelegateCommand<object>(o => OpenDialog(o));

        public static Task OpenDialog(object obj) => DialogHost.Show(obj);

        /// Update sort order when properties on items within the collection change
        private static void EnableLiveSorting(IEnumerable observableCollection)
        {
            ((ListCollectionView) CollectionViewSource.GetDefaultView(observableCollection)).IsLiveSorting = true;
        }

        private void SwitchInterfaces(object sender, RoutedEventArgs e)
        {
            // Can't navigate before the panes have been created
            if (DualPane == null) return;

            SwitchInterfaces();
        }

        private void SwitchInterfaces()
        {
            if (compactInterfaceMenuItem.IsChecked) frame.Navigate(SinglePane);
            else frame.Navigate(DualPane);
        }
    }
}
