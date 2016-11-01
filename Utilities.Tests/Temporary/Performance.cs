using System;
using System.Collections.ObjectModel;
using System.Diagnostics;

using NUnit.Framework;

namespace Utilities.Tests.Temporary
{
    public class Performance
    {
        [Test, Explicit]
        public void RemovePerformance()
        {
            //Findings: swapping on removal is successfully O(1) instead of default O(n)

            var fastRemoveCollection = new FastRemove();
            var standardRemoveCollection = new StandardRemove();

            var count = 50000;
            for (int i = 0; i < count; i++)
            {
                fastRemoveCollection.Add(i);
                standardRemoveCollection.Add(i);
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < count; i++)
            {
                fastRemoveCollection.RemoveAt(0);
            }

            // Stop unused warnings
            Debug.WriteLine(fastRemoveCollection[6] + standardRemoveCollection[7]);

            stopwatch.Stop();
            Console.WriteLine($"Fast removal {stopwatch.ElapsedMilliseconds}ms");

            stopwatch.Restart();
            for (int i = 0; i < count; i++)
            {
                standardRemoveCollection.RemoveAt(0);
            }

            stopwatch.Stop();
            Console.WriteLine($"Standard removal {stopwatch.ElapsedMilliseconds}ms");
        }
    }

    public class FastRemove : KeyedCollection<string, int>
    {
        /// <inheritdoc/>
        protected override void RemoveItem(int index)
        {
            var lastIndex = Count - 1;
            if (index != lastIndex)
            {
                Items[index] = Items[lastIndex];
            }
            base.RemoveItem(lastIndex);
        }

        /// <inheritdoc/>
        protected override string GetKeyForItem(int item)
        {
            return item.ToString();
        }
    }

    public class StandardRemove : KeyedCollection<string, int>
    {
        /// <inheritdoc/>
        protected override string GetKeyForItem(int item)
        {
            return item.ToString();
        }
    }
}
