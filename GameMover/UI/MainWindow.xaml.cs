using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

using GameMover.CustomWpfComponents;
using GameMover.Model;
using GameMover.ViewModels;

using MaterialDesignThemes.Wpf;

using Prism.Commands;

using Utilities.Collections;

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
                SetItemsSource(sourceGrid, mainWindowViewModel.SourceCollection);
                SetItemsSource(destinationGrid, mainWindowViewModel.DestinationCollection);

                EnableLiveSorting(mainWindowViewModel.DisplayedMappings);
            };
        }

        public static DelegateCommand<object> OpenDialogCommand { get; } = new DelegateCommand<object>(o => OpenDialog(o));

        public static Task OpenDialog(object obj) => DialogHost.Show(obj);

        private void SetItemsSource(MultiSelectDataGrid dataGrid, FolderCollection folderCollection)
        {
            var setCollectionView = new SetCollectionView<GameFolder, AsyncObservableKeyedSet<string, GameFolder>>(folderCollection.Folders);

            var mainWindowViewModel = (MainWindowViewModel) DataContext;

            setCollectionView.Filter = obj => mainWindowViewModel.Filter((GameFolder) obj);

            Observable.FromEventPattern<PropertyChangedEventArgs>(mainWindowViewModel, nameof(MainWindowViewModel.PropertyChanged))
                      .Where(pattern => pattern.EventArgs.PropertyName == nameof(MainWindowViewModel.Filter))
                      .Sample(TimeSpan.FromMilliseconds(200))
                      .ObserveOn(SynchronizationContext.Current)
                      .Subscribe(pattern => setCollectionView.NotifyFilterChanged());

            dataGrid.ItemsSource = setCollectionView;

            var firstCol = dataGrid.Columns.First();
            // Mark the UI with what direction it is sorted (places the correct column header arrow)
            firstCol.SortDirection = ListSortDirection.Ascending;

            // Actually sort the items
            dataGrid.Items.SortDescriptions.Add(new SortDescription(firstCol.SortMemberPath, firstCol.SortDirection.Value));
        }

        /// Update sort order when properties on items within the collection change
        private static void EnableLiveSorting(IEnumerable observableCollection)
        {
            ((ListCollectionView) CollectionViewSource.GetDefaultView(observableCollection)).IsLiveSorting = true;
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
