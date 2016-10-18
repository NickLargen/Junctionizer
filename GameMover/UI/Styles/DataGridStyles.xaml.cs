using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;

using GameMover.Model;

namespace GameMover.UI.Styles
{
    public partial class DataGridStyles
    {
        private void DataGridRow_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            var dataGridRow = sender as DataGridRow;
            var folder = dataGridRow?.Item as GameFolder;
            if (folder != null) Process.Start(folder.DirectoryInfo.FullName);
        }
    }
}
