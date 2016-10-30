using System;
using System.Collections;
using System.Collections.Generic;

namespace Utilities.Collections.KeyedSet
{
    public partial class KeyedIndexedSet<TKey, TItem>
    {
        #region Implementation of IList

        IEnumerator IEnumerable.GetEnumerator() => Items.GetEnumerator();

        void ICollection.CopyTo(Array array, int index) => ((ICollection) Items).CopyTo(array, index);

        bool ICollection.IsSynchronized => ((ICollection) Items).IsSynchronized;

        object ICollection.SyncRoot => ((ICollection) Items).SyncRoot;

        int IList.Add(object value)
        {
            Add((TItem) value);
            return Count - 1;
        }

        void IList.RemoveAt(int index) => RemoveAt(index);

        bool IList.Contains(object value) => ContainsItem((TItem) value);

        int IList.IndexOf(object value) => IndexOf((TItem) value);

        void IList.Insert(int index, object value) => Insert(index, (TItem) value);

        void IList.Remove(object value) => RemoveItem((TItem) value);

        object IList.this[int index]
        {
            get { return GetAt(index); }
            set { ReplaceAt(index, (TItem) value); }
        }

        #endregion


        #region Implementation of IList<TItem>

        void ICollection<TItem>.Add(TItem item) => Add(item);

        bool ICollection<TItem>.Contains(TItem item) => ContainsItem(item);

        void ICollection<TItem>.CopyTo(TItem[] array, int arrayIndex) => Items.CopyTo(array, arrayIndex);

        void IList<TItem>.Insert(int index, TItem item) => Insert(index, item);

        bool ICollection<TItem>.Remove(TItem item) => RemoveItem(item);

        void IList<TItem>.RemoveAt(int index) => RemoveAt(index);

        TItem IList<TItem>.this[int index]
        {
            get { return GetAt(index); }
            set { ReplaceAt(index, value); }
        }

        #endregion


        #region Implementation of IReadOnlyList<out TItem>

        TItem IReadOnlyList<TItem>.this[int index] => GetAt(index);

        #endregion
    }
}
