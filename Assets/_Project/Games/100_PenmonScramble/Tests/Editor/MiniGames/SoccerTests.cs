using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.Soccer.Core;

namespace PenmonScramble.Tests
{
    /// <summary>サッカーのピッチの上の計算</summary>
    public class SoccerTests
    {
        private const float Dt = SoccerRules.StepSeconds;

        private static SoccerWorld World(int red = 0, int blue = 0) => new SoccerWorld(SoccerPitchTable.Standard(), red, blue);

        /// <summary>入力を変えずに steps 歩進め、起きたことを返す</summary>
        private static List<SoccerEvent> Run(SoccerWorld world, int steps, params SoccerInput[] inputs)
        {
            var events = new List<SoccerEvent>();
            for (var i = 0; i < steps; i++)
            {
                world.Step(inputs, events);
            }

            return events;
        }

        private static int Steps(float seconds) => (int)MathF.Ceiling(seconds / Dt);

        private static void Throw(SoccerWorld world, Vec2 position, Vec2 velocity, float height = 0f, float verticalSpeed = 0f)
        {
            world.Ball.Place(position);
            world.Ball.Velocity = velocity;
            world.Ball.Height = height;
            world.Ball.VerticalSpeed = verticalSpeed;
        }

        /// <summary>持たせてから steps 歩シュートを押し、離して蹴る</summary>
        private static List<SoccerEvent> ChargeAndKick(SoccerWorld world, int chargeSteps, Vec2 stick)
        {
            var events = Run(world, chargeSteps, new SoccerInput(Vec2.Zero, true));
            events.AddRange(Run(world, 1, new SoccerInput(stick, false)));
            return events;
        }

        /// <summary>赤の1体の目の前にボールを置いて持たせる</summary>
        private static SoccerWorld HoldingWorld(Vec2 at)
        {
            var world = World(red: 1);
            world.Players[0].Position = at;
            world.Ball.Place(at + new Vec2(SoccerBalance.DribbleDistance, 0f));
            Run(world, 1);
            Assert.That(world.Ball.Holder, Is.EqualTo(0));
            return world;
        }

        // ---- 定義表 ----

        /// <summary>Id とスタジアムの番号は記録に残るので、一度配ったら変えない</summary>
        [Test]
        public void Table_SoccerIdAndStadiumsAreFixed()
        {
            var soccer = MiniGameTable.Get(MiniGameTable.SoccerId);

            Assert.That(MiniGameTable.SoccerId, Is.EqualTo("soccer"));
            Assert.That(soccer.LowerIsBetter, Is.False);
            Assert.That(soccer.Courses.Count, Is.EqualTo(5));
            for (var i = 0; i < soccer.Courses.Count; i++)
            {
                Assert.That(soccer.Courses[i].No, Is.EqualTo(i + 1));
                Assert.That(soccer.Courses[i].World, Is.EqualTo(i + 1));
            }
        }

        [TestCase(3, "+3")]
        [TestCase(0, "±0")]
        [TestCase(-1, "−1")]
        public void Guide_FormatsGoalDifference(int difference, string expected)
        {
            Assert.That(SoccerCourseGuide.FormatGoalDifference(difference), Is.EqualTo(expected));
        }

        // ---- ピッチ ----

        [Test]
        public void Pitch_HasOneMouthPerGoalAndNets()
        {
            var pitch = SoccerPitchTable.Standard();

            foreach (var goal in pitch.Goals)
            {
                Assert.That(pitch.Walls.Count(w => w.Kind == SoccerWallKind.GoalMouth && w.Team == goal.Team), Is.EqualTo(1));
                Assert.That(pitch.Walls.Count(w => w.Kind == SoccerWallKind.Net && w.Team == goal.Team), Is.EqualTo(3));
            }
        }

        [Test]
        public void Pitch_KickoffSpotsAreInsideAndOnOwnSide()
        {
            var pitch = SoccerPitchTable.Standard();

            for (var team = 0; team < SoccerRules.TeamCount; team++)
            {
                for (var i = 0; i < SoccerRules.PlayersPerTeam; i++)
                {
                    foreach (var kicking in new[] { true, false })
                    {
                        var spot = pitch.KickoffSpot(team, i, kicking);
                        Assert.That(pitch.IsInside(spot), Is.True, $"{team} {i} {kicking}");
                        Assert.That(spot.X * SoccerPitchDef.AttackDirection(team), Is.LessThan(0f), $"{team} {i} {kicking}");
                    }
                }
            }
        }

