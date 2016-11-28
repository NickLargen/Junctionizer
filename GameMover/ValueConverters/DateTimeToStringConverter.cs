using System;
using System.Globalization;
using System.Windows.Data;

namespace GameMover.ValueConverters
{
    public class DateTimeToStringConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return string.Empty;

            var dateTime = (DateTime) value;
            return dateTime == DateTime.MinValue ? "?" : dateTime.ToString(culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
