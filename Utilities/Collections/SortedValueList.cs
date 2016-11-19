#define DisableRemoveAll

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using JetBrains.Annotations;

namespace Utilities.Collections
{
    /// <summary>Items should be unique with respect to the item's IComparable{T} implementation. This collection is not thread safe.</summary>
    /// <typeparam name="T"></typeparam>
    public class SortedValueList<T> : ICollection<T>, IReadOnlyList<T> where T : IComparable<T>
    {
        [CanBeNull] private IComparer<T> _orderingComparer;
        /// <summary>Maintains the sorted collection first by the provided comparer, then by the default ordering of <typeparamref name="T"/>.</summary>
        public IComparer<T> OrderingComparer
        {
            get { return _orderingComparer ?? Comparer<T>.Default; }
            set {
                if (!Equals(value, OrderingComparer))
                {
                    _orderingComparer = value;
                    CompositeComparer = CreateCompositeComparer(OrderingComparer);
                }
            }
        }

        private IComparer<T> _compositeComparer;
        protected IComparer<T> CompositeComparer
        {
            get { return _compositeComparer; }
            set {
                if (!Equals(value, CompositeComparer))
                {
                    _compositeComparer = value;
                    BackingList.Sort(CompositeComparer);
                }
            }
        }

        protected SimpleList<T> BackingList { get; } = new SimpleList<T>();

        /// <inheritdoc cref="List{T}.Capacity"/>
        public int Capacity
        {
            get { return BackingList.Capacity; }
            set { BackingList.Capacity = value; }
        }

        /// <summary>Gets the number of elements contained in the <see cref="SortedValueList{T}"/></summary>
        public int Count => BackingList.Count;

        public bool IsReadOnly => false;

        public T this[int index] => BackingList[index];

        void ICollection<T>.Add(T item) => Add(item);

        /// <summary>Returns the zero based index the item was inserted at.</summary>
        public virtual int Add(T item)
        {
            var insertionIndex = FindCorrectLocation(item);
            BackingList.Insert(insertionIndex, item);
            return insertionIndex;
        }

        public IEnumerable<ItemIndexPair> AddAll([NotNull] IEnumerable<T> enumerable) => AddList(enumerable.ToList());


        public struct ItemIndexPair
        {
            public T Item { get; }
            public int Index { get; }

            public ItemIndexPair(T item, int index)
            {
                Item = item;
                Index = index;
            }

            public void Deconstruct(out T item, out int index)
            {
                index = Index;
                item = Item;
            }
        }

        protected IEnumerable<ItemIndexPair> AddList([NotNull] List<T> addList)
        {
            if (Count == 0)
            {
                BackingList.AddRange(addList);
                BackingList.Sort(CompositeComparer);

                return BackingList.Select((item, index) => new ItemIndexPair(item, index));
            }

            if (addList.Count == 1)
            {
                var addedIndex = Add(addList[0]);
                return new[] {new ItemIndexPair(addList[0], addedIndex)};
            }

            return AddMultiItemList(addList);
        }

        protected virtual IEnumerable<ItemIndexPair> AddMultiItemList(List<T> addList)
        {
            Debug.Assert(addList.Count > 1);

            // Sort the provided list and merge it into this one for O(n+k*log(k)) runtime instead of O(nk) when adding individually (where k is addList.Count)
            int originalListIndex = Count - 1;
            addList.Sort(CompositeComparer);

            // Create empty elements at the end of the list that we will fill in while merging
            BackingList.AppendEmptyElements(addList.Count);

            var addedItems = new List<ItemIndexPair>(addList.Count);
            //Merge the two lists
            for (int newListIndex = Count - 1, addListIndex = addList.Count - 1; addListIndex >= 0; newListIndex--)
            {
                var addListItem = addList[addListIndex];

                if (originalListIndex < 0 || CompositeComparer.Compare(this[originalListIndex], addListItem) <= 0)
                {
                    addedItems.Add(new ItemIndexPair(addListItem, newListIndex));
                    BackingList[newListIndex] = addListItem;
                    addListIndex--;
                }
                else
                {
                    BackingList[newListIndex] = this[originalListIndex];
                    originalListIndex--;
                }
            }

            // Return the added elements in ascending order
            return ((IEnumerable<ItemIndexPair>) addedItems).Reverse();
        }

        /// <summary>Returns the index that an item should be inserted at</summary>
        protected virtual int FindCorrectLocation(T item)
        {
            var searchIndex = IndexOf(item);
            return searchIndex < 0 ? ~searchIndex : searchIndex;
        }

