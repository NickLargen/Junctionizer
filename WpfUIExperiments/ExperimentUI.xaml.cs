using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Data;

using static WpfUIExperiments.TrivialViewModel;

namespace WpfUIExperiments
{
    /// <summary>Interaction logic for MainWindow.xaml</summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var listCollectionView = (ListCollectionView) CollectionViewSource.GetDefaultView(coll);

            listCollectionView.SortDescriptions.Add(new SortDescription());
            listCollectionView.IsLiveSorting = true;
            for (int i = 0; i < 1_0_000; i++)
            {
                coll.AddAsync(i);
            }

            Debug.WriteLine("********" + coll.Count);
            coll.WaitForQueuedTasks().GetAwaiter().GetResult();
            Debug.WriteLine("********" + coll.Count);
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var match = coll.First(i => i == (int) listBox.SelectedItem);


            coll.RemoveKeyAsync(listBox.SelectedIndex);
        }
    }
}
