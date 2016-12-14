using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;

using JetBrains.Annotations;

using PropertyChanged;

using Utilities.Collections;
using Utilities.Comparers;

namespace GameMover.CustomWpfComponents
{
    [ImplementPropertyChanged]
    public class SetCollectionView<T, TCollectionType>
        : CollectionView, IComparer<T>, ICollectionViewLiveShaping, INotifyPropertyChanged, INotifyCollectionChanged
        where T : IComparable<T>, INotifyPropertyChanged
        where TCollectionType : IEnumerable<T>, INotifyCollectionChanged
    {
        public SetCollectionView([NotNull] TCollectionType set, ObservableCollection<string> liveFilteringProperties = null) : base(set)
        {
            if (liveFilteringProperties != null) LiveFilteringProperties = liveFilteringProperties;

            Set = set;
            InternalSortedList = new LiveShapingSortedValueList<T>(this);
            InternalSortedList.AddAll(set);

            InternalSortedList.CollectionChanged += (sender, args) => OnCollectionChanged(args);

            ((INotifyCollectionChanged) SortDescriptions).CollectionChanged += OnSortDescriptionsCollectionChanged;
        }


        public TCollectionType Set { get; }


        #region Private Properties, Fields, Constants

        private const int UNKNOWN_INDEX = -1;

        private LiveShapingSortedValueList<T> InternalSortedList { get; }

        private IComparer<string> AscendingStringComparer { get; } = NaturalStringComparer.OrdinalIgnoreCase;
        private IComparer<string> DescendingStringComparer { get; } =
            new ReverseComparer<string>(NaturalStringComparer.OrdinalIgnoreCase.Compare);

        public override object CurrentItem => null;
        public override int CurrentPosition => -1;

        #endregion Private Fields


        #region INotifyPropertyChanged

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { PropertyChanged += value; }
            remove { PropertyChanged -= value; }
        }
        protected override event PropertyChangedEventHandler PropertyChanged;

        protected override void OnPropertyChanged(PropertyChangedEventArgs e) => PropertyChanged?.Invoke(this, e);

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

        #endregion


        #region INotifyCollectionChanged

        event NotifyCollectionChangedEventHandler INotifyCollectionChanged.CollectionChanged
        {
            add { CollectionChanged += value; }
            remove { CollectionChanged -= value; }
        }

