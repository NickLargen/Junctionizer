using System;
using System.IO;
using System.Threading.Tasks;

using MaterialDesignThemes.Wpf;

using Microsoft.WindowsAPICodePack.Dialogs;

using Prism.Interactivity.InteractionRequest;

namespace Junctionizer
{
    public struct Message
    {
        public Message(string content)
        {
            Content = content;
        }

        public string Content { get; }
    }

    public static class Dialogs
    {
        public static InteractionRequest<INotification> CloseDialogRequest { get; } = new InteractionRequest<INotification>();

        public static void CloseDialog()
        {
            CloseDialogRequest.Raise(null);
        }

        public static Task<object> Show(object content, DialogOpenedEventHandler openedEventHandler = null, DialogClosingEventHandler closingEventHandler = null)
        {
            CloseDialog();
            return DialogHost.Show(content, openedEventHandler, closingEventHandler);
        }

        /// <summary>Can only display one dialog at a time</summary>
        public static Func<string, Task<object>> DisplayMessageBox { get; set; } = message => Show(new Message(message));

        /// <summary>Wrapper for standard default values for opening a folder picker.</summary>
        public static CommonOpenFileDialog NewFolderDialog(string title)
        {
            return new CommonOpenFileDialog {
                Title = title,
                IsFolderPicker = true,
                AllowNonFileSystemItems = true,
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureReadOnly = false,
                EnsureValidNames = true,
                Multiselect = false,
                ShowPlacesList = true,
                ShowHiddenItems = false
            };
        }

        /// <summary>Shows an error message and then reprompts if the user selects an invalid entry. Returns null iff the dialog is cancelled.</summary>
        public static async Task<DirectoryInfo> PromptForDirectory(string dialogTitle, string initialDirectory = null)
        {
            var folderDialog = NewFolderDialog(dialogTitle);
            folderDialog.InitialDirectory = initialDirectory;

            while (folderDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                try
                {
                    return new DirectoryInfo(folderDialog.FileName);
                }
                catch (Exception)
                {
                    await DisplayMessageBox("Unable to use the selected location - did you select a valid directory path? Try choosing a more specific location.");
                }
            }

            return null;
        }
    }
}
