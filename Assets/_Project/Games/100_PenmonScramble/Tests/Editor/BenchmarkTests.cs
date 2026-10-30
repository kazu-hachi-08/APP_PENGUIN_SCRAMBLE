using NUnit.Framework;
using PenmonScramble.Core;
using PenmonScramble.Game;

namespace PenmonScramble.Tests
{
    public class BenchmarkTests
    {
        [Test]
        public void FrameStats_AverageLowAndWorst()
        {
            var stats = new FrameStats();
            // 99 フレームが 1/60 秒、1 フレームだけ 0.1 秒止まった
            for (var i = 0; i < 99; i++)
            {
                stats.Add(1f / 60f);
            }

            stats.Add(0.1f);

            Assert.That(stats.Frames, Is.EqualTo(100));
            Assert.That(stats.AverageFps, Is.EqualTo(100f / (99f / 60f + 0.1f)).Within(0.01f));
            Assert.That(stats.LowFps, Is.EqualTo(10f).Within(0.01f));
            Assert.That(stats.WorstMs, Is.EqualTo(100f).Within(0.01f));
        }

        [Test]
        public void FrameStats_IgnoresZeroFrames()
        {
            var stats = new FrameStats();
            stats.Add(0f);
            Assert.That(stats.Frames, Is.Zero);
            Assert.That(stats.AverageFps, Is.Zero);
            Assert.That(stats.LowFps, Is.Zero);
        }

        [Test]
        public void HeaviestArea_HasTheMostEnemies()
        {
            var heaviest = StageBenchmark.Scenario.HeaviestArea(out var stage, out var areaIndex);
            Assert.That(stage.Areas[areaIndex], Is.SameAs(heaviest));
            for (var no = 1; no <= StageTable.Count; no++)
            {
                foreach (var area in StageTable.Get(no).Areas)
                {
                    Assert.That(EnemyCount(area), Is.LessThanOrEqualTo(EnemyCount(heaviest)));
                }
            }
        }

        [Test]
        public void Scenarios_AreOneAreaStagesThatCanBePlayed()
        {
            foreach (var scenario in StageBenchmark.Scenario.CreateAll())
            {
                Assert.That(scenario.Stage.Areas.Count, Is.EqualTo(1), scenario.Name);
                var seats = new[] { 0, 1, 2, 3 };
                var parties = new UnitStats[seats.Length][];
                for (var i = 0; i < seats.Length; i++)
                {
                    parties[i] = TrialStage.PartyStats(TrialStage.RotatingParty(1, i, seats.Length));
                }

                var players = TrialStage.Create(scenario.Stage, 1, seats, parties, out var world);
                for (var tick = 0; tick < 300; tick++)
                {
                    foreach (var player in players)
                    {
                        world.SetInput(player.Id, StageBot.InputFor(world, player, tick * Balance.TickSeconds));
                    }

                    world.Tick();
                }

                Assert.That(world.Units.Count, Is.GreaterThan(players.Length), scenario.Name);
            }
        }

        [Test]
        public void StageBot_WalksTowardTheNearestEnemy()
        {
            var players = TrialStage.Create(StageTable.Stage1, 1, new[] { 0 }, new[] { TrialStage.PartyStats(TrialStage.RotatingParty(1, 0, 1)) }, out var world);
            for (var tick = 0; tick < 60 && world.Units.Count == 1; tick++)
            {
                world.Tick();
            }

            var player = players[0];
            var input = StageBot.InputFor(world, player, 0f);
            Unit nearest = null;
            foreach (var unit in world.Units)
            {
                if (unit.Team == Team.Enemy && (nearest == null || (unit.Position - player.Position).Length < (nearest.Position - player.Position).Length))
                {
                    nearest = unit;
                }
            }

            Assert.That(nearest, Is.Not.Null);
            var toEnemy = (nearest.Position - player.Position).Normalized;
            Assert.That(Vec2.Dot(input.Move, toEnemy), Is.GreaterThan(0.99f));
        }

        private static int EnemyCount(AreaDef area)
        {
            var count = 0;
            foreach (var wave in area.Waves)
            {
                count += wave.Count;
            }

            return count;
        }
    }
}
