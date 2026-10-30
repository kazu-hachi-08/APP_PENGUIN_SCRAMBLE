using NUnit.Framework;
using PenmonScramble.Core;
using static PenmonScramble.Tests.TestWorld;

namespace PenmonScramble.Tests
{
    /// <summary>吹っ飛んだ敵が別の敵に当たる連鎖（§4.4）</summary>
    public class ChainTests
    {
        private static readonly PlayerInput UseSkill1 = new PlayerInput(Vec2.Zero, true, false);

        /// <summary>プレイヤーが右の敵 A を殴り、A がその右の敵 B へ飛んでいく並び</summary>
        private static (StageWorld world, Unit player, Unit flyer, Unit target) Setup(float knockback)
        {
            var world = Create();
            var melee = Melee();
            melee.Knockback = knockback;
            var player = world.AddUnit(Team.Player, Stats(melee), Vec2.Zero, seat: 0);
            var flyer = AddDummyEnemy(world, new Vec2(1f, 0f));
            var target = AddDummyEnemy(world, new Vec2(2.3f, 0f));
            return (world, player, flyer, target);
        }

        [Test]
        public void StrongKnockback_HitsAndLaunchesAnotherEnemy()
        {
            var (world, player, _, target) = Setup(knockback: 12f);

            Press(world, player, UseSkill1);
            TickFor(world, 3);

            Assert.That(target.Hp, Is.LessThan(target.MaxHp), "ぶつけられた敵にダメージ");
            Assert.That(target.Position.X, Is.GreaterThan(2.4f), "ぶつけられた敵も飛ぶ");
        }

        [Test]
        public void WeakKnockback_OnlyPushes()
        {
            var (world, player, _, target) = Setup(knockback: Balance.ChainMinSpeed - 1f);

            Press(world, player, UseSkill1);
            TickFor(world, Seconds(1f));

            Assert.That(target.Hp, Is.EqualTo(target.MaxHp));
        }

        [Test]
        public void ChainDamage_ScalesWithSpeed()
        {
            var (slowWorld, slowPlayer, _, slowTarget) = Setup(knockback: 9f);
            var (fastWorld, fastPlayer, _, fastTarget) = Setup(knockback: 18f);

            Press(slowWorld, slowPlayer, UseSkill1);
            Press(fastWorld, fastPlayer, UseSkill1);
            TickFor(slowWorld, Seconds(0.5f));
            TickFor(fastWorld, Seconds(0.5f));

            Assert.That(slowTarget.Hp, Is.LessThan(slowTarget.MaxHp));
            Assert.That(fastTarget.Hp, Is.LessThan(slowTarget.Hp));
        }

        [Test]
        public void ChainDamage_DoesNotChargeSpecialGauge()
        {
            var (world, player, flyer, target) = Setup(knockback: 12f);

            Press(world, player, UseSkill1);
            TickFor(world, 3);

            Assert.That(target.Hp, Is.LessThan(target.MaxHp));
            var skillDamage = 10f;
            Assert.That(player.SpecialGauge, Is.EqualTo(skillDamage / flyer.MaxHp * Balance.SpecialGainPerEnemy).Within(Tolerance), "技のダメージの分だけたまる");
            Assert.That(flyer.Hp, Is.EqualTo(flyer.MaxHp - (int)skillDamage), "殴られた敵には技のダメージだけ");
        }
    }
}
