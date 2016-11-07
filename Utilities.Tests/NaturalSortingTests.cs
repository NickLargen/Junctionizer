using System;
using System.Collections.Generic;

using NUnit.Framework;

using Utilities.Comparers;
using Utilities.Testing;

namespace Utilities.Tests
{
    public class NaturalSortingTests : ExtendedAssertionHelper
    {
        private static IReadOnlyList<string> WindowsFileSystemSorting { get; } = new List<string> {
            "Folder 2 B",
            "Folder 2Alpha",
            "Folder 2C",
            "Folder 23BE",
            "Folder21EE",
            "Folder30D",
            "leading000035zeroes",
            "leading000211zeroes",
            "mix43ed65num23ber21letcters",
            "mix43ed65num23ber21letDters",
            "monkey32A",
            "monkey276",
            "monkey324",
            "Negative -3",
            "Negative -4",
            "New folder",
            "New folder '  39",
            "New folder -  45",
            "New folder - Copy",
            "New folder $  36",
            "New folder &  38",
            "new Folder () 40",
            "New folder (2)",
            "New folder )  41",
            "New folder ,  44",
            "New folder ;  59",
            "New folder @  64",
            "New folder _  95",
            "New folder `  96",
            "New folder +  43",
            "New folder =  61",
            "New folder 0  48",
            "sequence",
            "test (3)",
            "Test 2",
            "tesT 3",
            "Test 4",
            "test(3)",
            "test[3",
            "test]3",
            "test^3",
            "Test_3",
            "Test`3",
            "test3",
            "Test4",
            "Test22.2,2",
            "testa3",
            "testB3",
        };

        private static IReadOnlyList<string> Expected { get; } = new List<string> {
            "Folder 2 B",
            "Folder 2Alpha",
            "Folder 2C",
            "Folder 23BE",
            "Folder21EE",
            "Folder30D",
            "leading000035zeroes",
            "leading000211zeroes",
            "mix43ed65num23ber21letcters",
            "mix43ed65num23ber21letDters",
            "monkey32A",
            "monkey276",
            "monkey324",
            "Negative -3",
            "Negative -4",
            "New folder",
            "New folder $  36",
            "New folder &  38",
            "New folder '  39",
            "new Folder () 40",
            "New folder (2)",
            "New folder )  41",
            "New folder +  43",
            "New folder ,  44",
            "New folder -  45",
            "New folder - Copy",
            "New folder 0  48",
            "New folder ;  59",
            "New folder =  61",
            "New folder @  64",
            "New folder _  95",
            "New folder `  96",
            "sequence",
            "test (3)",
            "Test 2",
            "tesT 3",
            "Test 4",
            "test(3)",
            "test3",
            "Test4",
            "Test22.2,2",
            "test[3",
            "test]3",
            "test^3",
            "Test_3",
            "Test`3",
            "testa3",
            "testB3",
        };


        [Test]
        public void MatchesExpected()
        {
            var customSorted = new List<string>(WindowsFileSystemSorting);
            customSorted.Sort(NaturalStringComparer.InvariantIgnoreCase);

            const int columnSize = 27;
            Console.WriteLine($"{"Actual",columnSize} ***** Expected");
            for (int i = 0; i < customSorted.Count; i++)
            {
                Console.WriteLine(
                    $"{customSorted[i],-columnSize}  ---  {Expected[i],-columnSize}{(customSorted[i] != Expected[i] ? "DIFFERENT" : string.Empty)}");
            }

            Ensure(customSorted, Not.SameAs(Expected));
            Ensure(customSorted, Is.EqualTo(Expected));
        }
    }
}
