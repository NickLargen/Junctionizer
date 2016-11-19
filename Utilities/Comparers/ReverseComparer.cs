using System;
using System.Collections;
using System.Collections.Generic;

namespace Utilities.Comparers
{
    public class ReverseComparer<T> : SimpleComparer<T>
    {
        public static IComparer<T> Default => LazyDefault.Value;
        private static Lazy<IComparer<T>> LazyDefault { get; } =
            new Lazy<IComparer<T>>(() => new ReverseComparer<T>(Comparer<T>.Default.Compare));

        private Comparison<T> ExistingComparison { get; }

        public ReverseComparer(IComparer<T> existingComparer) : this(existingComparer.Compare) {}

        public ReverseComparer(Comparison<T> existingComparison)
        {
            ExistingComparison = existingComparison;
        }

        public override int Compare(T x, T y) => ExistingComparison(y, x);
    }

    public abstract class SimpleComparer<T> : IComparer<T>, IComparer
    {
        public abstract int Compare(T x, T y);

        int IComparer.Compare(object x, object y) => Compare((T) x, (T) y);
    }
}
