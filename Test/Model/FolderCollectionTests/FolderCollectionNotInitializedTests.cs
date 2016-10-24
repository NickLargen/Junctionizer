using GameMover.Model;

using NUnit.Framework;

namespace Test.Model.FolderCollectionTests
{
    public class FolderCollectionNotInitializedTests : TestBase
    {
        [Test]
        public void CanExecuteNoCorrespondingFolder()
        {
            var folderCollection = new FolderCollection();

            Assert.That(folderCollection.SelectLocationCommand.CanExecute, Is.True);

            Assert.That(folderCollection.ArchiveSelectedCommand.CanExecute, Is.False);
            Assert.That(folderCollection.CopySelectedCommand.CanExecute, Is.False);
            Assert.That(folderCollection.CreateSelectedJunctionCommand.CanExecute, Is.False);
            Assert.That(folderCollection.DeleteSelectedFoldersCommand.CanExecute, Is.False);
            Assert.That(folderCollection.DeleteSelectedJunctionsCommand.CanExecute, Is.False);
            Assert.That(folderCollection.SelectFoldersNotInOtherPaneCommand.CanExecute, Is.False);
        }
    }
}
