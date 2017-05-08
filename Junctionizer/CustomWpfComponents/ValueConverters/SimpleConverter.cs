using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;

using JetBrains.Annotations;

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

    [PublicAPI]
    public abstract class SimpleConverter<T1, T2> : MarkupExtensionCache, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is T1 typedValue))
            {
                if (ReferenceEquals(value, default(T1))) return Convert(default(T1), culture);

                throw new ArgumentException($"{GetType()} can only convert objects of type {typeof(T1)}, but received type {value.GetType()} with value '{value}'.");
            }

            return Convert(typedValue, culture);
        }

        public abstract T2 Convert(T1 value, CultureInfo culture);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is T2 typedValue))
            {
                if (ReferenceEquals(value, default(T2))) return ConvertBack(default(T2), culture);

                throw new ArgumentException($"{GetType()} can only convert back objects of type {typeof(T2)}, but received type {value.GetType()} with value '{value}'.");
            }


            return ConvertBack(typedValue, culture);
        }

        public virtual T1 ConvertBack(T2 value, CultureInfo culture) => throw new NotSupportedException();
    }
}
