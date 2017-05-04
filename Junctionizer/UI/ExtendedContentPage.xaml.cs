using System;

using Junctionizer.CustomWpfComponents;
using Junctionizer.Model;
using Junctionizer.ViewModels;

using Utilities.Collections;

namespace Junctionizer.UI
{
    public partial class ExtendedContentPage
    {
        public ExtendedContentPage()
        {
            InitializeComponent();

            DataContextChanged += (sender, args) => {
                var mainWindowViewModel = (MainWindowViewModel) DataContext;

                SetItemsSource(sourceGrid, mainWindowViewModel.SourceCollection, mainWindowViewModel);
                SetItemsSource(destinationGrid, mainWindowViewModel.DestinationCollection, mainWindowViewModel);
            };
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
