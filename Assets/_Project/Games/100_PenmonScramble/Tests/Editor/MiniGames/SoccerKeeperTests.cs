using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PenmonScramble.Core;
using PenmonScramble.Soccer.Core;

namespace PenmonScramble.Tests
{
    /// <summary>おもちゃのキーパー</summary>
    public class SoccerKeeperTests
    {
        private const float Dt = SoccerRules.StepSeconds;
        private const int Seeds = 50;

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

        /// <summary>キーパーのいるピッチで、赤の1体（＋青の blue 体）の目の前にボールを置いて持たせる</summary>
        private static SoccerWorld HoldingWorld(Vec2 at, int seed, int blue = 0)
        {
            var world = new SoccerWorld(SoccerPitchTable.Standard(), 1, blue, keepers: true, seed: seed);
            world.Players[0].Position = at;
            world.Ball.Place(at + new Vec2(SoccerBalance.DribbleDistance, 0f));
            Run(world, 1);
            Assert.That(world.Ball.Holder, Is.EqualTo(0));
            return world;
        }

        /// <summary>ため charge（0〜1）で stick の向きへ蹴り、3 秒進めた間に起きたこと</summary>
        private static List<SoccerEvent> Shoot(SoccerWorld world, float charge, Vec2 stick)
        {
            var chargeSteps = (int)MathF.Round(charge * SoccerBalance.ShotChargeSeconds / Dt);
            var events = Run(world, chargeSteps, new SoccerInput(Vec2.Zero, true));
            events.AddRange(Run(world, 1, new SoccerInput(stick, false)));
            events.AddRange(Run(world, Steps(3f)));
            return events;
        }

        private static bool Saved(List<SoccerEvent> events) => events.Any(e => e.Kind == SoccerEventKind.KeeperCatch && e.Team == SoccerRules.Blue);

        private static bool Scored(List<SoccerEvent> events) => events.Any(e => e.Kind == SoccerEventKind.Goal);

        // ---- 止める割合 ----

        [Test]
        public void Chance_RoomToSpareIsAlmostSureAndOutOfReachIsAlmostNever()
        {
            Assert.That(SoccerKeeperSave.Chance(1.7f, 0f, 0f, 1.6f), Is.EqualTo(SoccerBalance.KeeperMaxSave));
            Assert.That(SoccerKeeperSave.Chance(0f, 2f, 0f, 1.6f), Is.EqualTo(SoccerBalance.KeeperMinSave));
        }

        [Test]
        public void Chance_HighBallIsHarderToSave()
        {
            var low = SoccerKeeperSave.Chance(1f, 1f, 0f, 1.6f);
            var high = SoccerKeeperSave.Chance(1f, 1f, 1.1f, 1.6f);

            Assert.That(high, Is.LessThan(low));
        }

        [Test]
        public void Capability_NothingBeforeReactingThenUpToReach()
        {
            Assert.That(SoccerKeeperSave.DiveCapability(SoccerBalance.KeeperReactSeconds * 0.5f), Is.EqualTo(0f));
            Assert.That(SoccerKeeperSave.DiveCapability(10f), Is.EqualTo(SoccerBalance.KeeperDiveReach));
        }

        // ---- シュート ----

        /// <summary>真正面の弱い球はほぼ止める</summary>
        [Test]
        public void Shot_WeakShotStraightAtKeeperIsAlmostAlwaysSaved()
        {
            var saves = 0;
            for (var seed = 0; seed < Seeds; seed++)
            {
                var world = HoldingWorld(new Vec2(6f, 0f), seed);
                var events = Shoot(world, 0.3f, new Vec2(1f, 0f));
                saves += Saved(events) && !Scored(events) ? 1 : 0;
            }

            Assert.That(saves, Is.GreaterThanOrEqualTo(Seeds * 9 / 10));
        }

        /// <summary>端への強い球はほぼ入る</summary>
        [Test]
        public void Shot_HardShotIntoCornerAlmostAlwaysScores()
        {
            var goals = 0;
            for (var seed = 0; seed < Seeds; seed++)
            {
                var world = HoldingWorld(new Vec2(7f, 0f), seed);
                var corner = new Vec2(SoccerPitchTable.Standard().HalfLength, 2f) - world.Players[0].Position;
                var events = Shoot(world, 0.88f, corner);
                goals += Scored(events) ? 1 : 0;
            }

            Assert.That(goals, Is.GreaterThanOrEqualTo(Seeds * 9 / 10));
        }

        /// <summary>止めるかの乱数はホストの種で決まる（同じ種なら同じ結果。オンラインで写しとずれない）</summary>
        [Test]
        public void Shot_SameSeedGivesSameResult()
        {
            for (var seed = 0; seed < 10; seed++)
            {
                var first = Shoot(HoldingWorld(new Vec2(4f, 0f), seed), 0.6f, new Vec2(1f, 0.25f)).Select(e => e.ToString()).ToList();
                var second = Shoot(HoldingWorld(new Vec2(4f, 0f), seed), 0.6f, new Vec2(1f, 0.25f)).Select(e => e.ToString()).ToList();

                Assert.That(second, Is.EqualTo(first));
            }
        }

