using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PenmonScramble.Core;
using PenmonScramble.Soccer.Core;

namespace PenmonScramble.Tests
{
    /// <summary>サッカーの必殺シュート（Docs/MINIGAME_SOCCER_PLAN.md フェーズ 4）: チームのゲージ・カットイン・属性ごとの軌道</summary>
    public class SoccerSpecialTests
    {
        private const float Dt = SoccerRules.StepSeconds;

        private static int Steps(float seconds) => (int)MathF.Ceiling(seconds / Dt);

        private static List<SoccerEvent> Run(SoccerWorld world, int steps, params SoccerInput[] inputs)
        {
            var events = new List<SoccerEvent>();
            for (var i = 0; i < steps; i++)
            {
                world.Step(inputs, events);
            }

            return events;
        }

        /// <summary>赤 1体（と青 blue 体）。赤の子を shooter に置き、属性と★を決めて、足元のボールを持たせる</summary>
        private static SoccerWorld World(Element element, int stars, Vec2 shooter, int blue = 0, bool keepers = false)
        {
            var world = new SoccerWorld(SoccerPitchTable.Standard(), 1, blue, keepers);
            world.SetKind(0, element, stars);
            for (var i = 1; i <= blue; i++)
            {
                // 邪魔にならない所へ（必要なテストで置き直す）
                world.Players[i].Position = new Vec2(-10f, -6f + i);
            }

            var player = world.Players[0];
            player.Position = shooter;
            player.Velocity = Vec2.Zero;
            player.Facing = new Vec2(1f, 0f);
            world.Ball.Place(shooter + player.Facing * SoccerBalance.DribbleDistance);
            Run(world, 1);
            Assert.That(world.Ball.Holder, Is.EqualTo(0));
            return world;
        }

        private static readonly SoccerInput Special = new SoccerInput(Vec2.Zero, false, false, true);

        /// <summary>必殺シュートを押し、カットインが終わって蹴るまで進める</summary>
        private static List<SoccerEvent> Fire(SoccerWorld world)
        {
            world.SetGauge(SoccerRules.Red, 1f);
            var events = Run(world, 1, Special);
            events.AddRange(Run(world, Steps(SoccerBalance.SpecialCutInSeconds) + 1));
            return events;
        }

        // ---- ゲージ ----

        [Test]
        public void Gauge_FillsWhenPossessingAndTackling_UpToFull()
        {
            var world = World(Element.Ice, 1, new Vec2(0f, 0f), blue: 1);
            Assert.That(world.Gauge(SoccerRules.Red), Is.EqualTo(SoccerBalance.GaugePossess).Within(1e-5f));

            world.SetGauge(SoccerRules.Blue, 0.98f);
            var blue = world.Players[1];
            blue.Position = new Vec2(-1.5f, 0f);
            blue.Facing = new Vec2(1f, 0f);
            var inputs = new SoccerInput[2];
            inputs[1] = new SoccerInput(new Vec2(1f, 0f), true);
            var events = Run(world, Steps(0.4f), inputs);

            Assert.That(events.Any(e => e.Kind == SoccerEventKind.Tackle), Is.True);
            Assert.That(world.Gauge(SoccerRules.Blue), Is.EqualTo(1f));
        }

        [Test]
        public void Special_NeedsAFullGauge()
        {
            var world = World(Element.Ice, 1, new Vec2(0f, 0f));
            world.SetGauge(SoccerRules.Red, 0.99f);

            var events = Run(world, Steps(1f), Special);
            Assert.That(events.Any(e => e.Kind == SoccerEventKind.SpecialCutIn), Is.False);
            Assert.That(world.Ball.Holder, Is.EqualTo(0));
        }

        [Test]
        public void Special_UsesTheGauge_FreezesThePitchForTheCutIn_ThenKicks()
        {
            var world = World(Element.Ice, 2, new Vec2(0f, 0f));
            world.SetGauge(SoccerRules.Red, 1f);

            var start = Run(world, 1, Special);
            var cutIn = start.Single(e => e.Kind == SoccerEventKind.SpecialCutIn);
            Assert.That(cutIn.Strength, Is.EqualTo(2f));
            Assert.That(world.Gauge(SoccerRules.Red), Is.EqualTo(0f));
            var time = world.Time;
            var ball = world.Ball.Position;

            var during = Run(world, Steps(SoccerBalance.SpecialCutInSeconds) - 1);
            Assert.That(world.Time, Is.EqualTo(time));
            Assert.That(world.Ball.Position, Is.EqualTo(ball));
            Assert.That(during.Any(e => e.Kind == SoccerEventKind.SpecialKick), Is.False);

            var kick = Run(world, 2);
            Assert.That(kick.Any(e => e.Kind == SoccerEventKind.SpecialKick), Is.True);
            Assert.That(world.Special, Is.Not.Null);
            Assert.That(world.Ball.Holder, Is.EqualTo(SoccerBall.NoPlayer));
        }

        [Test]
        public void Special_CannotBePickedUpByFieldPlayers()
        {
            var world = World(Element.Thunder, 1, new Vec2(0f, 0f), blue: 1);
            world.Players[1].Position = new Vec2(10f, 1.7f);

            var events = Fire(world);
            events.AddRange(Run(world, Steps(1.5f)));

            Assert.That(events.Any(e => e.Kind == SoccerEventKind.Possess && e.Player == 1), Is.False);
        }

