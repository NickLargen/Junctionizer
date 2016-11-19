using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

using JetBrains.Annotations;

namespace Utilities.Collections
{
    /// <summary>This class is designed for data binding to a sorted view with algorithmically efficient insertion, retrieval, and deletion of non-null items in an unordered set that have a one-to-one correspondence with a set of non-null keys (eg any item that contains its unique identifier). Thread safety is achieved by brute force dispatching all write operations onto the same thread. The (unverified) expectation is that this does not incur an excessive overhead because collection change events fired outside of the UI thread would be marshalled back onto it anyway. Furthermore, firing collection change events from outside the UI thread (enabled by <see cref="BindingOperations.EnableCollectionSynchronization(IEnumerable,object)"/> causes ListCollectionView to bind to an internal ArrayList that it modifies using <see cref="CollectionChangeEventArgs"/> which runs counter to the goals of using a dictionary.</summary>
    /// <typeparam name="TKey">A value that can be easily calculated for any item.</typeparam>
    /// <typeparam name="TItem">The type of item in the collection.</typeparam>
    public class AsyncObservableKeyedSet<[NotNull] TKey, [NotNull] TItem>
        : IReadOnlyCollection<TItem>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        /// <summary>Runs all write operations on the <paramref name="context"/>. If context is not provided or it is null, SynchronizationContext.Current at the time of creation is used as the default value. If that is null, all further operations will either run on the calling thread or be dispatched to the thread pool and the default task scheduler, removing any thread safety guarantees.</summary>
        public AsyncObservableKeyedSet(Func<TItem, TKey> getKeyForItem,
            SynchronizationContext context = null,
            IReadOnlyList<TItem> initialItems = null,
            IEqualityComparer<TKey> comparer = null) : this(getKeyForItem, initialItems, comparer, context, null) {}

        /// <summary>Instead of providing a synchronization context that will be compared against for synchronous operation, a function can be provided that will cause write operations to execute in the current context it evaluates to true. This is especially useful for WPF because <see cref="DispatcherSynchronizationContext.CreateCopy"/> means logically equivalent contexts fail referential equality checks.</summary>
        public AsyncObservableKeyedSet(
            Func<TItem, TKey> getKeyForItem,
            Func<bool> isDesiredThread,
            IReadOnlyList<TItem> initialItems = null,
            IEqualityComparer<TKey> comparer = null) : this(getKeyForItem, initialItems, comparer, null, isDesiredThread) {}

        private AsyncObservableKeyedSet(
            Func<TItem, TKey> getKeyForItem,
            IReadOnlyList<TItem> initialItems,
            IEqualityComparer<TKey> comparer,
            SynchronizationContext context,
            Func<bool> isDesiredThread)
        {
            GetKeyForItem = getKeyForItem;
            initialItems = initialItems ?? Array.Empty<TItem>();
            KeyEqualityComparer = comparer ?? EqualityComparer<TKey>.Default;
            IsDesiredThread = isDesiredThread ?? DefaultThreadCheck;

            var startingSyncContext = SynchronizationContext.Current;
            TargetSyncContext = context ?? startingSyncContext;
            TaskScheduler taskScheduler;
            if (TargetSyncContext == null)
            {
                taskScheduler = TaskScheduler.Default;
            }
            else
            {
                if (startingSyncContext != TargetSyncContext) SynchronizationContext.SetSynchronizationContext(TargetSyncContext);

                taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

                if (startingSyncContext != TargetSyncContext) SynchronizationContext.SetSynchronizationContext(startingSyncContext);
            }
            TaskFactory = new TaskFactory(CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskContinuationOptions.None,
                taskScheduler);

            BackingDictionary = new Dictionary<TKey, TItem>(initialItems.Count, KeyEqualityComparer);

            initialItems.ForEach(item => InternalAdd(GetKeyForItem(item), item));
        }

        /// <summary>Number of items in the collection.</summary>
        public int Count => BackingDictionary.Count;

        /// <summary>A one-to-one function from <typeparamref name="TItem"/>-><typeparamref name="TKey"/> <see cref="http://en.wikipedia.org/wiki/Injective_function"/></summary>
        public Func<TItem, TKey> GetKeyForItem { get; }

        /// <summary>Determines whether to directly execute actions or to schedule to them to run on a different thread.</summary>
        public Func<bool> IsDesiredThread { get; }

        public bool IsReadOnly => false;

        /// <summary>The synchronization context that was provided in the constructor, otherwise <see cref="SynchronizationContext.Current"/> when this item was created.</summary>
        public SynchronizationContext TargetSyncContext { get; }

        /// <summary>Determines the equality of two keys.</summary>
        protected IEqualityComparer<TKey> KeyEqualityComparer { get; }

        /// <summary>The actual data structure that operations are delegated to.</summary>
        protected Dictionary<TKey, TItem> BackingDictionary { get; }

        /// <summary>Contains the desired settings for pushing work onto the primary thread.</summary>
        protected TaskFactory TaskFactory { get; }

        /// <summary>Default thread calculation if one is not provided in the constructor.</summary>
        protected virtual bool DefaultThreadCheck() => SynchronizationContext.Current == TargetSyncContext;

        /// <summary>Returns false if the item's key is already present (even if it's for a different item - your GetKeyForItem function should be a bijection). Otherwise returns true after item is added to the collection.</summary>
        public Task<bool> TryAddAsync([NotNull] TItem item)
        {
            return RunOnSynchronizationContext(() => {
                var key = GetKeyForItem(item);
                if (!BackingDictionary.ContainsKey(key))
                {
                    InternalAdd(key, item);
                    return true;
                }

                return false;
            });
        }

