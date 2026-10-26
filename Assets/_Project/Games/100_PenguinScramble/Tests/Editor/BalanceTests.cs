using NUnit.Framework;
using PenguinScramble.Core;

namespace PenguinScramble.Tests
{
    public class BalanceTests
    {
        [Test]
        public void TickSeconds_Is30PerSecond()
        {
            Assert.That(1f / Balance.TickSeconds, Is.EqualTo(30f).Within(0.001f));
        }
    }
}
