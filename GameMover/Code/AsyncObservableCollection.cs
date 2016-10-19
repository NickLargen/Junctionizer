using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace GameMover.Code
{

    public sealed class AsyncObservableCollection<T> : ObservableCollection<T> where T : INotifyPropertyChanged
    {

        private ICollectionView View { get; }

        public AsyncObservableCollection()
        {
            BindingOperations.EnableCollectionSynchronization(this, new object());
            CollectionChanged += FullObservableCollectionCollectionChanged;
            View = CollectionViewSource.GetDefaultView(this);
        }

        /// <summary>
        ///     Allow automatic dynamic ssorting when item propertiess change.
        /// </summary>
        private void OnItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (View.SortDescriptions.FirstOrDefault().PropertyName == e.PropertyName)
            {
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        private void FullObservableCollectionCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.Cast<INotifyPropertyChanged>())
                {
                    item.PropertyChanged -= OnItemPropertyChanged;
                }
            }
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems.Cast<INotifyPropertyChanged>())
                {
                    item.PropertyChanged += OnItemPropertyChanged;
                }
            }
        }

    }

}
