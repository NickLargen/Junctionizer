using System;
using System.Globalization;

namespace Junctionizer.CustomWpfComponents.ValueConverters
{
    /// <summary>Converts a double to its logarithm to facilitate a non-linear slider.</summary>
    public class LogScaleConverter : SimpleConverter<double, double>
    {
        public const int LOGARITHM_BASE = 2;

        public const double MINIMUM_EXPONENT = 10;
        public const double MAXIMUM_EXPONENT = 36;

        public const double EPSILON = 1E-5;

        public override double Convert(double value, CultureInfo culture)
        {
            if (double.IsPositiveInfinity(value)) return MAXIMUM_EXPONENT;
            if (double.IsNegativeInfinity(value)) return MINIMUM_EXPONENT;

            return Math.Log(value, LOGARITHM_BASE);
        }

        public override double ConvertBack(double doubleValue, CultureInfo culture)
        {
            if (Math.Abs(doubleValue - MAXIMUM_EXPONENT) < EPSILON) return double.PositiveInfinity;
            if (Math.Abs(doubleValue - MINIMUM_EXPONENT) < EPSILON) return double.NegativeInfinity;

            return Math.Pow(LOGARITHM_BASE, doubleValue);
        }
    }
}
