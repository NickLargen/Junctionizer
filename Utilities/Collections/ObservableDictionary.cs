using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Utilities.Collections
{
    public class ObservableDictionary<TKey, TValue>
        : IDictionary<TKey, TValue>, IDictionary, IReadOnlyDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        private Dictionary<TKey, TValue> MyDictionary { get; }

        public ObservableDictionary()
        {
            MyDictionary = new Dictionary<TKey, TValue>();
        }

        public ObservableDictionary(IEqualityComparer<TKey> comparer)
        {
            MyDictionary = new Dictionary<TKey, TValue>(comparer);
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
        void IDictionary.Add(object key, object value) => Add((TKey) key, (TValue) value);

        public void Add(TKey key, TValue value)
        {
            CheckReentrancy();
            MyDictionary.Add(key, value);

            OnCollectionAdded(new KeyValuePair<TKey, TValue>(key, value));
        }

        private void OnCollectionAdded(KeyValuePair<TKey, TValue> item)
        {
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(Constants.IndexerName);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }


        public void Clear()
        {
            CheckReentrancy();
            MyDictionary.Clear();

            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(Constants.IndexerName);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }


        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
            => ((ICollection<KeyValuePair<TKey, TValue>>) MyDictionary).Contains(item);

        bool IDictionary.Contains(object key) => ((IDictionary) MyDictionary).Contains(key);
        public bool ContainsKey(TKey key) => MyDictionary.ContainsKey(key);


        void ICollection.CopyTo(Array array, int index) => ((ICollection) MyDictionary).CopyTo(array, index);

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
            => ((ICollection<KeyValuePair<TKey, TValue>>) MyDictionary).CopyTo(array, arrayIndex);


        public int Count => MyDictionary.Count;


        IDictionaryEnumerator IDictionary.GetEnumerator() => ((IDictionary) MyDictionary).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable) MyDictionary).GetEnumerator();
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => MyDictionary.GetEnumerator();


        bool IDictionary.IsFixedSize => ((IDictionary) MyDictionary).IsFixedSize;


        bool IDictionary.IsReadOnly => ((IDictionary) MyDictionary).IsReadOnly;
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => ((ICollection<KeyValuePair<TKey, TValue>>) MyDictionary).IsReadOnly;


        bool ICollection.IsSynchronized => ((ICollection) MyDictionary).IsSynchronized;


        object IDictionary.this[object key]
        {
            get { return this[(TKey) key]; }
            set { this[(TKey) key] = (TValue) value; }
        }
        public TValue this[TKey key]
        {
            get { return MyDictionary[key]; }
            set {
                if (TryGetValue(key, out var existingValue))
                {
                    if (ReferenceEquals(existingValue, value)) return;

                    MyDictionary[key] = value;
                    OnPropertyChanged(Constants.IndexerName);
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                        new KeyValuePair<TKey, TValue>(key, value), new KeyValuePair<TKey, TValue>(key, existingValue)));
                }
                else
                {
                    MyDictionary[key] = value;
                    OnCollectionAdded(new KeyValuePair<TKey, TValue>(key, value));
                }
            }
        }


        ICollection IDictionary.Keys => ((IDictionary) MyDictionary).Keys;
        public ICollection<TKey> Keys => MyDictionary.Keys;
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => ((IReadOnlyDictionary<TKey, TValue>) MyDictionary).Keys;


        void IDictionary.Remove(object key) => Remove((TKey) key);

        public bool Remove(TKey key)
        {
            if (MyDictionary.TryGetValue(key, out var value))
            {
                MyDictionary.Remove(key);
                NotifySingleItemRemoved(new KeyValuePair<TKey, TValue>(key, value));
                return true;
            }

            return false;
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            if (((ICollection<KeyValuePair<TKey, TValue>>) MyDictionary).Remove(item))
            {
                NotifySingleItemRemoved(item);
                return true;
            }

            return false;
        }

        private void NotifySingleItemRemoved(KeyValuePair<TKey, TValue> item)
        {
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(Constants.IndexerName);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
        }


        object ICollection.SyncRoot => ((ICollection) MyDictionary).SyncRoot;


        public bool TryGetValue(TKey key, out TValue value) => MyDictionary.TryGetValue(key, out value);


        ICollection IDictionary.Values => ((IDictionary) MyDictionary).Values;
        public ICollection<TValue> Values => MyDictionary.Values;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => MyDictionary.Values;

        #region Details from Observable
        /// <summary>
        ///     Occurs when the collection changes, either by adding or removing an item.
        /// </summary>
        /// <remarks>
        ///     see <seealso cref="INotifyCollectionChanged" />
        /// </remarks>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                using (BlockReentrancy())
                {
                    CollectionChanged?.Invoke(this, e);
                }
            }
        }

        /// <summary>
        ///     Disallow reentrant attempts to change this dictionary. E.g. an event handler
        ///     of the CollectionChanged event is not allowed to make changes to this collection.
        /// </summary>
        /// <remarks>
        ///     typical usage is to wrap e.g. a OnCollectionChanged call with a using() scope:
        ///     <code>
        ///         using (BlockReentrancy())
        ///         {
        ///             CollectionChanged(this, new NotifyCollectionChangedEventArgs(action, item, index));
        ///         }
        /// </code>
        /// </remarks>
        private IDisposable BlockReentrancy()
        {
            _monitor.Enter();
            return _monitor;
        }

        /// <summary> Check and assert for reentrant attempts to change this collection. </summary>
        /// <exception cref="InvalidOperationException">
        ///     raised when changing the collection
        ///     while another collection change is still being notified to other listeners
        /// </exception>
        private void CheckReentrancy()
        {
            if (_monitor.Busy)
            {
                // we can allow changes if there's only one listener - the problem
                // only arises if reentrant changes make the original event args
                // invalid for later listeners.  This keeps existing code working
                // (e.g. Selector.SelectedItems).
                if (CollectionChanged != null && CollectionChanged.GetInvocationList().Length > 1) throw new InvalidOperationException("Observable dictionary reentrancy not allowed.");
            }
        }


        private class SimpleMonitor : IDisposable
        {
            private int _busyCount;

            public bool Busy => _busyCount > 0;

            public void Dispose()
            {
                --_busyCount;
            }

            public void Enter()
            {
                ++_busyCount;
            }
        }

        private readonly SimpleMonitor _monitor = new SimpleMonitor();
        #endregion
    }
}
