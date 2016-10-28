using NUnit.Framework;

using Utilities.Collections;

namespace UtilitiesTests.Collections
{
    public class MultiMapTests
    {
        private MultiMap<int, int> GetIntMultimap()
        {
            var intMultimap = new MultiMap<int, int>();


            return intMultimap;
        }

        [Test]
        public void Add()
        {
            var intMultiMap = new MultiMap<int, int>();
            intMultiMap.Add(1, 2);
            intMultiMap.Add(1, 3);
            intMultiMap.Add(1, 2);
            intMultiMap.Add(5, 252);
            intMultiMap.Add(4363461, 2);

            Assert.That(intMultiMap.Count, Is.EqualTo(4));
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