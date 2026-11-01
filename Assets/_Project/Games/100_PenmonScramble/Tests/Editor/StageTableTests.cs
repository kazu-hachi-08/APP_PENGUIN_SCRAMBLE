using NUnit.Framework;
using PenmonScramble.Core;

namespace PenmonScramble.Tests
{
    /// <summary>ステージの定義表（§6）: 5ワールド×3ステージ・最後のエリアのボス・ワールドの強さ・解放の順番</summary>
    public class StageTableTests
    {
        [Test]
        public void HasFifteenStages_NumberedInOrder()
        {
            Assert.That(Balance.WorldCount, Is.EqualTo(5));
            Assert.That(StageTable.Count, Is.EqualTo(Balance.WorldCount * StageTable.StagesPerWorld));
            for (var no = 1; no <= StageTable.Count; no++)
            {
                Assert.That(StageTable.Get(no).No, Is.EqualTo(no));
            }

            Assert.That(StageTable.Get(0), Is.Null);
            Assert.That(StageTable.Get(StageTable.Count + 1), Is.Null);
        }

        [Test]
        public void LastArea_HasBoss_AndWorldsEndWithBigBoss()
        {
            for (var no = 1; no <= StageTable.Count; no++)
            {
                var stage = StageTable.Get(no);
                var bossNo = stage.Areas[stage.Areas.Count - 1].Waves[0].BossSpeciesNo;
                var isWorldEnd = no % StageTable.StagesPerWorld == 0;
                Assert.That(BigBossTable.Get(bossNo) != null, Is.EqualTo(isWorldEnd), $"ステージ {no}");
                if (!isWorldEnd)
                {
                    Assert.That(SpeciesTable.Get(bossNo), Is.Not.Null, $"ステージ {no}");
                }
            }
        }

        [Test]
        public void EnemyPools_UseKnownSpecies()
        {
            for (var no = 1; no <= StageTable.Count; no++)
            {
                foreach (var area in StageTable.Get(no).Areas)
                {
                    foreach (var wave in area.Waves)
                    {
                        foreach (var species in wave.SpeciesPool)
                        {
                            Assert.That(SpeciesTable.Get(species), Is.Not.Null, $"ステージ {no}");
                        }
                    }
                }
            }
        }

        [Test]
        public void LaterWorlds_HaveStrongerEnemies()
        {
            var species = SpeciesTable.Get(1);
            var world1 = SpeciesTable.EnemyStats(species, playerCount: 1, world: 1);
            var world3 = SpeciesTable.EnemyStats(species, playerCount: 1, world: 3);

            Assert.That(world3.MaxHp, Is.GreaterThan(world1.MaxHp));
            Assert.That(world3.Attack, Is.GreaterThan(world1.Attack));
            Assert.That(StageTable.WorldOf(4), Is.EqualTo(2));
            Assert.That(new StageWorld(StageTable.Get(9), 1, seed: 1).World, Is.EqualTo(3));
        }

        [Test]
        public void Worlds4And5_EndWithNewBigBosses()
        {
            Assert.That(LastBoss(12).SpeciesNo, Is.EqualTo(BigBossTable.FulmarNo));
            Assert.That(LastBoss(15).SpeciesNo, Is.EqualTo(BigBossTable.SharkNo));
            Assert.That(StageTable.WorldOf(12), Is.EqualTo(4));
            Assert.That(StageTable.WorldOf(15), Is.EqualTo(5));
            Assert.That(new StageWorld(StageTable.Get(15), 1, seed: 1).World, Is.EqualTo(5));
        }

        [TestCase(10, 15)]
        [TestCase(11, 18)]
        [TestCase(13, 5)]
        [TestCase(14, 20)]
        public void Worlds4And5_FirstTwoStages_FightGiantInArena(int stageNo, int speciesNo)
        {
            var stage = StageTable.Get(stageNo);
            var last = stage.Areas[stage.Areas.Count - 1];

            Assert.That(last.IsArena, Is.True);
            Assert.That(LastBoss(stageNo).SpeciesNo, Is.EqualTo(speciesNo));
            Assert.That(LastBoss(stageNo).Rank, Is.EqualTo(BossRank.Giant));
            // 闘技場はボスのエリアだけ（ストーリーは闘技場1つで、ボスラッシュの「ROUND」の表示は出さない）
            Assert.That(stage.ArenaCount, Is.EqualTo(1));
        }

        [Test]
        public void Stages1To9_KeepTheirNumbersAndNames()
        {
            // 記録のクリア済みは番号で持つので、前からあるステージの番号と中身の位置は変えない
            Assert.That(StageTable.Get(1).Name, Is.EqualTo("海岸 1"));
            Assert.That(StageTable.Get(9).Name, Is.EqualTo("深海 3"));
            Assert.That(LastBoss(9).SpeciesNo, Is.EqualTo(BigBossTable.OrcaNo));
        }

        [Test]
        public void Unlock_GoesInOrder_ThroughWorld5()
        {
            var record = new PlayerRecord(SaveData.CreateNew());
            for (var no = 1; no <= StageTable.Count; no++)
            {
                Assert.That(record.IsUnlocked(no), Is.True, $"ステージ {no}");
                Assert.That(record.IsUnlocked(no + 1), Is.False, $"ステージ {no + 1}");
                var reward = record.RecordStage(no, cleared: true, enemiesDefeated: 0);
                Assert.That(reward.UnlockedStageNo, Is.EqualTo(no < StageTable.Count ? no + 1 : 0), $"ステージ {no}");
            }

            Assert.That(record.WorldsUnlocked, Is.GreaterThanOrEqualTo(Balance.WorldCount));
        }

        [Test]
        public void GachaPower_And_EnemyScale_RiseThroughWorld5()
        {
            for (var world = 2; world <= Balance.WorldCount; world++)
            {
                Assert.That(Balance.WorldEnemyScale(world), Is.GreaterThan(Balance.WorldEnemyScale(world - 1)), $"ワールド {world}");
                Assert.That(Balance.PowerMin(world), Is.GreaterThan(Balance.PowerMin(world - 1)), $"ワールド {world}");
                Assert.That(Balance.PowerMax(world), Is.GreaterThan(Balance.PowerMax(world - 1)), $"ワールド {world}");
            }
        }

        private static BossSpawn LastBoss(int stageNo)
        {
            var stage = StageTable.Get(stageNo);
            return stage.Areas[stage.Areas.Count - 1].Waves[0].Bosses[0];
        }
    }
}
