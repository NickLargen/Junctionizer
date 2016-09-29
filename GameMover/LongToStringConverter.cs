using System;
using System.Globalization;
using System.Windows.Data;

namespace GameMover
{

    internal class LongToStringConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (long) value == -1 ? "" : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.Equals("") ? -1 : value;
        }

    }

}
