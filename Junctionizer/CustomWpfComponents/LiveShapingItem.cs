using System.Windows;
using System.Windows.Data;

namespace Junctionizer.CustomWpfComponents
{
    public class LiveShapingItem<T> : DependencyObject
    {
        /// <summary>Provides enough thread safety for the desired purpose</summary>
        private volatile bool _isSortDirty;

        public LiveShapingItem(T item)
        {
            Item = item;
        }

        public bool IsSortDirty
        {
            get { return _isSortDirty; }
            set { _isSortDirty = value; }
        }
        public T Item { get; }

        public void AddBinding(string path, DependencyProperty dp)
        {
            BindingOperations.SetBinding(this, dp, new Binding(path) {Source = Item});
        }

        public void RemoveBinding(DependencyProperty dp)
        {
            BindingOperations.ClearBinding(this, dp);
        }

        /// <inheritdoc/>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
            base.OnPropertyChanged(e);
        }

        public static explicit operator T(LiveShapingItem<T> item) => item.Item;

        public event PropertyChangedCallback PropertyChanged;
    }
}