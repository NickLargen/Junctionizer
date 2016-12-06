using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace GameMover.CustomWpfComponents
{
    public class MultiSelectDataGrid : DataGrid
    {
        private bool IsFirstLoad {get; set;} = true;

        public MultiSelectDataGrid()
        {
            Loaded += (sender, args) => {
                if (IsFirstLoad)
                {
                    IsFirstLoad = false;
                    SelectedItemsList = SelectedItems;

                    var firstCol = Columns.First();
                    // Mark the UI with what direction it is sorted (places the correct column header arrow)
                    firstCol.SortDirection = ListSortDirection.Ascending;

                    // Actually sort the items
                    Items.SortDescriptions.Add(new SortDescription(firstCol.SortMemberPath, firstCol.SortDirection.Value));
                }
            };
        }

        public IList SelectedItemsList
        {
            get { return (IList) GetValue(SelectedItemsListProperty); }
            set { SetValue(SelectedItemsListProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemsListProperty =
            DependencyProperty.Register("SelectedItemsList", typeof(IList), typeof(MultiSelectDataGrid),
                new FrameworkPropertyMetadata(defaultValue: null, flags: FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    }
}
