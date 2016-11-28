using System;
using System.Globalization;
using System.Windows.Data;

using GameMover.Model;

namespace GameMover.ValueConverters
{
    internal class SizeToStringConverter : IValueConverter
    {
        public object Convert(object objectValue, Type targetType = null, object parameter = null, CultureInfo culture = null)
        {
            var value = (long) objectValue;


            if (value == GameFolder.JUNCTION_POINT_SIZE) return "∅";
            if (value == GameFolder.UNKNOWN_SIZE) return "?";

            if (value < 0) return string.Empty;

            const string format = "{0:N1} {1}";
            if (value >= 1 << 30) // Gigabyte
            {
                return string.Format(format, (value >> 20) / 1024f, "GB");
            }

            if (value >= 1 << 20) // Megabyte
            {
                return string.Format(format, (value >> 10) / 1024f, "MB");
            }

            if (value >= 1 << 10) // Kilobyte
            {
                return string.Format(format, value / 1024f, "KB");
            }

            return value + " B";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
