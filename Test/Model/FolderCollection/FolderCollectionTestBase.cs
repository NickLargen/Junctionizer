using System;
using System.IO;

using Microsoft.VisualBasic.FileIO;

using NUnit.Framework;

namespace Test.Model.FolderCollection
{

    public class FolderCollectionTestBase : TestBase
    {

        public DirectoryInfo RootDirectoryInfo { get; set; }
        public GameMover.Model.FolderCollection SourceCollection { get; set; }
        public GameMover.Model.FolderCollection DestinationCollection { get; set; }

        [SetUp]
        public void CreateTempFolder()
        {
            RootDirectoryInfo = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                nameof(GameMover) + "TestDirectory"));
            DeleteTempFolder();
            RootDirectoryInfo.Create();

            SourceCollection = new GameMover.Model.FolderCollection();
            DestinationCollection = new GameMover.Model.FolderCollection();
        }

        [TearDown]
        public void DeleteTempFolder()
        {
            if (RootDirectoryInfo?.Exists == true)
            {
                SourceCollection?.Dispose();
                DestinationCollection?.Dispose();

                FileSystem.DeleteDirectory(RootDirectoryInfo.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.ThrowException);
            }
        }

    }

}
