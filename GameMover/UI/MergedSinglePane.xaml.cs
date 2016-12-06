using System;

using GameMover.CustomWpfComponents;
using GameMover.ViewModels;

namespace GameMover.UI
{
    public partial class MergedSinglePane
    {
        public MergedSinglePane(MainWindowViewModel mainWindowViewModel)
        {
            InitializeComponent();

            DataContext = mainWindowViewModel;

            var setCollectionView = new SetCollectionView<MergedItem, MergedItemEnumerable>(mainWindowViewModel.MergedCollection);

            setCollectionView.Filter = obj => {
                var mergedItem = (MergedItem) obj;

                return mergedItem.SourceEntry != null && mainWindowViewModel.PassesFilter(mergedItem.SourceEntry)
                       || mergedItem.DestinationEntry != null && mainWindowViewModel.PassesFilter(mergedItem.DestinationEntry);
            };

            mainWindowViewModel.PassesFilterChangedObservable.Subscribe(pattern => {
                setCollectionView.NotifyFilterChanged();
            });

            mergedItemDataGrid.ItemsSource = setCollectionView;
        }
    }

}