        protected override event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <inheritdoc/>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            // If there is a pending refresh we don't need to emit collection change events because the entire view is going to be rebuilt with the current state
            // The WPF framework will throw an error if we do anyway
            if (!IsRefreshDeferred)
            {
                CollectionChanged?.Invoke(this, e);

                if (e.Action != NotifyCollectionChangedAction.Replace &&
                    e.Action != NotifyCollectionChangedAction.Move)
                {
                    OnPropertyChanged(nameof(Count));
                }
            }
        }

        #endregion


        #region Filtering

        /// <inheritdoc/>
        public override bool CanFilter => true;
        public void NotifyFilterChanged() => InternalSortedList.RecalculateFilter();

        #endregion


        #region Grouping

        /// <inheritdoc/>
        public override bool CanGroup => false;

        #endregion


        #region Sorting

        protected IComparer<T> ActiveComparer { get; set; }

        /// <inheritdoc/>
        public override bool CanSort => true;

        /// <summary> Return -, 0, or +, according to whether first occurs before, at, or after second (respectively)</summary>
        public virtual int Compare(T first, T second)
        {
            if (ActiveComparer == null) throw new InvalidOperationException("Active comparer should never be null.");

            return ActiveComparer.Compare(first, second);
        }

        public Comparer<IComparable> ComparableComparer { get; } =
            Comparer<IComparable>.Create((x, y) => x?.CompareTo(y) ?? (y == null ? 0 : -1));

        private IComparer<T> _customSort;

        /// <summary>Apply a custom sort, clearing any existing <see cref="CollectionView.SortDescriptions"/> and <see cref="LiveSortingProperties"/>.</summary>
        public IComparer<T> CustomSort
        {
            get { return _customSort; }
            set {
                if (AllowsCrossThreadChanges) VerifyAccess();
                _customSort = value;

                SortDescriptions.Clear();

                RefreshOrDefer();
            }
        }

        public sealed override SortDescriptionCollection SortDescriptions { get; } = new SortDescriptionCollection();

        // SortDescription was added/removed, refresh CollectionView
        private void OnSortDescriptionsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var sortDescriptionsSender = (SortDescriptionCollection) sender;

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                LiveSortingProperties.Clear();
                sortDescriptionsSender.ForEach(sortDescription => LiveSortingProperties.Add(sortDescription.PropertyName));
            }
            else if (e.Action != NotifyCollectionChangedAction.Move)
            {
                e.OldItems?.Cast<SortDescription>().ForEach(sortDescription => LiveSortingProperties.Remove(sortDescription.PropertyName));
                e.NewItems?.Cast<SortDescription>().ForEach(sortDescription => LiveSortingProperties.Add(sortDescription.PropertyName));
            }

            if (sortDescriptionsSender.Count > 0)
            {
                _customSort = null;
            }

            RefreshOrDefer();
        }

        #endregion


        #region Containment / Counting / Enumeration / Indexing

        public override bool Contains(object item)
        {
            VerifyRefreshNotDeferred();

            if (!(item is T)) return false;

            return InternalContains((T) item);
        }

        /// <summary>Return true if internal list contains the item.</summary>
        protected bool InternalContains(T item) => InternalSortedList.Contains(item);

        public override int Count
        {
            get {
                VerifyRefreshNotDeferred();
                return InternalCount;
            }
        }

        /// <summary>The number of items that can be displayed (pass the filter, if it exists).</summary>
        public int InternalCount => InternalSortedList.Count;
        public override bool IsEmpty => InternalCount == 0;

        protected sealed override IEnumerator GetEnumerator()
        {
            VerifyRefreshNotDeferred();
            return InternalGetEnumerator();
        }

        /// <summary>Return an enumerator for the internal collection.</summary>
        protected IEnumerator InternalGetEnumerator() => InternalSortedList.GetEnumerator();

        public sealed override int IndexOf(object item)
        {
            if (!(item is T)) throw new ArgumentException();

            VerifyRefreshNotDeferred();

            return InternalIndexOf((T) item);
        }

        public virtual int InternalIndexOf(T item) => InternalSortedList.IndexOf(item);

        public sealed override object GetItemAt(int index)
        {
            VerifyRefreshNotDeferred();

            return InternalItemAt(index);
        }

        protected object InternalItemAt(int index) => InternalSortedList[index];

        #endregion


        #region Refreshing

        private void VerifyRefreshNotDeferred()
        {
            if (AllowsCrossThreadChanges) VerifyAccess();

            // If the Refresh is being deferred to change filtering or sorting of the
            // data by this CollectionView, then CollectionView will not reflect the correct
            // state of the underlying data.

            if (IsRefreshDeferred) throw new InvalidOperationException("No check or change when deferred");
        }

        protected override void RefreshOverride()
        {
            PrepareComparer();

            InternalSortedList.Clear();
            InternalSortedList.OrderingComparer = ActiveComparer;
            InternalSortedList.Filter = Filter;
            InternalSortedList.AddAll(Set);

            // tell listeners everything has changed
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void PrepareComparer()
        {
            if (CustomSort != null)
            {
                ActiveComparer = CustomSort;
            }
            else if (SortDescriptions.Count == 0)
            {
                ActiveComparer = Comparer<T>.Default;
            }
            else
            {
                ActiveComparer = CreateComparerFromSortDescriptions();
            }
        }

        private static object GetNestedPropertyValue(object obj, string[] splitName)
        {
            for (int i = 0; i < splitName.Length - 1; i++)
            {
                obj = obj.GetType().GetProperty(splitName[i]).GetValue(obj);
            }

            return obj?.GetType().GetProperty(splitName[splitName.Length - 1]).GetValue(obj);
        }

        private IComparer<T> CreateComparerFromSortDescriptions()
        {
            IComparer<T> compoundComparer = null;

            // Reverse so that the sort descriptions are evaluated in order (the first sort description has the highest priority)
            foreach (var sortDescription in SortDescriptions.Reverse())
            {
                IComparer<T> nextComparer;
                var isAscending = sortDescription.Direction == ListSortDirection.Ascending;

                if (sortDescription.PropertyName == null)
                {
                    nextComparer = isAscending ? Comparer<T>.Default : ReverseComparer<T>.Default;
                }
                else
                {
                    var splitName = sortDescription.PropertyName.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries);
                    if (splitName.Length <= 1)
                    {
                        var propertyInfo = typeof(T).GetProperty(sortDescription.PropertyName);
                        if (propertyInfo.PropertyType == typeof(string))
                        {
                            Func<T, string> getString = item => (string) propertyInfo.GetValue(item);
                            var stringComparer = isAscending ? AscendingStringComparer : DescendingStringComparer;
                            nextComparer = Comparer<T>.Create((x, y) => stringComparer.Compare(getString(x), getString(y)));
                        }
                        else
                        {
                            Func<T, IComparable> getValue = item => (IComparable) propertyInfo.GetValue(item, null);

                            nextComparer = Comparer<T>.Create(
                                (first, second) => isAscending
                                                       ? ComparableComparer.Compare(getValue(first), getValue(second))
                                                       : ComparableComparer.Compare(getValue(second), getValue(first)));
                        }
                    }
                    else
                    {
                        //Performance: PropertyPath should be compared against this reflection heavy solution
                        Func<T, IComparable> getValue = item => (IComparable) GetNestedPropertyValue(item, splitName);

                        nextComparer = Comparer<T>.Create(
                            (first, second) => isAscending
                                                   ? ComparableComparer.Compare(getValue(first), getValue(second))
                                                   : ComparableComparer.Compare(getValue(second), getValue(first)));
                    }
                }

                var existingComparer = compoundComparer;
                compoundComparer = compoundComparer == null
                                       ? nextComparer
                                       : Comparer<T>.Create((x, y) => {
                                           var compareValue = nextComparer.Compare(x, y);
                                           return compareValue != 0 ? compareValue : existingComparer.Compare(x, y);
                                       });
            }

            return compoundComparer;
        }

        #endregion


        #region Process Collection Changes

        /// <summary>Handles CollectionChange events sent from the underylying collection.</summary>
        protected override void ProcessCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach ((T newItem, int addIndex) in InternalSortedList.AddAll(e.NewItems.Cast<T>()))
                    {
                        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItem, addIndex));
                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (T oldItem in e.OldItems.Cast<T>())
                    {
                        InternalSortedList.Remove(oldItem, actionBeforeRemove: removeIndex =>
                            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItem,
                                removeIndex)));
                    }

                    break;
                case NotifyCollectionChangedAction.Replace:
                    throw new NotSupportedException("Use a remove and then an add in order to maintain proper sort order.");
                case NotifyCollectionChangedAction.Move:
                    throw new NotSupportedException("Element ordering is controlled by the sort order.");
                case NotifyCollectionChangedAction.Reset:
                    // A refresh will raise the collection change event for us
                    RefreshOrDefer();
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion


        #region MoveCurrentTo Shortcuts - currency is not supported

        public override bool MoveCurrentToFirst() => !IsEmpty;

        public override bool MoveCurrentToLast() => !IsEmpty;

        public override bool MoveCurrentToNext() => !IsEmpty;

        public override bool MoveCurrentToPrevious() => !IsEmpty;

        public override bool MoveCurrentTo(object item) => !IsEmpty;

        /// <inheritdoc/>
        public override bool MoveCurrentToPosition(int position) => 0 <= position && position < InternalCount;

        #endregion


        #region Live Shaping

        /// <summary>Indicates that this view does not support turning live sorting on or off.</summary>
        public bool CanChangeLiveSorting => false;

        /// <summary>Indicates that this view does not support turning live filtering on or off.</summary>
        public bool CanChangeLiveFiltering => false;

        /// <summary>This view does not support grouping.</summary>
        public bool CanChangeLiveGrouping => false;

        /// <summary>Whethether live sorting is active.</summary>
        public bool? IsLiveSorting
        {
            get { return true; }
            set { throw new NotSupportedException(); }
        }

        /// <summary>Whethether live filtering is active.</summary>
        public bool? IsLiveFiltering
        {
            get { return true; }
            set { throw new NotSupportedException(); }
        }

        public bool? IsLiveGrouping
        {
            get { return false; }
            set { throw new NotSupportedException(); }
        }

        /// <summary>Strings describing properties that trigger live sorting in the format <see cref="SortDescription.PropertyName"/>. Automatically includes any sort descriptions.</summary>
        public ObservableCollection<string> LiveSortingProperties { get; } = new ObservableCollection<string>();

        /// <summary>Strings describing properties that trigger live reevaluation of <see cref="CollectionView.Filter"/> in the format <see cref="SortDescription.PropertyName"/>.</summary>
        public ObservableCollection<string> LiveFilteringProperties { get; } = new ObservableCollection<string>();

        [Obsolete("Live grouping is not currently supported.")]
        public ObservableCollection<string> LiveGroupingProperties => null;

        #endregion Live Shaping
    }
}
