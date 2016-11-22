using System;
using System.Globalization;
using System.Windows.Data;

namespace GameMover.ValueConverters
{
    /// <summary>Converts a double to its logarithm to facilitate a non-linear slider.</summary>
    public class LogScaleConverter : IValueConverter
    {
        public const int LOGARITHM_BASE = 2;

        public const double MINIMUM_EXPONENT = 10;
        public const double MAXIMUM_EXPONENT = 36;

        public const double EPSILON = 1E-5;

        public object Convert(object objectValue, Type targetType, object parameter, CultureInfo culture)
        {
            double value = (double) objectValue;

            if (double.IsPositiveInfinity(value)) return MAXIMUM_EXPONENT;
            if (double.IsNegativeInfinity(value)) return MINIMUM_EXPONENT;

            return Math.Log(value, LOGARITHM_BASE);
        }

        public object ConvertBack(object objectValue, Type targetType, object parameter, CultureInfo culture)
        {
            double value = (double) objectValue;

            if (Math.Abs(value - MAXIMUM_EXPONENT) < EPSILON) return double.PositiveInfinity;
            if (Math.Abs(value - MINIMUM_EXPONENT) < EPSILON) return double.NegativeInfinity;

            return Math.Pow(LOGARITHM_BASE, value);
        }
    }
}
