using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using JetBrains.Annotations;

using Prism.Commands;

using Utilities.Collections;

namespace Junctionizer.CustomWpfComponents
{
    /// <summary>Exists so that <see cref="DelegateListCommand{T}"/> can be referenced without specifying a type.</summary>
    public interface IDelegateListCommand : ICommand
    {
        int Count { get; }
        void RaiseCanExecuteChanged();
        bool CanExecute();
        void Execute();
    }

    /// <summary>A command that executes an action on every item in a collection. Maintains a count of the number of items the action will be executed on and returns false for CanExecute if that count is 0. RaiseCanExecuteChanged() must be called whenever it is possible that the number of items has changed.</summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    public class DelegateListCommand<T> : DelegateCommandBase, INotifyPropertyChanged, IDelegateListCommand
    {
        private Action<T> IndividualExecuteMethod { get; }
        private Func<IEnumerable<T>> ApplicableItemsFunc { get; }

        private int _count;
        public int Count
        {
            get => _count;
            private set {
                if (value != _count)
                {
                    _count = value;
                    RaisePropertyChanged();
                }
            }
        }

        public DelegateListCommand(Func<IEnumerable<T>> applicableItemsFunc, [NotNull] Action<T> individualExecuteMethod)
        {
            ApplicableItemsFunc = applicableItemsFunc;
            IndividualExecuteMethod = individualExecuteMethod;
        }

        public void Execute() => ApplicableItemsFunc().ForEach(IndividualExecuteMethod);

        public bool CanExecute() => Count > 0;

        protected override void OnCanExecuteChanged()
        {
            Count = ApplicableItemsFunc().Count();
            base.OnCanExecuteChanged();
        }

        protected override void Execute(object parameter) => Execute();

        protected override bool CanExecute(object parameter) => CanExecute();

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
