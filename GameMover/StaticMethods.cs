using System.Windows;

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
    }
}