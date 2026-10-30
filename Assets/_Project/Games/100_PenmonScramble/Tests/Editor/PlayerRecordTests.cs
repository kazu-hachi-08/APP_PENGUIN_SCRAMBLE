using System;
using NUnit.Framework;
using PenmonScramble.Core;

namespace PenmonScramble.Tests
{
    public class PlayerRecordTests
    {
        private static PlayerRecord NewRecord(int coins = 0, int freePullsUsed = Balance.FreePulls)
        {
            var data = SaveData.CreateNew();
            data.coins = coins;
            data.freePullsUsed = freePullsUsed;
            return new PlayerRecord(data);
        }

        private static PlayerRecord WithPenguins(params int[] speciesNos)
        {
            var record = NewRecord();
            for (var i = 0; i < speciesNos.Length; i++)
            {
                record.Data.penguins.Add(new OwnedPenguin(i + 1, speciesNos[i], 100 + i));
                if (i < Balance.PartySize)
                {
                    record.Data.party.Add(i + 1);
                }
            }

            PlayerRecord.Repair(record.Data);
            return record;
        }

        [Test]
        public void FirstThreePulls_AreFree_AndMakeParty()
        {
            var record = NewRecord(freePullsUsed: 0);
            var random = new Random(1);

            for (var i = 0; i < Balance.FreePulls; i++)
            {
                Assert.That(record.Pull(random), Is.Not.Null);
            }

            Assert.That(record.Coins, Is.Zero);
            Assert.That(record.FreePullsLeft, Is.Zero);
            Assert.That(record.Party.Count, Is.EqualTo(Balance.PartySize));
            Assert.That(record.CanPull(), Is.EqualTo(PullResult.NotEnoughCoins));
            Assert.That(record.Pull(random), Is.Null);
        }

        [Test]
        public void Pull_UsesCoins_AndGivesNewIds()
        {
            var record = NewRecord(coins: Balance.GachaCost * 2);
            var random = new Random(2);

            var a = record.Pull(random);
            var b = record.Pull(random);

            Assert.That(record.Coins, Is.Zero);
            Assert.That(a.id, Is.Not.EqualTo(b.id));
            Assert.That(record.Penguins.Count, Is.EqualTo(2));
        }

        [Test]
        public void Pull_WhenFull_Fails()
        {
            var record = NewRecord(coins: Balance.GachaCost);
            for (var i = 0; i < Balance.MaxPenguins; i++)
            {
                record.Data.penguins.Add(new OwnedPenguin(i + 1, 1, 100));
            }

            Assert.That(record.CanPull(), Is.EqualTo(PullResult.Full));
            Assert.That(record.Pull(new Random(3)), Is.Null);
            Assert.That(record.Coins, Is.EqualTo(Balance.GachaCost));
        }

        [Test]
        public void Pull_PowerIsInWorldRange_ScaledByStars()
        {
            var record = NewRecord(coins: Balance.GachaCost * Balance.MaxPenguins);
            var random = new Random(4);
            for (var i = 0; i < Balance.MaxPenguins; i++)
            {
                var penguin = record.Pull(random);
                var stars = SpeciesTable.Get(penguin.speciesNo).Stars;
                var scale = Balance.StarPowerScale(stars);
                Assert.That(penguin.power, Is.InRange((int)MathF.Floor(Balance.PowerMin(1) * scale), (int)MathF.Ceiling(Balance.PowerMax(1) * scale)));
            }
        }

        [Test]
        public void StarsFromRoll_FollowsRates()
        {
            Assert.That(Balance.StarsFromRoll(0), Is.EqualTo(1));
            Assert.That(Balance.StarsFromRoll(69), Is.EqualTo(1));
            Assert.That(Balance.StarsFromRoll(70), Is.EqualTo(2));
            Assert.That(Balance.StarsFromRoll(94), Is.EqualTo(2));
            Assert.That(Balance.StarsFromRoll(95), Is.EqualTo(3));
            Assert.That(Balance.StarsFromRoll(99), Is.EqualTo(3));
        }

        [Test]
        public void Sell_GivesCoins_ButNotPartyMembers()
        {
            // ★1 を4体: 3体が編成、4体目は売れる
            var record = WithPenguins(1, 2, 3, 6);

            Assert.That(record.Sell(1), Is.False);
            Assert.That(record.Sell(4), Is.True);
            Assert.That(record.Coins, Is.EqualTo(Balance.SellPrice(1)));
            Assert.That(record.Find(4), Is.Null);
        }

        [Test]
        public void SetPartySlot_SwapsWhenAlreadyInParty()
        {
            var record = WithPenguins(1, 2, 3, 6);
            var before = new[] { record.Party[0], record.Party[1], record.Party[2] };

            Assert.That(record.SetPartySlot(0, before[2]), Is.True);
            Assert.That(record.Party, Is.EqualTo(new[] { before[2], before[1], before[0] }));

            Assert.That(record.SetPartySlot(1, 4), Is.True);
            Assert.That(record.Party[1], Is.EqualTo(4));
            Assert.That(record.SetPartySlot(1, 99), Is.False);
        }

        [Test]
        public void RecordStage_FirstClearGivesBonus_AndUnlocksNext()
        {
            var record = NewRecord();
            Assert.That(record.IsUnlocked(1), Is.True);
            Assert.That(record.IsUnlocked(2), Is.False);

            Assert.That(record.RecordStage(1, cleared: true, enemiesDefeated: 30), Is.EqualTo(Balance.ClearCoins + Balance.FirstClearBonusCoins));
            Assert.That(record.IsCleared(1), Is.True);
            Assert.That(record.IsUnlocked(2), Is.True);

            Assert.That(record.RecordStage(1, cleared: true, enemiesDefeated: 30), Is.EqualTo(Balance.ClearCoins));
            Assert.That(record.Data.clearedStages.Count, Is.EqualTo(1));
        }

        [Test]
        public void RecordStage_Failed_GivesCoinsPerEnemy()
        {
            var record = NewRecord();

            Assert.That(record.RecordStage(1, cleared: false, enemiesDefeated: 12), Is.EqualTo(12 * Balance.FailCoinsPerEnemy));
            Assert.That(record.IsCleared(1), Is.False);
        }

        [Test]
        public void PartyStats_UsesPower()
        {
            var record = WithPenguins(1, 2, 3);

            var stats = record.PartyStats();

            Assert.That(stats.Length, Is.EqualTo(Balance.PartySize));
            var first = record.Find(record.Party[0]);
            Assert.That(stats[0].SpeciesNo, Is.EqualTo(first.speciesNo));
            Assert.That(stats[0].MaxHp, Is.EqualTo(SpeciesTable.PlayerStats(SpeciesTable.Get(first.speciesNo), first.power).MaxHp));
        }

        [Test]
        public void Sort_ByPower_ThenNewest()
        {
            var penguins = new[] { new OwnedPenguin(1, 3, 100), new OwnedPenguin(2, 1, 200), new OwnedPenguin(3, 2, 100) };

            var byPower = PenguinSort.Sorted(penguins, PenguinSortMode.Power);
            var byNumber = PenguinSort.Sorted(penguins, PenguinSortMode.Number);
            var newest = PenguinSort.Sorted(penguins, PenguinSortMode.Newest);

            Assert.That(byPower.ConvertAll(p => p.id), Is.EqualTo(new[] { 2, 3, 1 }));
            Assert.That(byNumber.ConvertAll(p => p.id), Is.EqualTo(new[] { 2, 3, 1 }));
            Assert.That(newest.ConvertAll(p => p.id), Is.EqualTo(new[] { 3, 2, 1 }));
        }
    }
}
