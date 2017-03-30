using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Input;

using Junctionizer.Model;

namespace Junctionizer.UI.Styles
{
    public partial class DataGridStyles
    {
        private void DataGridRow_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;

            var dataGridRow = sender as DataGridRow;

            if (dataGridRow?.Item is GameFolder folder)
            {
                OpenInFileExplorer(folder);
            }
            else if (dataGridRow?.Item is MergedItem mergedItem)
            {
                if (mergedItem.SourceEntry?.IsJunction == false) OpenInFileExplorer(mergedItem.SourceEntry);
                if (mergedItem.DestinationEntry?.IsJunction == false) OpenInFileExplorer(mergedItem.DestinationEntry);
            }
        }

        private static void OpenInFileExplorer(GameFolder folder)
        {
            var path = folder.DirectoryInfo.FullName;
            ErrorHandling.ThrowIfDirectoryNotFound(path);
            Process.Start(path);
        }
    }
}
