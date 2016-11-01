using NUnit.Framework;

using Utilities.Collections;
using Utilities.Testing;


namespace Utilities.Tests.Collections
{
    public class MultiMapTests : ExtendedAssertionHelper
    {
        private MultiMap<int, int> GetIntMultimap()
        {
            var intMultimap = new MultiMap<int, int>();


            return intMultimap;
        }

        [Test]
        public void Add()
        {
            var intMultiMap = new MultiMap<int, int> {{1, 2}, {5, 252}, {4363461, 2}};

            Ensure(intMultiMap.Count, Is.EqualTo(3), "Adding individual items failed...");

            intMultiMap.Add(1,34);
            intMultiMap.Add(1,35);

//            Ensure(intMultiMap, Count.EqualTo(5));
        }

        [Test]
        public void Contains()
        {
            var intMultiMap = new MultiMap<int, int>();
        }

        [Test]
        public void GetEnumerator()
        {
            var intMultiMap = new MultiMap<int, int>();
        }

        [Test]
        public void Remove()
        {
            var intMultiMap = new MultiMap<int, int>();
        }

        [Test]
        public void TryGetValue()
        {
            var intMultiMap = new MultiMap<int, int>();
        }

        [Test]
        public void This()
        {
            var intMultiMap = new MultiMap<int, int>();
        }

        [Test]
        public void Values()
        {
            var intMultiMap = new MultiMap<int, int>();
        }
    }
}
