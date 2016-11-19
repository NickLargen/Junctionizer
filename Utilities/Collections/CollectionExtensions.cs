using System;
using System.Collections.Generic;
using System.Linq;

namespace Utilities.Collections
{
    public static class CollectionExtensions
    {
        /// Concatenate a single element to the end of an IEnumerable.
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> enumerable, T value)
        {
            foreach (var item in enumerable)
            {
                yield return item;
            }

            yield return value;
        }

        public static IEnumerable<T> Flatten<T>(this IEnumerable<IEnumerable<T>> enumerableEnumerable)
        {
            return enumerableEnumerable.SelectMany(it => it);
        }

        // Enables List's ForEach syntax on any IEnumerable
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
            {
                action(item);
            }
        }

        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> keyValuePair, out TKey key, out TValue value)
        {
            key = keyValuePair.Key;
            value = keyValuePair.Value;
        }
    }
}
