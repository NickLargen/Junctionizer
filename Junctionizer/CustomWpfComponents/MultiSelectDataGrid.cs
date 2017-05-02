using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Junctionizer.CustomWpfComponents
{
    public class MultiSelectDataGrid : DataGrid
    {
        private bool IsFirstLoad { get; set; } = true;

        /// <summary>Should be set in xaml so that the correct value is available during the first load</summary>
        public bool SortOnFirstLoad { get; set; }

        public MultiSelectDataGrid()
        {
            Loaded += (sender, args) => {
                if (IsFirstLoad)
                {
                    IsFirstLoad = false;
                    SelectedItemsList = SelectedItems;

                    if (SortOnFirstLoad)
                    {
                        var firstCol = Columns.First();
                        // Mark the UI with what direction it is sorted (places the correct column header arrow)
                        firstCol.SortDirection = ListSortDirection.Ascending;

                        // Actually sort the items
                        Items.SortDescriptions.Add(new SortDescription(firstCol.SortMemberPath, firstCol.SortDirection.Value));
                    }
                }
            };
        }

        public IReadOnlyList<int> DisplayIndices
        {
            get => (IReadOnlyList<int>) GetValue(DisplayIndicesProperty);
            set => SetValue(DisplayIndicesProperty, value);
        }

        public static readonly DependencyProperty DisplayIndicesProperty =
            DependencyProperty.Register(nameof(DisplayIndices), typeof(IReadOnlyList<int>), typeof(MultiSelectDataGrid), new FrameworkPropertyMetadata((source, e) => {
                var dataGrid = (MultiSelectDataGrid) source;
                if (dataGrid.Columns.Count == dataGrid.DisplayIndices.Count)
                {
                    for (var i = 0; i < dataGrid.DisplayIndices.Count; i++)
                    {
                        dataGrid.Columns[i].DisplayIndex = dataGrid.DisplayIndices[i];
                    }
                }
            }));

        protected override void OnColumnReordered(DataGridColumnEventArgs e)
        {
            base.OnColumnReordered(e);
            DisplayIndices = Columns.Select(column => column.DisplayIndex).ToList();
        }

        public IList SelectedItemsList
        {
            get => (IList) GetValue(SelectedItemsListProperty);
            set => SetValue(SelectedItemsListProperty, value);
        }

        public static readonly DependencyProperty SelectedItemsListProperty =
            DependencyProperty.Register("SelectedItemsList", typeof(IList), typeof(MultiSelectDataGrid),
                new FrameworkPropertyMetadata(defaultValue: null, flags: FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    }
}
