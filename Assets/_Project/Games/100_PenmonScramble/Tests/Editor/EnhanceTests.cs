using NUnit.Framework;
using PenmonScramble.Core;

namespace PenmonScramble.Tests
{
    public class EnhanceTests
    {
        // 編成は id 1〜3（種類 6・7・11。どれも★1）。強化する子は id 4（アデリン★1・パワー 100）
        private const int TargetId = 4;
        private const int TargetSpecies = 1;
        private const int TargetPower = 100;

        private static PlayerRecord NewRecord(int coins = 10000)
        {
            var data = SaveData.CreateNew();
            data.coins = coins;
            data.penguins.Add(new OwnedPenguin(1, 6, 100));
            data.penguins.Add(new OwnedPenguin(2, 7, 100));
            data.penguins.Add(new OwnedPenguin(3, 11, 100));
            data.penguins.Add(new OwnedPenguin(TargetId, TargetSpecies, TargetPower));
            data.party.AddRange(new[] { 1, 2, 3 });
            data.nextPenguinId = TargetId + 1;
            return new PlayerRecord(data);
        }

        private static int Add(PlayerRecord record, int speciesNo, int power)
        {
            var id = record.Data.nextPenguinId++;
            record.Data.penguins.Add(new OwnedPenguin(id, speciesNo, power));
            return id;
        }

        [Test]
        public void Enhance_AddsStarGainAndPartOfPower_AndPaysCoins()
        {
            var record = NewRecord(coins: 1000);
            var material = Add(record, 2, 100);

            var plan = record.Enhance(TargetId, new[] { material });

            var gain = Balance.EnhanceGain(1, 100);
            Assert.That(gain, Is.EqualTo(18));
            Assert.That(plan.Result, Is.EqualTo(EnhanceResult.Ok));
            Assert.That(record.Find(TargetId).Power, Is.EqualTo(TargetPower + gain));
            Assert.That(record.Find(TargetId).basePower, Is.EqualTo(TargetPower));
            Assert.That(record.Find(material), Is.Null);
            Assert.That(record.Coins, Is.EqualTo(1000 - Balance.EnhanceCoinsPerMaterial(TargetPower)));
        }

        [Test]
        public void Enhance_StopsAtCap_AndReportsOverflow()
        {
            var record = NewRecord();
            var materials = new[] { Add(record, 5, 300), Add(record, 10, 300) };

            var plan = record.Enhance(TargetId, materials);

            var cap = Balance.PowerCap(TargetPower, 0);
            Assert.That(cap, Is.EqualTo(150));
            Assert.That(record.Find(TargetId).Power, Is.EqualTo(cap));
            Assert.That(plan.Overflow, Is.EqualTo(TargetPower + 2 * Balance.EnhanceGain(3, 300) - cap));
            Assert.That(plan.NeedsConfirm, Is.True);
        }

        [Test]
        public void SameSpecies_BreaksLimit_BeforeCapIsApplied()
        {
            var record = NewRecord();
            var same = Add(record, TargetSpecies, 300);
            var other = Add(record, 10, 300);

            var plan = record.Enhance(TargetId, new[] { same, other });

            Assert.That(plan.LimitBroken, Is.True);
            Assert.That(record.Find(TargetId).limitBreak, Is.EqualTo(1));
            Assert.That(plan.CapAfter, Is.EqualTo(Balance.PowerCap(TargetPower, 1)));
            Assert.That(record.Find(TargetId).Power, Is.EqualTo(plan.CapAfter));
        }

        [Test]
        public void LimitBreak_StopsAtMax_ButStillAddsPower()
        {
            var record = NewRecord();
            record.Find(TargetId).limitBreak = Balance.MaxLimitBreak;
            var same = Add(record, TargetSpecies, 80);

            var plan = record.Enhance(TargetId, new[] { same });

            Assert.That(plan.Result, Is.EqualTo(EnhanceResult.Ok));
            Assert.That(plan.LimitBroken, Is.False);
            Assert.That(record.Find(TargetId).limitBreak, Is.EqualTo(Balance.MaxLimitBreak));
            Assert.That(record.Find(TargetId).Power, Is.EqualTo(TargetPower + Balance.EnhanceGain(1, 80)));
        }

