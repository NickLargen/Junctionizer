using GameMover.Model;

using NUnit.Framework;

using TestingUtilities;

namespace GameMover.Tests
{
    public class FolderMappingTests : ExtendedAssertionHelper
    {
        [Test]
        public void CaseInsensitiveEquals()
        {
            var lower = new DirectoryMapping("a", "b");
            var upper = new DirectoryMapping("A", "B");

            Ensure(lower == upper);
            Ensure(lower.Equals(upper));
            Ensure(Equals(lower, upper));
            Assert.AreEqual(lower, upper);
            Ensure(ReferenceEquals(lower, upper), False);
            Ensure(lower != upper, False);
        }
    }
}