        // ---- 属性ごとの軌道（キーパーなし。ゴールの口へ入ることを確かめる）----

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Ice_FreezesOpponentsInItsPath_ForLongerWithMoreStars(int stars)
        {
            var world = World(Element.Ice, stars, new Vec2(4f, 0f), blue: 1);
            var blocker = world.Players[1];
            blocker.Position = new Vec2(9f, 0.9f);

            var events = Fire(world);
            events.AddRange(Run(world, Steps(1f)));

            var freeze = events.Single(e => e.Kind == SoccerEventKind.Freeze);
            Assert.That(freeze.Player, Is.EqualTo(1));
            Assert.That(freeze.Strength, Is.EqualTo(SoccerBalance.IceFreezeSeconds(stars)));
            Assert.That(world.Score(SoccerRules.Red), Is.EqualTo(1), "凍らせて貫く");
            Run(world, Steps(SoccerBalance.IceFreezeSeconds(stars)));
            Assert.That(blocker.IsFrozen, Is.False);
        }

        [Test]
        public void IceFreeze_LastsLongerWithMoreStars()
        {
            Assert.That(SoccerBalance.IceFreezeSeconds(3), Is.GreaterThan(SoccerBalance.IceFreezeSeconds(1)));
        }

        [TestCase(1)]
        [TestCase(3)]
        public void Water_HopsOverTheGround_AsManyTimesAsItsStars(int stars)
        {
            var world = World(Element.Water, stars, new Vec2(-4f, -3f));

            var events = Fire(world);
            var landings = 0;
            for (var i = 0; i < Steps(2f) && world.Special != null; i++)
            {
                landings += Run(world, 1).Count(e => e.Kind == SoccerEventKind.Land);
            }

            // 跳ねる回数 ＋ 最後に着いた1回（跳ね終わる前にゴールへ入ることもある）
            Assert.That(landings, Is.LessThanOrEqualTo(stars + 1));
            Assert.That(landings, Is.GreaterThanOrEqualTo(1));
            Run(world, Steps(2f));
            Assert.That(world.Score(SoccerRules.Red), Is.EqualTo(1));
        }

        [TestCase(1)]
        [TestCase(3)]
        public void Wind_CurvesAroundIntoTheGoal(int stars)
        {
            var world = World(Element.Wind, stars, new Vec2(4f, 0f));
            Fire(world);

            // 狙いはキーパーのいない（いなければ蹴る子の Y の側の）柱の内側。まっすぐな線から外へふくらんで回り込む
            var target = new Vec2(world.Pitch.GoalOf(SoccerRules.Blue).LineX, world.Pitch.GoalOf(SoccerRules.Blue).HalfWidth - SoccerBalance.SpecialPostInset);
            var from = new Vec2(4f + SoccerBalance.DribbleDistance, 0f);
            var farthest = 0f;
            for (var i = 0; i < Steps(2f) && !world.IsGoalScored; i++)
            {
                Run(world, 1);
                farthest = Math.Max(farthest, DistanceFromLine(from, target, world.Ball.Position));
            }

            Assert.That(world.Score(SoccerRules.Red), Is.EqualTo(1));
            Assert.That(farthest, Is.GreaterThan(0.5f));
        }

        private static float DistanceFromLine(Vec2 a, Vec2 b, Vec2 p)
        {
            var closest = PenmonScramble.MiniGames.Core.MiniGameGeometry.ClosestOnSegment(a, b, p);
            return Vec2.Distance(closest, p);
        }

        [Test]
        public void Wind_CurvesAwayFromTheWall_WhenShotFromTheCorner()
        {
            var world = World(Element.Wind, 3, new Vec2(8f, 6f));
            var events = Fire(world);
            events.AddRange(Run(world, Steps(2f)));

            Assert.That(events.Any(e => e.Kind == SoccerEventKind.WallHit), Is.False);
            Assert.That(world.Score(SoccerRules.Red), Is.EqualTo(1));
        }

        [Test]
        public void Thunder_VanishesThenAppearsFasterAhead()
        {
            var world = World(Element.Thunder, 2, new Vec2(0f, 0f));
            Fire(world);
            Assert.That(world.Special.IsHidden, Is.True);
            var start = world.Ball.Position;

            Run(world, Steps(SoccerBalance.ThunderHiddenSeconds));
            Assert.That(world.Special.IsHidden, Is.False);
            Assert.That(world.Ball.Position.X - start.X, Is.GreaterThan(SoccerBalance.ThunderBlinkDistance(2) * 0.8f));
            Assert.That(world.Ball.Velocity.Length, Is.EqualTo(SoccerBalance.ThunderSpeed(2)).Within(0.01f));

            Run(world, Steps(1f));
            Assert.That(world.Score(SoccerRules.Red), Is.EqualTo(1));
        }

        // ---- キーパー ----

        [Test]
        public void Keeper_SavesSpecialsLessWithMoreStars()
        {
            Assert.That(SoccerBalance.KeeperSpecialSave(1), Is.GreaterThan(SoccerBalance.KeeperSpecialSave(3)));
            Assert.That(SoccerBalance.KeeperSpecialSave(3), Is.GreaterThan(0f), "必ず入るわけではない");
        }

        [Test]
        public void Keeper_StopsSomeButNotAllSpecials()
        {
            var goals = 0;
            const int tries = 40;
            for (var seed = 0; seed < tries; seed++)
            {
                var world = new SoccerWorld(SoccerPitchTable.Standard(), 1, 0, keepers: true, seed: seed);
                world.SetKind(0, Element.Ice, 1);
                var player = world.Players[0];
                player.Position = new Vec2(4f, 0f);
                player.Facing = new Vec2(1f, 0f);
                world.Ball.Place(player.Position + player.Facing * SoccerBalance.DribbleDistance);
                Run(world, 1);
                Fire(world);
                Run(world, Steps(1.5f));
                goals += world.Score(SoccerRules.Red);
            }

            Assert.That(goals, Is.GreaterThan(tries / 4));
            Assert.That(goals, Is.LessThan(tries));
        }
    }
}
