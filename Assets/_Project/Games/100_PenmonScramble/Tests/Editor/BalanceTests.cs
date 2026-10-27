using NUnit.Framework;
using PenmonScramble.Core;

namespace PenmonScramble.Tests
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
