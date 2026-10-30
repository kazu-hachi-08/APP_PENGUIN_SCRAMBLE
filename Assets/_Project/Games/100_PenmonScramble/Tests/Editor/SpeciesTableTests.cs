using System.Collections.Generic;
using NUnit.Framework;
using PenmonScramble.Core;

namespace PenmonScramble.Tests
{
    /// <summary>20種類・40個の技の定義表（§5.3）と、そこから作る数値（§5.1）</summary>
    public class SpeciesTableTests
    {
        [Test]
        public void Table_Has20SpeciesNumbered1To20()
        {
            Assert.That(SpeciesTable.Species.Count, Is.EqualTo(SpeciesTable.Count));
            for (var i = 0; i < SpeciesTable.Count; i++)
            {
                Assert.That(SpeciesTable.Species[i].No, Is.EqualTo(i + 1));
                Assert.That(SpeciesTable.Get(i + 1), Is.SameAs(SpeciesTable.Species[i]));
            }

            Assert.That(SpeciesTable.Get(0), Is.Null);
            Assert.That(SpeciesTable.Get(SpeciesTable.Count + 1), Is.Null);
        }

        [Test]
        public void Stars_Are10And6And4_WithOneThreeStarPerElement()
        {
            var stars = new int[4];
            var threeStarElements = new HashSet<Element>();
            foreach (var species in SpeciesTable.Species)
            {
                stars[species.Stars]++;
                if (species.Stars == 3)
                {
                    threeStarElements.Add(species.Element);
                }
            }

            Assert.That(stars[1], Is.EqualTo(10));
            Assert.That(stars[2], Is.EqualTo(6));
            Assert.That(stars[3], Is.EqualTo(4));
            Assert.That(threeStarElements.Count, Is.EqualTo(4));
        }

        [Test]
        public void AllFortySkills_HaveNamesAndUsableNumbers()
        {
            var names = new HashSet<string>();
            foreach (var species in SpeciesTable.Species)
            {
                foreach (var skill in new[] { species.Skill1, species.Skill2 })
                {
                    Assert.That(skill.Name, Is.Not.Empty);
                    Assert.That(names.Add(skill.Name), Is.True, $"技の名前がかぶっている: {skill.Name}");
                    Assert.That(skill.Power, Is.GreaterThan(0f), skill.Name);
                    Assert.That(skill.Cooldown, Is.GreaterThan(0f), skill.Name);
                    Assert.That(skill.Range, Is.GreaterThan(0f), skill.Name);
                }

                // 技1は連打できる技、技2は待ち時間が長い派手な技（§4.2）
                Assert.That(species.Skill2.Cooldown, Is.GreaterThan(species.Skill1.Cooldown), species.Name);
            }

            Assert.That(names.Count, Is.EqualTo(40));
        }

        [Test]
        public void AllSevenShapes_AreUsed()
        {
            var shapes = new HashSet<SkillShape>();
            foreach (var species in SpeciesTable.Species)
            {
                shapes.Add(species.Skill1.Shape);
                shapes.Add(species.Skill2.Shape);
            }

            Assert.That(shapes.Count, Is.EqualTo(7));
        }

        [Test]
        public void PlayerStats_FollowPowerFormula()
        {
            var species = SpeciesTable.Get(5);
            var stats = SpeciesTable.PlayerStats(species, 200);

            Assert.That(stats.MaxHp, Is.EqualTo((int)System.MathF.Round(species.HpRate * 200)));
            Assert.That(stats.Attack, Is.EqualTo(species.AttackRate * 2f).Within(0.0001f));
            Assert.That(stats.Skills, Is.EqualTo(new[] { species.Skill1, species.Skill2 }));
            Assert.That(stats.SpeciesNo, Is.EqualTo(5));
            Assert.That(stats.Element, Is.EqualTo(Element.Ice));
        }

        [Test]
        public void EnemyStats_UseOnlySkill1_WithLongerCooldown()
        {
            var species = SpeciesTable.Get(10);
            var stats = SpeciesTable.EnemyStats(species, playerCount: 2);

            Assert.That(stats.Skills, Is.EqualTo(new[] { species.Skill1 }));
            Assert.That(stats.CooldownScale, Is.EqualTo(Balance.EnemyCooldownScale));
            Assert.That(stats.MaxHp, Is.EqualTo((int)System.MathF.Round(species.HpRate * Balance.EnemyPower * 1.6f)));
        }

        [Test]
        public void RotatingParty_CoversAll20InSevenSoloRounds()
        {
            var seen = new HashSet<int>();
            for (var round = 1; round <= 7; round++)
            {
                var party = TrialStage.RotatingParty(round, 0, 1);
                Assert.That(party.Length, Is.EqualTo(Balance.PartySize));
                foreach (var species in party)
                {
                    seen.Add(species.No);
                }
            }

            Assert.That(seen.Count, Is.EqualTo(SpeciesTable.Count));
        }

        [Test]
        public void SpecialMoves_ExistForEveryElement_AndAreMarkedSpecial()
        {
            foreach (Element element in System.Enum.GetValues(typeof(Element)))
            {
                var special = SpecialMoves.For(element);
                Assert.That(special.IsSpecial, Is.True, element.ToString());
                Assert.That(special.Name, Is.Not.Empty);
            }
        }
    }
}
