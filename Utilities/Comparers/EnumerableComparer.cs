using System;
using System.Collections.Generic;

namespace Utilities.Comparers
{
    /// <summary>Iterates through two enumerables simultaneously comparing each pair of elements. If one element is determined to be less than the other, its containing enumerable is immediately considered to be the smaller enumerable. Otherwise the comparison of their count is returned. -1, 0, and 1 are used for &lt;, =, and >, respectively.</summary>
    /// <typeparam name="T">The type of the elements in the enumerables.</typeparam>
    public class EnumerableComparer<T> : IComparer<IEnumerable<T>>
    {
        /// <summary>If a comparison function is not provided Comparer&lt;T&gt;.Default.Compare is used.</summary>
        public EnumerableComparer(Func<T, T, int> elementComparisonFunciton = null)
        {
            ElementComparisonFunction = elementComparisonFunciton ?? Comparer<T>.Default.Compare;
        }

        private Func<T, T, int> ElementComparisonFunction { get; }

        /// <inheritdoc/>
        public int Compare(IEnumerable<T> x, IEnumerable<T> y) => Compare(x, y, ElementComparisonFunction);

        public static int Compare(IEnumerable<T> x, IEnumerable<T> y, IComparer<T> elementComparer)
            => Compare(x, y, elementComparer.Compare);

        public static int Compare(IEnumerable<T> x, IEnumerable<T> y, Func<T, T, int> elementComparisonFunction)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (x == null) return -1;
            if (y == null) return 1;

            using (var xEnumerator = x.GetEnumerator())
            using (var yEnumerator = y.GetEnumerator())
            {
                while (xEnumerator.MoveNext())
                {
                    if (!yEnumerator.MoveNext()) return 1;

                    var elementCompare = elementComparisonFunction(xEnumerator.Current, yEnumerator.Current);
                    if (elementCompare != 0) return elementCompare;
                }

                return yEnumerator.MoveNext() ? -1 : 0;
            }
        }
    }
}
