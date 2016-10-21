using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows;
using System.Windows.Input;

using Microsoft.WindowsAPICodePack.Dialogs;

using Prism.Commands;

namespace GameMover.Code
{

    public static class StaticMethods
    {

        internal const string NoItemsSelected = "No folder selected.",
                              InvalidPermission = "Invalid permission";

        public delegate void ErrorHandler(string message, Exception e = null);

        public static ErrorHandler HandleError { get; set; } = (message, exception) => {
            MessageBox.Show(message);
            Debug.WriteLine(exception);
        };

        public static Action<Action> DisplayBusyDuring { get; set; } = action => {
            Mouse.OverrideCursor = Cursors.Wait;
            action();
            Mouse.OverrideCursor = null;
        };

        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
            {
                action(item);
            }
        }

        public static TCommand ObservesCollection<TCommand, TCollection>(this TCommand command,
            Expression<Func<TCollection>> propertyExpression)
            where TCommand : DelegateCommand
            where TCollection : INotifyCollectionChanged
        {
            propertyExpression.Compile()().CollectionChanged += (sender, args) => command.RaiseCanExecuteChanged();
            command.ObservesProperty(propertyExpression);
            return command;
        }

        /// <summary>
        ///     Includes itself and all subdirectories (recursive) that can be opened by the current user.
        /// </summary>
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
                    // Skip system directories that are not the root (eg C:\)
                    if ((info.Attributes & FileAttributes.System) != 0 && info.Parent != null) continue;

                    if (!info.IsReparsePoint())
                    {
                        foreach (var directoryInfo in info.EnumerateDirectories(searchPattern, SearchOption.TopDirectoryOnly))
                        {
                            directoriesToSearch.Push(directoryInfo);
                        }
                    }

                    isAccessible = true;
                }
                catch (UnauthorizedAccessException) {}

                if (isAccessible) yield return info;
            }
        }

        /// <summary>
        ///     Concatenate a single element to the end of an IEnumerable.
        /// </summary>
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> enumerable, T value)
        {
            foreach (var item in enumerable)
            {
                yield return item;
            }

            yield return value;
        }

        /// <summary>
        ///     Wrapper for standard default values for opening a folder picker.
        /// </summary>
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

        public static void HandleIOExceptionsDuring(Action action)
        {
            try
            {
                action();
            }
            catch (IOException e)
            {
                // Handle drive failures
                var message = e.Message;
                var maybeFullPath = e.GetType()
                                     .GetField("_maybeFullPath",
                                         BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (maybeFullPath != null)
                {
                    message += $" \"{maybeFullPath.GetValue(e)}\"";
                }
                HandleError(message, e);
            }
        }

        private const string CountString = "Count";
        private const string IndexerName = "Item[]";

        private static MethodInfo OnPropertyChangedMethodInfo { get; } = typeof(ObservableCollection<object>).GetMethod("OnPropertyChanged",
            BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, new[] {typeof(string)}, null);
        private static MethodInfo OnCollectionChangedMethodInfo { get; } =
            typeof(ObservableCollection<object>).GetMethod("OnCollectionChanged",
                BindingFlags.NonPublic | BindingFlags.Instance, Type.DefaultBinder, new[] {typeof(NotifyCollectionChangedEventArgs)}, null);

        private static PropertyInfo ItemsPropertyInfo { get; } = typeof(ObservableCollection<object>).GetProperty("Items",
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);

        /// <summary>
        /// Clears the current items, adds the provided range, and then sends a single CollectionChanged event.
        /// 
        /// Implemented using reflection on an extension method so that it can be used after data binding to a <see cref="System.Windows.Controls.SelectedItemCollection"/>.
        /// </summary>
        public static void ReplaceWithRange<T>(this ObservableCollection<T> self, IEnumerable<T> newItems)
        {
            var backingItems = (List<T>) ItemsPropertyInfo.GetValue(self);

            backingItems.Clear();

            backingItems.AddRange(newItems);
            OnPropertyChangedMethodInfo.Invoke(self, new object[] {CountString});
            OnPropertyChangedMethodInfo.Invoke(self, new object[] {IndexerName});
            OnCollectionChangedMethodInfo.Invoke(self,
                new object[] {new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset)});
        }

    }

}
