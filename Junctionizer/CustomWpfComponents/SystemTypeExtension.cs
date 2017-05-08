using System;
using System.Windows.Markup;

namespace Junctionizer.CustomWpfComponents
{
    public class SystemTypeExtension : MarkupExtension
    {
        private object _parameter;

        public int Int
        {
            set => _parameter = value;
        }
        public double Double
        {
            set => _parameter = value;
        }
        public bool Bool
        {
            set => _parameter = value;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return _parameter;
        }
    }
}
