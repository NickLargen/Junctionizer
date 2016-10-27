using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

using NUnit.Framework;

using Utilities;
using Utilities.Collections;

#pragma warning disable 168

namespace Test
{
    public class ConcreteKeyedList : KeyedCollection<float, int>
    {
        /// <inheritdoc />
        protected override float GetKeyForItem(int item)
        {
            throw new NotImplementedException();
        }

    }

    // When using a numerical key with KeyedCollection this[] functions as a dictionary, so the only way to retrieve an item by index is to cast to IList
    // This is seems like it would frequently NOT be what you expect - if your key is a long and use an int to try and index, it still uses it as a key

    public class TemporaryTests
    {
        [Test]
        public void Collections()
        {
        }

        [Test]
        public void Unnamed()
        {
            /*var hashSet = new HashSet<string>();
            hashSet.Add("cat");
            hashSet.Add("bear");
            hashSet.Add("lion");
            hashSet.Add("dog");
            hashSet.Add("elephant");*/

            HashSet<int> hashSet;
            SortedSet<string> sortedSet;
            SortedList<string, string> sortedList;


            ImmutableList<int> immutableList;
            ImmutableQueue<int> immutableQueue;
            ImmutableStack<int> immutableStack;
            ImmutableHashSet<int> immutableHashSet;
            ImmutableSortedSet<int> immutableSortedSet;
            ImmutableDictionary<int, string> immutableDictionary;
            ImmutableSortedDictionary<int, string> immutableSortedDictionary;
        }
    }
}
