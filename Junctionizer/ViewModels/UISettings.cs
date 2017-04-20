using System.Linq;
using System.Reflection;

using PropertyChanged;

namespace Junctionizer.ViewModels
{
    [ImplementPropertyChanged]
    public class UISettings
    {
        public static bool IsCompactInterface { get; set; } = true;

        private UISettings()
        {
            var propertyNames = GetType()
                .GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static)
                .Where(propertyInfo => propertyInfo.PropertyType != typeof(UISettings))
                .Select(propertyInfo => propertyInfo.Name)
                .ToArray();

            Settings.StateTracker
                    .Configure(this)
                    .AddProperties(propertyNames)
                    .Apply();
        }

        public static UISettings Instance { get; } = new UISettings();
    }
}
