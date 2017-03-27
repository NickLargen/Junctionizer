using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Junctionizer.Model;

using NUnit.Framework;

using TestingUtilities;

using Utilities;

namespace Junctionizer.Tests.Model.FolderCollectionTests
{
    public class InitializedFolderCollections : ExtendedAssertionHelper
    {
        private DirectoryInfo RootDirectoryInfo { get; set; }
        private FolderCollection SourceCollection { get; set; }
        private FolderCollection DestinationCollection { get; set; }

        [SetUp]
        public void CreateTempFolder()
        {
            RootDirectoryInfo = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                nameof(Junctionizer) + "TestDirectory"));

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
            Ensure(SourceCollection.CorrespondingCollection, Is.EqualTo(DestinationCollection));
            Ensure(DestinationCollection.CorrespondingCollection, Is.EqualTo(SourceCollection));
        }

        [Test]
        public void SelectAll()
        {
            Ensure(SourceCollection.Folders, Not.Empty);
            SourceCollection.SelectFolders(SourceCollection.Folders);
            Ensure(SourceCollection.SelectedItems, Is.EquivalentTo(SourceCollection.Folders));
            Ensure(SourceCollection.AllSelectedGameFolders, Is.EquivalentTo(SourceCollection.Folders));
        }

        [Test]
        public void SelectNotInDestination()
        {
            Assert.That(SourceCollection.SelectFoldersNotInOtherPaneCommand.CanExecute());
            SourceCollection.SelectFoldersNotInOtherPaneCommand.Execute();

            var selectedFolders = SourceCollection.AllSelectedGameFolders.ToList();
            Ensure(selectedFolders.Count, Is.EqualTo(3));
            Ensure(selectedFolders, Has.All.Property(nameof(GameFolder.IsJunction)).EqualTo(false));
        }

        [Test]
        public void SelectNotInSource()
        {
            Ensure(DestinationCollection.SelectFoldersNotInOtherPaneCommand.CanExecute());
            DestinationCollection.SelectFoldersNotInOtherPaneCommand.Execute();

            var selectedFolders = DestinationCollection.AllSelectedGameFolders.ToList();
            Ensure(selectedFolders, Is.Empty);
        }

        [Test]
        public void DeleteAll()
        {
            SourceCollection.SelectFolders(SourceCollection.Folders);

            Ensure(SourceCollection.Folders, Is.Not.Empty);
            Ensure(SourceCollection.AllSelectedGameFolders, Is.Not.Empty);

            SourceCollection.DeleteJunctions(SourceCollection.SelectedJunctions);
            SourceCollection.DeleteFolders(SourceCollection.SelectedFolders).RunTaskSynchronously();

            // The deletion needs to propagate through the filesystemwatcher
            while (SourceCollection.Folders.Count != 0)
            {
                Console.WriteLine(SourceCollection.Folders.Count);
                Thread.Sleep(100);
            }

            Ensure(SourceCollection.Folders, Is.Empty);
        }
    }
}
