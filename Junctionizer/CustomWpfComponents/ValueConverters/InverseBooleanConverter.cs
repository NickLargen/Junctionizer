using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Junctionizer.CustomWpfComponents.ValueConverters
{
    public class InverseBooleanConverter : MarkupExtension, IValueConverter
    {
        public static InverseBooleanConverter Instance { get; } = new InverseBooleanConverter();
        public override object ProvideValue(IServiceProvider serviceProvider) => Instance;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool boolean)) throw new NotSupportedException();

            return !boolean;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
