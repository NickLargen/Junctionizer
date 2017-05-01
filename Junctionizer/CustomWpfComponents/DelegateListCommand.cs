using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using JetBrains.Annotations;

namespace Junctionizer.CustomWpfComponents
{
    /// <summary>Exists so that <see cref="ListCommandBase{T}"/> can be referenced without specifying a type.</summary>
    public interface IListCommand : ICommand
    {
        int Count { get; }
        bool CanExecute();
        void Execute();
    }

    /// <summary>A command that executes an action on every item in a collection. Maintains a count of the number of items the action will be executed on and returns false for CanExecute if that count is 0. </summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    public abstract class ListCommandBase<T> : INotifyPropertyChanged, IListCommand
    {
        protected Func<IEnumerable<T>> ApplicableItemsFunc { get; }

        protected ListCommandBase(Func<IEnumerable<T>> applicableItemsFunc)
        {
            ApplicableItemsFunc = applicableItemsFunc;
        }

        private int _count;
        public int Count
        {
            get => _count;
            protected set {
                if (value != _count)
                {
                    _count = value;
                    RaisePropertyChanged();
                }
            }
        }

        public bool CanExecute(object parameter)
        {
            Count = ApplicableItemsFunc().Count();
            return Count > 0;
        }

        public abstract void Execute(object parameter);

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute() => CanExecute(null);
        public void Execute() => Execute(null);

        public virtual event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
