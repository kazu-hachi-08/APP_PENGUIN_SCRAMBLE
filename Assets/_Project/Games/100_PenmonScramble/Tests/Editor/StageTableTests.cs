using NUnit.Framework;
using PenmonScramble.Core;

namespace PenmonScramble.Tests
{
    /// <summary>ステージの定義表（§6）: 3ワールド×3ステージ・最後のエリアのボス・ワールドの強さ</summary>
    public class StageTableTests
    {
        [Test]
        public void HasNineStages_NumberedInOrder()
        {
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
    }
}
