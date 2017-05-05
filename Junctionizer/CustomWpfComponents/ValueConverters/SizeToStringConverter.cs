using System;
using System.Globalization;

using Junctionizer.Model;

namespace Junctionizer.CustomWpfComponents.ValueConverters
{
    internal class SizeToStringConverter : SimpleConverter<long, string>
    {
        public override string Convert(long longValue, CultureInfo culture)
        {
            return Convert(longValue);
        }

        public static string Convert(long longValue)
        {
            if (longValue == GameFolder.JUNCTION_POINT_SIZE) return "∅";
            if (longValue == GameFolder.UNKNOWN_SIZE) return "?";

            if (longValue < 0) return string.Empty;

            const string format = "{0:N1} {1}";
            if (longValue >= 1 << 30) // Gigabyte
            {
                return string.Format(format, (longValue >> 20) / 1024f, "GB");
            }

            if (longValue >= 1 << 20) // Megabyte
            {
                return string.Format(format, (longValue >> 10) / 1024f, "MB");
            }

            if (longValue >= 1 << 10) // Kilobyte
            {
                return string.Format(format, longValue / 1024f, "KB");
            }

            return longValue + " B";
        }
    }
}
