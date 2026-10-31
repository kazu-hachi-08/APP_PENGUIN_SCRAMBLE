using NUnit.Framework;
using PenmonScramble.Core;
using static PenmonScramble.Tests.TestWorld;

namespace PenmonScramble.Tests
{
    /// <summary>大ボス（§6.2）: 赤い範囲で予告してから攻撃する・範囲の外なら当たらない・もぐっている間は効かない</summary>
    public class BigBossTests
    {
        private static Unit AddBoss(StageWorld world, int no, Vec2 position, int attackIndex = 0)
        {
            var boss = world.AddUnit(Team.Enemy, BigBossTable.Stats(BigBossTable.Get(no), playerCount: 1, world: 1), position);
            boss.Brain.NextAttackIndex = attackIndex;
            return boss;
        }

        private static Unit AddPlayer(StageWorld world, Vec2 position)
        {
            return world.AddUnit(Team.Player, Stats(Melee()), position, seat: 0);
        }

        /// <summary>最初の攻撃の予告が出るまで進める</summary>
        private static void WaitForWarning(StageWorld world)
        {
            for (var i = 0; i < Seconds(Balance.BigBossFirstAttackDelay) + 2 && world.Warnings.Count == 0; i++)
            {
                world.Tick();
            }

            Assert.That(world.Warnings.Count, Is.GreaterThan(0));
        }

        private static float TelegraphOf(int no, int attackIndex) => BigBossTable.Get(no).Attacks[attackIndex].TelegraphSeconds;

        [Test]
        public void Charge_WarnsFirst_ThenHitsPlayerOnThePath()
        {
            var world = Create();
            var boss = AddBoss(world, BigBossTable.SkuaNo, new Vec2(0f, 0f));
            var player = AddPlayer(world, new Vec2(4f, 0f));

            WaitForWarning(world);
            Assert.That(player.Hp, Is.EqualTo(player.MaxHp));
            Assert.That(world.Warnings[0].Overlaps(player.Position, player.Radius), Is.True);

            TickFor(world, Seconds(TelegraphOf(BigBossTable.SkuaNo, 0)) + Seconds(1f));
            Assert.That(player.Hp, Is.LessThan(player.MaxHp));
            Assert.That(world.Warnings.Count, Is.EqualTo(0));
            Assert.That(boss.Position.X, Is.GreaterThan(2f));
        }

        [Test]
        public void Charge_MissesPlayerWhoLeftTheWarning()
        {
            var world = Create();
            AddBoss(world, BigBossTable.SkuaNo, new Vec2(0f, 0f));
            var player = AddPlayer(world, new Vec2(4f, 0f));

            WaitForWarning(world);
            player.Position = new Vec2(4f, 6f);
            TickFor(world, Seconds(TelegraphOf(BigBossTable.SkuaNo, 0)) + Seconds(1f));

            Assert.That(player.Hp, Is.EqualTo(player.MaxHp));
        }

        [Test]
        public void Ambush_IsInvincibleWhileSubmerged_ThenRisesAtThePlayer()
        {
            var world = Create();
            var boss = AddBoss(world, BigBossTable.LeopardSealNo, new Vec2(-6f, 0f), attackIndex: 1);
            var player = AddPlayer(world, new Vec2(5f, 2f));

            WaitForWarning(world);
            Assert.That(boss.IsSubmerged, Is.True);
            Assert.That(boss.IsInvincible, Is.True);
            var spot = world.Warnings[0].From;

            TickFor(world, Seconds(TelegraphOf(BigBossTable.LeopardSealNo, 1)));
            Assert.That(boss.IsSubmerged, Is.False);
            Assert.That(Vec2.Distance(boss.Position, spot), Is.LessThan(boss.Radius + player.Radius + 0.5f));
            Assert.That(player.Hp, Is.LessThan(player.MaxHp));
        }

        [Test]
        public void Wave_HitsOnlyTheBandAtThePlayersHeight()
        {
            var world = Create();
            AddBoss(world, BigBossTable.OrcaNo, new Vec2(8f, 0f));
            var target = AddPlayer(world, new Vec2(0f, 5f));

            WaitForWarning(world);
            var safe = AddPlayer(world, new Vec2(0f, -5f));
            TickFor(world, Seconds(TelegraphOf(BigBossTable.OrcaNo, 0)));

            Assert.That(target.Hp, Is.LessThan(target.MaxHp));
            Assert.That(safe.Hp, Is.EqualTo(safe.MaxHp));
        }

        [Test]
        public void Rain_WarnsAtEveryPlayerAndExtraSpots()
        {
            // ばらばらの円の数は広さで変わるので、ふつうのエリアの広さで数える
            var world = new StageWorld(StageLayout.Area(null, 0), seed: 1);
            AddBoss(world, BigBossTable.SkuaNo, new Vec2(0f, 0f), attackIndex: 1);
            AddPlayer(world, new Vec2(4f, 3f));
            AddPlayer(world, new Vec2(-4f, -3f));

            WaitForWarning(world);

            var extra = BigBossTable.Get(BigBossTable.SkuaNo).Attacks[1].Skill.Count;
            Assert.That(world.Warnings.Count, Is.EqualTo(2 + extra));
        }

        [Test]
        public void DefeatedBoss_RemovesItsWarnings()
        {
            var world = Create();
            var boss = AddBoss(world, BigBossTable.SkuaNo, new Vec2(0f, 0f));
            AddPlayer(world, new Vec2(4f, 0f));
            WaitForWarning(world);

            boss.Hp = 0;
            world.Tick();

            Assert.That(world.Warnings.Count, Is.EqualTo(0));
        }

        [Test]
        public void Snapshot_SendsWarningsAndSubmerged()
        {
            var world = Create();
            AddBoss(world, BigBossTable.LeopardSealNo, new Vec2(-6f, 0f), attackIndex: 1);
            AddPlayer(world, new Vec2(5f, 2f));
            var recorder = new StageRecorder(world, round: 1);
            WaitForWarning(world);

            var decoded = StageCodec.DecodeSnapshot(StageCodec.EncodeSnapshot(recorder.Capture()));

            Assert.That(decoded.Warnings.Count, Is.EqualTo(1));
            Assert.That(decoded.Warnings[0].From, Is.EqualTo(world.Warnings[0].From));
            Assert.That(decoded.Warnings[0].HalfWidth, Is.EqualTo(world.Warnings[0].HalfWidth));
            Assert.That(decoded.TryGetBoss(out var boss), Is.True);
            Assert.That(boss.Has(UnitFlags.Submerged), Is.True);
            Assert.That(boss.SpeciesNo, Is.EqualTo(BigBossTable.LeopardSealNo));
        }
    }
}
