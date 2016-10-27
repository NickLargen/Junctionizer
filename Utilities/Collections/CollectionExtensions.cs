using System;
using System.Collections.Generic;

namespace Utilities.Collections
{
    public static class CollectionExtensions
    {
        // Enables List's ForEach syntax on any IEnumerable
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
            {
                action(item);
            }
        }

        /// Concatenate a single element to the end of an IEnumerable.
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> enumerable, T value)
        {
            foreach (var item in enumerable)
            {
                yield return item;
            }

            yield return value;
        }
    }
}
