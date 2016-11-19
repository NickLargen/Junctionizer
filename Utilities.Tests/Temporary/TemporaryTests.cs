using System;

using NUnit.Framework;

using TestingUtilities;

namespace Utilities.Tests.Temporary
{
    // When using a numerical key with KeyedCollection this[] functions as a dictionary, so the only way to retrieve an item by index is to cast to IList
    // This is seems like it would frequently NOT be what you expect - if your key is a long and use an int to try and index, it still uses it as a key

    public class TemporaryTests : ExtendedAssertionHelper
    {

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

        [Test]
        public void StructSizes()
        {
            //Findings: nested structs have exactly the same memory usage as a flat struct

            long startBytes, stopBytes;
            var count = 20000;
            startBytes = GC.GetTotalMemory(true);
            FlatStruct[] a = new FlatStruct[count];
            stopBytes = GC.GetTotalMemory(true);
            var flatStructBytes = stopBytes - startBytes;
            Console.WriteLine(flatStructBytes);

            startBytes = GC.GetTotalMemory(true);
            NestedStruct[] b = new NestedStruct[count];
            stopBytes = GC.GetTotalMemory(true);
            var nestedStructBytes = stopBytes - startBytes;
            Console.WriteLine(nestedStructBytes);

            Ensure(flatStructBytes, EqualTo(nestedStructBytes));
        }

        public struct FlatStruct
        {
            public int First, Second, Third;
        }

        public struct TwoStruct
        {
            public int One, Two;
        }

        public struct NestedStruct
        {
            public TwoStruct InnerStruct;
            public int Three;
        }
    }
}
