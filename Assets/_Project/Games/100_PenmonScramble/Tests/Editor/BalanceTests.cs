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

        [TestCase(1, 1.0f)]
        [TestCase(2, 1.6f)]
        [TestCase(3, 2.2f)]
        [TestCase(4, 2.8f)]
        public void EnemyHpScale_FollowsPlayerCount(int players, float expected)
        {
            Assert.That(Balance.EnemyHpScale(players), Is.EqualTo(expected).Within(0.001f));
        }

        [Test]
        public void EnemyHpScale_ClampsOutOfRange()
        {
            Assert.That(Balance.EnemyHpScale(0), Is.EqualTo(Balance.EnemyHpScale(1)));
            Assert.That(Balance.EnemyHpScale(9), Is.EqualTo(Balance.EnemyHpScale(Balance.MaxPlayers)));
        }
    }
}
