using System.Globalization;

namespace Junctionizer.CustomWpfComponents.ValueConverters
{
    public class NullToFalseConverter : SimpleConverter<object, bool>
    {
        public override bool Convert(object value, CultureInfo culture)
        {
            return value != null;
        }
    }
}
