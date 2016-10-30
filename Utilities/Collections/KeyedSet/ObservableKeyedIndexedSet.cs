using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

using JetBrains.Annotations;

namespace Utilities.Collections.KeyedSet
{
    public class ObservableKeyedIndexedSet<TKey, TItem> : KeyedIndexedSet<TKey, TItem>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        /// <inheritdoc/>
        public ObservableKeyedIndexedSet([NotNull] Func<TItem, TKey> getKeyForItem, int capacity = 0,
            IEqualityComparer<TKey> comparer = null) : base(getKeyForItem, capacity, comparer) {}


        #region Overrides of KeyedIndexedSet<TKey,TItem>

        /// <inheritdoc/>
        protected override bool InternalInsert(int index, TItem item, bool throwIfDuplicateKey = false)
        {
            CheckReentrancy();

            if (base.InternalInsert(index, item, throwIfDuplicateKey))
            {
                OnCollectionAdded(index, item);
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public override void ReplaceAt(int index, TItem item)
        {
            CheckReentrancy();

            var existingItem = GetAt(index);
            base.ReplaceAt(index, item);
            OnCollectionReplaced(index, item, existingItem);
        }

        /// <inheritdoc/>
        protected override TItem InternalRemoveAt(int index)
        {
            CheckReentrancy();

            var removedItem = base.InternalRemoveAt(index);
            OnCollectionRemoved(index, removedItem);
            return removedItem;
        }

        /// <inheritdoc/>
        public override void Clear()
        {
            CheckReentrancy();

            base.Clear();
            OnCollectionCleared();
        }

        #endregion


        private void OnCollectionCleared()
        {
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(Constants.IndexerName);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void OnCollectionAdded(int index, TItem item)
        {
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(Constants.IndexerName);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        private void OnCollectionRemoved(int index, TItem item)
        {
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(Constants.IndexerName);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        private void OnCollectionReplaced(int index, TItem newItem, TItem oldItem)
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
        /// <seealso cref="INotifyCollectionChanged"/>
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

        /// <summary>Disallow reentrant attempts to change this dictionary. E.g. an event handler of the CollectionChanged event is not allowed to make changes to this collection.</summary>
        private IDisposable BlockReentrancy()
        {
            _monitor.Enter();
            return _monitor;
        }

        /// <summary> Check and assert for reentrant attempts to change this collection. </summary>
        /// <exception cref="InvalidOperationException">raised when changing the collection while another collection change is still being notified to other listeners</exception>
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
