using System;

using GameMover.CustomWpfComponents;
using GameMover.Model;
using GameMover.ViewModels;

using Utilities.Collections;

namespace GameMover.UI
{
    public partial class DualPane 
    {
        public DualPane(MainWindowViewModel mainWindowViewModel)
        {
            InitializeComponent();

            DataContext = mainWindowViewModel;

            SetItemsSource(sourceGrid, mainWindowViewModel.SourceCollection, mainWindowViewModel);
            SetItemsSource(destinationGrid, mainWindowViewModel.DestinationCollection, mainWindowViewModel);
        }
        
        private void SetItemsSource(MultiSelectDataGrid dataGrid, FolderCollection folderCollection, MainWindowViewModel mainWindowViewModel)
        {
            var setCollectionView = new SetCollectionView<GameFolder, AsyncObservableKeyedSet<string, GameFolder>>(folderCollection.Folders, mainWindowViewModel.LiveFilteringProperties);

            setCollectionView.Filter = obj => mainWindowViewModel.PassesFilter((GameFolder) obj);
            
            mainWindowViewModel.PassesFilterChangedObservable.Subscribe(pattern => setCollectionView.NotifyFilterChanged());

            dataGrid.ItemsSource = setCollectionView;
        }
    }
}
