using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using GameMover.Model;

using NUnit.Framework;

namespace GameMover.Tests.Model.FolderCollectionTests
{
    public class InitializedFolderCollections : TestBase
    {
        private DirectoryInfo RootDirectoryInfo { get; set; }
        private FolderCollection SourceCollection { get; set; }
        private FolderCollection DestinationCollection { get; set; }

        [SetUp]
        public void CreateTempFolder()
        {
            RootDirectoryInfo = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                nameof(GameMover) + "TestDirectory"));

            SourceCollection?.Dispose();
            DestinationCollection?.Dispose();

            var(_, sourceDirectory, destinationDirectory) = TestData.SetupTestData(RootDirectoryInfo);

            (SourceCollection, DestinationCollection) = FolderCollection.Factory.CreatePair();
            SourceCollection.Location = sourceDirectory.FullName;
            DestinationCollection.Location = destinationDirectory.FullName;
        }

        [Test]
        public void CorrespondingCollectionsEqual()
        {
            Assert.That(SourceCollection.CorrespondingCollection, Is.EqualTo(DestinationCollection));
            Assert.That(DestinationCollection.CorrespondingCollection, Is.EqualTo(SourceCollection));
        }

        [Test]
        public void SelectAll()
        {
            SourceCollection.SelectFolders(SourceCollection.Folders);
            Assert.That(SourceCollection.SelectedItems, Is.EquivalentTo(SourceCollection.Folders));
            Assert.That(SourceCollection.SelectedFolders, Is.EquivalentTo(SourceCollection.Folders));
        }

        [Test]
        public void SelectNotInDestination()
        {
            Assert.That(SourceCollection.SelectFoldersNotInOtherPaneCommand.CanExecute);
            SourceCollection.SelectFoldersNotInOtherPaneCommand.Execute();

            var selectedFolders = SourceCollection.SelectedFolders.ToList();
            Assert.That(selectedFolders.Count, Is.EqualTo(3));
            Assert.That(selectedFolders, Has.All.Property(nameof(GameFolder.IsJunction)).EqualTo(false));
        }

        [Test]
        public void SelectNotInSource()
        {
            Assert.That(DestinationCollection.SelectFoldersNotInOtherPaneCommand.CanExecute);
            DestinationCollection.SelectFoldersNotInOtherPaneCommand.Execute();

            var selectedFolders = DestinationCollection.SelectedFolders.ToList();
            Assert.That(selectedFolders, Is.Empty);
        }

        [Test]
        public async Task DeleteAll()
        {
            SourceCollection.SelectFolders(SourceCollection.Folders);

            Assert.That(SourceCollection.Folders, Is.Not.Empty);

            await SourceCollection.DeleteSelectedFolders();

            Assert.That(SourceCollection.Folders, Is.Empty);
        }
    }
}
