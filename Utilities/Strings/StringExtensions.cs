using System;

namespace Utilities.Strings
{
    public static class StringExtensions
    {
        /// Returns the provided string up to but not including <paramref name="value"/>. Returns itself if <paramref name="value"/> is not present.
        public static string SubstringUntil(this string self, string value)
        {
            var length = self.IndexOf(value, StringComparison.OrdinalIgnoreCase);
            return length < 0 ? self : self.Substring(0, length);
        }
    }
}
