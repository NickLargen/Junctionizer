using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using GameMover.ViewModels;

using MaterialDesignThemes.Wpf;

using Prism.Commands;

using WpfBindingErrors;

[assembly: CLSCompliant(false)]

namespace GameMover.UI
{

    /// <summary>
    ///     Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {

        //TODO: Delete on a junction gives recycle bin prompt but it's just for the junction
        //TODO: Save locations between runs

        //TODO: Invalid input handling
        //TODO: Check for permissions everywhere

        //Feature: select all corresponding elements
        //Feature: support drag and drop
        //Feature: Update folder size in the UI if it is changed externally
        //Feature: Single pane merged UI

        public MainWindow()
        {
            //Silence freezable trace warnings since they don't seem to represent an actual problem
            PresentationTraceSources.FreezableSource.Switch.Level = SourceLevels.Error;
            BindingExceptionThrower.Attach();
            InitializeComponent();

            SetInitialSort(sourceGrid);
            SetInitialSort(destinationGrid);

            ((MainWindowViewModel) DataContext).Initialize();
        }

        public static DelegateCommand<object> OpenDialogDelegateCommand { get; } = new DelegateCommand<object>(async obj => {
            await DialogHost.Show(obj);
        });

        private static void SetInitialSort(DataGrid dataGrid)
        {
            var firstCol = dataGrid.Columns.First();
            // Mark the UI with what direction it is sorted (places the correct column header arrow)
            firstCol.SortDirection = ListSortDirection.Ascending;

            // Actually sort the items
            dataGrid.Items.SortDescriptions.Add(new SortDescription(firstCol.SortMemberPath, firstCol.SortDirection.Value));
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
