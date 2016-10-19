using NUnit.Framework;

namespace Test.Model.FolderCollection
{
    
    public class FolderCollectionNotInitializedTests : FolderCollectionTestBase
    {

        [Test]
        public void CanExecuteNoCorrespondingFolder()
        {
            Assert.That(SourceCollection.SelectLocationCommand.CanExecute, Is.True);

            Assert.That(SourceCollection.ArchiveCommand.CanExecute, Is.False);
            Assert.That(SourceCollection.CopyCommand.CanExecute, Is.False);
            Assert.That(SourceCollection.CreateJunctionCommand.CanExecute, Is.False);
            Assert.That(SourceCollection.DeleteFoldersCommand.CanExecute, Is.False);
            Assert.That(SourceCollection.DeleteJunctionsCommand.CanExecute, Is.False);
            Assert.That(SourceCollection.SelectFoldersNotInOtherPaneCommand.CanExecute, Is.False);
        }

    }
}