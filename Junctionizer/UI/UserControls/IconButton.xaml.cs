using System.Windows;

using MaterialDesignThemes.Wpf;

namespace Junctionizer.UI.UserControls
{
    /// <summary>Interaction logic for IconButton.xaml</summary>
    public partial class IconButton
    {
        public IconButton()
        {
            InitializeComponent();
        }

        public string Text
        {
            get => (string) GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(IconButton));

        public PackIconKind IconKind
        {
            get => (PackIconKind) GetValue(IconKindProperty);
            set => SetValue(IconKindProperty, value);
        }

        public static readonly DependencyProperty IconKindProperty =
            DependencyProperty.Register(nameof(IconKind), typeof(PackIconKind), typeof(IconButton));
    }
}
