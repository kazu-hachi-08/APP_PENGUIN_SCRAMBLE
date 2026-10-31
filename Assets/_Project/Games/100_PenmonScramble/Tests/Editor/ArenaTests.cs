using NUnit.Framework;
using PenmonScramble.Core;
using static PenmonScramble.Tests.TestWorld;

namespace PenmonScramble.Tests
{
    /// <summary>大ボスの闘技場（§6.1・§6.2）: 楕円の形・柵で跳ね返る・出入口から入って戻れない・敵と予告が楕円の中に出る</summary>
    public class ArenaTests
    {
        private static readonly int[] Pool = { 1 };

        private static AreaShape CenteredArena()
        {
            var half = new Vec2(Balance.ArenaWidth, Balance.ArenaHeight) * 0.5f;
            return AreaShape.Ellipse(-half, half);
        }

        private static Unit AddPlayer(StageWorld world, Vec2 position, int seat = 0)
        {
            return world.AddUnit(Team.Player, Stats(Melee()), position, seat);
        }

        private static Unit AddBoss(StageWorld world, int no, Vec2 position, int attackIndex)
        {
            var boss = world.AddUnit(Team.Enemy, BigBossTable.Stats(BigBossTable.Get(no), playerCount: 1, world: 1), position);
            boss.Brain.NextAttackIndex = attackIndex;
            return boss;
        }

        private static void WaitForWarning(StageWorld world)
        {
            for (var i = 0; i < Seconds(Balance.BigBossFirstAttackDelay) + 2 && world.Warnings.Count == 0; i++)
            {
                world.Tick();
            }

            Assert.That(world.Warnings.Count, Is.GreaterThan(0));
        }

        /// <summary>ふつうのエリア1つ → 大ボスの闘技場、の2エリアのステージ</summary>
        private static StageWorld CreateStageWithArena()
        {
            var stage = new StageDef(1, "test", new AreaDef(new WaveDef(1, Pool)), new AreaDef(new WaveDef(1, Pool, BigBossTable.SkuaNo)));
            return new StageWorld(stage, playerCount: 1, seed: 1);
        }

        /// <summary>最初のエリアの敵を出して倒し、出入口を開ける</summary>
        private static void OpenFirstGate(StageWorld world)
        {
            TickFor(world, Seconds(Balance.FirstWaveDelaySeconds));
            foreach (var unit in world.Units)
            {
                if (unit.Team == Team.Enemy)
                {
                    unit.Hp = 0;
                }
            }

            world.Tick();
            Assert.That(world.IsGateOpen, Is.True);
        }

        private static void Walk(StageWorld world, Unit unit, Vec2 move, float seconds)
        {
            world.SetInput(unit.Id, new PlayerInput(move, skill1: false, skill2: false));
            TickFor(world, Seconds(seconds));
            world.SetInput(unit.Id, default);
        }

        [Test]
        public void BigBossArea_IsWideEllipse_AndOtherAreasKeepTheirPlace()
        {
            var stage = StageTable.Get(3);
            var last = stage.Areas.Count - 1;
            for (var i = 0; i < last; i++)
            {
                var area = StageLayout.Area(stage, i);
                Assert.That(area.IsEllipse, Is.False);
                Assert.That(area.Min, Is.EqualTo(new Vec2((i - 0.5f) * Balance.AreaWidth, -Balance.AreaHeight * 0.5f)));
                Assert.That(area.Max, Is.EqualTo(new Vec2((i + 0.5f) * Balance.AreaWidth, Balance.AreaHeight * 0.5f)));
            }

            var arena = StageLayout.Area(stage, last);
            Assert.That(arena.IsEllipse, Is.True);
            Assert.That(arena.Width, Is.EqualTo(Balance.ArenaWidth));
            Assert.That(arena.Height, Is.EqualTo(Balance.ArenaHeight));
            Assert.That(arena.Min.X, Is.EqualTo(StageLayout.Area(stage, last - 1).Max.X));
            Assert.That(arena.Center.Y, Is.EqualTo(0f));
        }

        [Test]
        public void StageBossArea_StaysSquare()
        {
            var stage = StageTable.Get(1);
            Assert.That(StageLayout.Area(stage, stage.Areas.Count - 1).IsEllipse, Is.False);
        }

