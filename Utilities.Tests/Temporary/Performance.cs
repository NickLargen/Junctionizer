using System;
using System.Collections.Generic;
using System.Diagnostics;

using NUnit.Framework;

using Utilities.Comparers;

namespace Utilities.Tests.Temporary
{
    public class Performance
    {
        [Test]
        public void Sorting()
        {
            var random = new Random();
            var count = 1_000_000;
            var intsBuiltInSort = new List<int>(count);
            var intsReverse = new List<int>(count);
            var intsInsertion = new List<int>(count);
            for (int i = 0; i < count; i++)
            {
                var next = random.Next(count);
                intsBuiltInSort.Add(next);
                intsReverse.Add(next);
//                intsInsertion.Add(next);
            }

            var comparer = Comparer<int>.Create((x, y) => x - y);

            var stopwatch = Stopwatch.StartNew();
            intsBuiltInSort.Sort(comparer);
            stopwatch.Stop();
            Console.WriteLine("Standard compare " + stopwatch.ElapsedMilliseconds);

            stopwatch.Restart();
            intsBuiltInSort.Reverse();
            stopwatch.Stop();
            Console.WriteLine("Reverse " + stopwatch.ElapsedMilliseconds);

            intsBuiltInSort.Reverse();

            stopwatch.Restart();
            intsReverse.Sort(new ReverseComparer<int>(Comparer<int>.Default));
            stopwatch.Stop();
            Console.WriteLine("Reverse compare " + stopwatch.ElapsedMilliseconds);


//            stopwatch.Restart();
//            intsInsertion.InsertionSort(Comparer<int>.Default);
//            stopwatch.Stop();
//            Console.WriteLine(stopwatch.ElapsedMilliseconds);
        }
    }
}
