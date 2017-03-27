using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Input;

using Microsoft.WindowsAPICodePack.Dialogs;

using Prism.Commands;

namespace Junctionizer
{
    public static class StaticMethods
    {
        /// <summary>Whether or not to prevent a directory that is being monitored from being renamed or deleted.</summary>
        public static bool LockActiveDirectory { get; set; } = true;

        public static Action<Action> DisplayBusyDuring { get; set; } = action => {
            Mouse.OverrideCursor = Cursors.Wait;
            action();
            Mouse.OverrideCursor = null;
        };

        public static TCommand ObservesCollection<TCommand, TCollection>(this TCommand command,
            Expression<Func<TCollection>> propertyExpression)
            where TCommand : DelegateCommand
            where TCollection : INotifyCollectionChanged
        {
            propertyExpression.Compile()().CollectionChanged += (sender, args) => command.RaiseCanExecuteChanged();
            command.ObservesProperty(propertyExpression);
            return command;
        }

        /// <summary>Includes itself and all subdirectories (recursive) that can be opened by the current user.</summary>
        public static IEnumerable<DirectoryInfo> EnumerateAllAccessibleDirectories(this DirectoryInfo self, string searchPattern = "*")
        {
            var directoriesToSearch = new Stack<DirectoryInfo>(64);
            directoriesToSearch.Push(self);

            while (directoriesToSearch.Count != 0)
            {
                var info = directoriesToSearch.Pop();
                if (info.FullName.Length > 255) continue;

                var isAccessible = false;
                try
                {
                    // Skip system directories that are not the root (eg C:\). Newly created directories (detected from a FileSystemWatcher Created event) can have their attributes initialized to -1- assuming they are accessible is not technically correct, but should function fine for our current use case.
                    if ((info.Attributes & FileAttributes.System) != 0 && (int) info.Attributes != -1 && info.Parent != null) continue;

                    if (!info.IsReparsePoint())
                    {
                        foreach (var directoryInfo in info.EnumerateDirectories(searchPattern, SearchOption.TopDirectoryOnly))
                        {
                            directoriesToSearch.Push(directoryInfo);
                        }
                    }

                    isAccessible = true;
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException) {}

                if (isAccessible) yield return info;
            }
        }

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

        /// <summary>Clears the current items, adds the provided range. Implemented using reflection on an extension method so that it can be used after data binding to a <see cref="System.Windows.Controls.SelectedItemCollection"/>. Currently sends a CollectionChanged event for every item added.</summary>
        public static void ReplaceSelectedItems(this ObservableCollection<object> self, IEnumerable<object> newItems)
        {
            var type = self.GetType();
            bool isSelectedItemCollection = type.Name == "SelectedItemCollection";
            if(isSelectedItemCollection) type.GetMethod("BeginUpdateSelectedItems", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(self, Array.Empty<object>());
            self.Clear();
            foreach (var item in newItems)
            {
                self.Add(item);
            }

            if (isSelectedItemCollection) type.GetMethod("EndUpdateSelectedItems", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(self, Array.Empty<object>());
        }
    }
}
