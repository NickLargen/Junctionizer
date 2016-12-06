using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

using GameMover.Model;

using JetBrains.Annotations;

using Utilities.Collections;

namespace GameMover.ViewModels
{
    public class MergedItemEnumerable : IEnumerable<MergedItem>, INotifyCollectionChanged
    {
        [NotNull]
        public FolderCollection SourceCollection { get; }
        [NotNull]
        public FolderCollection DestinationCollection { get; }

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
                switch (e.Action) {
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
                    if(isFromSourceCollection) mergedItem.SourceEntry = null;
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

        public IEnumerator<MergedItem> GetEnumerator() => Items.Values.GetEnumerator();

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

                MergedItem item = new MergedItem (sourceEntry: sourceFolder, destinationEntry: destinationFolder);
                yield return item;
            }

            foreach (var sourceFolder in sourceFoldersDictionary.Values)
            {
                yield return new MergedItem (sourceEntry: sourceFolder);
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e) => CollectionChanged?.Invoke(this, e);

        /// <inheritdoc/>
        public event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}
