using NUnit.Framework;
using PenmonScramble.Core;

namespace PenmonScramble.Tests
{
    /// <summary>ペンモン画面に出すもの: 図鑑の文・技の目安・技の実演（SkillPractice）</summary>
    public class PenmonProfileTests
    {
        // 実演を回して確かめる長さ（秒）。いちばん待ち時間の長い技（4.5秒）が2回は出る長さ
        private const float PracticeSeconds = 14f;

        [Test]
        public void Texts_AllSpeciesHavePersonalityAndTwoSkillTexts()
        {
            Assert.That(SpeciesTexts.Count, Is.EqualTo(SpeciesTable.Count));
            foreach (var species in SpeciesTable.Species)
            {
                Assert.That(SpeciesTexts.Personality(species.No), Is.Not.Empty, species.Name);
                Assert.That(SpeciesTexts.Skill(species.No, 0), Is.Not.Empty, species.Skill1.Name);
                Assert.That(SpeciesTexts.Skill(species.No, 1), Is.Not.Empty, species.Skill2.Name);
            }

            Assert.That(SpeciesTexts.Personality(SpeciesTable.Count + 1), Is.Empty);
        }

        [Test]
        public void Ratings_AreBetween1And3_ForAllSkills()
        {
            foreach (var species in SpeciesTable.Species)
            {
                foreach (var skill in new[] { species.Skill1, species.Skill2 })
                {
                    Assert.That(SkillProfile.PowerRating(skill), Is.InRange(1, SkillProfile.MaxRating), skill.Name);
                    Assert.That(SkillProfile.QuicknessRating(skill), Is.InRange(1, SkillProfile.MaxRating), skill.Name);
                    Assert.That(SkillProfile.AreaRating(skill), Is.InRange(1, SkillProfile.MaxRating), skill.Name);
                }
            }
        }

        [Test]
        public void Ratings_FollowTheNumbers()
        {
            var adelin = SpeciesTable.Get(1);
            // パンチ: 連打できて、狭くて弱い / 腹すべり: 待つ代わりに強い
            Assert.That(SkillProfile.QuicknessRating(adelin.Skill1), Is.EqualTo(3));
            Assert.That(SkillProfile.AreaRating(adelin.Skill1), Is.EqualTo(1));
            Assert.That(SkillProfile.PowerRating(adelin.Skill1), Is.LessThan(SkillProfile.PowerRating(adelin.Skill2)));
            Assert.That(SkillProfile.QuicknessRating(adelin.Skill2), Is.LessThan(SkillProfile.QuicknessRating(adelin.Skill1)));
            // 5か所に落ちる雷は範囲がいちばん広い
            Assert.That(SkillProfile.AreaRating(SpeciesTable.Get(20).Skill2), Is.EqualTo(SkillProfile.MaxRating));
        }

        [Test]
        public void ShapeLabel_AddsFeature()
        {
            Assert.That(SkillProfile.ShapeLabel(SpeciesTable.Get(2).Skill2), Is.EqualTo("拡散・3本"));
            Assert.That(SkillProfile.ShapeLabel(SpeciesTable.Get(10).Skill1), Is.EqualTo("弾・貫通"));
            Assert.That(SkillProfile.ShapeLabel(SpeciesTable.Get(8).Skill2), Is.EqualTo("設置・吸いこみ"));
            Assert.That(SkillProfile.ShapeLabel(SpeciesTable.Get(1).Skill1), Is.EqualTo("近接"));
        }

        [Test]
        public void SpeciesRatios_AreWithinBarRange()
        {
            foreach (var species in SpeciesTable.Species)
            {
                Assert.That(SpeciesProfile.HpRatio(species), Is.InRange(SpeciesProfile.MinRatio, 1f));
                Assert.That(SpeciesProfile.AttackRatio(species), Is.InRange(SpeciesProfile.MinRatio, 1f));
                Assert.That(SpeciesProfile.SpeedRatio(species), Is.InRange(SpeciesProfile.MinRatio, 1f));
            }

            // ジェンツは世界一速い
            Assert.That(SpeciesProfile.SpeedRatio(SpeciesTable.Get(3)), Is.EqualTo(1f));
        }

        /// <summary>どの技も、実演の的に届いて当たり、くり返し出る。的は倒れない</summary>
        [Test]
        public void Practice_EverySkillHitsDummiesRepeatedly()
        {
            foreach (var species in SpeciesTable.Species)
            {
                for (var slot = 0; slot < 2; slot++)
                {
                    var practice = new SkillPractice(SpeciesTable.PlayerStats(species, Balance.TrialPower), slot);
                    var used = 0;
                    var hits = 0;
                    practice.World.SkillUsed += _ => used++;
                    practice.World.Hit += e => hits += e.TargetId != practice.Player.Id ? 1 : 0;
                    for (var i = 0; i < PracticeSeconds / Balance.TickSeconds; i++)
                    {
                        practice.Tick();
                    }

                    var name = species.GetSkill(slot).Name;
                    Assert.That(used, Is.GreaterThanOrEqualTo(2), name);
                    Assert.That(hits, Is.GreaterThan(0), name);
                    Assert.That(practice.World.CountAlive(Team.Enemy), Is.EqualTo(3), name);
                }
            }
        }

        [Test]
        public void Practice_PlayerComesBackHomeAfterDash()
        {
            var practice = new SkillPractice(SpeciesTable.PlayerStats(SpeciesTable.Get(3), Balance.TrialPower), 1);
            var home = practice.Player.Position;
            var left = false;
            for (var i = 0; i < PracticeSeconds / Balance.TickSeconds; i++)
            {
                practice.Tick();
                left |= Vec2.Distance(practice.Player.Position, home) > 1f;
            }

            Assert.That(left, Is.True);
            // 2回目の突進の途中でも、戻ってきてから出しているので、元の位置より後ろへは下がっていない
            Assert.That(practice.Player.Position.X, Is.GreaterThanOrEqualTo(home.X - 0.1f));
        }
    }
}