        [Test]
        public void Shot_KeeperDivesTowardShot()
        {
            var world = HoldingWorld(new Vec2(7f, 0f), 0);
            var events = Shoot(world, 0.88f, new Vec2(SoccerPitchTable.Standard().HalfLength, 2f) - world.Players[0].Position);
            var dive = events.First(e => e.Kind == SoccerEventKind.KeeperDive);

            Assert.That(dive.Team, Is.EqualTo(SoccerRules.Blue));
            Assert.That(dive.Strength, Is.EqualTo(1f), "ボールの行く Y の正の側へ飛ぶ");
        }

        [Test]
        public void Shot_WideShotDoesNotMakeKeeperDive()
        {
            var world = HoldingWorld(new Vec2(4f, 0f), 0);
            var events = Shoot(world, 0.6f, new Vec2(1f, 0.8f));

            Assert.That(events.Any(e => e.Kind == SoccerEventKind.KeeperDive), Is.False);
            Assert.That(Scored(events), Is.False);
        }

        // ---- 取る・投げる ----

        [Test]
        public void Catch_SlowBallIsTakenThenThrownToTeammate()
        {
            var world = new SoccerWorld(SoccerPitchTable.Standard(), 1, 1, keepers: true);
            var red = world.Keepers[SoccerRules.Red];
            world.Players[0].Position = new Vec2(-6f, 4f);
            world.Ball.Place(red.Position + new Vec2(2f, 0f));
            world.Ball.Velocity = new Vec2(-5f, 0f);

            var events = Run(world, Steps(0.8f));
            Assert.That(events.Any(e => e.Kind == SoccerEventKind.KeeperCatch && e.Team == SoccerRules.Red), Is.True);
            Assert.That(world.Ball.KeeperTeam, Is.EqualTo(SoccerRules.Red));
            Assert.That(world.Ball.IsHeld, Is.True);

            events = Run(world, Steps(SoccerBalance.KeeperHoldSeconds + 2.5f));
            var thrown = events.First(e => e.Kind == SoccerEventKind.KeeperThrow);
            Assert.That(thrown.Player, Is.EqualTo(0));
            Assert.That(events.Any(e => e.Kind == SoccerEventKind.Possess && e.Player == 0), Is.True, "投げた球が味方の足元に届く");
        }

        [Test]
        public void Catch_KeeperTakesBallFromDribblingOpponent()
        {
            var world = HoldingWorld(new Vec2(10f, 0f), 0);
            var events = new List<SoccerEvent>();
            for (var i = 0; i < Steps(1.5f) && !events.Any(e => e.Kind == SoccerEventKind.KeeperCatch); i++)
            {
                events.AddRange(Run(world, 1, new SoccerInput(new Vec2(1f, 0f), false)));
            }

            var catchEvent = events.First(e => e.Kind == SoccerEventKind.KeeperCatch);

            Assert.That(catchEvent.Team, Is.EqualTo(SoccerRules.Blue));
            Assert.That(catchEvent.Player, Is.EqualTo(0));
            Assert.That(world.Ball.Holder, Is.EqualTo(SoccerBall.NoPlayer));
            Assert.That(Scored(events), Is.False);
        }

        [Test]
        public void Catch_KeeperDoesNotTakeBallFromOwnTeam()
        {
            var world = HoldingWorld(new Vec2(-10f, 0f), 0);
            world.Players[0].Facing = new Vec2(-1f, 0f);
            var events = Run(world, Steps(1.5f), new SoccerInput(new Vec2(-1f, 0f), false));

            Assert.That(events.Any(e => e.Kind == SoccerEventKind.KeeperCatch), Is.False);
            Assert.That(world.Ball.Holder, Is.EqualTo(0));
        }

        [Test]
        public void Body_PlayersCannotWalkThroughKeeper()
        {
            var world = new SoccerWorld(SoccerPitchTable.Standard(), 1, 0, keepers: true);
            var blue = world.Keepers[SoccerRules.Blue];
            world.Players[0].Position = blue.Position + new Vec2(-2f, 0f);
            world.Ball.Place(new Vec2(-10f, 6f));
            Run(world, Steps(2f), new SoccerInput(new Vec2(1f, 0f), false));

            var gap = Vec2.Distance(world.Players[0].Position, blue.Position);
            Assert.That(gap, Is.GreaterThanOrEqualTo(SoccerBalance.KeeperRadius + SoccerBalance.PlayerRadius - 0.01f));
        }

        [Test]
        public void Kickoff_PutsKeepersBackInTheMiddle()
        {
            var world = HoldingWorld(new Vec2(7f, 0f), 0);
            Shoot(world, 0.88f, new Vec2(1f, 0.3f));
            world.Kickoff(SoccerRules.Red, null);

            foreach (var keeper in world.Keepers)
            {
                Assert.That(keeper.Position.Y, Is.EqualTo(0f));
                Assert.That(keeper.State, Is.EqualTo(SoccerKeeperState.Guard));
            }

            Assert.That(world.Ball.KeeperTeam, Is.EqualTo(SoccerBall.NoTeam));
        }
    }
}