        // ---- ボール ----

        /// <summary>一定の摩擦なので、止まるまでの距離は 速さ² ÷ (2 × 摩擦) くらい</summary>
        [TestCase(4f)]
        [TestCase(8f)]
        public void Ball_RollsToPredictableDistance(float speed)
        {
            var world = World();
            Throw(world, Vec2.Zero, new Vec2(0f, speed));

            Run(world, Steps(3f));

            var expected = speed * speed / (2f * SoccerBalance.RollDecel);
            Assert.That(world.Ball.Velocity, Is.EqualTo(Vec2.Zero));
            Assert.That(world.Ball.Position.Y, Is.EqualTo(expected).Within(0.25f));
        }

        [Test]
        public void Ball_BouncesOffBoardAndLosesSpeed()
        {
            var world = World();
            const float speed = 6f;
            Throw(world, new Vec2(0f, 6.5f), new Vec2(0f, speed));

            var events = Run(world, Steps(0.5f));

            Assert.That(events.Any(e => e.Kind == SoccerEventKind.WallHit), Is.True);
            Assert.That(world.Ball.Velocity.Y, Is.LessThan(0f));
            Assert.That(-world.Ball.Velocity.Y, Is.LessThan(speed * SoccerBalance.WallBounce));
            Assert.That(world.Pitch.IsInside(world.Ball.Position), Is.True);
        }

        /// <summary>1歩で半径より長く進む速い球でも、壁をすり抜けない</summary>
        [Test]
        public void Ball_FastShotDoesNotTunnelThroughBoard()
        {
            var world = World();
            Throw(world, new Vec2(0f, 7f), new Vec2(0f, SoccerBalance.MaxShotSpeed));

            Run(world, 3);

            Assert.That(world.Pitch.IsInside(world.Ball.Position), Is.True);
            Assert.That(world.Ball.Velocity.Y, Is.LessThan(0f));
        }

        [Test]
        public void Ball_LoftedBallLandsAndRolls()
        {
            var world = World();
            Throw(world, new Vec2(-10f, 0f), new Vec2(4f, 0f), 0f, SoccerBalance.LoftUpSpeed);

            var events = Run(world, Steps(3f));

            Assert.That(events.Any(e => e.Kind == SoccerEventKind.Land), Is.True);
            Assert.That(world.Ball.IsAirborne, Is.False);
        }

        // ---- 持つ / 離す ----

        [Test]
        public void Pickup_WalkingIntoBallHoldsItInFront()
        {
            var world = World(red: 1);
            world.Players[0].Position = new Vec2(-3f, 0f);

            var events = Run(world, Steps(1f), new SoccerInput(new Vec2(1f, 0f), false));

            Assert.That(world.Ball.Holder, Is.EqualTo(0));
            Assert.That(events.Count(e => e.Kind == SoccerEventKind.Possess), Is.EqualTo(1));
            var player = world.Players[0];
            Assert.That(Vec2.Distance(world.Ball.Position, player.Position + player.Facing * SoccerBalance.DribbleDistance), Is.LessThan(0.01f));
        }

        [Test]
        public void Pickup_TooHighBallIsNotHeld()
        {
            var world = World(red: 1);
            world.Players[0].Position = Vec2.Zero;
            Throw(world, new Vec2(0.3f, 0f), Vec2.Zero, 1.5f, 0f);

            Run(world, 1);

            Assert.That(world.Ball.Holder, Is.EqualTo(SoccerBall.NoPlayer));
        }

        [Test]
        public void Pickup_CarrierIsSlower()
        {
            var free = World(red: 1);
            free.Players[0].Position = new Vec2(-3f, 5f);
            var carrying = HoldingWorld(new Vec2(-3f, 0f));

            var right = new SoccerInput(new Vec2(1f, 0f), false);
            Run(free, Steps(1f), right);
            Run(carrying, Steps(1f), right);

            Assert.That(carrying.Players[0].Velocity.Length, Is.EqualTo(SoccerBalance.RunSpeed * SoccerBalance.CarrySpeedScale).Within(0.01f));
            Assert.That(free.Players[0].Velocity.Length, Is.EqualTo(SoccerBalance.RunSpeed).Within(0.01f));
        }

