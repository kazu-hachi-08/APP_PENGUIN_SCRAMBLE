using NUnit.Framework;
using PenmonScramble.Core;
using PenmonScramble.Golf.Core;

namespace PenmonScramble.Tests
{
    /// <summary>ゴルフの操作まわりの計算: パワーゲージ・打つ子の選び方・打数の呼び名</summary>
    public class GolfControlTests
    {
        private const float Cycle = GolfRules.GaugeCycleSeconds;

        // ---- パワーゲージ ----

        [Test]
        public void Gauge_RisesThenFallsAtConstantSpeed()
        {
            Assert.That(GolfPowerGauge.PowerAt(0f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(GolfPowerGauge.PowerAt(Cycle * 0.25f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(GolfPowerGauge.PowerAt(Cycle * 0.5f), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(GolfPowerGauge.PowerAt(Cycle * 0.75f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(GolfPowerGauge.PowerAt(Cycle), Is.EqualTo(0f).Within(1e-5f));
        }

        /// <summary>端で止まらない: 100% の前後どちらでも、同じ時間だけ離れれば同じだけ下がる</summary>
        [Test]
        public void Gauge_DoesNotDwellAtEnds()
        {
            const float nudge = 0.05f;
            var drop = 1f - GolfPowerGauge.PowerAt(Cycle * 0.5f + nudge);
            Assert.That(drop, Is.EqualTo(nudge / (Cycle * 0.5f)).Within(1e-4f));
            Assert.That(GolfPowerGauge.PowerAt(Cycle - nudge), Is.EqualTo(GolfPowerGauge.PowerAt(nudge)).Within(1e-4f));
        }

        [Test]
        public void Gauge_KeepsGoingAfterSeveralCycles()
        {
            Assert.That(GolfPowerGauge.PowerAt(Cycle * 3f + Cycle * 0.25f), Is.EqualTo(0.5f).Within(1e-4f));
        }

        [Test]
        public void Gauge_StopKeepsPowerAndTickNoLongerMovesIt()
        {
            var gauge = new GolfPowerGauge();
            gauge.Start();
            gauge.Tick(Cycle * 0.2f);

            var stopped = gauge.Stop();
            gauge.Tick(Cycle * 0.2f);

            Assert.That(stopped, Is.EqualTo(0.4f).Within(1e-4f));
            Assert.That(gauge.Power, Is.EqualTo(stopped));
            Assert.That(gauge.Running, Is.False);
        }

        [Test]
        public void Gauge_StartAndResetGoBackToZero()
        {
            var gauge = new GolfPowerGauge();
            gauge.Start();
            gauge.Tick(Cycle * 0.3f);
            gauge.Start();
            Assert.That(gauge.Power, Is.EqualTo(0f));

            gauge.Tick(Cycle * 0.3f);
            gauge.Reset();
            Assert.That(gauge.Power, Is.EqualTo(0f));
            Assert.That(gauge.Running, Is.False);
        }

        // ---- 打つ子 ----

        [Test]
        public void Team_TakesPartyInOrderWithElementAndStars()
        {
            var team = new GolfTeam(new[] { 5, 1, 3 });

            Assert.That(team.Members.Count, Is.EqualTo(3));
            Assert.That(team.Members[0].SpeciesNo, Is.EqualTo(5));
            Assert.That(team.Members[0].Stars, Is.EqualTo(SpeciesTable.Get(5).Stars));
            Assert.That(team.Members[1].Element, Is.EqualTo(SpeciesTable.Get(1).Element));
            Assert.That(team.Selected, Is.EqualTo(0));
        }

        [Test]
        public void Team_EmptyOrUnknownPartyFallsBackToDefault()
        {
            var empty = new GolfTeam(new int[0]);
            var unknown = new GolfTeam(new[] { 999 });

            foreach (var team in new[] { empty, unknown })
            {
                Assert.That(team.Members.Count, Is.EqualTo(GolfRules.DefaultSpecies.Length));
                Assert.That(team.Members[0].SpeciesNo, Is.EqualTo(GolfRules.DefaultSpecies[0]));
            }
        }

        [Test]
        public void Team_SkipsUnknownSpecies()
        {
            var team = new GolfTeam(new[] { 1, 999, 3 });

            Assert.That(team.Members.Count, Is.EqualTo(2));
            Assert.That(team.Members[1].SpeciesNo, Is.EqualTo(3));
        }

        [Test]
        public void Team_SelectNextWrapsAround()
        {
            var team = new GolfTeam(new[] { 1, 3, 5 });

            Assert.That(team.SelectNext(), Is.True);
            Assert.That(team.SelectNext(), Is.True);
            Assert.That(team.Current.SpeciesNo, Is.EqualTo(5));
            Assert.That(team.SelectNext(), Is.True);
            Assert.That(team.Current.SpeciesNo, Is.EqualTo(1));
        }

        [Test]
        public void Team_SelectRejectsOutOfRangeAndSameIndex()
        {
            var team = new GolfTeam(new[] { 1, 3, 5 });

            Assert.That(team.Select(0), Is.False);
            Assert.That(team.Select(3), Is.False);
            Assert.That(team.Select(-1), Is.False);
            Assert.That(team.Select(2), Is.True);
            Assert.That(team.Selected, Is.EqualTo(2));
        }

        [Test]
        public void Team_SingleMemberCannotSwitch()
        {
            var team = new GolfTeam(new[] { 1 });

            Assert.That(team.SelectNext(), Is.False);
            Assert.That(team.Selected, Is.EqualTo(0));
        }

        // ---- 打数の呼び名 ----

        [TestCase(1, 3, "ホールインワン")]
        [TestCase(1, 1, "ホールインワン")]
        [TestCase(2, 4, "イーグル")]
        [TestCase(2, 3, "バーディー")]
        [TestCase(3, 3, "パー")]
        [TestCase(4, 3, "ボギー")]
        [TestCase(5, 3, "ダブルボギー")]
        [TestCase(7, 3, "4 オーバー")]
        public void ScoreNames_ForStrokes(int strokes, int par, string expected)
        {
            Assert.That(GolfScoreNames.ForStrokes(strokes, par), Is.EqualTo(expected));
        }

        [TestCase(2, 3, "−1")]
        [TestCase(3, 3, "±0")]
        [TestCase(5, 3, "+2")]
        public void ScoreNames_Difference(int strokes, int par, string expected)
        {
            Assert.That(GolfScoreNames.Difference(strokes, par), Is.EqualTo(expected));
        }
    }
}
