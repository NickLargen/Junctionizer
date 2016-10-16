using GameMover.Model;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Test
{
    [TestClass]
    public class FolderMappingTests : TestBase
    {

        [TestMethod]
        public void CaseInsensitiveEquals()
        {
            var lower = new FolderMapping("a", "b");
            var upper = new FolderMapping("A", "B");

            Assert.IsTrue(lower == upper);
            Assert.IsTrue(lower.Equals(upper));
            Assert.IsTrue(Equals(lower, upper));
            Assert.AreEqual(lower, upper);
            Assert.IsFalse(ReferenceEquals(lower , upper));
            Assert.IsFalse(lower != upper);
        }
    }
}
