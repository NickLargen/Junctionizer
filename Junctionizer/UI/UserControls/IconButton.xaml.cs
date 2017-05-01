using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

using MaterialDesignThemes.Wpf;

namespace Junctionizer.UI.UserControls
{
    /// <summary>Interaction logic for IconButton.xaml</summary>
    [ContentProperty(nameof(Text))]
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

        public Dock IconPosition 
        {
            get => (Dock) GetValue(IconPositionProperty);
            set => SetValue(IconPositionProperty, value);
        }

        public static readonly DependencyProperty IconPositionProperty =
            DependencyProperty.Register(nameof(IconPosition), typeof(Dock), typeof(IconButton), new UIPropertyMetadata(Dock.Left));
    }
}
