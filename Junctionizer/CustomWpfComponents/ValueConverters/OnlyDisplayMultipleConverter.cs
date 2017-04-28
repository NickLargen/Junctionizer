using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Junctionizer.CustomWpfComponents.ValueConverters
{
    public class OnlyDisplayMultipleConverter : MarkupExtension, IValueConverter
    {
        public static OnlyDisplayMultipleConverter Instance { get; } = new OnlyDisplayMultipleConverter();
        public override object ProvideValue(IServiceProvider serviceProvider) => Instance;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is int intValue)) throw new NotSupportedException();

            return intValue > 1 ? value : string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
