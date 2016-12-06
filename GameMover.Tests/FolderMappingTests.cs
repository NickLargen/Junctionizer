using GameMover.Model;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GameMover.Tests
{
    [TestClass]
    public class FolderMappingTests 
    {
        [TestMethod]
        public void CaseInsensitiveEquals()
        {
            var lower = new DirectoryMapping("a", "b");
            var upper = new DirectoryMapping("A", "B");

            Assert.IsTrue(lower == upper);
            Assert.IsTrue(lower.Equals(upper));
            Assert.IsTrue(Equals(lower, upper));
            Assert.AreEqual(lower, upper);
            Assert.IsFalse(ReferenceEquals(lower, upper));
            Assert.IsFalse(lower != upper);
        }
    }
}
