using System;
using System.IO;

using Microsoft.VisualBasic.FileIO;

using NUnit.Framework;

namespace Test.Model.FolderCollection
{
    
    public class FolderCollectionNotInitializedTests : TestBase
    {

        protected DirectoryInfo RootDirectoryInfo { get; private set; }
        protected GameMover.Model.FolderCollection SourceCollection { get; set; }
        protected GameMover.Model.FolderCollection DestinationCollection { get; set; }

        [SetUp]
        protected void CreateTempFolder()
        {
            RootDirectoryInfo = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                nameof(GameMover) + "TestDirectory"));
            DeleteTempFolder();
            RootDirectoryInfo.Create();

            SourceCollection = new GameMover.Model.FolderCollection();
            DestinationCollection = new GameMover.Model.FolderCollection();
        }

        [TearDown]
        protected void DeleteTempFolder()
        {
            if (RootDirectoryInfo?.Exists == true)
            {
                SourceCollection?.Dispose();
                DestinationCollection?.Dispose();

                FileSystem.DeleteDirectory(RootDirectoryInfo.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
            }
        }

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