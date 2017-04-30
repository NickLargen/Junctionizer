using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Junctionizer.CustomWpfComponents.ValueConverters
{
    public class TrueToParameterConverter : MarkupExtension, IValueConverter
    {
        public static TrueToParameterConverter Instance { get; } = new TrueToParameterConverter();
        public override object ProvideValue(IServiceProvider serviceProvider) => Instance;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool boolean)) throw new NotSupportedException();

            return boolean ? parameter : null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
