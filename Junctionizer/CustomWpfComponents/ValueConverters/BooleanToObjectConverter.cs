using System.Globalization;

namespace Junctionizer.CustomWpfComponents.ValueConverters
{
    public class BooleanToObjectConverter : SimpleConverter<bool, object>
    {
        public object TrueValue { get; set; }
        public object FalseValue { get; set; } = null;

        public override object Convert(bool value, CultureInfo culture)
        {
            return value ? TrueValue : FalseValue;
        }
    }
}
