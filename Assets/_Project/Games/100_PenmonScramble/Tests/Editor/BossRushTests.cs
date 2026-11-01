using NUnit.Framework;
using PenmonScramble.Core;
using static PenmonScramble.Tests.TestWorld;

namespace PenmonScramble.Tests
{
    /// <summary>ボスラッシュ（§6.6）: 5ステージの定義・解放の順番・報酬の計算・ゲストの記録・待ちの窓の選択の通信</summary>
    public class BossRushTests
    {
        private static PlayerRecord NewRecord() => new PlayerRecord(SaveData.CreateNew());

        [Test]
        public void HasFiveStages_NumberedFrom101()
        {
            Assert.That(RushTable.Count, Is.EqualTo(5));
            for (var no = RushTable.FirstNo; no <= RushTable.LastNo; no++)
            {
                var stage = RushTable.Get(no);
                Assert.That(stage.No, Is.EqualTo(no));
                Assert.That(Stages.Get(no), Is.SameAs(stage));
                // 通信ではステージの番号を 1 バイトで送る
                Assert.That(no, Is.LessThanOrEqualTo(byte.MaxValue));
                Assert.That(stage.World, Is.EqualTo(no - RushTable.FirstNo + 1), $"ステージ {no}");
                Assert.That(Stages.IsDev(no), Is.False);
            }

            Assert.That(RushTable.Get(RushTable.FirstNo - 1), Is.Null);
            Assert.That(RushTable.Get(RushTable.LastNo + 1), Is.Null);
        }

        [Test]
        public void EveryStage_IsThreeArenas_GiantsThenBigBossThenSerious()
        {
            for (var no = RushTable.FirstNo; no <= RushTable.LastNo; no++)
            {
                var stage = RushTable.Get(no);
                Assert.That(stage.Areas.Count, Is.EqualTo(RushTable.RoundsPerStage), $"ステージ {no}");
                Assert.That(stage.ArenaCount, Is.EqualTo(RushTable.RoundsPerStage), $"ステージ {no}");
                // 最初から闘技場に立って始める（入口の通り道が無い）
                Assert.That(StageLayout.HasPassage(stage, 0), Is.False);

                var giants = RushTable.BossesOf(stage, 1);
                Assert.That(giants.Count, Is.EqualTo(2), $"ステージ {no}");
                foreach (var giant in giants)
                {
                    Assert.That(giant.Rank, Is.EqualTo(BossRank.Giant));
                    Assert.That(SpeciesTable.Get(giant.SpeciesNo), Is.Not.Null);
                }

                var boss = RushTable.BossesOf(stage, 2);
                Assert.That(boss.Count, Is.EqualTo(1));
                Assert.That(boss[0].Rank, Is.EqualTo(BossRank.Normal));
                Assert.That(BigBossTable.Get(boss[0].SpeciesNo), Is.Not.Null);

                var serious = RushTable.BossesOf(stage, 3);
                Assert.That(serious.Count, Is.EqualTo(1));
                Assert.That(serious[0].Rank, Is.EqualTo(BossRank.Serious));
                Assert.That(BigBossTable.Get(serious[0].SpeciesNo), Is.Not.Null);
                Assert.That(serious[0].SpeciesNo, Is.Not.EqualTo(boss[0].SpeciesNo), $"ステージ {no}: 2戦目と3戦目は別の大ボス");
            }
        }

        [TestCase(101, BigBossTable.LeopardSealNo, BigBossTable.SkuaNo)]
        [TestCase(102, BigBossTable.SkuaNo, BigBossTable.LeopardSealNo)]
        [TestCase(103, BigBossTable.FulmarNo, BigBossTable.OrcaNo)]
        [TestCase(104, BigBossTable.OrcaNo, BigBossTable.FulmarNo)]
        [TestCase(105, BigBossTable.FulmarNo, BigBossTable.SharkNo)]
        public void Lineup_MatchesThePlan(int no, int bigBoss, int seriousBoss)
        {
            var stage = RushTable.Get(no);
            Assert.That(RushTable.BossesOf(stage, 2)[0].SpeciesNo, Is.EqualTo(bigBoss));
            Assert.That(RushTable.BossesOf(stage, 3)[0].SpeciesNo, Is.EqualTo(seriousBoss));
        }

