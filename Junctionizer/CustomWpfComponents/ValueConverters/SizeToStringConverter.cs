using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

using Junctionizer.Model;

namespace Junctionizer.CustomWpfComponents.ValueConverters
{
    internal class SizeToStringConverter : MarkupExtension, IValueConverter
    {
        public static SizeToStringConverter Instance { get; } = new SizeToStringConverter();
        public override object ProvideValue(IServiceProvider serviceProvider) => Instance;

        public object Convert(object value, Type targetType = null, object parameter = null, CultureInfo culture = null)
        {
            if (!(value is long longValue)) throw new NotSupportedException();

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

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
