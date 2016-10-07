using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Windows;

using Prism.Commands;

namespace GameMover
{

    internal static class StaticMethods
    {

        internal const string NoItemsSelected = "No folder selected.",
                              InvalidPermission = "Invalid permission";


        public delegate void ErrorDisplayer(string message, Exception e = null);

        public static ErrorDisplayer DisplayError = (message, exception) => {
            MessageBox.Show(message);
            Debug.WriteLine(exception);
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

    }

}
