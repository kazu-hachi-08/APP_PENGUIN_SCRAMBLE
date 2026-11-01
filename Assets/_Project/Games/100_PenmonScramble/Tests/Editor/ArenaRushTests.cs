using NUnit.Framework;
using PenmonScramble.Core;
using static PenmonScramble.Tests.TestWorld;

namespace PenmonScramble.Tests
{
    /// <summary>闘技場の連戦（ボスラッシュの土台）: 闘技場が続く形と通り道・闘技場から始まる・戦いの合間の回復・難易度・通信</summary>
    public class ArenaRushTests
    {
        private static readonly int[] Pool = { 1 };

        /// <summary>闘技場 → 闘技場 の2戦。どちらも雑魚1体だけ</summary>
        private static StageDef TwoArenas() => new StageDef(DevStageTable.ArenaGauntletNo, "test", 1,
            AreaDef.Arena(new WaveDef(1, Pool)), AreaDef.Arena(new WaveDef(1, Pool)));

        private static Unit AddPlayer(StageWorld world, Vec2 position, int seat = 0)
        {
            return world.AddUnit(Team.Player, new[] { Stats(Melee()), Stats(Melee()) }, position, seat);
        }

        /// <summary>最初の波を出して、敵を全部倒す</summary>
        private static void DefeatArena(StageWorld world)
        {
            TickFor(world, Seconds(Balance.FirstWaveDelaySeconds) + 1);
            foreach (var unit in world.Units)
            {
                if (unit.Team == Team.Enemy)
                {
                    unit.Hp = 0;
                }
            }

            world.Tick();
            world.Tick();
        }

