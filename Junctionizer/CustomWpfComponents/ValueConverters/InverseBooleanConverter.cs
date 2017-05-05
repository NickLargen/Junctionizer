using System.Globalization;

namespace Junctionizer.CustomWpfComponents.ValueConverters
{
    public class InverseBooleanConverter : SimpleConverter<bool, bool>
    {
        public override bool Convert(bool value, CultureInfo culture)
        {
            return !value;
        }
    }
}
