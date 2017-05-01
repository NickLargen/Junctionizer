using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using Junctionizer.Model;

namespace Junctionizer.UI.Columns
{
    public partial class DependentOnFinalSizeColumn
    {
        public DependentOnFinalSizeColumn()
        {
            InitializeComponent();
        }

        public override BindingBase Binding
        {
            get => base.Binding;

            set {
                base.Binding = value;

                var path = ((Binding) Binding).Path.Path;
                var lastDotIndex = path.LastIndexOf('.');
                var pathToFolder = lastDotIndex < 0 ? string.Empty : path.Substring(0, lastDotIndex + 1);

                AddCellStyle(pathToFolder);
            }
        }

        private void AddCellStyle(string pathToFolder)
        {
            CellStyle = new Style(typeof(DataGridCell), (Style) Application.Current.FindResource("RightAlignCell"));
            var trigger = new DataTrigger {
                Binding = new Binding(pathToFolder + nameof(GameFolder.IsSizeOutdated)),
                Value = "True"
            };

            trigger.Setters.Add(new Setter(FontStyleProperty, FontStyles.Italic));
            trigger.Setters.Add(new Setter(FontWeightProperty, FontWeights.SemiBold));

            CellStyle.Triggers.Add(trigger);
        }
    }
}
