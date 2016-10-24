using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using GameMover.Code;

using Microsoft.VisualBasic.FileIO;

using NUnit.Framework;

namespace Test
{
    public class TestData
    {
        private static IReadOnlyList<string> CapitalLetters { get; } =
            Enumerable.Range(start: 'A', count: 3).Select(i => ((char) i).ToString()).ToList();

        [Test]
        [Explicit]
        public static void SetupManualTestData()
        {
            SetupTestData(
                new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "Manual Testing For " + nameof(GameMover))));
        }

        public static (DirectoryInfo root, DirectoryInfo source, DirectoryInfo destination) SetupTestData(DirectoryInfo rootDirectoryInfo)
        {
            if (Directory.Exists(rootDirectoryInfo.FullName))
            {
                FileSystem.DeleteDirectory(rootDirectoryInfo.FullName, UIOption.OnlyErrorDialogs, RecycleOption.DeletePermanently,
                    UICancelOption.ThrowException);
            }

            rootDirectoryInfo.Create();

            var sourceDirectory = rootDirectoryInfo.CreateSubdirectory("Source");
            var destinationDirectory = rootDirectoryInfo.CreateSubdirectory("Destination");

            foreach (var capitalLetter in CapitalLetters)
            {
                sourceDirectory.CreateSubdirectory(capitalLetter);
                var name = capitalLetter + capitalLetter;
                JunctionPoint.Create(sourceDirectory.CreateSubdirectory("1" + name), destinationDirectory.CreateSubdirectory(name), true);
            }

            return (rootDirectoryInfo, sourceDirectory, destinationDirectory);
        }
    }
}
