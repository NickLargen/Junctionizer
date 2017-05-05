using Prism.Mvvm;

namespace Junctionizer.ViewModels
{
    public class ViewModelBase : BindableBase
    {
        // Allows easy binding in xaml
        public UISettings UISettings { get; } = UISettings.Instance;
    }
}
