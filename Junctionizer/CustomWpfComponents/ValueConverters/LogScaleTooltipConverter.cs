using System;
using System.Globalization;

namespace Junctionizer.CustomWpfComponents.ValueConverters
{
    /// <summary>For converting the value of a logarithmic slider representing a file size to its actual value in a human readable format for displaying in a tooltip (eg it coud map 20 to 1 MB).</summary>
    public class LogScaleTooltipConverter : SimpleConverter<double, string>
    {
        public override string Convert(double doubleValue, CultureInfo culture)
        {
            if (Math.Abs(doubleValue - LogScaleConverter.MAXIMUM_EXPONENT) < LogScaleConverter.EPSILON) return $"{double.PositiveInfinity}";
            if (Math.Abs(doubleValue - LogScaleConverter.MINIMUM_EXPONENT) < LogScaleConverter.EPSILON) return "0";

            var size = (long) Math.Pow(LogScaleConverter.LOGARITHM_BASE, doubleValue);
            return SizeToStringConverter.Convert(size);
        }
    }
}