        /// <summary>蹴った子は、足元に残ったボールでも少しの間は持てない（蹴った瞬間にまた拾わないように）</summary>
        [Test]
        public void Kick_KickerCannotRepickRightAway()
        {
            var world = HoldingWorld(new Vec2(-5f, 0f));
            ChargeAndKick(world, 1, Vec2.Zero);
            Assert.That(world.Ball.Holder, Is.EqualTo(SoccerBall.NoPlayer));

            // 蹴ったボールを足元へ戻して止める
            var feet = world.Players[0].Position + new Vec2(SoccerBalance.DribbleDistance, 0f);
            var lockSeconds = world.Ball.LockSeconds;
            world.Ball.Position = feet;
            world.Ball.Velocity = Vec2.Zero;

            Run(world, (int)MathF.Round(lockSeconds / Dt) - 1);
            Assert.That(world.Ball.Holder, Is.EqualTo(SoccerBall.NoPlayer));

            Run(world, 2);
            Assert.That(world.Ball.Holder, Is.EqualTo(0));
        }

        // ---- シュート ----

        [Test]
        public void Kick_LongerChargeIsFasterAndFullChargeIsLofted()
        {
            var weak = HoldingWorld(new Vec2(-5f, 0f));
            var full = HoldingWorld(new Vec2(-5f, 0f));

            var weakKick = ChargeAndKick(weak, 1, Vec2.Zero).Single(e => e.Kind == SoccerEventKind.Kick);
            var fullKick = ChargeAndKick(full, Steps(SoccerBalance.ShotChargeSeconds) + 2, Vec2.Zero).Single(e => e.Kind == SoccerEventKind.Kick);

            Assert.That(weakKick.Lofted, Is.False);
            Assert.That(fullKick.Lofted, Is.True);
            Assert.That(full.Ball.Velocity.Length, Is.GreaterThan(weak.Ball.Velocity.Length));
            Assert.That(full.Ball.IsAirborne, Is.True);
        }

        [Test]
        public void Kick_GoesInStickDirection()
        {
            var world = HoldingWorld(new Vec2(-5f, 0f));

            ChargeAndKick(world, 1, new Vec2(0f, 1f));

            Assert.That(world.Ball.Velocity.Normalized.Y, Is.GreaterThan(0.99f));
        }

        /// <summary>ゴールの口から少し外れた向きは、口の方へ最大の角度まで寄せる</summary>
        [Test]
        public void Aim_TurnsTowardGoalMouthUpToLimit()
        {
            var goal = SoccerPitchTable.Standard().GoalOf(SoccerRules.Blue);
            var from = new Vec2(4f, 0f);
            var max = SoccerBalance.AimAssistDegrees * MathF.PI / 180f;

            // 口の上の柱から 10° 外れている → 柱まで寄る
            var post = MathF.Atan2(goal.HalfWidth - SoccerBalance.BallRadius, goal.LineX - from.X);
            var slightlyOff = Vec2.Right.Rotated(post + 10f * MathF.PI / 180f);
            var turned = SoccerAim.Assist(from, slightlyOff, goal);
            Assert.That(MathF.Atan2(turned.Y, turned.X), Is.EqualTo(post).Within(0.001f));

            // 大きく外れた向き（80°）→ 最大の角度だけ寄る
            var far = 80f * MathF.PI / 180f;
            var turnedFar = SoccerAim.Assist(from, Vec2.Right.Rotated(far), goal);
            Assert.That(MathF.Atan2(turnedFar.Y, turnedFar.X), Is.EqualTo(far - max).Within(0.001f));
        }

        [Test]
        public void Aim_KeepsDirectionInsideMouthOrAwayFromGoal()
        {
            var goal = SoccerPitchTable.Standard().GoalOf(SoccerRules.Blue);
            var from = new Vec2(4f, 0f);

            Assert.That(SoccerAim.Assist(from, Vec2.Right, goal), Is.EqualTo(Vec2.Right));
            Assert.That(SoccerAim.Assist(from, new Vec2(-1f, 0.2f), goal), Is.EqualTo(new Vec2(-1f, 0.2f).Normalized));
        }

        // ---- ゴール ----

