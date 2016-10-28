using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

#pragma warning disable 168

namespace UtilitiesTests.Temporary
{
    // When using a numerical key with KeyedCollection this[] functions as a dictionary, so the only way to retrieve an item by index is to cast to IList
    // This is seems like it would frequently NOT be what you expect - if your key is a long and use an int to try and index, it still uses it as a key

    public class TemporaryTests
    {
        public struct FlatStruct
        {
            public int first, second, third;
        }

        public struct TwoStruct
        {
            public int one, two;
        }

        public struct NestedStruct
        {
            public TwoStruct innerStruct;
            public int three;
        }

        [Test]
        public void StructSizes()
        {
            long StartBytes, StopBytes;
            var count = 20000;
            StartBytes = System.GC.GetTotalMemory(true);
            FlatStruct[] a = new FlatStruct[count];
            StopBytes = System.GC.GetTotalMemory(true);
            Console.WriteLine(StopBytes - StartBytes);

            StartBytes = System.GC.GetTotalMemory(true);
            NestedStruct[] b = new NestedStruct[count];
            StopBytes = System.GC.GetTotalMemory(true);
             Console.WriteLine(StopBytes - StartBytes);
                
        }

        /*[Test]
        public void Unnamed()
        {
            /*var hashSet = new HashSet<string>();
            hashSet.Add("cat");
            hashSet.Add("bear");
            hashSet.Add("lion");
            hashSet.Add("dog");
            hashSet.Add("elephant");#1#

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
        }*/
    }
}