        public Task AddAsync([NotNull] TItem item)
        {
            return RunOnSynchronizationContext(() => {
                InternalAdd(GetKeyForItem(item), item);
            });
        }

        /// <summary>Warning: Performs no input validation or reentrancy checks.</summary>
        protected virtual void InternalAdd([NotNull] TKey key, [NotNull] TItem item)
        {
            BackingDictionary.Add(key, item);
            OnCollectionAdded(item);
        }

        public Task AddAllAsync([NotNull, ItemNotNull] IEnumerable<TItem> enumerable)
        {
            return RunOnSynchronizationContext(() => {
                InternalAddAll(enumerable);
            });
        }

        protected virtual void InternalAddAll([NotNull, ItemNotNull] IEnumerable<TItem> enumerable)
        {
            var list = enumerable.ToList();
            foreach (var item in list) BackingDictionary.Add(GetKeyForItem(item), item);

            OnCollectionAdded(list);
        }

        /// <summary>
        ///     <see cref="Dictionary{TKey,TValue}.Clear"/>
        /// </summary>
        public Task ClearAsync()
        {
            return RunOnSynchronizationContext(() => {
                BackingDictionary.Clear();
                OnCollectionReset();
            });
        }

        /// <summary>
        ///     <see cref="Dictionary{TKey,TValue}.Remove(TKey)"/>
        /// </summary>
        public Task<bool> RemoveKeyAsync([NotNull] TKey key)
        {
            return RunOnSynchronizationContext(() => {
                if (TryGetValue(key, out var item))
                {
                    BackingDictionary.Remove(key);
                    OnCollectionRemoved(item);
                    return true;
                }

                return false;
            });
        }


        public Task RemoveAllAsync([NotNull, ItemNotNull] IEnumerable<TItem> enumerable)
        {
            return RunOnSynchronizationContext(() => {
                InternalRemoveAll(enumerable);
            });
        }

        private void InternalRemoveAll(IEnumerable<TItem> enumerable)
        {
            var removedItems = enumerable.Where(item => BackingDictionary.Remove(GetKeyForItem(item))).ToList();

            OnCollectionRemoved(removedItems);
        }

        /// <summary>Under normal circumstances if an item's key changes while it is in the collection it will no longer be accessible. This method provides a way to maintain consistent internal state. No CollectionChanged events are raised (since this method does not alter the collection itself).</summary>
        public Task UpdateKeyAsync([NotNull] TItem itemBeingModified, [NotNull] Action actionThatModifiesKey)
        {
            return RunOnSynchronizationContext(() => {
                BackingDictionary.Remove(GetKeyForItem(itemBeingModified));
                actionThatModifiesKey();
                BackingDictionary.Add(GetKeyForItem(itemBeingModified), itemBeingModified);
            });
        }

        public TItem this[[NotNull] TKey key]
        {
            get { return BackingDictionary[key]; }
            /* set {
 //                BackingDictionary[key] = value
             }*/
        }

        /// <summary>
        ///     <see cref="Dictionary{TKey,TValue}.TryGetValue"/>
        /// </summary>
        public bool TryGetValue([NotNull] TKey key, out TItem item) => BackingDictionary.TryGetValue(key, out item);

        /// <summary>
        ///     <see cref="Dictionary{TKey,TValue}.ContainsKey"/>
        /// </summary>
        public bool ContainsKey([NotNull] TKey key) => BackingDictionary.ContainsKey(key);

        /// <summary>
        ///     <see cref="Dictionary{TKey,TValue}.ValueCollection.GetEnumerator"/>
        /// </summary>
        public IEnumerator<TItem> GetEnumerator() => BackingDictionary.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private Task RunOnSynchronizationContext([NotNull] Action action)
        {
            if (IsDesiredThread())
            {
                action();
                return Task.CompletedTask;
            }

            return TaskFactory.StartNew(action);
        }

        private Task<T> RunOnSynchronizationContext<T>([NotNull] Func<T> function)
        {
            return IsDesiredThread() ? Task.FromResult(function()) : TaskFactory.StartNew(function);
        }
        
        #region ===== Notification =====

        private void OnCollectionReset()
        {
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(Constants.IndexerName);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void OnCollectionAdded(TItem item)
        {
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(Constants.IndexerName);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        private void OnCollectionAdded(List<TItem> list)
        {
            if (list.Any())
            {
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(Constants.IndexerName);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, list));
            }
        }

        private void OnCollectionRemoved(TItem item)
        {
            OnPropertyChanged(nameof(Count));
            OnPropertyChanged(Constants.IndexerName);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
        }

        private void OnCollectionRemoved(List<TItem> list)
        {
            if (list.Any())
            {
                OnPropertyChanged(nameof(Count));
                OnPropertyChanged(Constants.IndexerName);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, list));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>Occurs when the collection changes, either by adding or removing an item. </summary>
        /// <seealso cref="INotifyCollectionChanged"/>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs param)
        {
            CollectionChanged?.Invoke(this, param);
        }

#if FALSE //Exceptions throw during CheckReentrancy are discarded during async methods
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
                if (CollectionChanged != null && CollectionChanged.GetInvocationList().Length > 1) throw new InvalidOperationException($"{nameof(AsyncObservableKeyedSet<TKey, TItem>)} reentrancy not allowed.");
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
#endif

        #endregion
    }
}
