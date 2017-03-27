using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

using JetBrains.Annotations;

using Microsoft.VisualStudio.Threading;

using Prism.Commands;

using PropertyChanged;

using Utilities.Collections;

namespace Junctionizer.Model
{
    [ImplementPropertyChanged]
    public class MergedItemEnumerable : IEnumerable<MergedItem>, INotifyCollectionChanged
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
        public IEnumerable<MergedItem> SelectedMergedItems =>
            SelectedItems.Reverse()
                         .Cast<MergedItem>()
                         .Where(mi => mi.SourceEntry?.IsBeingDeleted != true && mi.DestinationEntry?.IsBeingDeleted != true);

        [NotNull]
        private Func<GameFolder, string> KeySelector { get; }

        public Dictionary<string, MergedItem> Items { get; } = new Dictionary<string, MergedItem>();

        public MergedItemEnumerable(FolderCollection sourceCollection, FolderCollection destinationCollection)
        {
            SourceCollection = sourceCollection;
            DestinationCollection = destinationCollection;

            KeySelector = SourceCollection.Folders.GetKeyForItem;


            GetExistingValues().ForEach(item => Items.Add(item.Name, item));

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
                        GetExistingValues().ForEach(item => Items.Add(item.Name, item));
                        OnCollectionChanged(e);
                        break;
                    case NotifyCollectionChangedAction.Add:
                        AddItems(e.NewItems.Cast<GameFolder>(), isFromSourceCollection);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        RemoveItems(e.OldItems.Cast<GameFolder>(), isFromSourceCollection);
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

        private void AddItems(IEnumerable<GameFolder> folders, bool isFromSourceCollection)
        {
            var addedItems = new List<MergedItem>();
            foreach (var folder in folders)
            {
                if (Items.TryGetValue(folder.Name, out var existingItem))
                {
                    Debug.Assert(isFromSourceCollection ? existingItem.SourceEntry == null : existingItem.DestinationEntry == null);

                    if (isFromSourceCollection) existingItem.SourceEntry = folder;
                    else existingItem.DestinationEntry = folder;
                }
                else
                {
                    var newItem = isFromSourceCollection ? new MergedItem(sourceEntry: folder) : new MergedItem(destinationEntry: folder);
                    Items.Add(folder.Name, newItem);
                    addedItems.Add(newItem);
                }
            }

            if (addedItems.Any()) OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, changedItems: addedItems));
        }

        private void RemoveItems(IEnumerable<GameFolder> folders, bool isFromSourceCollection)
        {
            var removedItems = new List<MergedItem>();
            foreach (var folder in folders)
            {
                var mergedItem = Items[folder.Name];
                if ((isFromSourceCollection ? mergedItem.DestinationEntry : mergedItem.SourceEntry) != null)
                {
                    if (isFromSourceCollection) mergedItem.SourceEntry = null;
                    else mergedItem.DestinationEntry = null;
                }
                else
                {
                    removedItems.Add(mergedItem);
                    Items.Remove(folder.Name);
                }
            }

            if (removedItems.Any()) OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, changedItems: removedItems));
        }

        /// <inheritdoc/>
        public IEnumerable<MergedItem> GetExistingValues()
        {
            var sourceFoldersDictionary = SourceCollection.Folders.ToDictionary(KeySelector);

            foreach (var destinationFolder in DestinationCollection.Folders)
            {
                /*
                var junctionFolders = SourceCollection.GetFoldersByJunctionTarget(destinationFolder.DirectoryInfo).ToList();

                if (junctionFolders.Any())
                {
                    if (junctionFolders.Count > 1) throw new NotSupportedException("Why would you have two junction points with the same target in one folder?");

                    var junctionFolder = junctionFolders[0];
                    sourceFoldersDictionary.Remove(GetKeyForItem(junctionFolder));
                    item.SourceEntry = junctionFolder;
                }*/


                var key = KeySelector(destinationFolder);
                if (sourceFoldersDictionary.TryGetValue(key, out var sourceFolder))
                {
                    sourceFoldersDictionary.Remove(key);
                }

                MergedItem item = new MergedItem(sourceEntry: sourceFolder, destinationEntry: destinationFolder);
                yield return item;
            }

            foreach (var sourceFolder in sourceFoldersDictionary.Values)
            {
                yield return new MergedItem(sourceEntry: sourceFolder);
            }
        }

        public IEnumerator<MergedItem> GetEnumerator() => Items.Values.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);

        /// <inheritdoc/>
        public event NotifyCollectionChangedEventHandler CollectionChanged;


        /// <summary>Results in the folder in neither location.</summary>
        [AutoLazy.Lazy]
        public DelegateCommand DeleteCommand => new DelegateCommand(() => {
            DeleteSelected().Forget();
        }, () => SelectedMergedItems.Any());

        private async Task DeleteSelected()
        {
            var sourceFolders = SelectedMergedItems.Select(mi => mi.SourceEntry).Where(folder => folder != null).ToList();
            SourceCollection.DeleteJunctions(sourceFolders);
            await SourceCollection.DeleteFolders(sourceFolders);
            await DestinationCollection.DeleteFolders(SelectedMergedItems
                .Select(mi => mi.DestinationEntry)
                .Where(folder => folder != null));
        }


        /// <summary>Results in the folder in destination with a junction pointing to it from source.</summary>
        [AutoLazy.Lazy]
        public DelegateCommand ArchiveCommand => new DelegateCommand(() => {
            SourceCollection.ArchiveFolders(ArchivableItems().Select(mi => mi.SourceEntry)).Forget();
        }, () => ArchivableItems().Any());

        private IEnumerable<MergedItem> ArchivableItems() => SelectedMergedItems.Where(mi => mi.SourceEntry?.IsJunction == false);


        /// <summary>Results in folder in source location, not in destination.</summary>
        [AutoLazy.Lazy]
        public DelegateCommand RestoreCommand => new DelegateCommand(() => {
            Task.WhenAll(RestorableItems().Select(Restore)).Forget();
        }, () => RestorableItems().Any());

        private IEnumerable<MergedItem> RestorableItems() => SelectedMergedItems.Where(mi => mi.DestinationEntry?.IsJunction == false);

        private async Task Restore(MergedItem mergedItem)
        {
            Debug.Assert(mergedItem.DestinationEntry?.IsJunction == false);

            var createdFolder = await SourceCollection.CopyFolder(mergedItem.DestinationEntry);
            if (createdFolder != null) await DestinationCollection.DeleteFolder(mergedItem.DestinationEntry);
        }


        /// <summary>Results in the folder existing in both locations</summary>
        [AutoLazy.Lazy]
        public DelegateCommand MirrorCommand => new DelegateCommand(() => {
            Task.WhenAll(MirrorableItems().Select(Mirror)).Forget();
        }, () => MirrorableItems().Any());

        private IEnumerable<MergedItem> MirrorableItems() => SelectedMergedItems.Where(mi => !(mi.SourceEntry?.IsJunction == false && mi.DestinationEntry?.IsJunction == false));

        private async Task Mirror(MergedItem mergedItem)
        {
            Debug.Assert(!(mergedItem.SourceEntry?.IsJunction == false && mergedItem.DestinationEntry?.IsJunction == false));

            if (mergedItem.DestinationEntry?.IsJunction == false)
            {
                await SourceCollection.CopyFolder(mergedItem.DestinationEntry);
            }
            else
            {
                Debug.Assert(mergedItem.SourceEntry != null);
                await DestinationCollection.CopyFolder(mergedItem.SourceEntry);
            }
        }
    }
}