        [Test]
        public void World_StartsInTheFirstArena_WithBothGiants()
        {
            var stage = RushTable.Get(RushTable.FirstNo);
            var world = new StageWorld(stage, playerCount: 1, seed: 1);
            TickFor(world, Seconds(Balance.FirstWaveDelaySeconds) + 1);

            var giants = 0;
            foreach (var unit in world.Units)
            {
                giants += unit.Team == Team.Enemy && unit.IsGiant ? 1 : 0;
            }

            Assert.That(world.ArenaRound, Is.EqualTo(1));
            Assert.That(giants, Is.EqualTo(2));
        }

        [Test]
        public void Unlock_FirstStageNormalOnly_AtTheStart()
        {
            var record = NewRecord();

            Assert.That(record.IsRushUnlocked(101, Difficulty.Normal), Is.True);
            Assert.That(record.IsRushUnlocked(101, Difficulty.Hard), Is.False);
            Assert.That(record.IsRushUnlocked(102, Difficulty.Normal), Is.False);
            Assert.That(record.IsRushUnlocked(100, Difficulty.Normal), Is.False);
            Assert.That(record.IsRushUnlocked(1, Difficulty.Normal), Is.False);
        }

        [Test]
        public void Unlock_NextStageByNormal_NextDifficultyByTheOneBelow()
        {
            var record = NewRecord();
            record.RecordRush(101, Difficulty.Normal, cleared: true, roundsWon: 3, enemiesDefeated: 10);

            Assert.That(record.IsRushUnlocked(102, Difficulty.Normal), Is.True);
            Assert.That(record.IsRushUnlocked(101, Difficulty.Hard), Is.True);
            Assert.That(record.IsRushUnlocked(101, Difficulty.Extreme), Is.False);
            Assert.That(record.IsRushUnlocked(102, Difficulty.Hard), Is.False);

            // むずかしいをクリアしても、次のステージのむずかしいは開かない（次のステージのふつうから）
            record.RecordRush(101, Difficulty.Hard, cleared: true, roundsWon: 3, enemiesDefeated: 10);
            Assert.That(record.IsRushUnlocked(101, Difficulty.Extreme), Is.True);
            Assert.That(record.IsRushUnlocked(102, Difficulty.Hard), Is.False);
        }

        [Test]
        public void Reward_FirstClear_AddsTheSameAgain_AndReportsUnlocks()
        {
            var record = NewRecord();
            var reward = record.RecordRush(101, Difficulty.Normal, cleared: true, roundsWon: 3, enemiesDefeated: 10);

            Assert.That(reward.Coins, Is.EqualTo(Balance.RushClearCoins(101, Difficulty.Normal)));
            Assert.That(reward.FirstClearBonus, Is.EqualTo(Balance.RushClearCoins(101, Difficulty.Normal)));
            Assert.That(reward.UnlockedStageNo, Is.EqualTo(102));
            Assert.That(reward.UnlockedDifficulty, Is.EqualTo(Difficulty.Hard));
            Assert.That(record.Coins, Is.EqualTo(reward.Total));

            var again = record.RecordRush(101, Difficulty.Normal, cleared: true, roundsWon: 3, enemiesDefeated: 10);
            Assert.That(again.FirstClearBonus, Is.EqualTo(0));
            Assert.That(again.UnlockedStageNo, Is.EqualTo(0));
            Assert.That(again.UnlockedDifficulty, Is.Null);
        }

        [TestCase(101, Difficulty.Normal, 210)]
        [TestCase(103, Difficulty.Normal, 350)]
        [TestCase(103, Difficulty.Hard, 600)]
        [TestCase(103, Difficulty.Extreme, 900)]
        [TestCase(105, Difficulty.Extreme, 1260)]
        public void Reward_ClearCoins_DependOnStageAndDifficulty(int stageNo, Difficulty difficulty, int coins)
        {
            Assert.That(Balance.RushClearCoins(stageNo, difficulty), Is.EqualTo(coins));
        }