        [Test]
        public void Walking_CannotLeaveTheEllipse()
        {
            var arena = CenteredArena();
            var world = new StageWorld(arena, seed: 1);
            var player = AddPlayer(world, new Vec2(10f, 4f));

            Walk(world, player, new Vec2(1f, 1f).Normalized, 6f);

            Assert.That(arena.Contains(player.Position, player.Radius - Tolerance), Is.True);
            // 角に引っかからず、柵に沿って奥まで進んでいる
            Assert.That(player.Position.X, Is.GreaterThan(11f));
        }

        [Test]
        public void KnockedIntoFence_BouncesOffTheCurve()
        {
            var arena = CenteredArena();
            var world = new StageWorld(arena, seed: 1);
            var player = AddPlayer(world, new Vec2(10f, 5f));
            player.KnockbackVelocity = new Vec2(10f, 10f);

            TickFor(world, Seconds(0.25f));

            Assert.That(arena.Contains(player.Position, player.Radius - Tolerance), Is.True);
            // 外向き（右上）の速さが、中心へ向かう速さに返っている
            Assert.That(Vec2.Dot(player.KnockbackVelocity, player.Position), Is.LessThan(0f));
        }

        [Test]
        public void Confine_KeepsSlidingSpeed_AndReturnsSixtyPercentInward()
        {
            var arena = CenteredArena();
            const float margin = 0.5f;
            var (position, velocity) = arena.Confine(new Vec2(0f, 9f), new Vec2(3f, 10f), margin, Balance.WallRestitution);

            Assert.That(position.Y, Is.EqualTo(Balance.ArenaHeight * 0.5f - margin).Within(0.01f));
            Assert.That(velocity.X, Is.EqualTo(3f).Within(0.01f));
            Assert.That(velocity.Y, Is.EqualTo(-10f * Balance.WallRestitution).Within(0.01f));
        }

        [Test]
        public void GateToArena_OnlyOpensAtTheDoorway()
        {
            var world = CreateStageWithArena();
            var player = AddPlayer(world, new Vec2(5f, 3.5f));
            OpenFirstGate(world);

            Walk(world, player, Vec2.Right, 2f);

            var wallX = StageLayout.Area(null, 0).Max.X;
            Assert.That(player.Position.X, Is.LessThanOrEqualTo(wallX - player.Radius + Tolerance));
            Assert.That(world.AreaIndex, Is.EqualTo(0));
        }

        [Test]
        public void EnteringThroughDoorway_StartsArena_AndCannotGoBack()
        {
            var world = CreateStageWithArena();
            var player = AddPlayer(world, new Vec2(5f, 0f), seat: 0);
            var lagger = AddPlayer(world, new Vec2(-5f, 3f), seat: 1);
            OpenFirstGate(world);

            for (var i = 0; i < Seconds(5f) && world.AreaIndex == 0; i++)
            {
                world.SetInput(player.Id, new PlayerInput(Vec2.Right, skill1: false, skill2: false));
                world.Tick();
            }

            Assert.That(world.AreaIndex, Is.EqualTo(1));
            Assert.That(world.IsGateOpen, Is.False);
            world.Tick();
            var arena = world.Area;
            Assert.That(arena.IsEllipse, Is.True);
            Assert.That(arena.Contains(lagger.Position, lagger.Radius - Tolerance), Is.True);

            Walk(world, player, -Vec2.Right, 3f);
            Assert.That(arena.Contains(player.Position, player.Radius - Tolerance), Is.True);
        }

        [Test]
        public void ArenaEnemies_AppearInsideTheEllipse()
        {
            var world = CreateStageWithArena();
            var player = AddPlayer(world, new Vec2(5f, 0f));
            OpenFirstGate(world);
            player.Position = new Vec2(world.AreaMax.X - Balance.ArenaWidth + Balance.AreaEnterDepth + 0.5f, 0f);
            world.Tick();
            TickFor(world, Seconds(Balance.FirstWaveDelaySeconds) + 1);

            var arena = world.Area;
            var enemies = 0;
            foreach (var unit in world.Units)
            {
                if (unit.Team == Team.Enemy)
                {
                    enemies++;
                    Assert.That(arena.Contains(unit.Position, unit.Radius - Tolerance), Is.True, unit.ToString());
                }
            }

            Assert.That(enemies, Is.EqualTo(2));
        }

