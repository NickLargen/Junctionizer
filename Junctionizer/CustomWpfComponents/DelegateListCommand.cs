﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;

using JetBrains.Annotations;

using Prism.Commands;

namespace Junctionizer.CustomWpfComponents
{
    /// <summary>Exists so that <see cref="DelegateListCommandBase{T}"/> can be referenced without specifying a type.</summary>
    public interface IDelegateListCommand : ICommand
    {
        int Count { get; }
        void RaiseCanExecuteChanged();
        bool CanExecute();
        void Execute();
    }

    /// <summary>A command that executes an action on every item in a collection. Maintains a count of the number of items the action will be executed on and returns false for CanExecute if that count is 0. RaiseCanExecuteChanged() must be called whenever it is possible that the number of items has changed.</summary>
    /// <typeparam name="T">The type of items in the list.</typeparam>
    public abstract class DelegateListCommandBase<T> : DelegateCommandBase, INotifyPropertyChanged, IDelegateListCommand
    {
        protected Func<IEnumerable<T>> ApplicableItemsFunc { get; }

        protected DelegateListCommandBase(Func<IEnumerable<T>> applicableItemsFunc)
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

        protected override void OnCanExecuteChanged()
        {
            Count = ApplicableItemsFunc().Count();
            base.OnCanExecuteChanged();
        }

        protected override bool CanExecute(object parameter) => Count > 0;

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
