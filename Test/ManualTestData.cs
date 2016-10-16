using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using GameMover.External_Code;

using Microsoft.VisualBasic.FileIO;

using NUnit.Framework;

namespace Test
{

    public class ManualTestData
    {

        private static IReadOnlyList<string> CapitalLetters { get; } =
            Enumerable.Range(start: 'A', count: 3).Select(i => ((char) i).ToString()).ToList();

        [Test]
        [Explicit]
        public void SetupManualTestData()
        {
            var rootDirectoryInfo = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                "Manual Testing For " + nameof(GameMover)));

            if (rootDirectoryInfo?.Exists == true)
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
        }

    }

}
