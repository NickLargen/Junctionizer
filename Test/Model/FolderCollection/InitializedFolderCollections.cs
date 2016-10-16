using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using NUnit.Framework;
using static NUnit.Framework.Assert;

namespace Test.Model.FolderCollection
{
    public class InitializedFolderCollections : FolderCollectionNotInitializedTests
    {

        private static IReadOnlyList<string> CapitalLetters { get; } = Enumerable.Range(start: 'A', count: 3).Select(i => ((char)i).ToString()).ToList();

        [SetUp]
        public void CreateFolders()
        {
            SourceCollection.CorrespondingCollection = DestinationCollection;

            var sourceDirectory = RootDirectoryInfo.CreateSubdirectory("Source");
            var destinationDirectory = RootDirectoryInfo.CreateSubdirectory("Destination");

            foreach (var capitalLetter in CapitalLetters)
            {
                sourceDirectory.CreateSubdirectory(capitalLetter);
                destinationDirectory.CreateSubdirectory(capitalLetter + capitalLetter);
            }

            SourceCollection.Location = sourceDirectory.FullName;
            DestinationCollection.Location = destinationDirectory.FullName;
        }

        [Test]
        public void CorrespondingCollectionsEqual()
        {
            That(SourceCollection.CorrespondingCollection, Is.EqualTo(DestinationCollection));
            That(DestinationCollection.CorrespondingCollection, Is.EqualTo(SourceCollection));

            SourceCollection.CorrespondingCollection = null;

            That(SourceCollection.CorrespondingCollection, Is.EqualTo(null));
            That(DestinationCollection.CorrespondingCollection, Is.EqualTo(null));
        }

        [Test]
        public void SelectAll()
        {
            SourceCollection.SelectFolders(SourceCollection.Folders);
            That(SourceCollection.SelectedItems, Is.EquivalentTo(SourceCollection.Folders));
            That(SourceCollection.SelectedFolders, Is.EquivalentTo(SourceCollection.Folders));
        }

        /*[Test]
        public async Task DeleteAll()
        {
            SourceCollection.SelectFolders(SourceCollection.Folders);
            await SourceCollection.DeleteFoldersCommand.Execute();

            That(SourceCollection.Folders, Is.Empty);
        }*/
    }
}
