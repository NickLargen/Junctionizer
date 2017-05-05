using System.Globalization;
using System.Windows;

namespace Junctionizer.CustomWpfComponents.ValueConverters
{
    public class BooleanToVisibilityConverter : SimpleConverter<bool, Visibility>
    {
        public bool Invert { get; set; } = false;

        public override Visibility Convert(bool value, CultureInfo culture)
        {
            return (Invert ? !value : value) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
