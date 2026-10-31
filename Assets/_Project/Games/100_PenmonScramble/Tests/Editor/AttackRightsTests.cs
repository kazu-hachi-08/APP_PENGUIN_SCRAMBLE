using System.Collections.Generic;
using NUnit.Framework;
using PenmonScramble.Core;
using static PenmonScramble.Tests.TestWorld;

namespace PenmonScramble.Tests
{
    /// <summary>攻撃の順番待ち: プレイヤー1人に同時に攻撃してよい雑魚の数をしぼる（§4.7）</summary>
    public class AttackRightsTests
    {
        // 技が届く距離（体の半径 0.5 + 届く距離 1 × 0.8 = 1.3）の内側で、隣どうしが重ならない半径
        private const float RingRadius = 1.2f;

        private static Unit AddReadyEnemy(StageWorld world, Vec2 position)
        {
            var enemy = world.AddUnit(Team.Enemy, Stats(Melee(power: 1f)), position);
            // 最初の待ち時間はばらけさせてあるので、すぐ構えられるようにそろえる
            enemy.Cooldowns[0] = 0f;
            return enemy;
        }

        private static List<Unit> AddRing(StageWorld world, Vec2 center, int count, float radius)
        {
            var enemies = new List<Unit>();
            for (var i = 0; i < count; i++)
            {
                var angle = 2f * System.MathF.PI * i / count;
                enemies.Add(AddReadyEnemy(world, center + new Vec2(System.MathF.Cos(angle), System.MathF.Sin(angle)) * radius));
            }

            return enemies;
        }

        /// <summary>殴られ続けてもダウンしないプレイヤー（ダウンすると敵が狙わなくなり、数えられないため）</summary>
        private static Unit AddSturdyPlayer(StageWorld world)
        {
            var stats = Stats(Melee());
            stats.MaxHp = 10000;
            return world.AddUnit(Team.Player, stats, Vec2.Zero);
        }

        private static int CountWindingUp(List<Unit> enemies) => enemies.FindAll(e => e.IsWindingUp).Count;

        [Test]
        public void SurroundedByTen_AtMostTwoAttackAtOnce()
        {
            var world = Create();
            var player = AddSturdyPlayer(world);
            var enemies = AddRing(world, Vec2.Zero, 10, 4f);

            var maxWindingUp = 0;
            var maxRights = 0;
            for (var i = 0; i < Seconds(6f); i++)
            {
                world.Tick();
                maxWindingUp = System.Math.Max(maxWindingUp, CountWindingUp(enemies));
                maxRights = System.Math.Max(maxRights, enemies.FindAll(e => e.HasAttackRight).Count);
            }

            Assert.That(maxRights, Is.EqualTo(Balance.AttackersPerPlayer));
            Assert.That(maxWindingUp, Is.InRange(1, Balance.AttackersPerPlayer), "攻撃はしてくるが、2体まで");
        }

        [Test]
        public void RightsGoToNearest_AndPassOnWhenHolderFalls()
        {
            var world = Create();
            world.AddUnit(Team.Player, Stats(Melee()), Vec2.Zero);
            var nearest = AddReadyEnemy(world, new Vec2(2f, 0f));
            var second = AddReadyEnemy(world, new Vec2(0f, 2.5f));
            var farthest = AddReadyEnemy(world, new Vec2(-3f, 0f));

            world.Tick();
            Assert.That(nearest.HasAttackRight, Is.True);
            Assert.That(second.HasAttackRight, Is.True);
            Assert.That(farthest.HasAttackRight, Is.False);

            nearest.Hp = 0;
            world.Tick();

            Assert.That(farthest.HasAttackRight, Is.True, "倒れた敵の権利が次の敵に渡る");
        }

        [Test]
        public void TwoPlayers_FourCanWindUp()
        {
            var world = Create();
            var left = new Vec2(-6f, 0f);
            var right = new Vec2(6f, 0f);
            world.AddUnit(Team.Player, Stats(Melee()), left, seat: 0);
            world.AddUnit(Team.Player, Stats(Melee()), right, seat: 1);
            var enemies = AddRing(world, left, 5, RingRadius);
            enemies.AddRange(AddRing(world, right, 5, RingRadius));

            world.Tick();

            Assert.That(CountWindingUp(enemies), Is.EqualTo(2 * Balance.AttackersPerPlayer));
            Assert.That(CountWindingUp(enemies.GetRange(0, 5)), Is.EqualTo(Balance.AttackersPerPlayer), "それぞれ近いプレイヤーの枠を使う");
        }

        [Test]
        public void DownedPlayer_HoldsNoRights()
        {
            var world = Create();
            world.AddUnit(Team.Player, Stats(Melee()), new Vec2(-6f, 0f), seat: 0);
            var downed = world.AddUnit(Team.Player, Stats(Melee()), new Vec2(6f, 0f), seat: 1);
            downed.Hp = 0;
            var enemies = AddRing(world, new Vec2(6f, 0f), 3, RingRadius);

            world.Tick();

            Assert.That(CountWindingUp(enemies), Is.EqualTo(0));
            Assert.That(enemies.FindAll(e => e.HasAttackRight).Count, Is.EqualTo(Balance.AttackersPerPlayer), "元気なプレイヤーの枠を取り合う");
        }

        [Test]
        public void StageBoss_IsNotCounted()
        {
            var world = Create();
            world.AddUnit(Team.Player, Stats(Melee()), Vec2.Zero);
            var enemies = AddRing(world, Vec2.Zero, 3, RingRadius);
            var boss = world.AddUnit(Team.Enemy, SpeciesTable.BossStats(SpeciesTable.Get(1), 1), new Vec2(0f, -RingRadius));
            boss.Cooldowns[0] = 0f;
            boss.Cooldowns[1] = 0f;

            world.Tick();

            Assert.That(boss.IsWindingUp, Is.True);
            Assert.That(CountWindingUp(enemies), Is.EqualTo(Balance.AttackersPerPlayer));
        }

        [Test]
        public void AfterAttack_ReleasesRight()
        {
            var world = Create();
            AddSturdyPlayer(world);
            var enemy = AddReadyEnemy(world, new Vec2(RingRadius, 0f));
            world.Tick();
            Assert.That(enemy.HasAttackRight, Is.True);

            TickFor(world, Seconds(Balance.EnemyWindUpSeconds) + 2);

            Assert.That(enemy.IsWindingUp, Is.False);
            Assert.That(enemy.HasAttackRight, Is.False, "撃ったら待ち時間の間は手放す");
        }

        [Test]
        public void WaitingEnemy_StaysOutOfReach_AndCircles()
        {
            var world = Create();
            var player = AddSturdyPlayer(world);
            var waiting = AddReadyEnemy(world, new Vec2(0f, 6f));
            // 待ち時間が明けていない雑魚は権利を取れず、外で待つ
            waiting.Cooldowns[0] = 100f;

            TickFor(world, Seconds(2f));
            var settled = waiting.Position;
            TickFor(world, Seconds(1f));

            var reach = player.Radius + waiting.Skills[0].Range;
            Assert.That(waiting.HasAttackRight, Is.False);
            Assert.That(Vec2.Distance(waiting.Position, player.Position), Is.GreaterThan(reach), "技が届く距離の外で待つ");
            Assert.That(Vec2.Distance(waiting.Position, settled), Is.GreaterThan(0.1f), "止まって並ばず、横へ回り込む");
        }
    }
}
