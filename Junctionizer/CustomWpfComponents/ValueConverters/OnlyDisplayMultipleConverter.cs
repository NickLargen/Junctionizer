using System.Globalization;

namespace Junctionizer.CustomWpfComponents.ValueConverters
{
    public class OnlyDisplayMultipleConverter : SimpleConverter<int, string>
    {
        public override string Convert(int value, CultureInfo culture)
        {
            return value > 1 ? value.ToString() : string.Empty;
        }
    }
}
