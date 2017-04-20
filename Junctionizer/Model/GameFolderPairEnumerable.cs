using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Prism.Commands;

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

        [NotNull]
        public IEnumerable<GameFolderPair> SelectedFolderPairs =>
            SelectedItems.Reverse()
                         .Cast<GameFolderPair>()
                         .Where(pair => pair.SourceEntry?.IsBeingDeleted != true && pair.DestinationEntry?.IsBeingDeleted != true);

        [NotNull]
        private Func<GameFolder, string> KeySelector { get; }

        public Dictionary<string, GameFolderPair> Items { get; } = new Dictionary<string, GameFolderPair>();

        public GameFolderPairEnumerable(FolderCollection sourceCollection, FolderCollection destinationCollection)
        {
            SourceCollection = sourceCollection;
            DestinationCollection = destinationCollection;

            KeySelector = SourceCollection.Folders.GetKeyForItem;

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
        public DelegateCommand DeleteCommand => new DelegateCommand(() => {
            DeleteSelected().Forget();
        }, () => SelectedFolderPairs.Any());

        private async Task DeleteSelected()
        {
            var sourceFolders = SelectedFolderPairs.Select(pair => pair.SourceEntry).Where(folder => folder != null).ToList();
            SourceCollection.DeleteJunctions(sourceFolders);
            await SourceCollection.DeleteFolders(sourceFolders);
            await DestinationCollection.DeleteFolders(SelectedFolderPairs
                .Select(pair => pair.DestinationEntry)
                .Where(folder => folder != null));
        }


        /// <summary>Results in the folder in destination with a junction pointing to it from source.</summary>
        [AutoLazy.Lazy]
        public DelegateCommand ArchiveCommand => new DelegateCommand(() => {
            var itemsWithSource = ArchivableItems().ToLookup(pair => pair.SourceEntry != null);
            SourceCollection.ArchiveFolders(itemsWithSource[true].Select(pair => pair.SourceEntry)).Forget();
            DestinationCollection.CreateJunctionsTo(itemsWithSource[false].Select(pair => pair.DestinationEntry));
        }, () => SourceCollection.BothCollectionsInitialized && ArchivableItems().Any());

        private IEnumerable<GameFolderPair> ArchivableItems()
            => SelectedFolderPairs.Where(pair => pair.SourceEntry?.IsJunction == false
                                                 || pair.SourceEntry == null && pair.DestinationEntry?.IsJunction == false);


        /// <summary>Results in folder in source location, not in destination.</summary>
        [AutoLazy.Lazy]
        public DelegateCommand RestoreCommand => new DelegateCommand(() => {
            Task.WhenAll(RestorableItems().Select(Restore)).Forget();
        }, () => SourceCollection.BothCollectionsInitialized && RestorableItems().Any());

        private IEnumerable<GameFolderPair> RestorableItems() => SelectedFolderPairs.Where(pair => pair.DestinationEntry?.IsJunction == false);

        private async Task Restore(GameFolderPair gameFolderPair)
        {
            Debug.Assert(gameFolderPair.DestinationEntry?.IsJunction == false);

            var createdFolder = await SourceCollection.CopyFolder(gameFolderPair.DestinationEntry);
            if (createdFolder != null) await DestinationCollection.DeleteFolder(gameFolderPair.DestinationEntry);
        }


        /// <summary>Results in the folder existing in both locations</summary>
        [AutoLazy.Lazy]
        public DelegateCommand MirrorCommand => new DelegateCommand(() => {
            Task.WhenAll(MirrorableItems().Select(Mirror)).Forget();
        }, () => SourceCollection.BothCollectionsInitialized && MirrorableItems().Any());

        private IEnumerable<GameFolderPair> MirrorableItems() => SelectedFolderPairs.Where(pair => !(pair.SourceEntry?.IsJunction == false && pair.DestinationEntry?.IsJunction == false));

        private async Task Mirror(GameFolderPair gameFolderPair)
        {
            Debug.Assert(!(gameFolderPair.SourceEntry?.IsJunction == false && gameFolderPair.DestinationEntry?.IsJunction == false));

            if (gameFolderPair.DestinationEntry?.IsJunction == false)
            {
                await SourceCollection.CopyFolder(gameFolderPair.DestinationEntry);
            }
            else
            {
                Debug.Assert(gameFolderPair.SourceEntry != null);
                await DestinationCollection.CopyFolder(gameFolderPair.SourceEntry);
            }
        }
    }
}
