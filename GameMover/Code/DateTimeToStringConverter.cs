using System;
using System.Globalization;
using System.Windows.Data;

namespace GameMover.Code
{
    /// <summary>Allows a non-nullable DateTime to display the empty string by using DateTime.MinValue</summary>
    public class DateTimeToStringConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var dateTime = (DateTime) value;
            return dateTime == DateTime.MinValue ? string.Empty : dateTime.ToString(culture);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