        /// <summary>Removes all elements from the <see cref="SortedValueList{T}"/>.</summary>
        public virtual void Clear() => BackingList.Clear();

        /// <summary>Determines whether an element is in the <see cref="SortedValueList{T}"/>.</summary>
        public bool Contains(T item) => IndexOf(item) >= 0;

        public void CopyTo(T[] array, int arrayIndex) => BackingList.CopyTo(array, arrayIndex);

        protected Comparer<T> CreateCompositeComparer([NotNull] IComparer<T> newComparer)
        {
            if (Equals(newComparer, Comparer<T>.Default)) return Comparer<T>.Default;

            return Comparer<T>.Create((first, second) => {
                var firstCompare = newComparer.Compare(first, second);
                return firstCompare == 0 ? Comparer<T>.Default.Compare(first, second) : firstCompare;
            });
        }

        /// <summary>Returns an enumerator that iterates through the <see cref="SortedValueList{T}"/> in order of the current <see cref="CompositeComparer"/>.</summary>
        public IEnumerator<T> GetEnumerator() => BackingList.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>Returns the zero based index of the element if it exists, otherwise the bit complement of where it would exist. Assumes that the collection's items are unique with respect to the actual comparer.</summary>
        public virtual int IndexOf(T item) => BackingList.BinarySearch(item, CompositeComparer);

        /// <summary>If the item is found in this collection then it is removed. Returns the <see cref="IndexOf"/> the item before it was removed (a negative number for elements not present in the collection).</summary>
        public int Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0) RemoveAt(index);
            return index;
        }

        /// <summary>If the item is found in this collection the action is executed with its index as the argument and then it is removed. Returns the <see cref="IndexOf"/> the item before it was removed (a negative number for elements not present in the collection).</summary>
        public int Remove(T item, Action<int> actionBeforeRemove)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                actionBeforeRemove.Invoke(index);
                RemoveAt(index);
            }

            return index;
        }

        bool ICollection<T>.Remove(T item) => Remove(item) >= 0;
#if !DisableRemoveAll
        public IEnumerable<ItemIndexPair> RemoveAll([NotNull] IEnumerable<T> enumerable) => RemoveList(enumerable.ToList());

        protected IEnumerable<ItemIndexPair> RemoveList([NotNull] List<T> removeList)
        {
            // If this collection is empty then there is no need to even look at the provided items
            if (Count == 0) return removeList.Select(item => new ItemIndexPair(item, -1));

            if (removeList.Count == 1)
            {
                var removedIndex = Remove(removeList[0]);
                return new[] {new ItemIndexPair(removeList[0], removedIndex)};
            }

            return RemoveMultiItemList(removeList);
        }
        
        /// <summary>
        /// needs testing
        /// </summary>
        protected virtual IEnumerable<ItemIndexPair> RemoveMultiItemList(List<T> removeList)
        {
            Debug.Assert(removeList.Count > 1);

            removeList.Sort(CompositeComparer);
            var removedItems = new List<ItemIndexPair>();

            int newListIndex = 0;
            for (int originalListIndex = 0, removeListIndex = 0; originalListIndex < BackingList.Count; originalListIndex++)
            {
                // If we've run out of items to remove then a comparison to null would be 1
                int originalComparedToItemToRemove = 1;
                for (; removeListIndex < removeList.Count; removeListIndex++)
                {
                    // If the next item to be removed comes before the current item then it does not exist in this collection
                    var itemToRemove = removeList[removeListIndex];
                    originalComparedToItemToRemove = CompositeComparer.Compare(BackingList[originalListIndex], itemToRemove);

                    if (originalComparedToItemToRemove <= 0) break;
                }

                if (originalComparedToItemToRemove == 0)
                {
                    // Don't include the next item, it was in the remove list
                    var itemToRemove = removeList[removeListIndex];
                    removedItems.Add(new ItemIndexPair(itemToRemove, originalListIndex));
                }
                else
                {
                    BackingList[newListIndex] = BackingList[originalListIndex];
                    newListIndex++;
                }
            }

            // The order of returned items matters because it affects the index they would exist at - we want to return the last elements first
            return ((IEnumerable<ItemIndexPair>) removedItems).Reverse();
        }
#endif

        /// <inheritdoc cref="List{T}.RemoveAt"/>
        public virtual void RemoveAt(int index) => BackingList.RemoveAt(index);

        /// <inheritdoc cref="List{T}.TrimExcess"/>
        public void TrimExcess() => BackingList.TrimExcess();
    }
}
