using System;
using System.Collections.Specialized;

using Junctionizer.CustomWpfComponents;
using Junctionizer.Model;
using Junctionizer.ViewModels;

namespace Junctionizer.UI
{
    public partial class CompactContentPage
    {
        public CompactContentPage(MainWindowViewModel mainWindowViewModel)
        {
            InitializeComponent();
            
            DataContext = mainWindowViewModel;

            var setCollectionView = new SetCollectionView<GameFolderPair, GameFolderPairEnumerable>(mainWindowViewModel.FolderPairCollection);

            mainWindowViewModel.LiveFilteringProperties.CollectionChanged += (sender, args) => {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        setCollectionView.LiveFilteringProperties.Add(nameof(GameFolderPair.SourceEntry) + "." + args.NewItems[0]);
                        setCollectionView.LiveFilteringProperties.Add(nameof(GameFolderPair.DestinationEntry) + "." + args.NewItems[0]);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        setCollectionView.LiveFilteringProperties.Remove(nameof(GameFolderPair.SourceEntry) + "." + args.OldItems[0]);
                        setCollectionView.LiveFilteringProperties.Remove(nameof(GameFolderPair.DestinationEntry) + "." + args.OldItems[0]);
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        throw new NotSupportedException();
                    case NotifyCollectionChangedAction.Move:
                        throw new NotSupportedException();
                    case NotifyCollectionChangedAction.Reset:
                        throw new NotSupportedException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            };

            setCollectionView.Filter = obj => {
                var folderPair = (GameFolderPair) obj;

                return folderPair.SourceEntry != null && mainWindowViewModel.PassesFilter(folderPair.SourceEntry)
                       || folderPair.DestinationEntry != null && mainWindowViewModel.PassesFilter(folderPair.DestinationEntry);
            };

            mainWindowViewModel.PassesFilterChangedObservable.Subscribe(pattern => {
                setCollectionView.NotifyFilterChanged();
            });

            compactDataGrid.ItemsSource = setCollectionView;
        }
    }

}
