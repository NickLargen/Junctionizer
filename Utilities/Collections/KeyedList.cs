/*
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Utilities.Collections
{

    // ReSharper disable UnusedMember.Global
    // ReSharper disable UnusedParameter.Global
    public class KeyedList<TKey, TItem> /* : IList<TItem>, IReadOnlyList<TItem>, IList #1#
        /*,IDictionary<TKey,TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue>#1#
    {
        public int Count => Items.Count;
        public bool IsReadOnly => false;

        public TItem this[TKey key]
        {
            get { {throw new NotImplementedException();} }
        }
        public int Capacity
        {
            get { return Items.Capacity; }
            set { Items.Capacity = value; }
        }

        public Dictionary<TKey, TItem>.KeyCollection Keys => Dictionary.Keys;
        public Dictionary<TKey, TItem>.ValueCollection Values => Dictionary.Values;
        public IEqualityComparer<TKey> Comparer { get; }

        protected Func<TItem, TKey> GetKeyForItem { get; }
        protected Dictionary<TKey, TItem> Dictionary { get; }
        protected List<TItem> Items { get; }

        public KeyedList(Func<TItem, TKey> getKeyForItem, int capacity = 0, IEqualityComparer<TKey> comparer = null)
        {
            Comparer = comparer ?? EqualityComparer<TKey>.Default;
            Dictionary = new Dictionary<TKey, TItem>(capacity, Comparer);
            Items = new List<TItem>(capacity);
            GetKeyForItem = getKeyForItem;

            var a = new Set
        }

        // Read(Contains, IndexOf, Get, []) Write(Add, Insert[Replace], Remove, []=)



        public void Add(TItem item)  {throw new NotImplementedException();}
        public void AddRange(IEnumerable<TItem> items)  {throw new NotImplementedException();}
        public void Clear()  {throw new NotImplementedException();}
        public bool ContainsKey(TKey key) {throw new NotImplementedException();} 
        public bool ContainsItem(TItem item)  {throw new NotImplementedException();}
        public int IndexOf(TItem item)  {throw new NotImplementedException();}
        public int IndexOf(TItem item, int index)  {throw new NotImplementedException();}
        public int IndexOf(TItem item, int index, int count)  {throw new NotImplementedException();}
        public List<TItem> GetRange(int index, int count)  {throw new NotImplementedException();}
        public TItem Get(TKey key)  {throw new NotImplementedException();}
        public TItem GetAt(int index)  {throw new NotImplementedException();}
        public void Insert(int index, TItem item)  {throw new NotImplementedException();}
        public void InsertRange(int index, IEnumerable<TItem> collection)  {throw new NotImplementedException();}
        public bool RemoveKey(TKey key)  {throw new NotImplementedException();}
        public bool RemoveItem(TItem item)  {throw new NotImplementedException();}
        public bool RemoveItems(IEnumerable<TItem> collection) { throw new NotImplementedException(); }
        public void RemoveAt(int index)  {throw new NotImplementedException();}
        public void RemoveRange(int index, int count)  {throw new NotImplementedException();}
        public bool TryGetValue(TKey key, out TItem value)  {throw new NotImplementedException();}

        public void UpdateKey(TItem item, Action<TItem> actionThatChangesKey)  {throw new NotImplementedException();}
        public ()  {throw new NotImplementedException();}
        public ()  {throw new NotImplementedException();}
        public ()  {throw new NotImplementedException();}
        public ()  {throw new NotImplementedException();}
    }
}
*/
