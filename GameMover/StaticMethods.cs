using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public static void TraverseBackwards<T>(this IList list, Action<T> action) {
            //Iterate backwards so that the supplied action can remove elements from the list
            for (int i = list.Count - 1; i >= 0; i--) {
                action((T) list[i]);
            }
        }

        public static void TraverseBackwards<T>(this IList<T> list, Action<T> action) {
            for (int i = list.Count - 1; i >= 0; i--) {
                action(list[i]);
            }
        }

    }
}