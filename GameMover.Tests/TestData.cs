using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using GameMover.Code;

using NUnit.Framework;

using Utilities.Collections;

namespace GameMover.Tests
{
    public static class TestData
    {
        private static IReadOnlyList<string> CapitalLetters { get; } =
            Enumerable.Range(start: 'A', count: 3).Select(i => ((char) i).ToString()).ToList();

        public static void SetupManualTestData()
        {
            SetupTestData(
                new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "Manual Testing For " + nameof(GameMover))));
        }

        public static (DirectoryInfo root, DirectoryInfo source, DirectoryInfo destination) SetupTestData(DirectoryInfo rootDirectoryInfo)
        {
            rootDirectoryInfo.Create();

            var sourceDirectory = rootDirectoryInfo.CreateSubdirectory("Source");
            var destinationDirectory = rootDirectoryInfo.CreateSubdirectory("Destination");
            
            sourceDirectory.GetDirectories().Select(info => info.FullName).ForEach(DeleteDirectoryRecursive);
            destinationDirectory.GetDirectories().Select(info => info.FullName).ForEach(DeleteDirectoryRecursive);

            foreach (var capitalLetter in CapitalLetters)
            {
                sourceDirectory.CreateSubdirectory(capitalLetter);
                var name = capitalLetter + capitalLetter;
                JunctionPoint.Create(sourceDirectory.CreateSubdirectory("1" + name), destinationDirectory.CreateSubdirectory(name), true);
            }

            return (rootDirectoryInfo, sourceDirectory, destinationDirectory);
        }

        private static void DeleteDirectoryRecursive(string targetDir)
        {
            var directoryInfo = new DirectoryInfo(targetDir);
            if (directoryInfo.Exists == false) return;

            if (directoryInfo.Attributes.HasFlag(FileAttributes.System) || directoryInfo.Attributes.HasFlag(FileAttributes.System)) throw new InvalidOperationException();

            if (directoryInfo.IsReparsePoint() == false)
            {
                foreach (string file in Directory.GetFiles(targetDir))
                {
                    File.Delete(file);
                }
                foreach (string dir in Directory.GetDirectories(targetDir))
                {
                    DeleteDirectoryRecursive(dir);
                }
            }


            Directory.Delete(targetDir, false);
        }
    }
}
