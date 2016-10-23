using System.Collections.ObjectModel;
using System.Windows.Data;

namespace GameMover.Code
{
    public sealed class AsyncObservableCollection<T> : ObservableCollection<T>
    {
        public AsyncObservableCollection()
        {
            BindingOperations.EnableCollectionSynchronization(this, new object());

            var listCollectionView = CollectionViewSource.GetDefaultView(this) as ListCollectionView;
            if (listCollectionView != null) listCollectionView.IsLiveSorting = true;
        }
    }
}
