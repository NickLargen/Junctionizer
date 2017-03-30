using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;

using JetBrains.Annotations;

using Utilities.Collections;

namespace Junctionizer.CustomWpfComponents
{
    /// <summary>Only notifies when it changes due to live shaping.
    /// <para/>
    /// All operations that modify the collection must occur on the UI thread.</summary>
    public class LiveShapingSortedValueList<T> : SortedValueList<T>, INotifyCollectionChanged
        where T : IComparable<T>, INotifyPropertyChanged
    {
        private enum LiveShapingCategory
        {
            Filtering,
            Sorting
        }

        private struct LiveShapingProperty
        {
            public LiveShapingCategory Category { get; }
            public string PropertyName { get; }

            public LiveShapingProperty(string propertyName, LiveShapingCategory category)
            {
                PropertyName = propertyName;
                Category = category;
            }
        }


        private static readonly ConcurrentDictionary<string, DependencyProperty> DependencyPropertiesCache =
            new ConcurrentDictionary<string, DependencyProperty>();

        /// <inheritdoc/>
        public LiveShapingSortedValueList(ICollectionViewLiveShaping liveShapingView)
        {
            LiveShapingView = liveShapingView;

            if (LiveShapingView.CanChangeLiveFiltering || LiveShapingView.CanChangeLiveSorting || LiveShapingView.CanChangeLiveGrouping)
                throw new NotSupportedException(
                    $"{nameof(LiveShapingSortedValueList<T>)} does not support changes in live shaping properties.");

            if (liveShapingView.IsLiveGrouping == true) throw new NotSupportedException("(Live) grouping is not supported.");

            if (LiveShapingView.IsLiveSorting == false && LiveShapingView.IsLiveFiltering == false) throw new NotSupportedException($"Only use {nameof(LiveShapingSortedValueList<T>)} if you are actually live shaping.");

            LiveShapingView.LiveSortingProperties.CollectionChanged += CreateCollectionChangedEventHandlerFor(LiveShapingCategory.Sorting);
            LiveShapingView.LiveFilteringProperties.CollectionChanged += CreateCollectionChangedEventHandlerFor(LiveShapingCategory.Filtering);
        }

        [NotNull]
        private NotifyCollectionChangedEventHandler CreateCollectionChangedEventHandlerFor(LiveShapingCategory category)
        {
            return (sender, args) => {
                if (args.Action == NotifyCollectionChangedAction.Reset)
                {
                    ClearAllLiveShapingPropertiesOf(category);
                    ((IEnumerable<string>) sender).ForEach(
                        newPropertyName => AddLiveShapingProperty(newPropertyName, category));
                }
                else if (args.Action != NotifyCollectionChangedAction.Move)
                {
                    args.OldItems?.Cast<string>()
                        .ForEach(oldPropertyName => RemoveLiveShapingProperty(oldPropertyName, category));
                    args.NewItems?.Cast<string>()
                        .ForEach(newPropertyName => AddLiveShapingProperty(newPropertyName, category));
                }
            };
        }

        private void AddLiveShapingProperty(string newPropertyName, LiveShapingCategory category)
        {
            var newDp = DependencyPropertiesCache.GetOrAdd(GetDependencyPropertyNameFor(newPropertyName, category),
                name => DependencyProperty.Register(name, typeof(object), typeof(LiveShapingSortedValueList<>)));

            var lsp = new LiveShapingProperty(newPropertyName, category);
            ActiveDependencyProperties.Add(lsp, newDp);
            LiveShapingItems.Values.ForEach(lsi => RegisterBinding(lsi, lsp, newDp));
        }

        private void ClearAllLiveShapingPropertiesOf(LiveShapingCategory category)
        {
            List<LiveShapingProperty> lspsToRemove = new List<LiveShapingProperty>();
            foreach (var adpPair in ActiveDependencyProperties.Where(pair => pair.Key.Category == category))
            {
                var dependencyProperty = adpPair.Value;
                LiveShapingItems.Values.ForEach(lsi => lsi.RemoveBinding(dependencyProperty));

                lspsToRemove.Add(adpPair.Key);
            }

            lspsToRemove.ForEach(lsp => ActiveDependencyProperties.Remove(lsp));
        }

        private void RemoveLiveShapingProperty(string oldSortPropertyName, LiveShapingCategory category)
        {
            var liveShapingProperty = new LiveShapingProperty(oldSortPropertyName, category);
            var oldDp = ActiveDependencyProperties[liveShapingProperty];

            LiveShapingItems.Values.ForEach(lsi => lsi.RemoveBinding(oldDp));
            ActiveDependencyProperties.Remove(liveShapingProperty);
        }


        private static LiveShapingCategory GetCategoryOfDependencyProperty(DependencyProperty dp)
        {
            return Enum.GetValues(typeof(LiveShapingCategory))
                       .Cast<LiveShapingCategory>()
                       .Single(category => dp.Name.StartsWith(category.ToString(), StringComparison.Ordinal));
        }

        [NotNull]
        private static string GetDependencyPropertyNameFor(string propertyName, LiveShapingCategory category)
            => $"{category} {typeof(T).Name} {propertyName} (Live)";

        /// <summary>SortPropertyName -> DependencyProperty</summary>
        private Dictionary<LiveShapingProperty, DependencyProperty> ActiveDependencyProperties { get; } = new Dictionary<LiveShapingProperty, DependencyProperty>();

        private IEqualityComparer<T> EqualityComparer { get; } = EqualityComparer<T>.Default;

        private Dictionary<T, LiveShapingItem<T>> LiveShapingItems { get; } = new Dictionary<T, LiveShapingItem<T>>();

        public ICollectionViewLiveShaping LiveShapingView { get; }

        protected HashSet<T> FilteredItems { get; } = new HashSet<T>();

        private Predicate<object> _filter;
        public Predicate<object> Filter
        {
            get { return _filter; }
            set {
                if (value != _filter)
                {
                    _filter = value;
                    RecalculateFilter();
                }
            }
        }

        public void RecalculateFilter()
        {
            if (Filter == null)
            {
                FilteredItems.ForEach(item => InternalAdd(item));
                FilteredItems.Clear();
            }
            else
            {
                // Remove the items that no longer pass the filter
                var itemsRemovedByNewFilter = new List<T>();
                for (int i = BackingList.Count - 1; i >= 0; i--)
                {
                    if (!PassesFilter(BackingList[i]))
                    {
                        itemsRemovedByNewFilter.Add(BackingList[i]);
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, BackingList[i], i));
                        base.RemoveAt(i);
                    }
                }

                // Add the previously filtered items that that now pass 
                FilteredItems.RemoveWhere(item => {
                    var insertionIndex = InternalAdd(item);
                    if (insertionIndex >= 0)
                    {
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, insertionIndex));
                        return true;
                    }

                    return false;
                });

                // Don't move items into the filtered list until it has been iterated over
                FilteredItems.UnionWith(itemsRemovedByNewFilter);
            }
        }

        private bool PassesFilter(T item) => Filter == null || Filter(item);

        public override int Add(T item)
        {
            CreateLiveShapingItem(item);
            return base.Add(item);
        }

        protected override int InternalAdd(T item)
        {
            if (PassesFilter(item)) return base.InternalAdd(item);

            AddFilteredItem(item);
            return -1;
        }

        public override IEnumerable<ItemIndexPair> AddAll(IEnumerable<T> enumerable)
        {
            var elementsPassingFilter = enumerable.ToLookup(PassesFilter);

            // Avoid evalutating the provided enumerable multiple times
            elementsPassingFilter[true].Concat(elementsPassingFilter[false]).ForEach(CreateLiveShapingItem);

            elementsPassingFilter[false].ForEach(AddFilteredItem);
            return base.AddAll(elementsPassingFilter[true]);
        }

        private void AddFilteredItem(T item) => FilteredItems.Add(item);

        /// <inheritdoc/>
        protected override IEnumerable<ItemIndexPair> AddMultiItemList(List<T> addList)
        {
            // We can't use the base implementation because property changes may invalidate the current sort order at any time
            addList.Sort(CompositeComparer);
            return addList.Select(item => new ItemIndexPair(item, InternalAdd(item)));
        }

        /// <inheritdoc/>
        public override void Clear()
        {
            LiveShapingItems.Values.ForEach(OnLiveShapingItemRemoved);
            LiveShapingItems.Clear();
            base.Clear();
        }

        private void CreateLiveShapingItem(T item)
        {
            var liveShapingItem = new LiveShapingItem<T>(item);
            liveShapingItem.PropertyChanged += OnDependencyPropertyChanged;
            LiveShapingItems.Add(item, liveShapingItem);
            foreach (KeyValuePair<LiveShapingProperty, DependencyProperty> pair in ActiveDependencyProperties)
            {
                RegisterBinding(liveShapingItem, pair.Key, pair.Value);
            }
        }

        private void RegisterBinding(LiveShapingItem<T> liveShapingItem, LiveShapingProperty lsp, DependencyProperty dp)
        {
            liveShapingItem.AddBinding(lsp.PropertyName, dp);
            if ( lsp.Category == LiveShapingCategory.Sorting)
            {
                PropertyChangedEventManager.AddHandler(liveShapingItem.Item, OnItemSortPropertyChanged, lsp.PropertyName);
            }
        }

        /// <summary>Returns a correct position in the list for the provided item, even if the item is in the collection in the wrong place (as long as IsSortDirty is accurate)</summary>
        protected override int FindCorrectLocation(T item)
        {
            var searchValue = InternalBinarySearch(0, Count, item, false);
            return searchValue < 0 ? ~searchValue : searchValue;
        }

        /// <inheritdoc/>
        public override int IndexOf(T item)
        {
            var indexOf = base.IndexOf(item);
            // If any items have been changed the list may not be fully sorted and the binary search may fail
            return indexOf >= 0 ? indexOf : LinearSearch(item);
        }

        private int InternalBinarySearch(int index, int count, T item, bool checkForEquality)
        {
            int lo = index;
            int hi = index + count - 1;
            while (lo <= hi)
            {
                int mid = lo + ((hi - lo) >> 1);
                // Ignore items that need to be resorted
                int usableMid = mid;
                while (usableMid <= hi && LiveShapingItems[BackingList[usableMid]].IsSortDirty)
                {
                    if (checkForEquality && EqualityComparer.Equals(BackingList[usableMid], item)) return usableMid;

                    usableMid++;
                }

                if (usableMid > hi)
                {
                    //everything in [mid,hi] had a dirty sort
                    usableMid = mid - 1;

                    while (usableMid >= lo && LiveShapingItems[BackingList[usableMid]].IsSortDirty)
                    {
                        if (checkForEquality && EqualityComparer.Equals(BackingList[usableMid], item)) return usableMid;

                        usableMid--;
                    }

                    if (usableMid < lo)
                    {
                        //everything in [lo,hi] was dirty, so place it after that interval
                        return ~(hi + 1);
                    }

                    var usableOrder = CompositeComparer.Compare(item, BackingList[usableMid]);

                    if (usableOrder == 0)
                    {
                        return checkForEquality && !EqualityComparer.Equals(BackingList[usableMid], item) ? ~usableMid : usableMid;
                    }

                    if (usableOrder < 0)
                    {
                        hi = usableMid - 1;
                    }
                    else
                    {
                        //we already checked that [mid,hi] is dirty, so instead of setting lo = mid + 1 we can just end here
                        return ~(hi + 1);
                    }
                }
                else
                {
                    var usableOrder = CompositeComparer.Compare(item, BackingList[usableMid]);
                    if (usableOrder == 0)
                    {
                        return checkForEquality && !EqualityComparer.Equals(BackingList[usableMid], item) ? ~usableMid : usableMid;
                    }
                    // we know that [mid,usableMid) are dirty and should be ignored
                    if (usableOrder < 0)
                    {
                        hi = mid - 1;
                    }
                    else
                    {
                        lo = usableMid + 1;
                    }
                }
            }

            return ~lo;
        }

        /// <summary>Will return a negative number if the item is not found, which may happen even if the item is in the collection</summary>
        private int InternalBinarySearchWithEqualityCheck(T item) => InternalBinarySearch(0, Count, item, true);

        private int LinearSearch(T item) => BackingList.IndexOf(item);

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);

        private void OnDependencyPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            T item = (T) (LiveShapingItem<T>) dependencyObject;

            switch (GetCategoryOfDependencyProperty(args.Property))
            {
                case LiveShapingCategory.Sorting:
                    int originalBinarySearchIndex = InternalBinarySearchWithEqualityCheck(item);
                    int actualIndex;
                    int targetIndex;

                    if (originalBinarySearchIndex >= 0)
                    {
                        Debug.Assert(BackingList[originalBinarySearchIndex].Equals(item));

                        actualIndex = originalBinarySearchIndex;
                        targetIndex = FindCorrectLocation(item);
                    }
                    else
                    {
                        actualIndex = LinearSearch(item);
                        targetIndex = ~originalBinarySearchIndex;
                    }

                    LiveShapingItems[item].IsSortDirty = false;
                    if (actualIndex >= 0)
                    {
                        // adjust targetIndex if the item at actualIndex will no longer be there
                        if (actualIndex < targetIndex) targetIndex--;
                        if (targetIndex != actualIndex)
                        {
                            BackingList.Move(actualIndex, targetIndex);
                            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, targetIndex,
                                actualIndex));
                        }
                    }
                    break;
                case LiveShapingCategory.Filtering:
                    var passesFilter = PassesFilter(item);

                    if (passesFilter)
                    {
                        if (FilteredItems.Remove(item))
                        {
                            var insertionIndex = base.Add(item);
                            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, insertionIndex));
                        }
                    }
                    else
                    {
                        if (!FilteredItems.Contains(item))
                        {
                            var removalIndex = IndexOf(item);

                            // It is possible that the liveshapingitem has been registered but the item has not yet been added to this collection (causing index = -1), in which case this is a noop
                            if (removalIndex >= 0)
                            {
                                base.RemoveAt(removalIndex);
                                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, removalIndex));
                                FilteredItems.Add(item);
                            }
                        }
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            /* Debug.WriteLine(args.Property + "  " + string.Join(", ",
                                 BackingList.OfType<GameFolderPair>()
                                            .Select(
                                                folderPair => {
                                                    if (folderPair.DestinationEntry == null) return "-99";
 
                                                    return SizeToStringConverter.Instance.Convert(folderPair.DestinationEntry?.Size)
                                                           + (LiveShapingItems[folderPair as T].IsSortDirty ? "" : "✓");
                                                })
                             ));*/
        }

        private void OnItemSortPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (LiveShapingItems.TryGetValue((T) sender, out var liveShapingItem)) liveShapingItem.IsSortDirty = true;
            else Debug.WriteLine($"{sender} not found");
        }

        /// <summary>Should always be called before a LiveShapingItem is removed</summary>
        /// <param name="liveShapingItem"></param>
        private void OnLiveShapingItemRemoved(LiveShapingItem<T> liveShapingItem)
        {
            liveShapingItem.PropertyChanged -= OnDependencyPropertyChanged;

            ActiveDependencyProperties.Keys.ForEach(lsp => {
                PropertyChangedEventManager.RemoveHandler((T) liveShapingItem, OnItemSortPropertyChanged, lsp.PropertyName);
            });
        }

        /// <inheritdoc/>
        public override void RemoveAt(int index)
        {
            OnLiveShapingItemRemoved(LiveShapingItems[BackingList[index]]);
            LiveShapingItems.Remove(BackingList[index]);
            base.RemoveAt(index);
        }

#if !DisableRemoveAll
/// <inheritdoc/>
        protected override IEnumerable<ItemIndexPair> RemoveMultiItemList(List<T> removeList)
        {
            removeList.Sort(CompositeComparer);
            return ((IEnumerable<T>) removeList)
                .Reverse()
                .Select(item => new ItemIndexPair(item, Remove(item)))
                .Where(pair => pair.Index >= 0);
        }
#endif
    }
}