        [Test]
        public void Reward_ClearCoins_GrowWithStageAndDifficulty()
        {
            // 先のステージ・上の難易度ほど多い（育ちきった編成で前のステージを回すのが一番もうかる、にならないように）
            for (var no = RushTable.FirstNo; no <= RushTable.LastNo; no++)
            {
                for (var d = 1; d < Balance.DifficultyCount; d++)
                {
                    Assert.That(Balance.RushClearCoins(no, (Difficulty)d), Is.GreaterThan(Balance.RushClearCoins(no, (Difficulty)(d - 1))));
                }

                if (no > RushTable.FirstNo)
                {
                    Assert.That(Balance.RushClearCoins(no, Difficulty.Normal), Is.GreaterThan(Balance.RushClearCoins(no - 1, Difficulty.Normal)));
                }
            }
        }

        [Test]
        public void Reward_Fail_PaysPerRoundWonAndPerEnemy()
        {
            var record = NewRecord();
            var reward = record.RecordRush(101, Difficulty.Hard, cleared: false, roundsWon: 2, enemiesDefeated: 10);

            // 101 のむずかしいのクリア 360 × 15% = 54 を勝った戦いの数だけ ＋ 倒した数 × 1
            Assert.That(reward.Coins, Is.EqualTo(2 * 54 + 10 * Balance.FailCoinsPerEnemy));
            Assert.That(reward.FirstClearBonus, Is.EqualTo(0));
            Assert.That(record.IsRushCleared(101, Difficulty.Hard), Is.False);
        }

        [Test]
        public void Reward_LastStageAndHardest_UnlockNothingMore()
        {
            var record = NewRecord();
            var last = record.RecordRush(RushTable.LastNo, Difficulty.Normal, cleared: true, roundsWon: 3, enemiesDefeated: 0);
            Assert.That(last.UnlockedStageNo, Is.EqualTo(0));
            Assert.That(last.UnlockedDifficulty, Is.EqualTo(Difficulty.Hard));

            var hardest = record.RecordRush(101, Difficulty.Extreme, cleared: true, roundsWon: 3, enemiesDefeated: 0);
            Assert.That(hardest.UnlockedDifficulty, Is.Null);
        }

        [Test]
        public void Guest_ClearOfALockedStage_IsRecordedForThemToo()
        {
            var guest = NewRecord();
            Assert.That(guest.IsRushUnlocked(104, Difficulty.Hard), Is.False);

            var reward = guest.RecordRush(104, Difficulty.Hard, cleared: true, roundsWon: 3, enemiesDefeated: 5);

            Assert.That(guest.IsRushCleared(104, Difficulty.Hard), Is.True);
            Assert.That(guest.IsRushUnlocked(104, Difficulty.Extreme), Is.True);
            Assert.That(reward.FirstClearBonus, Is.EqualTo(Balance.RushFirstClearBonus(104, Difficulty.Hard)));
            // ストーリーのクリア済みには何も付かない
            Assert.That(guest.Data.clearedStages, Is.Empty);
        }

        [Test]
        public void Record_IgnoresUnknownStages()
        {
            var record = NewRecord();
            var reward = record.RecordRush(3, Difficulty.Normal, cleared: true, roundsWon: 3, enemiesDefeated: 5);

            Assert.That(reward.Total, Is.EqualTo(0));
            Assert.That(record.Data.clearedRush, Is.Empty);
        }

        [Test]
        public void RushKey_IsStageTimesTenPlusDifficulty()
        {
            Assert.That(PlayerRecord.RushKey(103, Difficulty.Extreme), Is.EqualTo(1032));
        }

        [Test]
        public void Choice_RoundTrips()
        {
            var data = StageCodec.EncodeRushChoice(new RushChoice(104, Difficulty.Hard));

            Assert.That(StageCodec.TryDecodeRushChoice(data, out var choice), Is.True);
            Assert.That(choice.StageNo, Is.EqualTo(104));
            Assert.That(choice.Difficulty, Is.EqualTo(Difficulty.Hard));
        }

        [Test]
        public void Choice_RejectsBrokenValues()
        {
            Assert.That(StageCodec.TryDecodeRushChoice(StageCodec.EncodeRushChoice(new RushChoice(3, Difficulty.Normal)), out _), Is.False);
            Assert.That(StageCodec.TryDecodeRushChoice(StageCodec.EncodeRushChoice(new RushChoice(101, (Difficulty)7)), out _), Is.False);
            Assert.That(StageCodec.TryDecodeRushChoice(new byte[] { 0 }, out _), Is.False);
        }
    }
}
