using NUnit.Framework;
using PenmonScramble.Core;
using static PenmonScramble.Tests.TestWorld;

namespace PenmonScramble.Tests
{
    /// <summary>プレイヤーのペンモンが攻撃を受けた直後の短い無敵（§4.7）</summary>
    public class HitInvincibleTests
    {
        private static readonly PlayerInput SwitchPress = new PlayerInput(Vec2.Zero, false, false, special: false, swap: true);
        private static readonly PlayerInput Skill1Press = new PlayerInput(Vec2.Zero, skill1: true, skill2: false);

        // 構えの終わりは秒数の丸めで前後に1回ずれることがあるので、その分の余裕を取る
        private static int UntilEnemyHits => Seconds(Balance.EnemyWindUpSeconds) + 2;

        private static Unit AddPlayer(StageWorld world)
        {
            return world.AddUnit(Team.Player, new[] { Stats(Melee()), Stats(Melee()) }, Vec2.Zero, seat: 0);
        }

        private static Unit AddAttacker(StageWorld world, Vec2 position)
        {
            var enemy = world.AddUnit(Team.Enemy, Stats(Melee(power: 5f)), position);
            // 最初の待ち時間はばらけさせてあるので、すぐ構えるようにそろえる
            enemy.Cooldowns[0] = 0f;
            return enemy;
        }

        /// <summary>target に当たった数を数え始める。返した関数で、その時点までの数が分かる</summary>
        private static System.Func<int> CountHitsOn(StageWorld world, Unit target)
        {
            var hits = 0;
            world.Hit += e =>
            {
                if (e.TargetId == target.Id)
                {
                    hits++;
                }
            };
            return () => hits;
        }

        [Test]
        public void TwoEnemiesHitTogether_OnlyFirstLands()
        {
            var world = Create();
            var player = AddPlayer(world);
            AddAttacker(world, new Vec2(1.2f, 0f));
            AddAttacker(world, new Vec2(-1.2f, 0f));
            var hits = CountHitsOn(world, player);

            TickFor(world, UntilEnemyHits);

            Assert.That(hits(), Is.EqualTo(1));
            Assert.That(player.IsInvincible, Is.True);
        }

        [Test]
        public void HitInvincible_EndsAfterShortTime()
        {
            var world = Create();
            var player = AddPlayer(world);
            AddAttacker(world, new Vec2(1.2f, 0f));
            var hits = CountHitsOn(world, player);
            for (var i = 0; i < UntilEnemyHits && hits() == 0; i++)
            {
                world.Tick();
            }

            Assert.That(hits(), Is.EqualTo(1));
            TickFor(world, Seconds(Balance.HitInvincibleSeconds) - 2);
            Assert.That(player.IsInvincible, Is.True);

            TickFor(world, 3);
            Assert.That(player.IsInvincible, Is.False);
        }

        [Test]
        public void SwitchedInMember_DoesNotKeepHitInvincible()
        {
            var world = Create();
            var player = AddPlayer(world);
            AddAttacker(world, new Vec2(1.2f, 0f));
            var hits = CountHitsOn(world, player);
            TickFor(world, UntilEnemyHits);
            Assert.That(hits(), Is.EqualTo(1));

            Press(world, player, SwitchPress);

            Assert.That(player.ActiveIndex, Is.EqualTo(1));
            Assert.That(player.IsInvincible, Is.False);
        }

        [Test]
        public void Hit_DoesNotShortenSpecialInvincible()
        {
            var world = Create();
            var player = AddPlayer(world);
            AddAttacker(world, new Vec2(1.2f, 0f));
            player.InvincibleSeconds = Balance.SpecialInvincibleSeconds;
            var hits = CountHitsOn(world, player);

            TickFor(world, Seconds(1f));

            Assert.That(hits(), Is.EqualTo(0));
            Assert.That(player.IsInvincible, Is.True);
            Assert.That(player.InvincibleSeconds, Is.GreaterThan(Balance.SpecialInvincibleSeconds - 1.1f));
        }

        [Test]
        public void EnemyHitByPlayer_DoesNotBecomeInvincible()
        {
            var world = Create();
            var player = world.AddUnit(Team.Player, Stats(Melee()), Vec2.Zero);
            var enemy = AddDummyEnemy(world, new Vec2(1f, 0f));
            player.Facing = Vec2.Right;

            Press(world, player, Skill1Press);

            Assert.That(enemy.Hp, Is.LessThan(enemy.MaxHp));
            Assert.That(enemy.IsInvincible, Is.False);
        }
    }
}
