using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Junctionizer.CustomWpfComponents.ValueConverters
{
    /// <summary>For converting the value of a logarithmic slider representing a file size to its actual value in a human readable format for displaying in a tooltip (eg it coud map 20 to 1 MB).</summary>
    public class LogScaleTooltipConverter : MarkupExtension, IValueConverter
    {
        public static LogScaleTooltipConverter Instance { get; } = new LogScaleTooltipConverter();
        public override object ProvideValue(IServiceProvider serviceProvider) => Instance;

        public object Convert(object objectValue, Type targetType, object parameter, CultureInfo culture)
        {
            double value = (double) objectValue;

            if (Math.Abs(value - LogScaleConverter.MAXIMUM_EXPONENT) < LogScaleConverter.EPSILON) return double.PositiveInfinity;
            if (Math.Abs(value - LogScaleConverter.MINIMUM_EXPONENT) < LogScaleConverter.EPSILON) return 0;

            var size = (long) Math.Pow(LogScaleConverter.LOGARITHM_BASE, value);
            return SizeToStringConverter.Instance.Convert(size);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }

    }
}
