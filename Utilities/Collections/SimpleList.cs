using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Utilities.Collections
{
    [DebuggerDisplay("Count = {" + nameof(Count) + "}")]
    public class SimpleList<T> : IList<T>, IReadOnlyList<T>
    {
        private const int DEFAULT_CAPACITY = 4;

        private const int MAX_ARRAY_LENGTH = 0X7FEFFFFF;

        private T[] Items { get; set; }
        private int Version { get; set; }

        public int Count { get; private set; }

        /// <summary>Constructs a list of capacity 0.</summary>
        public SimpleList() : this(0) {}

        /// <summary>Constructs a List with a given initial capacity. The list is initially empty, but will have room for the given number of elements before any reallocations are required.</summary>
        public SimpleList(int capacity)
        {
            Items = new T[capacity];
        }

        /// <summary>Gets and sets the capacity of this list.  The capacity is the size of the internal array used to hold items.  When set, the internal array of the list is reallocated to the given capacity.</summary>
        public int Capacity
        {
            get { return Items.Length; }
            set {
                if (value < Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(Capacity), "Capacity cannot be smaller than the current count.");
                }

                if (value != Items.Length)
                {
                    T[] newItems = new T[value];
                    if (Count > 0)
                    {
                        Array.Copy(Items, 0, newItems, 0, Count);
                    }
                    Items = newItems;
                }
            }
        }

        bool ICollection<T>.IsReadOnly => false;

        public T this[int index]
        {
            get {
                // Following trick can reduce the range check by one
                if ((uint) index >= (uint) Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                return Items[index];
            }

            set {
                if ((uint) index >= (uint) Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                Items[index] = value;
                Version++;
            }
        }

        // Adds the given object to the end of this list. The size of the list is
        // increased by one. If required, the capacity of the list is doubled
        // before adding the new element.
        //
        public void Add(T item)
        {
            if (Count == Items.Length) EnsureCapacity(Count + 1);
            Items[Count++] = item;
            Version++;
        }

        // Adds the elements of the given collection to the end of this list. If
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.
        //
        public void AddRange(IEnumerable<T> collection) => InsertRange(Count, collection);

        /// <summary>
        /// Adds the specified number of default(<typeparamref name="T"/>) elements to the end of this list.
        /// </summary>
        public void AppendEmptyElements(int count)
        {
            EnsureCapacity(Count + count);
            Count += count;
        }

        public ReadOnlyCollection<T> AsReadOnly() => new ReadOnlyCollection<T>(this);

        // Searches a section of the list for a given element using a binary search
        // algorithm. Elements of the list are compared to the search value using
        // the given IComparer interface. If comparer is null, elements of
        // the list are compared to the search value using the IComparable
        // interface, which in that case must be implemented by all elements of the
        // list and the given search value. This method assumes that the given
        // section of the list is already sorted; if this is not the case, the
        // result will be incorrect.
        //
        // The method returns the index of the given value in the list. If the
        // list does not contain the given value, the method returns a negative
        // integer. The bitwise complement operator (~) can be applied to a
        // negative result to produce the index of the first element (if any) that
        // is larger than the given search value. This is also the index at which
        // the search value should be inserted into the list in order for the list
        // to remain sorted.
        // 
        // The method uses the Array.BinarySearch method to perform the
        // search.
        // 
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
            => Array.BinarySearch<T>(Items, index, count, item, comparer);

        public int BinarySearch(T item) => BinarySearch(0, Count, item, null);

        public int BinarySearch(T item, IComparer<T> comparer) => BinarySearch(0, Count, item, comparer);

        // Clears the contents of List.
        public void Clear()
        {
            if (Count > 0)
            {
                Array.Clear(Items, 0, Count); // Don't need to do this but we clear the elements so that the gc can reclaim the references.
                Count = 0;
                Version++;
            }
        }

        // Contains returns true if the specified element is in the List.
        // It does a linear, O(n) search.  Equality is determined by calling
        // item.Equals().
        //
        public bool Contains(T item)
        {
            if (item == null)
            {
                for (int i = 0; i < Count; i++) if (Items[i] == null) return true;

                return false;
            }
            else
            {
                EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
                for (int i = 0; i < Count; i++)
                {
                    if (equalityComparer.Equals(Items[i], item)) return true;
                }

                return false;
            }
        }

        public void CopyTo(T[] array) => CopyTo(array, 0);

        public void CopyTo(T[] array, int arrayIndex) => Array.Copy(Items, 0, array, arrayIndex, Count);

        // Ensures that the capacity of this list is at least the given minimum
        // value. If the currect capacity of the list is less than min, the
        // capacity is increased to twice the current capacity or to min,
        // whichever is larger.
        public void EnsureCapacity(int min)
        {
            if (Items.Length < min)
            {
                int newCapacity = Items.Length == 0 ? DEFAULT_CAPACITY : Items.Length * 2;
                // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
                // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
                if ((uint) newCapacity > MAX_ARRAY_LENGTH) newCapacity = MAX_ARRAY_LENGTH;
                if (newCapacity < min) newCapacity = min;
                Capacity = newCapacity;
            }
        }

        // Returns an enumerator for this list with the given
        // permission for removal of elements. If modifications made to the list 
        // while an enumeration is in progress, the MoveNext and 
        // GetObject methods of the enumerator will throw an exception.
        //
        public Enumerator GetEnumerator() => new Enumerator(this);

        /// <internalonly/>
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => new Enumerator(this);

        // Returns the index of the first occurrence of a given value in a range of
        // this list. The list is searched forwards from beginning to end.
        // The elements of the list are compared to the given value using the
        // Object.Equals method.
        // 
        // This method uses the Array.IndexOf method to perform the
        // search.
        // 
        public int IndexOf(T item) => Array.IndexOf(Items, item, 0, Count);

        // Inserts an element into this list at a given index. The size of the list
        // is increased by one. If required, the capacity of the list is doubled
        // before inserting the new element.
        // 
        public void Insert(int index, T item)
        {
            // Note that insertions at the end are legal.
            if ((uint) index > (uint) Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (Count == Items.Length) EnsureCapacity(Count + 1);
            if (index < Count)
            {
                Array.Copy(Items, index, Items, index + 1, Count - index);
            }
            Items[index] = item;
            Count++;
            Version++;
        }

        // Inserts the elements of the given collection at a given index. If
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.  Ranges may be added
        // to the end of the list by setting index to the List's size.
        //
        public void InsertRange(int index, IEnumerable<T> enumerable)
        {
            if ((uint) index > (uint) Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            if (enumerable is ICollection<T> collection)
            {
                int count = collection.Count;
                if (count > 0)
                {
                    EnsureCapacity(Count + count);
                    if (index < Count)
                    {
                        Array.Copy(Items, index, Items, index + count, Count - index);
                    }

                    // If we're inserting a List into itself, we want to be able to deal with that.
                    if (ReferenceEquals(this, collection))
                    {
                        // Copy first part of _items to insert location
                        Array.Copy(Items, 0, Items, index, index);
                        // Copy last part of _items back to inserted location
                        Array.Copy(Items, index + count, Items, index * 2, Count - index);
                    }
                    else
                    {
                        T[] itemsToInsert = new T[count];
                        collection.CopyTo(itemsToInsert, 0);
                        itemsToInsert.CopyTo(Items, index);
                    }
                    Count += count;
                }
            }
            else
            {
                foreach (var item in enumerable)
                {
                    Insert(index++, item);
                }
            }

            Version++;
        }

        /// <summary>
        /// An efficient implementation of
        /// <code><para/>
        /// var item = list[index];
        /// list.RemoveAt(index);
        /// list.Insert(destinationIndex, item);</code><para/>
        /// Note for sorting purposes: if index &lt; destinationIndex the item at index will appear AFTER the item at destinationIndex after this operation, while if index > destinationIndex it will appear BEFORE it
        /// </summary>
        public void Move(int index, int destinationIndex)
        {
            if (index >= Count || destinationIndex >= Count) throw new ArgumentOutOfRangeException();

            if (index == destinationIndex) return;

            var itemToMove = Items[index];

            if (index < destinationIndex)
            {
                Array.Copy(Items, index + 1, Items, index, destinationIndex - index);
            }
            else
            {
                Array.Copy(Items, destinationIndex, Items, destinationIndex + 1, index - destinationIndex);
            }
            Items[destinationIndex] = itemToMove;
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one.
        // 
        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one.
        // 
        public void RemoveAt(int index)
        {
            if ((uint) index >= (uint) Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            Count--;
            if (index < Count)
            {
                Array.Copy(Items, index + 1, Items, index, Count - index);
            }
            Items[Count] = default(T);
            Version++;
        }

        // Sorts the elements in this list.  Uses the default comparer and 
        // Array.Sort.
        public void Sort() => Sort(0, Count, null);

        // Sorts the elements in this list.  Uses Array.Sort with the
        // provided comparer.
        public void Sort(IComparer<T> comparer) => Sort(0, Count, comparer);

        // Sorts the elements in a section of this list. The sort compares the
        // elements to each other using the given IComparer interface. If
        // comparer is null, the elements are compared to each other using
        // the IComparable interface, which in that case must be implemented by all
        // elements of the list.
        // 
        // This method uses the Array.Sort method to sort the elements.
        // 
        public void Sort(int index, int count, IComparer<T> comparer)
        {
            if (Count - index < count) throw new ArgumentException();

            Array.Sort<T>(Items, index, count, comparer);
            Version++;
        }

        public void Sort(Comparison<T> comparison)
        {
            if (Count > 0)
            {
                Array.Sort(Items, 0, Count, Comparer<T>.Create(comparison));
            }
        }

        // Sets the capacity of this list to the size of the list. This method can
        // be used to minimize a list's memory overhead once it is known that no
        // new elements will be added to the list. To completely clear a list and
        // release all memory referenced by the list, execute the following
        // statements:
        // 
        // list.Clear();
        // list.TrimExcess();
        // 
        public void TrimExcess()
        {
            int threshold = (int) ((double) Items.Length * 0.9);
            if (Count < threshold)
            {
                Capacity = Count;
            }
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly SimpleList<T> _list;
            private int _index;
            private readonly int _version;
            private T _current;

            internal Enumerator(SimpleList<T> list)
            {
                _list = list;
                _index = 0;
                _version = list.Version;
                _current = default(T);
            }

            public void Dispose() {}

            public bool MoveNext()
            {
                SimpleList<T> localList = _list;

                if (_version == localList.Version && (uint) _index < (uint) localList.Count)
                {
                    _current = localList.Items[_index];
                    _index++;
                    return true;
                }

                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                if (_version != _list.Version)
                {
                    throw new InvalidOperationException();
                }

                _index = _list.Count + 1;
                _current = default(T);
                return false;
            }

            public T Current => _current;

            object System.Collections.IEnumerator.Current
            {
                get {
                    if (_index == 0 || _index == _list.Count + 1)
                    {
                        throw new InvalidOperationException();
                    }

                    return Current;
                }
            }

            void System.Collections.IEnumerator.Reset()
            {
                if (_version != _list.Version)
                {
                    throw new InvalidOperationException();
                }

                _index = 0;
                _current = default(T);
            }
        }
    }
}
