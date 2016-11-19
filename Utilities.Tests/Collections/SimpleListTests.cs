using NUnit.Framework;

using TestingUtilities;

using Utilities.Collections;

namespace Utilities.Tests.Collections
{
    public class SimpleListTests : ExtendedAssertionHelper
    {
        [Test]
        public void MoveToLaterPosition()
        {
            var digits = new SimpleList<int>() {0, 1, 2, 3, 4, 5, 6, 7, 8, 9};

            digits.Move(3, 6);
            Ensure(digits, EqualTo(new[] {0, 1, 2, 4, 5, 6, 3, 7, 8, 9}));
        }

        [Test]
        public void MoveToEarlierPosition()
        {
            var digits = new SimpleList<int>() {0, 1, 2, 3, 4, 5, 6, 7, 8, 9};

            digits.Move(5, 2);
            Ensure(digits, EqualTo(new[] {0, 1, 5, 2, 3, 4, 6, 7, 8, 9}));
        }

        [Test]
        public void MoveToSamePosition()
        {
            var digits = new SimpleList<int>() {0, 1, 2, 3, 4, 5, 6, 7, 8, 9};

            digits.Move(4, 4);
            Ensure(digits, EqualTo(new[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9}));
        }

        [Test]
        public void MoveArgumentOutOfRange()
        {
            var digits = new SimpleList<int>() {0, 1, 2, 3, 4, 5, 6, 7, 8, 9};

            EnsureException(() => digits.Move(2, 9), Hurls.Nothing);
            EnsureException(() => digits.Move(2, 10), Hurls.ArgumentOutOfRangeException);
            EnsureException(() => digits.Move(10, 2), Hurls.ArgumentOutOfRangeException);
            EnsureException(() => digits.Move(10, 10), Hurls.ArgumentOutOfRangeException);
        }
    }
}
