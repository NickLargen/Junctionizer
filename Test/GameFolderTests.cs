using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GameMover.Model;

using NUnit.Framework;

namespace Test
{

    public class GameFolderTests : TestBase
    {

        [Test]
        public void Equality()
        {
            var first = new GameFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory));
            var second = new GameFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory));

            var differentFolder = new GameFolder(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

            Assert.That(first, Is.EqualTo(second));
            Assert.That(Equals(first, second));
            Assert.That(first.CompareTo(second) == 0);
            Assert.That(first == second);
            Assert.That(!ReferenceEquals(first, second));

            Assert.That(first, Is.Not.EqualTo(differentFolder));
            Assert.That(!Equals(first, differentFolder));
            Assert.That(first.CompareTo(differentFolder) != 0);
            Assert.That(first != differentFolder);
            Assert.That(!ReferenceEquals(first, differentFolder));
        }
    }
}
