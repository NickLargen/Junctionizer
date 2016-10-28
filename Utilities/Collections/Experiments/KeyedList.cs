using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Utilities.Collections.Experiments
{
    /// <summary>
    ///     <see cref="KeyedCollection{TKey,TItem}"/>
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [DebuggerDisplay(nameof(Count) + " = {" + nameof(Count) + "}")]
    public abstract class KeyedList<TKey, TValue> : IList<TValue> /*, IList*/, IReadOnlyList<TValue>
    {
        protected abstract TKey GetKeyForItem(TValue item);

        private const int DEFAULT_THRESHOLD = 0;
        private const string DUPLICATE_KEY = "An item with the same key has already been added.";

        private readonly IEqualityComparer<TKey> _comparer;
        private readonly int _threshold;
        private IDictionary<TKey, TValue> _dict;
        private int _keyCount;
        private readonly IList<TValue> _list;

        protected KeyedList(IEqualityComparer<TKey> comparer = null, int dictionaryCreationThreshold = DEFAULT_THRESHOLD)
        {
            if (comparer == null)
            {
                comparer = EqualityComparer<TKey>.Default;
            }

            if (dictionaryCreationThreshold == -1)
            {
                dictionaryCreationThreshold = int.MaxValue;
            }

            if (dictionaryCreationThreshold < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(dictionaryCreationThreshold));
            }

            _list = new List<TValue>();
            _comparer = comparer;
            _threshold = dictionaryCreationThreshold;
        }

        public IEqualityComparer<TKey> Comparer => _comparer;

        public int Count => _list.Count;
        public bool IsReadOnly => false;

        protected IList<TValue> List => _list;
        protected IDictionary<TKey, TValue> Dictionary => _dict;

        private void CreateDictionary()
        {
            _dict = new Dictionary<TKey, TValue>(_comparer);
            foreach (TValue item in this)
            {
                TKey key = GetKeyForItem(item);
                if (key != null)
                {
                    _dict.Add(key, item);
                }
            }
        }

        public TValue this[TKey key]
        {
            get {
                if (key == null)
                {
                    throw new ArgumentNullException(nameof(key));
                }

                if (_dict != null)
                {
                    return _dict[key];
                }

                foreach (TValue value in this)
                {
                    if (_comparer.Equals(GetKeyForItem(value), key)) return value;
                }

                throw new KeyNotFoundException();
            }
        }

        /// <inheritdoc/>
        public TValue this[int index]
        {
            get { return _list[index]; }
            set { SetItem(index, value); }
        }

        private void SetItem(int index, TValue item)
        {
            TKey newKey = GetKeyForItem(item);
            TKey oldKey = GetKeyForItem(_list[index]);

            if (_comparer.Equals(oldKey, newKey))
            {
                if (newKey != null && _dict != null)
                {
                    _dict[newKey] = item;
                }
            }
            else
            {
                if (newKey != null)
                {
                    AddKey(newKey, item);
                }

                if (oldKey != null)
                {
                    RemoveKey(oldKey);
                }
            }
            _list[index] = item;
        }


        #region Addition / Replacement

        public void Add(TValue value)
        {
            // this does not work
            Insert(Count, value);
        }

        public void Insert(int index, TValue item)
        {
            TKey key = GetKeyForItem(item);
            if (key != null)
            {
                AddKey(key, item);
            }
            _list.Insert(index, item);
        }

        /// <summary> Add an entry to the dictionary (but not the list). </summary>
        private void AddKey(TKey key, TValue item)
        {
            if (_dict != null)
            {
                _dict.Add(key, item);
            }
            else if (_keyCount == _threshold)
            {
                CreateDictionary();
                _dict.Add(key, item);
            }
            else
            {
                if (ContainsKey(key))
                {
                    throw new ArgumentException(DUPLICATE_KEY);
                }

                _keyCount++;
            }
        }

        #endregion


        private void ChangeItemKey(TValue item, TKey newKey)
        {
            // check if the item exists in the collection
            if (!ContainsValue(item))
            {
                throw new ArgumentException("Item doesn't exist");
            }

            TKey oldKey = GetKeyForItem(item);
            if (!_comparer.Equals(oldKey, newKey))
            {
                if (newKey != null)
                {
                    AddKey(newKey, item);
                }

                if (oldKey != null)
                {
                    RemoveKey(oldKey);
                }
            }
        }

        public void Clear()
        {
            _list.Clear();
            _dict?.Clear();

            _keyCount = 0;
        }


        #region Contains

        /// <inheritdoc/>
        public bool Contains(TValue value)
        {
            return ContainsValue(value);
        }

        /// <inheritdoc/>
        public bool ContainsKey(TKey key)
        {
            return _dict?.ContainsKey(key) ?? this.Any(value => _comparer.Equals(GetKeyForItem(value), key));
        }

        /// <inheritdoc/>
        public bool ContainsValue(TValue value)
        {
            TKey key;
            if (_dict == null || (key = GetKeyForItem(value)) == null)
            {
                return _list.Contains(value);
            }

            return _dict.TryGetValue(key, out TValue itemInDict) && EqualityComparer<TValue>.Default.Equals(itemInDict, value);
        }

        #endregion


        /// <inheritdoc/>
        void ICollection<TValue>.CopyTo(TValue[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public IEnumerator<TValue> GetEnumerator() => _list.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <inheritdoc/>
        public int IndexOf(TValue item) => _list.IndexOf(item);


        #region Removal

        /// <inheritdoc/>
        public bool Remove(TKey key)
        {
            // todo test code contracts
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (_dict != null)
            {
                return _dict.ContainsKey(key) && Remove(_dict[key]);
            }

            for (var i = 0; i < Count; i++)
            {
                if (_comparer.Equals(GetKeyForItem(_list[i]), key))
                {
                    RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public bool Remove(TValue value)
        {
            int index = IndexOf(value);
            if (index < 0) return false;

            RemoveAt(index);
            return true;
        }

        public void RemoveAt(int index)
        {
            TKey key = GetKeyForItem(_list[index]);
            if (key != null)
            {
                RemoveKey(key);
            }
            _list.RemoveAt(index);
        }

        /// <summary>Remove an entry from the dictionary (but not the list).</summary>
        private void RemoveKey(TKey key)
        {
            Debug.Assert(key != null, "key shouldn't be null!");
            if (_dict != null)
            {
                _dict.Remove(key);
            }
            else
            {
                _keyCount--;
            }
        }

        #endregion
    }
}
