using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Utilities.Collections
{
    public abstract class ObservabledKeyedCollection<TKey, TValue>
        : KeyedCollection<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        /// <inheritdoc />
        protected ObservabledKeyedCollection() {}

        /// <inheritdoc />
        protected ObservabledKeyedCollection(IEqualityComparer<TKey> comparer) : base(comparer) {}

        /// <inheritdoc />
        protected ObservabledKeyedCollection(IEqualityComparer<TKey> comparer, int dictionaryCreationThreshold)
            : base(comparer, dictionaryCreationThreshold) {}

        public bool TryGetValue(TKey key, out TValue value)
        {
            return Dictionary.TryGetValue(key, out value);
        }

        /// <inheritdoc />
        protected override void ClearItems()
        {
            CheckReentrancy();
            base.ClearItems();

            OnCollectionCleared();
        }

        /// <inheritdoc />
        protected override void InsertItem(int index, TValue item)
        {
            CheckReentrancy();
            base.InsertItem(index, item);

            OnCollectionAdded(index, item);
        }

        /// <inheritdoc />
        protected override void RemoveItem(int index)
        {
            CheckReentrancy();
            var item = this[index];
            base.RemoveItem(index);

            OnCollectionRemoved(index, item);
        }

        /// <inheritdoc />
        protected override void SetItem(int index, TValue item)
        {
            CheckReentrancy();
            var oldItem = this[index];
            base.SetItem(index, item);

            OnCollectionReplaced(index, item, oldItem);
        }

        private void OnCollectionCleared()
        {
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(Constants.IndexerName);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void OnCollectionAdded(int index, TValue item)
        {
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(Constants.IndexerName);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        private void OnCollectionRemoved(int index, TValue item)
        {
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(Constants.IndexerName);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        private void OnCollectionReplaced(int index, TValue newItem, TValue oldItem)
        {
            OnPropertyChanged(Constants.IndexerName);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, oldItem, index));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>Occurs when the collection changes, either by adding or removing an item. </summary>
        /// <seealso cref="INotifyCollectionChanged" />
        public event NotifyCollectionChangedEventHandler CollectionChanged;

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
    }
}
