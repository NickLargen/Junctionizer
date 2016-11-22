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

namespace GameMover.CustomWpfComponents
{
    /// <summary>Only notifies when it changes due to live shaping.
    /// <para/>
    /// All operations that modify the collection must occur on the UI thread.</summary>
    public class LiveShapingSortedValueList<T> : SortedValueList<T>, INotifyCollectionChanged
        where T : IComparable<T>, INotifyPropertyChanged
    {
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

            LiveShapingView.LiveSortingProperties.CollectionChanged += (sender, args) => {
                if (args.Action == NotifyCollectionChangedAction.Reset)
                {
                    ClearAllLiveSortingProperties();
                    ((IEnumerable<string>) sender).ForEach(AddLiveSortingProperty);
                }
                else if (args.Action != NotifyCollectionChangedAction.Move)
                {
                    args.OldItems?.Cast<string>().ForEach(RemoveLiveSortingProperty);
                    args.NewItems?.Cast<string>().ForEach(AddLiveSortingProperty);
                }
            };
        }

        private void AddLiveSortingProperty(string newSortPropertyName)
        {
            var newDp = DependencyPropertiesCache.GetOrAdd(GetDependencyPropertyNameFor(newSortPropertyName),
                name => DependencyProperty.RegisterAttached(name, typeof(object), typeof(LiveShapingSortedValueList<>)));

            ActiveDependencyProperties.Add(newSortPropertyName, newDp);
            LiveShapingItems.Values.ForEach(lsi => RegisterBinding(lsi, newSortPropertyName, newDp));
        }

        private void ClearAllLiveSortingProperties()
        {
            LiveShapingItems.Values.ForEach(lsi => lsi.ClearAllBindings());
            ActiveDependencyProperties.Clear();
        }

        private void RemoveLiveSortingProperty(string oldSortPropertyName)
        {
            var oldDp = ActiveDependencyProperties[oldSortPropertyName];

            LiveShapingItems.Values.ForEach(lsi => lsi.RemoveBinding(oldDp));
            ActiveDependencyProperties.Remove(oldSortPropertyName);
        }

        [NotNull]
        private static string GetDependencyPropertyNameFor(string sortPropertyName) => $"LiveSort {typeof(T).Name} {sortPropertyName}";

        /// <summary>SortPropertyName -> DependencyProperty</summary>
        public Dictionary<string, DependencyProperty> ActiveDependencyProperties { get; } = new Dictionary<string, DependencyProperty>();

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
                FilteredItems.ForEach(item => Add(item));
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
                        RemoveAt(i);
                    }
                }

                // Add the previously filtered items that that now pass 
                FilteredItems.RemoveWhere(item => {
                    if (PassesFilter(item))
                    {
                        var insertionIndex = Add(item);
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, insertionIndex));
                        return true;
                    }

                    return false;
                });

                // Don't add move items into the filtered list until it has been iterated over
                FilteredItems.UnionWith(itemsRemovedByNewFilter);
            }
        }

        private bool PassesFilter(T item) => Filter == null || Filter(item);

        public override int Add(T item)
        {
            if (PassesFilter(item)) return base.Add(item);

            AddFilteredItem(item);
            return -1;
        }

        protected override void BeforeItemInserted(T item)
        {
            CreateLiveShapingItem(item);
            base.BeforeItemInserted(item);
        }

        public override IEnumerable<ItemIndexPair> AddAll(IEnumerable<T> enumerable)
        {
            var elementsPassingFilter = enumerable.ToLookup(PassesFilter);
            elementsPassingFilter[false].ForEach(AddFilteredItem);
            return base.AddAll(elementsPassingFilter[true]);
        }

        private void AddFilteredItem(T item) => FilteredItems.Add(item);

        /// <inheritdoc/>
        protected override IEnumerable<ItemIndexPair> AddMultiItemList(List<T> addList)
        {
            // We can't use the base implementation because property changes may invalidate the current sort order at any time
            addList.Sort(CompositeComparer);
            return addList.Select(item => new ItemIndexPair(item, Add(item)));
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
            foreach (KeyValuePair<string, DependencyProperty> pair in ActiveDependencyProperties)
            {
                RegisterBinding(liveShapingItem, pair.Key, pair.Value);
            }
        }

        private void RegisterBinding(LiveShapingItem<T> liveShapingItem, string sortPropertyName, DependencyProperty dp)
        {
            liveShapingItem.AddBinding(sortPropertyName, dp);
            PropertyChangedEventManager.AddHandler(liveShapingItem.Item, OnItemPropertyChanged, sortPropertyName);
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

                Debug.Assert(actualIndex != targetIndex);
            }

            LiveShapingItems[item].IsSortDirty = false;
            if (actualIndex >= 0 && targetIndex != actualIndex)
            {
                // adjust targetIndex if the item at actualIndex will no longer be there
                if (actualIndex < targetIndex) targetIndex--;
                BackingList.Move(actualIndex, targetIndex);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, item, targetIndex, actualIndex));
            }
        }

        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            LiveShapingItems[(T) sender].IsSortDirty = true;
        }

        /// <summary>Should always be called before a LiveShapingItem is removed</summary>
        /// <param name="liveShapingItem"></param>
        private void OnLiveShapingItemRemoved(LiveShapingItem<T> liveShapingItem)
        {
            liveShapingItem.PropertyChanged -= OnDependencyPropertyChanged;

            ActiveDependencyProperties.Keys.ForEach(sortPropertyName => {
                PropertyChangedEventManager.RemoveHandler((T) liveShapingItem, OnItemPropertyChanged, sortPropertyName);
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
