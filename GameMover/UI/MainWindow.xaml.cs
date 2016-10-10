using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

using GameMover.ViewModels;

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

        //BUG: double clicking column to resize introduces the empty column on the right
        //BUG: when junction scanning for small folders the search can be fast enough that it completes before the dialog exists, meaning the close command does nothing and the dialog stays open


        //Performance: notify collection change only once when batch selecting items
        //Performance: sorting by size on hdd hangs ui
        //Performance: test opening giant folder

        //Feature: Loading spinners when calculating size of a directory (it's useful for checking size of Blizzard games on hdd)
        //Feature: select all corresponding elements
        //Feature: support drag and drop
        //Feature: Update folder size in the UI if it is changed externally

        public MainWindow()
        {
            BindingExceptionThrower.Attach();
            InitializeComponent();

            SetInitialSort(installGrid);
            SetInitialSort(storageGrid);

            ((ViewModel) DataContext).Initialize();
        }

        private static void SetInitialSort(DataGrid dataGrid)
        {
            var firstCol = dataGrid.Columns.First();
            // Mark the UI with what direction it is sorted (places the correct column header arrow)
            firstCol.SortDirection = ListSortDirection.Ascending;

            // Actually sort the items
            dataGrid.Items.SortDescriptions.Add(new SortDescription(firstCol.SortMemberPath, firstCol.SortDirection.Value));
        }


        private void HideStorage(object sender, RoutedEventArgs e)
        {
            storageColumnDefinition.Width = new GridLength(0);
        }

        private void ShowStorage(object sender, RoutedEventArgs e)
        {
            storageColumnDefinition.Width = new GridLength(.5, GridUnitType.Star);
        }

    }

}
