using System;
using System.Globalization;
using System.Windows.Data;

using static GameMover.ValueConverters.LogScaleConverter;

namespace GameMover.ValueConverters
{
    /// <summary>For converting the value of a logarithmic slider representing a file size to its actual value in a human readable format for displaying in a tooltip (eg it coud map 20 to 1 MB).</summary>
    public class LogScaleTooltipConverter : IValueConverter
    {
        private SizeToStringConverter LongSizeToStringConverter { get; } = new SizeToStringConverter();

        public object Convert(object objectValue, Type targetType, object parameter, CultureInfo culture)
        {
            double value = (double) objectValue;

            if (Math.Abs(value - MAXIMUM_EXPONENT) < EPSILON) return double.PositiveInfinity;
            if (Math.Abs(value - MINIMUM_EXPONENT) < EPSILON) return 0;

            var size = (long) Math.Pow(LOGARITHM_BASE, value);
            return LongSizeToStringConverter.Convert(size);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