        [Test]
        public void Goal_RollingThroughMouthScores()
        {
            var world = World(red: 1);
            world.Players[0].Position = new Vec2(-10f, 0f);
            Throw(world, new Vec2(10f, 1f), new Vec2(10f, 0f));

            var events = Run(world, Steps(1f));

            var goal = events.Single(e => e.Kind == SoccerEventKind.Goal);
            Assert.That(goal.Team, Is.EqualTo(SoccerRules.Red));
            Assert.That(world.Score(SoccerRules.Red), Is.EqualTo(1));
            Assert.That(world.Score(SoccerRules.Blue), Is.EqualTo(0));
            Assert.That(world.IsGoalScored, Is.True);
        }

        [Test]
        public void Goal_BlueScoresInLeftGoal()
        {
            var world = World();
            Throw(world, new Vec2(-10f, -1f), new Vec2(-10f, 0f));

            Run(world, Steps(1f));

            Assert.That(world.Score(SoccerRules.Blue), Is.EqualTo(1));
        }

        /// <summary>バーより高い球はゴールの口で跳ね返り、点にならない</summary>
        [Test]
        public void Goal_BallAboveBarDoesNotScore()
        {
            var world = World();
            var bar = world.Pitch.GoalOf(SoccerRules.Blue).BarHeight;
            Throw(world, new Vec2(13f, 0f), new Vec2(10f, 0f), bar + 1f, 0f);

            var events = Run(world, Steps(0.3f));

            Assert.That(events.Any(e => e.Kind == SoccerEventKind.Goal), Is.False);
            Assert.That(world.Score(SoccerRules.Red), Is.EqualTo(0));
            Assert.That(world.Ball.Velocity.X, Is.LessThan(0f));
        }

        [Test]
        public void Goal_ShotBesideMouthBouncesOffBoard()
        {
            var world = World();
            Throw(world, new Vec2(10f, 5f), new Vec2(10f, 0f));

            var events = Run(world, Steps(1f));

            Assert.That(events.Any(e => e.Kind == SoccerEventKind.Goal), Is.False);
            Assert.That(world.Pitch.IsInside(world.Ball.Position), Is.True);
        }

        [Test]
        public void Goal_DribblingIntoGoalScoresAndReleasesBall()
        {
            var world = HoldingWorld(new Vec2(10f, 0f));

            var events = Run(world, Steps(2f), new SoccerInput(new Vec2(1f, 0f), false));

            Assert.That(events.Single(e => e.Kind == SoccerEventKind.Goal).Player, Is.EqualTo(0));
            Assert.That(world.Ball.IsHeld, Is.False);
            Assert.That(world.Players[0].Position.X, Is.LessThan(world.Pitch.HalfLength));
        }

        [Test]
        public void Goal_NobodyCanHoldUntilKickoff()
        {
            var world = World(red: 1);
            world.Players[0].Position = new Vec2(0f, 6f);
            Throw(world, new Vec2(12f, 0f), new Vec2(12f, 0f));
            Run(world, Steps(0.5f));
            Assert.That(world.IsGoalScored, Is.True);

            // 点が入った後は、足元にボールがあっても持てない
            Throw(world, new Vec2(5f, 0f), Vec2.Zero);
            world.Players[0].Position = new Vec2(4.5f, 0f);
            Run(world, 1);
            Assert.That(world.Ball.IsHeld, Is.False);

            var events = new List<SoccerEvent>();
            world.Kickoff(SoccerRules.Blue, events);
            Assert.That(world.IsGoalScored, Is.False);
            Assert.That(world.Ball.Position, Is.EqualTo(Vec2.Zero));
            Assert.That(events.Single().Kind, Is.EqualTo(SoccerEventKind.Kickoff));
        }

        // ---- 詰まり ----

        [Test]
        public void Idle_UntouchedBallRollsTowardNearestPlayer()
        {
            var world = World(red: 1);
            world.Players[0].Position = new Vec2(-5f, 0f);
            Throw(world, new Vec2(5f, 7f), Vec2.Zero);

            var events = Run(world, Steps(SoccerBalance.IdleNudgeSeconds) + 1);

            Assert.That(events.Count(e => e.Kind == SoccerEventKind.Nudge), Is.EqualTo(1));
            Assert.That(world.Ball.Velocity.X, Is.LessThan(0f));
        }
    }
}
