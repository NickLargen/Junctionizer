using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;

namespace GameMover.External_Code
{

    public class AsyncObservableCollection<T> : ObservableCollection<T>
    {

        private readonly SynchronizationContext _synchronizationContext = SynchronizationContext.Current;

        public AsyncObservableCollection() {}

        public AsyncObservableCollection(IEnumerable<T> list)
            : base(list) {}

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (SynchronizationContext.Current == _synchronizationContext)
            {
                // Execute the CollectionChanged event on the current thread
                BaseOnCollectionChanged(e);
            }
            else
            {
                // Raises the CollectionChanged event on the creator thread
                _synchronizationContext.Send(BaseOnCollectionChanged, e);
            }
        }

        private void BaseOnCollectionChanged(object param)
        {
            // We are in the creator thread, call the base implementation directly
            base.OnCollectionChanged((NotifyCollectionChangedEventArgs) param);
        }

        public void RaiseCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CheckReentrancy();

            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(e);
        }

//        public void RaisePropertyChanged(PropertyChangedEventArgs e) => OnPropertyChanged(e);

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (SynchronizationContext.Current == _synchronizationContext)
            {
                // Execute the PropertyChanged event on the current thread
                BaseOnPropertyChanged(e);
            }
            else
            {
                // Raises the PropertyChanged event on the creator thread
                _synchronizationContext.Send(BaseOnPropertyChanged, e);
            }
        }

        private void BaseOnPropertyChanged(object param)
        {
            // We are in the creator thread, call the base implementation directly
            base.OnPropertyChanged((PropertyChangedEventArgs) param);
        }

    }

}