        [Test]
        public void AtCap_WithoutLimitBreak_HasNoEffect()
        {
            var record = NewRecord();
            record.Find(TargetId).boost = Balance.PowerCap(TargetPower, 0) - TargetPower;
            var material = Add(record, 2, 100);

            var plan = record.Enhance(TargetId, new[] { material });

            Assert.That(plan.Result, Is.EqualTo(EnhanceResult.NoEffect));
            Assert.That(record.Find(material), Is.Not.Null);
        }

        [Test]
        public void PartyMembersAndTargetItself_CannotBeMaterials()
        {
            var record = NewRecord();

            Assert.That(record.PlanEnhance(TargetId, new[] { 1 }).Result, Is.EqualTo(EnhanceResult.InvalidMaterial));
            Assert.That(record.PlanEnhance(TargetId, new[] { TargetId }).Result, Is.EqualTo(EnhanceResult.InvalidMaterial));
            Assert.That(record.Enhance(TargetId, new[] { 1 }).Result, Is.EqualTo(EnhanceResult.InvalidMaterial));
            Assert.That(record.Find(1), Is.Not.Null);
        }

        [Test]
        public void DuplicateMaterial_IsInvalid()
        {
            var record = NewRecord();
            var material = Add(record, 2, 100);

            Assert.That(record.PlanEnhance(TargetId, new[] { material, material }).Result, Is.EqualTo(EnhanceResult.InvalidMaterial));
        }

        [Test]
        public void TooManyMaterials_IsRefused()
        {
            var record = NewRecord();
            var materials = new int[Balance.MaxEnhanceMaterials + 1];
            for (var i = 0; i < materials.Length; i++)
            {
                materials[i] = Add(record, 2, 10);
            }

            Assert.That(record.PlanEnhance(TargetId, materials).Result, Is.EqualTo(EnhanceResult.TooManyMaterials));
        }

        [Test]
        public void NotEnoughCoins_KeepsEverything()
        {
            var record = NewRecord(coins: Balance.EnhanceCoinsPerMaterial(TargetPower) - 1);
            var material = Add(record, 2, 100);

            var plan = record.Enhance(TargetId, new[] { material });

            Assert.That(plan.Result, Is.EqualTo(EnhanceResult.NotEnoughCoins));
            Assert.That(record.Find(TargetId).Power, Is.EqualTo(TargetPower));
            Assert.That(record.Find(material), Is.Not.Null);
            Assert.That(record.Coins, Is.EqualTo(Balance.EnhanceCoinsPerMaterial(TargetPower) - 1));
        }

        [Test]
        public void StrongerMaterial_NeedsConfirm()
        {
            var record = NewRecord();
            var weak = Add(record, 2, 50);
            var strong = Add(record, 2, 200);

            Assert.That(record.PlanEnhance(TargetId, new[] { weak }).NeedsConfirm, Is.False);
            Assert.That(record.PlanEnhance(TargetId, new[] { strong }).NeedsConfirm, Is.True);
        }

        [Test]
        public void Candidates_SameSpeciesFirst_PartyLast()
        {
            var record = NewRecord();
            var other = Add(record, 2, 50);
            var same = Add(record, TargetSpecies, 90);

            var ids = record.MaterialCandidates(TargetId).ConvertAll(p => p.id);

            Assert.That(ids, Is.EqualTo(new[] { same, other, 1, 2, 3 }));
        }

        [Test]
        public void Repair_ClampsGrowth()
        {
            var record = NewRecord();
            record.Find(TargetId).limitBreak = 99;
            record.Find(TargetId).boost = -5;

            PlayerRecord.Repair(record.Data);

            Assert.That(record.Find(TargetId).limitBreak, Is.EqualTo(Balance.MaxLimitBreak));
            Assert.That(record.Find(TargetId).boost, Is.Zero);
        }
    }
}
