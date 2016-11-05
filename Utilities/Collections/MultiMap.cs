using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Utilities.Collections
{
    public class MultiMap<TKey, TValue> : IDictionary<TKey, LinkedList<TValue>>, IDictionary<TKey, TValue>
    {
        #region Constructors

        public MultiMap()
        {
            BackingDictionary = new Dictionary<TKey, LinkedList<TValue>>();
        }

        public MultiMap(int capacity)
        {
            BackingDictionary = new Dictionary<TKey, LinkedList<TValue>>(capacity);
        }

        public MultiMap(IEqualityComparer<TKey> comparer)
        {
            BackingDictionary = new Dictionary<TKey, LinkedList<TValue>>(comparer);
        }

        public MultiMap(int capacity, IEqualityComparer<TKey> comparer)
        {
            BackingDictionary = new Dictionary<TKey, LinkedList<TValue>>(capacity, comparer);
        }

        public MultiMap(IDictionary<TKey, LinkedList<TValue>> dictionary)
        {
            BackingDictionary = new Dictionary<TKey, LinkedList<TValue>>(dictionary);
        }

        public MultiMap(IDictionary<TKey, LinkedList<TValue>> dictionary, IEqualityComparer<TKey> comparer)
        {
            BackingDictionary = new Dictionary<TKey, LinkedList<TValue>>(dictionary, comparer);
        }

        #endregion


        private IDictionary<TKey, LinkedList<TValue>> BackingDictionary { get; }

        public void Add(TKey key, TValue value)
        {
            if (BackingDictionary.TryGetValue(key, out var existingLinkedList))
            {
                existingLinkedList.AddFirst(value);
            }
            else
            {
                var newLinkedList = new LinkedList<TValue>();
                newLinkedList.AddFirst(value);
                BackingDictionary.Add(key, newLinkedList);
            }
        }

        public bool Contains(TKey key, TValue value)
        {
            return BackingDictionary.TryGetValue(key, out var existingList) && existingList.Contains(value);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (KeyValuePair<TKey, LinkedList<TValue>> pair in BackingDictionary)
            {
                var key = pair.Key;
                foreach (TValue value in pair.Value)
                {
                    yield return new KeyValuePair<TKey, TValue>(key, value);
                }
            }
        }

        public bool Remove(TKey key, TValue value)
        {
            return BackingDictionary.TryGetValue(key, out var existingList) && existingList.Remove(value);
        }

        bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
        {
            if (BackingDictionary.TryGetValue(key, out var existingList) && existingList.Count > 0)
            {
                value = existingList.First.Value;
                return true;
            }

            value = default(TValue);
            return false;
        }

        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get {
                if (((IDictionary<TKey, TValue>) this).TryGetValue(key, out var value))
                {
                    return value;
                }

                throw new KeyNotFoundException();
            }
            set { Add(key, value); }
        }

        public ICollection<TValue> Values => new ReadOnlyCollection<TValue>(BackingDictionary.Values.SelectMany(list => list).ToList());
        
        
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return Contains(item.Key, item.Value);
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key, item.Value);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #region Pass through calls to the backing dictionary

        public int Count => BackingDictionary.Count;

        public bool IsReadOnly => BackingDictionary.IsReadOnly;

        public LinkedList<TValue> this[TKey key]
        {
            get { return BackingDictionary[key]; }
            set { BackingDictionary[key] = value; }
        }

        public ICollection<TKey> Keys => BackingDictionary.Keys;

        ICollection<LinkedList<TValue>> IDictionary<TKey, LinkedList<TValue>>.Values => BackingDictionary.Values;

        void ICollection<KeyValuePair<TKey, LinkedList<TValue>>>.Add(KeyValuePair<TKey, LinkedList<TValue>> item)
        {
            BackingDictionary.Add(item);
        }

        void IDictionary<TKey, LinkedList<TValue>>.Add(TKey key, LinkedList<TValue> value)
        {
            BackingDictionary.Add(key, value);
        }

        public void Clear()
        {
            BackingDictionary.Clear();
        }

        bool ICollection<KeyValuePair<TKey, LinkedList<TValue>>>.Contains(KeyValuePair<TKey, LinkedList<TValue>> item)
        {
            return BackingDictionary.Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return BackingDictionary.ContainsKey(key);
        }

        void ICollection<KeyValuePair<TKey, LinkedList<TValue>>>.CopyTo(KeyValuePair<TKey, LinkedList<TValue>>[] array, int arrayIndex)
        {
            BackingDictionary.CopyTo(array, arrayIndex);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        IEnumerator<KeyValuePair<TKey, LinkedList<TValue>>> IEnumerable<KeyValuePair<TKey, LinkedList<TValue>>>.GetEnumerator()
        {
            return BackingDictionary.GetEnumerator();
        }

        bool ICollection<KeyValuePair<TKey, LinkedList<TValue>>>.Remove(KeyValuePair<TKey, LinkedList<TValue>> item)
        {
            return BackingDictionary.Remove(item);
        }

        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            return BackingDictionary.Remove(key);
        }

        bool IDictionary<TKey, LinkedList<TValue>>.Remove(TKey key)
        {
            return BackingDictionary.Remove(key);
        }

        public bool TryGetValue(TKey key, out LinkedList<TValue> value)
        {
            return BackingDictionary.TryGetValue(key, out value);
        }

        #endregion
    }
}
