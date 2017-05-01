using System.Windows;
using System.Windows.Markup;

using Junctionizer.CustomWpfComponents;

namespace Junctionizer.UI.UserControls
{
    /// <summary>Interaction logic for ListActionButton.xaml</summary>
    [ContentProperty(nameof(ButtonContent))]
    public partial class ListActionButton
    {
        public ListActionButton()
        {
            InitializeComponent();
        }

        public object ButtonContent
        {
            get => GetValue(ButtonContentProperty);
            set => SetValue(ButtonContentProperty, value);
        }

        public static readonly DependencyProperty ButtonContentProperty =
            DependencyProperty.Register(nameof(ButtonContent), typeof(object), typeof(ListActionButton));


        public IListCommand Command
        {
            get => (IListCommand) GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(IListCommand), typeof(ListActionButton));


        public Style ButtonStyle
        {
            get => (Style) GetValue(ButtonStyleProperty);
            set => SetValue(ButtonStyleProperty, value);
        }

        public static readonly DependencyProperty ButtonStyleProperty =
            DependencyProperty.Register(nameof(ButtonStyle), typeof(Style), typeof(ListActionButton));
    }
}
