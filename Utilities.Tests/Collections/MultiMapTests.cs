using System.Collections.Generic;

using NUnit.Framework;

using Utilities.Collections;
using Utilities.Testing;


namespace Utilities.Tests.Collections
{
    public class MultiMapTests : ExtendedAssertionHelper
    {
        [Test]
        public void Add()
        {
            var intMultiMap = new MultiMap<int, int> {{1, 2}, {5, 252}, {4363461, 2}};
            Ensure(intMultiMap.Count, Is.EqualTo(3), "Adding individual items failed...");

            intMultiMap.Add(1, 34);
            intMultiMap.Add(1, 35);
            Ensure(intMultiMap, Count.EqualTo(5), "Count should reflect the number of values, not the number of keys.");

            intMultiMap.Add(1, 35);
            intMultiMap.Add(1, 35);
            Ensure(intMultiMap, Count.EqualTo(7), "Duplicate values should be allowed.");
        }

        [Test]
        public void BasicContainsBehavior()
        {
            var intMultiMap = new MultiMap<int, int> {{1, 2}, {2, 13}, {2, 15}, {5, 252}, {3461, 4256}};

            Ensure(intMultiMap.Contains(1, 2));
            Ensure(intMultiMap.Contains(2, 13));
            Ensure(intMultiMap.Contains(2, 15));
            Ensure(intMultiMap.Contains(2, 16), False);

            Ensure(intMultiMap.ContainsKey(2));
            Ensure(intMultiMap.ContainsKey(3), False);
        }

        [Test]
        public void ContainsReturnsFalseIfAllOfAKeysValuesAreRemoved()
        {
            var intMultiMap = new MultiMap<int, int> {{1, 2}, {2, 13}, {2, 15}, {5, 252}, {3461, 4256}};
            
            intMultiMap.Remove(2, 13);
            intMultiMap.Remove(2, 15);

            Ensure(intMultiMap.ContainsKey(2), False);
        }

        [Test]
        public void GetEnumerator()
        {
            var intMultiMap = new MultiMap<int, int> {{1, 2}, {2, 13}, {2, 15}, {2, 15}, {5, 252}, {3461, 4256}};

            var count = 0;
            foreach (KeyValuePair<int, int> pair in intMultiMap)
            {
                count++;
                Ensure(intMultiMap, Has.Member(pair));
                Ensure(intMultiMap.Contains(pair.Key, pair.Value));
            }

            Ensure(count, EqualTo(6));
        }

        [Test]
        public void IsReadOnly()
        {
            var intMultiMap = new MultiMap<int, int> {{1, 2}};

            Ensure(intMultiMap, Not.Empty);
            Ensure(intMultiMap.IsReadOnly, False);
        }

        [Test]
        public void Remove()
        {
            var intMultiMap = new MultiMap<int, int> {{1, 2}, {2, 13}, {2, 15}, {2, 15}, {5, 252}, {3461, 4256}};

            Ensure(intMultiMap.Remove(1,2));
            Ensure(intMultiMap.Remove(2,16), False);
            Ensure(intMultiMap.Remove(2,15));
            Ensure(intMultiMap.Remove(1,2), False);
        }

        [Test]
        public void This()
        {
            var intMultiMap = new MultiMap<int, int> {{1, 2}, {2, 13}, {2, 15}, {2, 15}, {5, 252}, {3461, 4256}};

            Ensure(intMultiMap[1], EquivalentTo(new int[] {2}));
            Ensure(intMultiMap[2], EquivalentTo(new[] {13, 15, 15}));

            EnsureException(() => intMultiMap[543], Hurls.KeyNotFoundException);
        }

        [Test]
        public void Keys()
        {
            var intMultiMap = new MultiMap<int, int> {{1, 2}, {2, 13}, {2, 15}, {2, 15}, {5, 252}, {3461, 4256}};

            Ensure(intMultiMap.Keys, EquivalentTo(new[] {1, 2, 5, 3461}));
        }

        [Test]
        public void Values()
        {
            var intMultiMap = new MultiMap<int, int> {{1, 2}, {2, 13}, {2, 15}, {2, 15}, {5, 252}, {3461, 4256}};

            Ensure(intMultiMap.Values, EquivalentTo(new[] {2, 13, 15, 15, 252, 4256}));
        }
    }
}