        [Test]
        public void BigBoss_AppearsInTheMiddleOfArena()
        {
            var world = CreateStageWithArena();
            var player = AddPlayer(world, new Vec2(5f, 0f));
            OpenFirstGate(world);
            player.Position = new Vec2(world.AreaMax.X - Balance.ArenaWidth + Balance.AreaEnterDepth + 0.5f, 0f);
            world.Tick();
            TickFor(world, Seconds(Balance.FirstWaveDelaySeconds) + 1);

            foreach (var unit in world.Units)
            {
                if (unit.Team == Team.Enemy && unit.IsBoss)
                {
                    Assert.That(Vec2.Distance(unit.Position, world.Area.Center), Is.LessThan(1f));
                    return;
                }
            }

            Assert.Fail("大ボスが出ていない");
        }

        [Test]
        public void Rain_AddsMoreSpotsInArena_AllInsideTheEllipse()
        {
            var arena = CenteredArena();
            var world = new StageWorld(arena, seed: 1);
            AddBoss(world, BigBossTable.SkuaNo, Vec2.Zero, attackIndex: 1);
            AddPlayer(world, new Vec2(6f, 3f));

            WaitForWarning(world);

            var extra = BigBossTable.Get(BigBossTable.SkuaNo).Attacks[1].Skill.Count;
            Assert.That(world.Warnings.Count - 1, Is.GreaterThan(extra));
            foreach (var warning in world.Warnings)
            {
                Assert.That(arena.Contains(warning.From, 0f), Is.True);
            }
        }

        [Test]
        public void Wave_RunsFenceToFence_OnThePlayersHalf()
        {
            var arena = CenteredArena();
            var world = new StageWorld(arena, seed: 1);
            AddBoss(world, BigBossTable.OrcaNo, new Vec2(8f, 0f), attackIndex: 0);
            AddPlayer(world, new Vec2(0f, 5f));

            WaitForWarning(world);

            var band = world.Warnings[0];
            var (minX, maxX) = arena.SpanX(band.From.Y, 0f);
            Assert.That(band.From.Y, Is.GreaterThan(0f));
            Assert.That(band.From.X, Is.EqualTo(minX).Within(Tolerance));
            Assert.That(band.To.X, Is.EqualTo(maxX).Within(Tolerance));
        }

        [Test]
        public void CrossingCharge_RunsToTheFence_AndStopsThere()
        {
            var arena = CenteredArena();
            var world = new StageWorld(arena, seed: 1);
            var boss = AddBoss(world, BigBossTable.OrcaNo, new Vec2(-10f, 0f), attackIndex: 1);
            AddPlayer(world, new Vec2(0f, 6f));

            WaitForWarning(world);

            // 予告は、走り出す所から右の柵まで（エリアの幅の半分より長い）
            var path = world.Warnings[0];
            Assert.That(path.To.X - path.From.X, Is.GreaterThan(Balance.ArenaWidth * 0.5f));
            Assert.That(arena.DistanceToWall(path.To, Vec2.Right, boss.Radius), Is.LessThan(0.05f));

            // 走り終えたら、また寄ってくるので、走り終えた瞬間を見る
            TickFor(world, Seconds(BigBossTable.Get(BigBossTable.OrcaNo).Attacks[1].TelegraphSeconds));
            for (var i = 0; i < Seconds(3f) && boss.IsDashing; i++)
            {
                world.Tick();
            }

            Assert.That(boss.IsDashing, Is.False);
            Assert.That(Vec2.Distance(boss.Position, path.To), Is.LessThan(0.5f));
            Assert.That(arena.Contains(boss.Position, boss.Radius - Tolerance), Is.True);
        }

        [Test]
        public void Guest_CanLookUpArenaShapeFromStageNo()
        {
            var world = new StageWorld(StageTable.Get(3), playerCount: 1, seed: 1);
            var snapshot = StageCodec.DecodeSnapshot(StageCodec.EncodeSnapshot(new StageRecorder(world, round: 1).Capture()));

            var area = StageLayout.Area(StageTable.Get(snapshot.StageNo), snapshot.AreaCount - 1);
            Assert.That(area.IsEllipse, Is.True);
        }
    }
}
