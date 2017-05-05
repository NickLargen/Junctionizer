using System.Globalization;
using System.Windows.Media;

namespace Junctionizer.CustomWpfComponents.ValueConverters
{
    public class DarkenColorConverter : SimpleConverter<Color, Color>
    {
        public double Multiplier { get; set; } = 0.75;

        public override Color Convert(Color color, CultureInfo culture)
        {
            return Color.FromRgb((byte) (color.R * Multiplier), (byte) (color.G * Multiplier), (byte) (color.B * Multiplier));
        }
    }
}
