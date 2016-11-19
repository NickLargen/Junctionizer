using System;

using JetBrains.Annotations;

namespace Utilities.Comparers
{
    /// <summary>If both strings represent integers (as determined by int.TryParse) then their integer values are compared. Otherwise normal string comparison is used.</summary>
    public class StringOrIntegerComparer : System.Collections.Generic.IComparer<string>
    {
        public StringOrIntegerComparer(StringComparison comparisonType)
        {
            ComparisonType = comparisonType;
        }

        private StringComparison ComparisonType { get; }

        /// <inheritdoc/>
        public int Compare(string x, string y) => Compare(x, y, ComparisonType);

        public static int Compare([CanBeNull] string first, [CanBeNull] string second, StringComparison comparisonType)
        {
            if (int.TryParse(first, out var numericalFirst) && int.TryParse(second, out var numericalSecond))
            {
                return numericalFirst - numericalSecond;
            }

            return string.Compare(first, second, comparisonType);
        }
    }
}
