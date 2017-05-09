using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Junctionizer.CustomWpfComponents;

using PropertyChanged;

using Utilities;

namespace Junctionizer.Model
{
    [ImplementPropertyChanged]
    public class GameFolderPairEnumerable : IEnumerable<GameFolderPair>, INotifyCollectionChanged
    {
        [NotNull]
        public FolderCollection SourceCollection { get; }
        [NotNull]
        public FolderCollection DestinationCollection { get; }

        private ObservableCollection<object> _selectedItems = new ObservableCollection<object>();
        [NotNull]
        public ObservableCollection<object> SelectedItems
        {
            get => _selectedItems;
            set {
                _selectedItems = value;

                SelectedFolderPairs = SelectedItems.Reverse()
                                                   .Cast<GameFolderPair>()
                                                   .Where(pair => pair.IsBeingAccessed == false);

                Observable.FromEventPattern(_selectedItems, nameof(_selectedItems.CollectionChanged))
                          .Throttle(TimeSpan.FromMilliseconds(1))
                          .Subscribe(pattern => CheckCanExecute());
            }
        }

        private void CheckCanExecute()
        {
            DeleteCommand.RaiseCanExecuteChanged();
            ArchiveCommand.RaiseCanExecuteChanged();
            RestoreCommand.RaiseCanExecuteChanged();
            MirrorCommand.RaiseCanExecuteChanged();
        }

        [CanBeNull]
        private PauseTokenSource PauseTokenSource { get; }

        [NotNull]
        public IEnumerable<GameFolderPair> SelectedFolderPairs { get; set; } = Enumerable.Empty<GameFolderPair>();

        /// <summary>Returns Enumerable.Empty if SourceCollection and DestinationCollection do not currently both have a location.</summary>
        [NotNull]
        private IEnumerable<GameFolderPair> SelectedFolderPairsIfInitialized => SourceCollection.BothCollectionsInitialized ? SelectedFolderPairs : Enumerable.Empty<GameFolderPair>();

        public Dictionary<string, GameFolderPair> Items { get; } = new Dictionary<string, GameFolderPair>();

        public GameFolderPairEnumerable(FolderCollection sourceCollection, FolderCollection destinationCollection, [CanBeNull] PauseTokenSource pauseTokenSource)
        {
            SourceCollection = sourceCollection;
            DestinationCollection = destinationCollection;

            PauseTokenSource = pauseTokenSource;

            AddExistingValues();

            SourceCollection.Folders.CollectionChanged += BackingCollectionChangedHandler(true);
            DestinationCollection.Folders.CollectionChanged += BackingCollectionChangedHandler(false);
        }

