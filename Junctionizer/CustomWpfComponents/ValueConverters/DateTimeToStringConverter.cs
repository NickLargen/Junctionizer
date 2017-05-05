using System;
using System.Globalization;

namespace Junctionizer.CustomWpfComponents.ValueConverters
{
    public class DateTimeToStringConverter : SimpleConverter<DateTime, string>
    {
        /// <inheritdoc/>
        public override string Convert(DateTime dateTime, CultureInfo culture)
        {
            return dateTime == DateTime.MinValue ? "?" : dateTime.ToString(culture);
        }
    }
}
