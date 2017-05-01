using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

using Junctionizer.CustomWpfComponents;

namespace Junctionizer.UI.UserControls
{
    [ContentProperty(nameof(InnerContent))]
    public partial class ListActionButton
    {
        public ListActionButton()
        {
            InitializeComponent();
        }

        public Button InnerContent
        {
            get => (Button) GetValue(InnerContentProperty);
            set => SetValue(InnerContentProperty, value);
        }

        public static readonly DependencyProperty InnerContentProperty =
            DependencyProperty.Register(nameof(InnerContent), typeof(Button), typeof(ListActionButton));

        public IListCommand Command
        {
            get => (IListCommand) GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.Register(nameof(Command), typeof(IListCommand), typeof(ListActionButton), new FrameworkPropertyMetadata((source, e) => {
                var actionButton = (ListActionButton) source;
                actionButton.InnerContent.Command = actionButton.Command;
            }));
    }
}