        private NotifyCollectionChangedEventHandler BackingCollectionChangedHandler(bool isFromSourceCollection)
        {
            return (sender, e) => {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Reset:
                        Items.Clear();
                        AddExistingValues();
                        OnCollectionChanged(e);
                        break;
                    case NotifyCollectionChangedAction.Add:
                        var addedItems = AddItems(e.NewItems.Cast<GameFolder>(), isFromSourceCollection);
                        if (addedItems.Any()) OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, changedItems: addedItems));
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        var removedItems = RemoveItems(e.OldItems.Cast<GameFolder>(), isFromSourceCollection);
                        if (removedItems.Any()) OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, changedItems: removedItems));
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        throw new NotSupportedException();
                    case NotifyCollectionChangedAction.Move:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                CheckCanExecute();
            };
        }

        private void AddExistingValues()
        {
            AddItems(SourceCollection.Folders, isFromSourceCollection: true);
            AddItems(DestinationCollection.Folders, isFromSourceCollection: false);
        }

        private List<GameFolderPair> AddItems(IEnumerable<GameFolder> folders, bool isFromSourceCollection)
        {
            var addedItems = new List<GameFolderPair>();
            foreach (var folder in folders.Where(folder => !folder.IsJunction
                                                           || string.Equals(Path.GetFileNameWithoutExtension(folder.JunctionTarget), folder.Name, StringComparison.Ordinal)))
            {
                if (Items.TryGetValue(folder.Name, out var existingItem))
                {
                    if (isFromSourceCollection) existingItem.SourceEntry = folder;
                    else existingItem.DestinationEntry = folder;
                }
                else
                {
                    var newItem = isFromSourceCollection ? new GameFolderPair(sourceEntry: folder) : new GameFolderPair(destinationEntry: folder);
                    Items.Add(folder.Name, newItem);
                    addedItems.Add(newItem);
                }
            }
    
            foreach (var pair in addedItems)
            {
                PropertyChangedEventManager.AddHandler(pair, (s, e) => CheckCanExecute(), nameof(pair.IsBeingAccessed));
            }

            return addedItems;
        }

        private List<GameFolderPair> RemoveItems(IEnumerable<GameFolder> folders, bool isFromSourceCollection)
        {
            var removedItems = new List<GameFolderPair>();
            foreach (var folder in folders)
            {
                if (Items.TryGetValue(folder.Name, out var folderPair))
                {
                    if ((isFromSourceCollection ? folderPair.DestinationEntry : folderPair.SourceEntry) != null)
                    {
                        if (isFromSourceCollection) folderPair.SourceEntry = null;
                        else folderPair.DestinationEntry = null;
                    }
                    else
                    {
                        removedItems.Add(folderPair);
                        Items.Remove(folder.Name);
                    }
                }
            }

            return removedItems;
        }


        public IEnumerator<GameFolderPair> GetEnumerator() => Items.Values.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);

        /// <inheritdoc/>
        public event NotifyCollectionChangedEventHandler CollectionChanged;


        /// <summary>Results in the folder in neither location.</summary>
        [AutoLazy.Lazy]
        public IDelegateListCommand DeleteCommand => new PausingDelegateListCommand<GameFolderPair>(
            () => SelectedFolderPairs,
            DeleteAsync, PauseTokenSource, itemsToDelete => {
                var count = itemsToDelete.Count;
                return Dialogs.RequestBooleanPromptAsync($"Are you sure you want to move {count} {"item".Pluralize(count)} to the Recycle Bin?");
            });

        private Task DeleteAsync(GameFolderPair pair)
        {
            var newTasks = new List<Task>();
            if (pair.SourceEntry?.IsJunction == true) SourceCollection.DeleteJunction(pair.SourceEntry);
            else if (pair.SourceEntry != null) newTasks.Add(SourceCollection.DeleteFolderOrJunctionAsync(pair.SourceEntry));

            if (pair.DestinationEntry != null) newTasks.Add(DestinationCollection.DeleteFolderOrJunctionAsync(pair.DestinationEntry));

            return Task.WhenAll(newTasks);
        }


        /// <summary>Results in the folder in destination with a junction pointing to it from source.</summary>
        [AutoLazy.Lazy]
        public IDelegateListCommand ArchiveCommand => new PausingDelegateListCommand<GameFolderPair>(
            () => SelectedFolderPairsIfInitialized.Where(pair => pair.SourceEntry?.IsJunction == false ||
                                                                 pair.SourceEntry == null && pair.DestinationEntry?.IsJunction == false),
            ArchiveAsync, PauseTokenSource);

        private async Task ArchiveAsync(GameFolderPair pair)
        {
            if (pair.SourceEntry != null) await SourceCollection.ArchiveAsync(pair.SourceEntry).ConfigureAwait(false);
            else if (pair.DestinationEntry?.IsJunction == false) SourceCollection.CreateJunctionTo(pair.DestinationEntry);
        }


        /// <summary>Results in folder in source location, not in destination.</summary>
        [AutoLazy.Lazy]
        public IDelegateListCommand RestoreCommand => new PausingDelegateListCommand<GameFolderPair>(
            () => SelectedFolderPairsIfInitialized.Where(pair => pair.DestinationEntry?.IsJunction == false),
            RestoreAsync, PauseTokenSource);

        private async Task RestoreAsync(GameFolderPair gameFolderPair)
        {
            Debug.Assert(gameFolderPair.DestinationEntry?.IsJunction == false);

            var createdFolder = await SourceCollection.CopyFolderAsync(gameFolderPair.DestinationEntry).ConfigureAwait(false);
            if (createdFolder != null) await DestinationCollection.DeleteFolderOrJunctionAsync(gameFolderPair.DestinationEntry).ConfigureAwait(false);
        }


        /// <summary>Results in the folder existing in both locations</summary>
        [AutoLazy.Lazy]
        public IDelegateListCommand MirrorCommand => new PausingDelegateListCommand<GameFolderPair>(
            () => SelectedFolderPairsIfInitialized.Where(pair => !(pair.SourceEntry?.IsJunction == false &&
                                                                   pair.DestinationEntry?.IsJunction == false)),
            MirrorAsync, PauseTokenSource);

        private async Task MirrorAsync(GameFolderPair gameFolderPair)
        {
            Debug.Assert(!(gameFolderPair.SourceEntry?.IsJunction == false && gameFolderPair.DestinationEntry?.IsJunction == false));

            if (gameFolderPair.DestinationEntry?.IsJunction == false)
            {
                await SourceCollection.CopyFolderAsync(gameFolderPair.DestinationEntry).ConfigureAwait(false);
            }
            else
            {
                Debug.Assert(gameFolderPair.SourceEntry != null);
                await DestinationCollection.CopyFolderAsync(gameFolderPair.SourceEntry).ConfigureAwait(false);
            }
        }
    }
}
