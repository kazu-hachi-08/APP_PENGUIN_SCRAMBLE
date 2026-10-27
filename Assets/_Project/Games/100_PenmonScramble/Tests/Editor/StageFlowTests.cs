using NUnit.Framework;
using PenmonScramble.Core;
using static PenmonScramble.Tests.TestWorld;

namespace PenmonScramble.Tests
{
    /// <summary>ステージの進み方（§6.1）: 波・出入口・次のエリア・ボス・クリア</summary>
    public class StageFlowTests
    {
        private static readonly int[] Pool = { 1 };

        private static StageWorld CreateStage(params AreaDef[] areas)
        {
            return new StageWorld(new StageDef("test", areas), playerCount: 1, seed: 1);
        }

        private static Unit AddPlayer(StageWorld world, Vec2 position, int seat = 0)
        {
            return world.AddUnit(Team.Player, Stats(Melee()), position, seat);
        }

        private static void WaitFirstWave(StageWorld world)
        {
            TickFor(world, Seconds(Balance.FirstWaveDelaySeconds));
        }

        /// <summary>敵を count 体だけ倒す（倒れた敵は次の更新で場から消える）</summary>
        private static void DefeatEnemies(StageWorld world, int count = int.MaxValue)
        {
            foreach (var unit in world.Units)
            {
                if (unit.Team == Team.Enemy && count-- > 0)
                {
                    unit.Hp = 0;
                }
            }

            world.Tick();
        }

        [Test]
        public void FirstWave_AppearsAfterDelay()
        {
            var world = CreateStage(new AreaDef(new WaveDef(3, Pool)));
            AddPlayer(world, new Vec2(-5f, 0f));

            world.Tick();
            Assert.That(world.CountAlive(Team.Enemy), Is.EqualTo(0));
            Assert.That(world.Result, Is.EqualTo(StageResult.Playing));

            WaitFirstWave(world);
            Assert.That(world.CountAlive(Team.Enemy), Is.EqualTo(3));
        }

        [Test]
        public void NextWave_AppearsWhenFewEnemiesLeft()
        {
            var world = CreateStage(new AreaDef(new WaveDef(5, Pool), new WaveDef(4, Pool)));
            AddPlayer(world, new Vec2(-5f, 0f));
            WaitFirstWave(world);

            DefeatEnemies(world, 5 - Balance.NextWaveEnemiesLeft - 1);
            Assert.That(world.CountAlive(Team.Enemy), Is.EqualTo(Balance.NextWaveEnemiesLeft + 1));

            DefeatEnemies(world, 1);
            Assert.That(world.CountAlive(Team.Enemy), Is.EqualTo(Balance.NextWaveEnemiesLeft + 4));
        }

        [Test]
        public void ClearingArea_OpensGate_ThenEnteringNextAreaClosesIt()
        {
            var world = CreateStage(new AreaDef(new WaveDef(2, Pool)), new AreaDef(new WaveDef(2, Pool)));
            var player = AddPlayer(world, new Vec2(-5f, 0f));
            WaitFirstWave(world);

            DefeatEnemies(world);
            Assert.That(world.IsGateOpen, Is.True);
            Assert.That(world.AreaMax, Is.EqualTo(StageLayout.AreaMax(1)));
            Assert.That(world.Result, Is.EqualTo(StageResult.Playing));

            player.Position = new Vec2(StageLayout.AreaMin(1).X + Balance.AreaEnterDepth + 0.5f, 0f);
            world.Tick();

            Assert.That(world.AreaIndex, Is.EqualTo(1));
            Assert.That(world.IsGateOpen, Is.False);
            Assert.That(world.AreaMin, Is.EqualTo(StageLayout.AreaMin(1)));
            Assert.That(world.AreaMax, Is.EqualTo(StageLayout.AreaMax(1)));
        }

        [Test]
        public void StandingOnOpenGate_DoesNotStartNextArea()
        {
            var world = CreateStage(new AreaDef(new WaveDef(1, Pool)), new AreaDef(new WaveDef(1, Pool)));
            var player = AddPlayer(world, new Vec2(-5f, 0f));
            WaitFirstWave(world);
            DefeatEnemies(world);

            player.Position = new Vec2(StageLayout.AreaMin(1).X + Balance.AreaEnterDepth * 0.5f, 0f);
            world.Tick();

            Assert.That(world.AreaIndex, Is.EqualTo(0));
            Assert.That(world.IsGateOpen, Is.True);
        }

        [Test]
        public void LaggingPlayer_IsPulledIntoNewArea()
        {
            var world = CreateStage(new AreaDef(new WaveDef(1, Pool)), new AreaDef(new WaveDef(1, Pool)));
            var leader = AddPlayer(world, new Vec2(-5f, 1f), seat: 0);
            var lagger = AddPlayer(world, new Vec2(-5f, -1f), seat: 1);
            WaitFirstWave(world);
            DefeatEnemies(world);

            leader.Position = new Vec2(StageLayout.AreaMin(1).X + Balance.AreaEnterDepth + 0.5f, 1f);
            TickFor(world, 2);

            Assert.That(lagger.Position.X, Is.GreaterThanOrEqualTo(StageLayout.AreaMin(1).X + lagger.Radius - Tolerance));
        }

        [Test]
        public void LastAreaCleared_IsCleared()
        {
            var world = CreateStage(new AreaDef(new WaveDef(2, Pool)));
            AddPlayer(world, new Vec2(-5f, 0f));
            WaitFirstWave(world);

            DefeatEnemies(world);

            Assert.That(world.Result, Is.EqualTo(StageResult.Cleared));
            Assert.That(world.EnemiesDefeated, Is.EqualTo(2));
        }

        [Test]
        public void BossWave_SpawnsBigStrongBoss()
        {
            var world = CreateStage(new AreaDef(new WaveDef(0, Pool, bossSpeciesNo: 4)));
            AddPlayer(world, new Vec2(-5f, 0f));
            WaitFirstWave(world);

            Unit boss = null;
            foreach (var unit in world.Units)
            {
                if (unit.Team == Team.Enemy)
                {
                    boss = unit;
                }
            }

            var species = SpeciesTable.Get(4);
            Assert.That(boss, Is.Not.Null);
            Assert.That(boss.IsBoss, Is.True);
            Assert.That(boss.SpeciesNo, Is.EqualTo(4));
            Assert.That(boss.Radius, Is.EqualTo(Balance.PenguinRadius * Balance.BossRadiusScale).Within(Tolerance));
            Assert.That(boss.MaxHp, Is.GreaterThan(SpeciesTable.EnemyMaxHp(species, 1) * 10));
            Assert.That(boss.Skills.Length, Is.EqualTo(2));
        }

        [Test]
        public void Boss_IsKnockedBackLessThanNormalEnemy()
        {
            var world = Create();
            var skill = Melee();
            skill.Knockback = 8f;
            var player = world.AddUnit(Team.Player, Stats(skill), Vec2.Zero);
            var normal = world.AddUnit(Team.Enemy, SpeciesTable.EnemyStats(SpeciesTable.Get(1), 1), new Vec2(0.95f, 0.3f));
            var boss = world.AddUnit(Team.Enemy, SpeciesTable.BossStats(SpeciesTable.Get(1), 1), new Vec2(1.4f, -0.6f));

            Press(world, player, new PlayerInput(Vec2.Zero, skill1: true, skill2: false));

            Assert.That(boss.KnockbackVelocity.Length, Is.LessThan(normal.KnockbackVelocity.Length));
        }
    }
}
