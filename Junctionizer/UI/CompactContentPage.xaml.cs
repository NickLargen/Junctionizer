using System;
using System.Collections.ObjectModel;

using Junctionizer.CustomWpfComponents;
using Junctionizer.Model;
using Junctionizer.ViewModels;

namespace Junctionizer.UI
{
    public partial class CompactContentPage
    {
        public CompactContentPage()
        {
            InitializeComponent();

            DataContextChanged += (source, e) => {
                var mainWindowViewModel = (MainWindowViewModel) DataContext;

                var liveFilteringProperties = new ObservableCollection<string>() {
                    nameof(GameFolderPair.SourceEntry) + "." + nameof(GameFolder.Size),
                    nameof(GameFolderPair.DestinationEntry) + "." + nameof(GameFolder.Size),
                };
                var setCollectionView = new SetCollectionView<GameFolderPair, GameFolderPairEnumerable>(mainWindowViewModel.FolderPairCollection, liveFilteringProperties);

                setCollectionView.Filter = obj => {
                    var folderPair = (GameFolderPair) obj;

                    return folderPair.SourceEntry != null && mainWindowViewModel.PassesFilter(folderPair.SourceEntry)
                           || folderPair.DestinationEntry != null && mainWindowViewModel.PassesFilter(folderPair.DestinationEntry);
                };

                mainWindowViewModel.PassesFilterChangedObservable.Subscribe(pattern => {
                    setCollectionView.NotifyFilterChanged();
                });

                compactDataGrid.ItemsSource = setCollectionView;
            };
        }
    }
}
