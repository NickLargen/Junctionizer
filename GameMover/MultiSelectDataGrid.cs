using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace GameMover
{

    internal class MultiSelectDataGrid : DataGrid
    {

        public MultiSelectDataGrid()
        {
            Loaded += (sender, args) => {
                SelectedItemsList = SelectedItems;
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
