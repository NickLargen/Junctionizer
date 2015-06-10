using System;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace GameMover
{
    internal static class StaticMethods
    {
        internal const string NoItemsSelected = "No folder selected.",
            InvalidPermission = "Invalid permission";


        public static void ShowMessage(string message)
        {
            MessageBox.Show(message);
        }

        internal static FolderBrowserDialog CreateFolderBrowserDialog(string defaultSelectedPath) {
            var folderBrowserDialog = new FolderBrowserDialog {
                Description = "Select directory containing your game folders.",
                SelectedPath = defaultSelectedPath,
                RootFolder = Environment.SpecialFolder.MyComputer
            };
            return folderBrowserDialog;
        }

    }
}