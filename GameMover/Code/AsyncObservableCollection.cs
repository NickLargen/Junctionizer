using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace GameMover.Code
{

    public sealed class AsyncObservableCollection<T> : ObservableCollection<T> where T : class, INotifyPropertyChanged
    {


        public AsyncObservableCollection()
        {
            BindingOperations.EnableCollectionSynchronization(this, new object());

            var listCollectionView = CollectionViewSource.GetDefaultView(this) as ListCollectionView;
            if (listCollectionView != null) listCollectionView.IsLiveSorting = true;
        }
    }

}
