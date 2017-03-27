using System;
using System.Collections.Specialized;

using Junctionizer.CustomWpfComponents;
using Junctionizer.Model;
using Junctionizer.ViewModels;

namespace Junctionizer.UI
{
    public partial class MergedSinglePane
    {
        public MergedSinglePane(MainWindowViewModel mainWindowViewModel)
        {
            InitializeComponent();

            DataContext = mainWindowViewModel;

            var setCollectionView = new SetCollectionView<MergedItem, MergedItemEnumerable>(mainWindowViewModel.MergedCollection);

            mainWindowViewModel.LiveFilteringProperties.CollectionChanged += (sender, args) => {
                switch (args.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        setCollectionView.LiveFilteringProperties.Add(nameof(MergedItem.SourceEntry) + "." + args.NewItems[0]);
                        setCollectionView.LiveFilteringProperties.Add(nameof(MergedItem.DestinationEntry) + "." + args.NewItems[0]);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        setCollectionView.LiveFilteringProperties.Remove(nameof(MergedItem.SourceEntry) + "." + args.OldItems[0]);
                        setCollectionView.LiveFilteringProperties.Remove(nameof(MergedItem.DestinationEntry) + "." + args.OldItems[0]);
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
