using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using GameMover.ViewModels;

using MaterialDesignThemes.Wpf;

using Prism.Commands;

using WpfBindingErrors;

[assembly: CLSCompliant(false)]

namespace GameMover.UI
{
    public partial class MainWindow
    {
        //TODO: Delete on a junction gives recycle bin prompt but it's just for the junction

        //Feature: select all corresponding elements
        //Feature: Single pane merged UI

        public MainWindow()
        {
            //Silence freezable trace warnings since they don't seem to represent an actual problem
            PresentationTraceSources.FreezableSource.Switch.Level = SourceLevels.Error;
            BindingExceptionThrower.Attach();
            InitializeComponent();

            var mainWindowViewModel = (MainWindowViewModel) DataContext;
            mainWindowViewModel.Initialize();

            Loaded += (sender, args) => {
                SetInitialSort(sourceGrid);
                SetInitialSort(destinationGrid);

                EnableLiveSorting(mainWindowViewModel.DisplayedMappings);
            };
        }

        public static DelegateCommand<object> OpenDialogCommand { get; } = new DelegateCommand<object>(o => OpenDialog(o));

        public static Task OpenDialog(object obj) => DialogHost.Show(obj);

        private static void SetInitialSort(DataGrid dataGrid)
        {
            var firstCol = dataGrid.Columns.First();
            // Mark the UI with what direction it is sorted (places the correct column header arrow)
            firstCol.SortDirection = ListSortDirection.Ascending;

            // Actually sort the items
            dataGrid.Items.SortDescriptions.Add(new SortDescription(firstCol.SortMemberPath, firstCol.SortDirection.Value));

            EnableLiveSorting((IList)dataGrid.ItemsSource);
        }

        /// Update sort order when properties on items within the collection change
        private static void EnableLiveSorting(IList observableCollection)
        {
            ((ListCollectionView)CollectionViewSource.GetDefaultView(observableCollection)).IsLiveSorting = true;
        }

        private void HideDestination(object sender, RoutedEventArgs e)
        {
            destinationColumnDefinition.Width = new GridLength(0);
        }

        private void ShowDestination(object sender, RoutedEventArgs e)
        {
            destinationColumnDefinition.Width = new GridLength(.5, GridUnitType.Star);
        }
    }
}
