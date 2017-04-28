using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Junctionizer.CustomWpfComponents.ValueConverters
{
    public class DateTimeToStringConverter : MarkupExtension, IValueConverter
    {
        public static DateTimeToStringConverter Instance { get; } = new DateTimeToStringConverter();
        public override object ProvideValue(IServiceProvider serviceProvider) => Instance;

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
