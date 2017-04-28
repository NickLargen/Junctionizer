using System;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;

namespace Junctionizer.CustomWpfComponents.ValueConverters
{
    public class DarkenColorConverter : MarkupExtension, IValueConverter
    {
        public static DarkenColorConverter Instance { get; } = new DarkenColorConverter();
        public override object ProvideValue(IServiceProvider serviceProvider) => Instance;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is Color color)) throw new NotSupportedException();
            
            double percentage = 0.8; // Default 
            if (parameter != null) double.TryParse(parameter.ToString(), out percentage);
            return Color.FromRgb((byte) (color.R * percentage), (byte) (color.G * percentage), (byte) (color.B * percentage));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