        [Test]
        public void ConsecutiveArenas_AreJoinedByAShortCorridor()
        {
            var stage = DevStageTable.ArenaGauntlet;
            var first = StageLayout.Area(stage, 0);
            var second = StageLayout.Area(stage, 1);

            Assert.That(first.IsEllipse, Is.True);
            Assert.That(first.Center.X, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(second.Min.X - first.Max.X, Is.EqualTo(Balance.ArenaCorridorLength).Within(Tolerance));

            var corridor = StageLayout.Passage(stage, 1);
            Assert.That(corridor.Min.X, Is.EqualTo(first.Max.X - Balance.AreaEnterDepth).Within(Tolerance));
            Assert.That(corridor.Max.X, Is.EqualTo(second.Min.X + Balance.AreaEnterDepth).Within(Tolerance));
            Assert.That(corridor.Height, Is.EqualTo(Balance.ArenaDoorHalfWidth * 2f).Within(Tolerance));
        }

        [Test]
        public void ArenaPassages_OnlyBetweenAreas()
        {
            var stage = DevStageTable.ArenaGauntlet;

            Assert.That(StageLayout.HasPassage(stage, 0), Is.False);
            Assert.That(StageLayout.HasPassage(stage, 1), Is.True);
            Assert.That(StageLayout.HasExitPassage(stage, 0), Is.True);
            Assert.That(StageLayout.HasExitPassage(stage, 2), Is.False);
        }

        [Test]
        public void StoryStages_KeepTheirLayout()
        {
            // 闘技場が1つだけのストーリーでは、すき間は空かない（エリアの位置は前と同じ）
            var stage = StageTable.Get(3);
            var last = stage.Areas.Count - 1;
            Assert.That(StageLayout.Area(stage, last).Min.X, Is.EqualTo((last - 0.5f) * Balance.AreaWidth).Within(Tolerance));
            Assert.That(StageLayout.Passage(stage, last), Is.EqualTo(StageLayout.Doorway(StageLayout.Area(stage, last))));
        }

        [Test]
        public void ArenaRounds_AreCountedPerArena()
        {
            var stage = DevStageTable.ArenaGauntlet;

            Assert.That(stage.ArenaCount, Is.EqualTo(3));
            Assert.That(stage.ArenaRoundOf(0), Is.EqualTo(1));
            Assert.That(stage.ArenaRoundOf(2), Is.EqualTo(3));
            Assert.That(StageTable.Get(3).ArenaRoundOf(0), Is.EqualTo(0));
        }

        [Test]
        public void GiantArea_IsArenaWithoutBigBoss()
        {
            var area = DevStageTable.ArenaGauntlet.Areas[0];

            Assert.That(area.IsArena, Is.True);
            Assert.That(area.Waves[0].Bosses.Count, Is.EqualTo(2));
            Assert.That(area.Waves[0].Bosses[0].Rank, Is.EqualTo(BossRank.Giant));
        }

        [Test]
        public void DevStage_IsFoundButNotStory()
        {
            Assert.That(Stages.Get(DevStageTable.ArenaGauntletNo), Is.SameAs(DevStageTable.ArenaGauntlet));
            Assert.That(Stages.IsDev(DevStageTable.ArenaGauntletNo), Is.True);
            Assert.That(Stages.IsDev(1), Is.False);
            Assert.That(StageTable.Get(DevStageTable.ArenaGauntletNo), Is.Null);
        }

        [Test]
        public void RushStage_StartsStandingInTheArena()
        {
            var players = TrialStage.Create(DevStageTable.ArenaGauntlet, 1, new[] { 0, 1 }, new[] { new[] { Stats(Melee()) }, new[] { Stats(Melee()) } }, out var world);

            Assert.That(world.Area.IsEllipse, Is.True);
            Assert.That(world.ArenaRound, Is.EqualTo(1));
            foreach (var player in players)
            {
                Assert.That(world.Area.Contains(player.Position, player.Radius), Is.True);
            }
        }

        [Test]
        public void WinningARound_HealsAndRevives_ThenOpensTheCorridor()
        {
            var world = new StageWorld(TwoArenas(), playerCount: 2, seed: 1);
            var hurt = AddPlayer(world, new Vec2(-10f, 0f), seat: 0);
            var downed = AddPlayer(world, new Vec2(-10f, 2f), seat: 1);
            hurt.Hp = 10;
            foreach (var member in downed.Members)
            {
                member.Hp = 0;
            }

            DefeatArena(world);

            Assert.That(world.IsGateOpen, Is.True);
            Assert.That(world.ArenaRoundsWon, Is.EqualTo(1));
            Assert.That(hurt.Hp, Is.EqualTo(10 + UnitStats.HpOf(hurt.MaxHp * Balance.RoundClearHealRatio)));
            Assert.That(hurt.Members[1].Hp, Is.EqualTo(hurt.Members[1].MaxHp), "控えも回復するが、最大を超えない");
            Assert.That(downed.IsDown, Is.False);
            Assert.That(downed.Hp, Is.EqualTo(UnitStats.HpOf(downed.MaxHp * Balance.RoundClearReviveRatio)));
            Assert.That(downed.Members[1].Hp, Is.EqualTo(0), "倒れた控えは起こさない");
        }

        [Test]
        public void WalkingThroughTheCorridor_StartsTheNextRound()
        {
            var stage = TwoArenas();
            var world = new StageWorld(stage, playerCount: 1, seed: 1);
            var player = AddPlayer(world, new Vec2(-10f, 0f));
            DefeatArena(world);

            // 通り道の真ん中は、どちらの楕円の外でも動ける範囲の中
            var corridor = StageLayout.Passage(stage, 1);
            player.Position = corridor.Center;
            world.Tick();
            Assert.That(player.Position.X, Is.EqualTo(corridor.Center.X).Within(Tolerance));
            Assert.That(world.AreaIndex, Is.EqualTo(0));

            player.Position = new Vec2(StageLayout.Area(stage, 1).Min.X + Balance.AreaEnterDepth + 0.5f, 0f);
            world.Tick();

            Assert.That(world.AreaIndex, Is.EqualTo(1));
            Assert.That(world.ArenaRound, Is.EqualTo(2));
            Assert.That(world.IsGateOpen, Is.False);
        }

        [Test]
        public void ClearingTheLastArena_CountsEveryRound()
        {
            var stage = TwoArenas();
            var world = new StageWorld(stage, playerCount: 1, seed: 1);
            var player = AddPlayer(world, new Vec2(-10f, 0f));
            DefeatArena(world);
            player.Position = new Vec2(StageLayout.Area(stage, 1).Min.X + Balance.AreaEnterDepth + 0.5f, 0f);
            world.Tick();
            DefeatArena(world);

            Assert.That(world.Result, Is.EqualTo(StageResult.Cleared));
            Assert.That(world.ArenaRoundsWon, Is.EqualTo(2));
        }

        [TestCase(Difficulty.Normal, 1f, 1f, 1f)]
        [TestCase(Difficulty.Hard, 2f, 1.6f, 0.9f)]
        [TestCase(Difficulty.Extreme, 3.5f, 3f, 0.65f)]
        public void Difficulty_ScalesEnemies(Difficulty difficulty, float hp, float attack, float telegraph)
        {
            Assert.That(Balance.DifficultyHpScale(difficulty), Is.EqualTo(hp));
            Assert.That(Balance.DifficultyAttackScale(difficulty), Is.EqualTo(attack));
            Assert.That(Balance.DifficultyTelegraphScale(difficulty), Is.EqualTo(telegraph));

            var stage = new StageDef(1, "test", new AreaDef(new WaveDef(1, Pool)));
            var world = new StageWorld(stage, playerCount: 1, seed: 1, difficulty);
            TickFor(world, Seconds(Balance.FirstWaveDelaySeconds) + 1);
            var enemy = world.Units[0];
            var normal = SpeciesTable.EnemyStats(SpeciesTable.Get(1), playerCount: 1);

            Assert.That(enemy.MaxHp, Is.EqualTo(UnitStats.HpOf(normal.MaxHp * hp)));
            Assert.That(enemy.Attack, Is.EqualTo(normal.Attack * attack).Within(Tolerance));
        }

        [TestCase(Difficulty.Normal, 1.5f)]
        [TestCase(Difficulty.Extreme, 1.5f * 2f)]
        public void ArenaEnemyScale_MultipliesHpAndAttack_AndExtremeAddsMore(Difficulty difficulty, float scale)
        {
            var stage = new StageDef(1, "test", 1, AreaDef.Arena(1.5f, 2f, new WaveDef(1, Pool)));
            var world = new StageWorld(stage, playerCount: 1, seed: 1, difficulty);
            TickFor(world, Seconds(Balance.FirstWaveDelaySeconds) + 1);
            var enemy = world.Units[0];
            var normal = SpeciesTable.EnemyStats(SpeciesTable.Get(1), playerCount: 1);

            Assert.That(enemy.MaxHp, Is.EqualTo(UnitStats.HpOf(normal.MaxHp * Balance.DifficultyHpScale(difficulty) * scale)));
            Assert.That(enemy.Attack, Is.EqualTo(normal.Attack * Balance.DifficultyAttackScale(difficulty) * scale).Within(Tolerance));
        }

        [Test]
        public void RushRounds_HaveTheirOwnEnemyScale()
        {
            for (var no = RushTable.FirstNo; no <= RushTable.LastNo; no++)
            {
                foreach (var area in RushTable.Get(no).Areas)
                {
                    Assert.That(area.EnemyScale, Is.GreaterThan(0f), $"ステージ {no}");
                    Assert.That(area.ExtremeScale, Is.GreaterThan(0f), $"ステージ {no}");
                }
            }

            // ストーリーのエリアは倍率を持たない
            foreach (var area in StageTable.Stage1.Areas)
            {
                Assert.That(area.EnemyScale, Is.EqualTo(1f));
                Assert.That(area.ExtremeScale, Is.EqualTo(1f));
            }
        }

        [Test]
        public void Difficulty_ShortensEnemyWindUp()
        {
            var stage = new StageDef(1, "test", new AreaDef(new WaveDef(1, Pool)));
            var world = new StageWorld(stage, playerCount: 1, seed: 1, Difficulty.Extreme);
            TickFor(world, Seconds(Balance.FirstWaveDelaySeconds) + 1);
            var enemy = world.Units[0];
            AddPlayer(world, enemy.Position + new Vec2(enemy.Radius + 0.6f, 0f));
            enemy.Cooldowns[0] = 0f;

            for (var i = 0; i < Seconds(2f) && enemy.WindUp == null; i++)
            {
                world.Tick();
            }

            Assert.That(enemy.WindUp, Is.Not.Null);
            Assert.That(enemy.WindUp.TotalSeconds, Is.EqualTo(Balance.EnemyWindUpSeconds * Balance.DifficultyTelegraphScale(Difficulty.Extreme)).Within(Tolerance));
        }

        [Test]
        public void Story_IsAlwaysNormal()
        {
            TrialStage.Create(StageTable.Stage1, 1, new[] { 0 }, new[] { new[] { Stats(Melee()) } }, out var world);

            Assert.That(world.Difficulty, Is.EqualTo(Difficulty.Normal));
        }

        [Test]
        public void Snapshot_SendsDifficultyAndRound()
        {
            var world = new StageWorld(TwoArenas(), playerCount: 1, seed: 1, Difficulty.Hard);
            AddPlayer(world, new Vec2(-10f, 0f));
            var recorder = new StageRecorder(world, round: 1);
            DefeatArena(world);

            var decoded = StageCodec.DecodeSnapshot(StageCodec.EncodeSnapshot(recorder.Capture()));

            Assert.That(decoded.StageNo, Is.EqualTo(DevStageTable.ArenaGauntletNo));
            Assert.That(decoded.Difficulty, Is.EqualTo(Difficulty.Hard));
            Assert.That(decoded.ArenaRound, Is.EqualTo(1));
            Assert.That(decoded.ArenaRoundsWon, Is.EqualTo(1));
        }

        [Test]
        public void Snapshot_SendsSeriousAndEnragedFlags()
        {
            var snapshot = new StageSnapshot();
            snapshot.Units.Add(new UnitState(1, Team.Enemy, Unit.NoSeat, BigBossTable.OrcaNo, Vec2.Zero, Vec2.Right, 10, 100, 1.2f,
                UnitFlags.Boss | UnitFlags.Serious | UnitFlags.Enraged));

            var decoded = StageCodec.DecodeSnapshot(StageCodec.EncodeSnapshot(snapshot));

            Assert.That(decoded.Units[0].Has(UnitFlags.Serious), Is.True);
            Assert.That(decoded.Units[0].Has(UnitFlags.Enraged), Is.True);
            Assert.That(decoded.Units[0].Has(UnitFlags.Boss), Is.True);
        }
    }
}
