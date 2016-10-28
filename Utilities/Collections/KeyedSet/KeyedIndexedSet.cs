using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using JetBrains.Annotations;

namespace Utilities.Collections.KeyedCollection
{
    /// <summary>Keys must be unique and non-null. Since keys are unique and an item must generate exactly one key, items are also guaranteed unique. Null items are currently disallowed because the expectation is sets without null is the common scenario and it assists in static analysis and prevents GetKeyForItem from needing to handle nulls.
    /// <para/>
    /// <para/>
    /// Implementation note: if the collection is only appended to it will be in insertion order.</summary>
    public partial class KeyedIndexedSet<TKey, TItem> : IList<TItem>, IReadOnlyList<TItem>, IList
        /*, ISet<TItem>*/
    {
        /// TODO: Swap with last element on removal- removeRange will have some edge cases, investigate implementing in terms of insert but still being able to override with onCollectionChanged, DO NOT PRESERVE INSERTION ORDER
        /// // Do the same thing for insertAt
        /// // Use position for o(1) removal
        /// 
        /// TODO: Investigate making KeyCollection and ValueCollection into Sets
        private const string DUPLICATE_KEY = "An item with the same key has already been added.";

        private struct PositionedValue
        {
            public TItem Item { get; }
            public int Position { get; }

            public PositionedValue(TItem item, int position)
            {
                Item = item;
                Position = position;
            }
        }

        public int Count => Items.Count;
        public bool IsReadOnly => false;
        public bool IsFixedSize => false;

        /// <inheritdoc cref="Get"/>
        public TItem this[[NotNull] TKey key] => Get(key);
        public int Capacity
        {
            get { return Items.Capacity; }
            set { Items.Capacity = value; }
        }

        public IEnumerable<TKey> Keys => Dictionary.Keys;
        public IEnumerable<TItem> Values => Items;
        public IEqualityComparer<TKey> Comparer { get; }

        private Func<TItem, TKey> GetKeyForItem { get; }
        private Dictionary<TKey, PositionedValue> Dictionary { get; }
        private List<TItem> Items { get; }

        /// <summary></summary>
        /// <param name="getKeyForItem"></param>
        /// <param name="capacity"></param>
        /// <param name="comparer"></param>
        public KeyedIndexedSet([NotNull] Func<TItem, TKey> getKeyForItem, int capacity = 0, IEqualityComparer<TKey> comparer = null)
        {
            Comparer = comparer ?? EqualityComparer<TKey>.Default;
            Dictionary = new Dictionary<TKey, PositionedValue>(capacity, Comparer);
            Items = new List<TItem>(capacity);
            GetKeyForItem = getKeyForItem;
        }

        // Read(IndexOf/Contains, Get/[]) Write(Add/Insert, Remove, Replace/Set/[]=)
        // SORT, BINARY SEARCH 


        #region ******************** Addition/Insertion ********************

        /// <summary>O(1) Appends the provided if it does not already exist in the collection.</summary>
        /// <returns>True if the item was added, false if the item already exists.</returns>
        /// <exception cref="ArgumentException">If the item is not present collection but another entry has the same key.</exception>
        public bool TryAdd([NotNull] TItem item)
        {
            return InternalInsert(Count, item, throwIfDuplicateKey: false);
        }

        /// <summary>O(1) Appends the provided item.</summary>
        /// <exception cref="ArgumentException">If the item's key is already present.</exception>
        public void Add([NotNull] TItem item)
        {
            InternalInsert(Count, item, throwIfDuplicateKey: true);
        }

        /// <summary>O(count) Modifies this set</summary>
        /// <returns>The number of items that were added.</returns>
        public virtual void UnionWith([NotNull] IEnumerable<TItem> items)
        {
            throw new NotImplementedException();
        }

        /// <summary>O(1)</summary>
        public bool Insert(int index, [NotNull] TItem item) => InternalInsert(index, item);

        /// <summary>O(count)</summary>
        public int InsertRange(int index, [NotNull] IEnumerable<TItem> collection)
        {
            throw new NotImplementedException();
        }

        /// <summary>Allows insertions where index == Count</summary>
        protected virtual bool InternalInsert(int index, [NotNull] TItem item, bool throwIfDuplicateKey = false)
        {
            if (index > Count) throw new ArgumentOutOfRangeException($"Cannot insert at index {index}, it is greater the collection size of {Count}.");

            var key = GetKeyForItem(item);
            if (TryGetValue(key, out var existingItem))
            {
                if (throwIfDuplicateKey) throw new ArgumentException(DUPLICATE_KEY);

                if (EqualityComparer<TItem>.Default.Equals(item, existingItem))
                {
                    // Item already exists in the set
                    return false;
                }

                throw new ArgumentException("Two unique items with identical keys cannot exist in a KeyedSet.");
            }

            if (index == Count)
            {
                Items.Add(item);
            }
            else
            {
                // An actual insertion into the middle, not an addition onto the end of the list
                Debug.WriteLine(
                    $"Consider using Add instead of inserting items into the middle of a {nameof(KeyedIndexedSet<TKey, TItem>)} for improved performance.");
                // Move the item at the desired location to the end so that we don't have to do an array copy
                var newIndex = Count;
                var itemToMove = Items[index];
                Items.Add(itemToMove);
                Dictionary[GetKeyForItem(itemToMove)] = new PositionedValue(itemToMove, newIndex);

                // Slot in the new item
                Items[index] = item;
            }
            Dictionary.Add(key, new PositionedValue(item, index));

            return true;
        }

        #endregion


        #region******************** Pure Retrieval/Search/Enumeration ********************

        /// <summary>O(1) Determines whether an item in this collection has the provided key.</summary>
        [Pure]
        public bool ContainsKey([NotNull] TKey key) => Dictionary.ContainsKey(key);

        /// <inheritdoc cref="ContainsItem(TItem,IEqualityComparer{TItem})"/>
        [Pure]
        public bool ContainsItem([NotNull] TItem item)
        {
            return ContainsItem(item, EqualityComparer<TItem>.Default);
        }

        /// <summary>O(1) Determines whether an item is in the collection.</summary>
        [Pure]
        public bool ContainsItem([NotNull] TItem item, [NotNull] IEqualityComparer<TItem> equalityComparer)
        {
            return TryGetValue(GetKeyForItem(item), out var existingItem) && equalityComparer.Equals(existingItem, item);
        }

        /// <summary>O(1) Returns the index of the item or -1 if it does not exist in the collection. The index is only guaranteed valid until the next insertion or removal.</summary>
        [Pure]
        public int IndexOf([NotNull] TItem item)
        {
            return Dictionary.TryGetValue(GetKeyForItem(item), out PositionedValue existingItem) ? existingItem.Position : -1;
        }

        /// <summary>O(1)</summary>
        /// <exception cref="KeyNotFoundException"></exception>
        [NotNull, Pure]
        public TItem Get([NotNull] TKey key) => Dictionary[key].Item;

        /// <summary>O(1)</summary>
        [NotNull, Pure]
        public TItem GetAt(int index) => Items[index];

        [NotNull, Pure]
        public IEnumerator<TItem> GetEnumerator() => Items.GetEnumerator();

        /// <summary>O(1) See <see cref="Dictionary{TKey,TValue}.TryGetValue"/></summary>
        public bool TryGetValue([NotNull] TKey key, out TItem value)
        {
            if (Dictionary.TryGetValue(key, out var positionValue))
            {
                value = positionValue.Item;
                return true;
            }

            value = default(TItem);
            return false;
        }

        #endregion


        #region ******************** REMOVAL ********************

        /// <summary>O(1)</summary>
        public virtual void Clear()
        {
            Dictionary.Clear();
            Items.Clear();
        }

        /// <summary>O(1)</summary>
        public bool RemoveKey([NotNull] TKey key)
        {
            //todo design code reuse
            if (Dictionary.TryGetValue(key, out PositionedValue existingItem))
            {
                RemoveAt(existingItem.Position);
                return true;
            }

            return false;
        }

        /// <summary>O(count)</summary>
        public bool RemoveKeys([NotNull] IEnumerable<TKey> collection)
        {
            throw new NotImplementedException();
        }

        /// <summary>O(1)</summary>
        public bool RemoveItem([NotNull] TItem item)
        {
            throw new NotImplementedException();
        }

        /// <summary>O(count)</summary>
//        public bool RemoveMultiple(IEnumerable<TItem> collection) { throw new NotImplementedException(); }
        public void ExceptWith([NotNull] IEnumerable<TItem> collection)
        {
            throw new NotImplementedException();
        }

        /// <summary>O(1)</summary>
        public virtual TItem RemoveAt(int index)
        {
            if (index >= Count) throw new ArgumentOutOfRangeException($"Cannot remove index {index}, it does not exist in a collection of size {Count}.");

            var itemToRemove = Items[index];
            var lastIndex = Count - 1;
            if (index != lastIndex)
            {
                Debug.WriteLine($"Removing item from the middle of a {nameof(KeyedIndexedSet<TKey, TItem>)}.");
                // Overwrite the index being deleted with the last item in the list
                var tailItem = Items[lastIndex];
                Items[index] = tailItem;
                Dictionary[GetKeyForItem(tailItem)] = new PositionedValue(tailItem, index);
            }
            Items.RemoveAt(lastIndex);
            Dictionary.Remove(GetKeyForItem(itemToRemove));

            return itemToRemove;
        }

        /// <summary>O(count)</summary>
        public void RemoveRange(int index, int count)
        {
            throw new NotImplementedException();
        }

        #endregion


        #region ******************** REPLACEMENT ********************

        /// <summary>O(1)</summary>
        public virtual void ReplaceAt(int index, [NotNull] TItem item)
        {
            Debug.WriteLine($"Replacing item within a {nameof(KeyedIndexedSet<TKey, TItem>)}.");
            //todo review best way to reuse code

            var existingItem = Items[index];
            Dictionary.Remove(GetKeyForItem(existingItem));
            Dictionary.Add(GetKeyForItem(item), new PositionedValue(item, index));
            Items[index] = item;
        }


        /*        /// <summary>O(1)</summary>
                public bool ReplaceItem([NotNull] TItem oldItem, [NotNull]TItem newItem) { throw new NotImplementedException(); }

                /// <summary>O(count)</summary>
                public bool ReplaceItems([NotNull]IEnumerable<TItem> collection) { throw new NotImplementedException(); }

                /// <summary>O(1)</summary>
                public void ReplaceAt(int index) { throw new NotImplementedException(); }

                /// <summary>O(count)</summary>
                public void ReplaceRange(int index,[NotNull] IEnumerable<TItem> collection) { throw new NotImplementedException(); }*/

        #endregion


        /// <summary>O(1)</summary>
        public void UpdateKey([NotNull] TItem item, [NotNull] Action<TItem> actionThatChangesKey)
        {
            throw new NotImplementedException();
        }





        //////                /// <summary>O(count)</summary>
        //////		public List<TItem> GetRange(int index, int count)  {throw new NotImplementedException();}
        //////////                /// <summary>O(n-index)</summary>
        //////////		public int IndexOf(TItem item, int index)  {throw new NotImplementedException();}
        //////////                /// <summary>O(count)</summary>
        //////////		public int IndexOf(TItem item, int index, int count)  {throw new NotImplementedException();}
    }
}
