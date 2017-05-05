using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;

namespace Junctionizer.CustomWpfComponents.ValueConverters
{
    public abstract class MarkupExtensionCache : MarkupExtension
    {
        private static ConcurrentDictionary<Type, MarkupExtensionCache> ExtensionDictionary { get; } = new ConcurrentDictionary<Type, MarkupExtensionCache>();

        // Subtypes with no public properties with setters will be treated as singletons
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var type = GetType();
            return type.GetProperties().Any(info => info.CanWrite) ? this : ExtensionDictionary.GetOrAdd(type, this);
        }
    }

    public abstract class SimpleConverter<T1, T2> : MarkupExtensionCache, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is T1 typedValue)) throw new ArgumentException(nameof(value));

            return Convert(typedValue, culture);
        }

        public abstract T2 Convert(T1 value, CultureInfo culture);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is T2 typedValue)) throw new ArgumentException(nameof(value));

            return ConvertBack(typedValue, culture);
        }

        public virtual T1 ConvertBack(T2 value, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
