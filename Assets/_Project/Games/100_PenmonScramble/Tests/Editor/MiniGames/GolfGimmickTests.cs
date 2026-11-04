using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PenmonScramble.Core;
using PenmonScramble.Golf.Core;

namespace PenmonScramble.Tests
{
    /// <summary>ゴルフのギミック（氷の床・押し流す帯・間欠泉・ワープ・動く物）とホールの時刻・コースの確認（Docs/MINIGAME_GOLF_PLAN.md フェーズ 6）</summary>
    public class GolfGimmickTests
    {
        private const int MaxSteps = 20000;
        private const float Dt = GolfBalance.StepSeconds;
        private static readonly Vec2 FarCup = new Vec2(40f, 40f);

        // ---- 地面の種類 ----

        /// <summary>氷の床では摩擦が小さいので、同じ速さでも遠くまで滑る</summary>
        [Test]
        public void IceFloor_SlidesFarther()
        {
            const float speed = 3f;
            var ball = new GolfBall(0, Vec2.Zero);
            ball.Launch(new Vec2(speed, 0f));

            Run(Flat(areas: new[] { GolfArea.Ice(GolfGeometry.Rect(-50f, -50f, 50f, 50f)) }), ball);

            var expected = speed * speed / (2f * GolfBalance.RollDecel * GolfBalance.IceFloorDecelScale);
            Assert.That(ball.Position.X, Is.EqualTo(expected).Within(0.2f));
        }

        /// <summary>摩擦より強い帯の上では止まれず、帯の外まで流される</summary>
        [Test]
        public void Flow_StrongerThanFrictionCarriesBallOut()
        {
            var band = GolfArea.Flow(GolfGeometry.Rect(-2f, -2f, 2f, 2f), new Vec2(GolfBalance.RollDecel * 1.5f, 0f));
            var hole = Flat(areas: new[] { band });
            var ball = new GolfBall(0, Vec2.Zero);
            var sim = new GolfBallSim(hole);

            Assert.That(sim.AllStopped(new[] { ball }), Is.False);
            Run(hole, ball);

            Assert.That(ball.Position.X, Is.GreaterThan(2f));
            Assert.That(band.Contains(ball.Position), Is.False);
        }

        /// <summary>弱い帯は止まった球を動かさないが、横切る球は流れの向きへそれる</summary>
        [Test]
        public void Flow_WeakFlowBendsCrossingBall()
        {
            var band = GolfArea.Flow(GolfGeometry.Rect(-50f, 2f, 50f, 4f), new Vec2(GolfBalance.RollDecel * 0.8f, 0f));
            var hole = Flat(areas: new[] { band });
            var resting = new GolfBall(1, new Vec2(10f, 3f));
            var ball = new GolfBall(0, Vec2.Zero);
            ball.Launch(new Vec2(0f, 5f));

            Run(hole, ball, resting);

            Assert.That(ball.Position.X, Is.GreaterThan(0.1f));
            Assert.That(resting.Position, Is.EqualTo(new Vec2(10f, 3f)));
        }

        /// <summary>壁に押しつけられた球も、そこで止まった扱いになる（転がる時間の上限まで待たない）</summary>
        [Test]
        public void Flow_BallPinnedAgainstWallSettles()
        {
            var band = GolfArea.Flow(GolfGeometry.Rect(-50f, -50f, 50f, 50f), new Vec2(GolfBalance.RollDecel * 2f, 0f));
            var hole = Flat(walls: new[] { new GolfWall(new Vec2(3f, -10f), new Vec2(3f, 10f)) }, areas: new[] { band });
            var ball = new GolfBall(0, Vec2.Zero);

            var steps = RunCounting(hole, ball);

            Assert.That(steps * Dt, Is.LessThan(5f));
            Assert.That(ball.Position.X, Is.EqualTo(3f - GolfBalance.BallRadius).Within(0.05f));
        }

        // ---- 間欠泉 ----

        [Test]
        public void Cycle_ActiveAtStartOfEachPeriod()
        {
            var cycle = new GolfCycle(3f, 1f, 0.5f);

            Assert.That(cycle.IsActive(0f), Is.True);
            Assert.That(cycle.IsActive(0.6f), Is.False);
            Assert.That(cycle.IsActive(2.6f), Is.True);
            Assert.That(cycle.UntilActive(1.5f), Is.EqualTo(1f).Within(1e-4f));
            Assert.That(cycle.UntilActive(0.2f), Is.EqualTo(0f));
        }

        /// <summary>間欠泉は噴いている間だけ球を跳ばす（噴いていなければただの地面）</summary>
        [TestCase(0f, true)]
        [TestCase(1.5f, false)]
        public void Geyser_LaunchesOnlyWhileErupting(float time, bool launches)
        {
            var geyser = GolfArea.Geyser(GolfGeometry.Rect(0.5f, -1f, 1.5f, 1f), 3f, new GolfCycle(3f, 1f));
            var hole = Flat(areas: new[] { geyser });
            var ball = new GolfBall(0, Vec2.Zero);
            ball.Launch(new Vec2(5f, 0f));
            var sim = new GolfBallSim(hole);
            var events = new List<GolfEvent>();

            for (var i = 0; i < 60; i++)
            {
                sim.Step(new[] { ball }, events, time);
            }

            Assert.That(events.Any(e => e.Kind == GolfEventKind.Geyser), Is.EqualTo(launches));
        }

        // ---- ワープ ----

        /// <summary>入口に入った球は、速さと向きを保って出口へ飛ぶ</summary>
        [Test]
        public void Warp_SendsBallToExitKeepingVelocity()
        {
            var warp = new GolfWarp(new Vec2(2f, 0f), new Vec2(20f, 20f));
            var hole = Flat(warps: new[] { warp });
            var ball = new GolfBall(0, Vec2.Zero);
            ball.Launch(new Vec2(3f, 0f));
            var sim = new GolfBallSim(hole);
            var events = new List<GolfEvent>();

            for (var i = 0; i < MaxSteps && !events.Any(e => e.Kind == GolfEventKind.Warp); i++)
            {
                sim.Step(new[] { ball }, events);
            }

            Assert.That(Vec2.Distance(ball.Position, warp.Exit), Is.LessThan(0.1f));
            Assert.That(ball.Velocity.X, Is.GreaterThan(0f));
            Assert.That(Math.Abs(ball.Velocity.Y), Is.LessThan(1e-4f));
            Assert.That(events.Single(e => e.Kind == GolfEventKind.Warp).Strength, Is.EqualTo(0f));
        }

        // ---- 動く物 ----

        [Test]
        public void Mover_BlockSlidesBetweenEnds()
        {
            var block = GolfMover.Block(new Vec2(0f, 5f), 2f, 1f, new Vec2(4f, 0f), 4f);

            Assert.That(block.Center(0f), Is.EqualTo(new Vec2(0f, 5f)));
            Assert.That(block.Center(2f).X, Is.EqualTo(4f).Within(1e-4f));
            Assert.That(block.Center(4f).X, Is.EqualTo(0f).Within(1e-4f));
            // まん中で一番速い（1往復 4 秒・幅 4m なら π m/s）
            Assert.That(block.VelocityAt(block.Center(1f), 1f).X, Is.EqualTo(MathF.PI).Within(1e-3f));
        }

        [Test]
        public void Mover_WindmillTipMovesAroundHub()
        {
            var windmill = GolfMover.Windmill(new Vec2(5f, 5f), 2f, 4, 4f, spin: 1);
            var tip = windmill.WallAt(0, 0f).B;
            var quarter = windmill.WallAt(0, 1f).B;

            Assert.That(tip.X, Is.EqualTo(7f).Within(1e-4f));
            Assert.That(quarter.Y, Is.EqualTo(7f).Within(1e-3f));
            // 羽根の先の速さ ＝ 半径 × 角の速さ（左回り）
            var velocity = windmill.VelocityAt(tip, 0f);
            Assert.That(velocity.Y, Is.EqualTo(2f * MathF.PI * 2f / 4f).Within(1e-3f));
        }

        /// <summary>動いてくる壁に当たった止まった球は、壁の動く向きへ弾かれる</summary>
        [Test]
        public void Mover_PushesRestingBall()
        {
            var block = GolfMover.Block(new Vec2(-2f, 0f), 1f, 1f, new Vec2(4f, 0f), 4f);
            var hole = Flat(movers: new[] { block });
            var ball = new GolfBall(0, Vec2.Zero);
            var sim = new GolfBallSim(hole);
            var events = new List<GolfEvent>();

            for (var t = 0f; t < 1.5f && ball.Velocity.IsNearlyZero; t += Dt)
            {
                sim.Step(new[] { ball }, events, t);
            }

            Assert.That(ball.Velocity.X, Is.GreaterThan(0.5f));
        }

        /// <summary>動く物の通る範囲で止まった球は、範囲の外へよけられる（打順の終わりに GolfHoleWorld が呼ぶ）</summary>
        [TestCase(true)]
        [TestCase(false)]
        public void Mover_BallInSweepIsMovedAside(bool windmill)
        {
            var mover = windmill
                ? GolfMover.Windmill(new Vec2(0f, 6f), 1.5f, 4, 4f)
                : GolfMover.Block(new Vec2(-2f, 5f), 2f, 1f, new Vec2(4f, 0f), 4f);
            var sim = new GolfBallSim(Flat(movers: new[] { mover }));
            var ball = new GolfBall(0, new Vec2(0.3f, 5f));
            var outside = new GolfBall(1, new Vec2(0f, -3f));
            var events = new List<GolfEvent>();

            sim.ClearSweeps(new[] { ball, outside }, events);

            Assert.That(mover.TryPushOutOfSweep(ball.Position, out _), Is.False);
            Assert.That(outside.Position, Is.EqualTo(new Vec2(0f, -3f)));
            Assert.That(events.Count(e => e.Kind == GolfEventKind.Nudged), Is.EqualTo(1));
        }

        // ---- ホールの時刻 ----

        /// <summary>時刻は狙っている間・転がっている間・Idle で進む。Idle は持ち時間を減らさない</summary>
        [Test]
        public void HoleTime_AdvancesThroughPhases()
        {
            var world = new GolfHoleWorld(Flat(), GolfTurnOrder.SeatOrder(1));

            world.Tick(0.5f);
            Assert.That(world.Time, Is.EqualTo(0.5f).Within(1e-4f));
            var left = world.ShotTimeLeft;

            world.Idle(1f);
            Assert.That(world.Time, Is.EqualTo(1.5f).Within(1e-4f));
            Assert.That(world.ShotTimeLeft, Is.EqualTo(left));

            world.Shoot(0, 0f, 0.3f);
            world.Tick(0.25f);
            world.Idle(5f);
            Assert.That(world.Time, Is.EqualTo(1.75f).Within(0.02f));
        }

        /// <summary>写しの時刻は、少しのずれは少しずつ、大きなずれはその場でホストの時刻に合う</summary>
        [Test]
        public void Mirror_FollowsHostTime()
        {
            var course = new GolfCourseDef(1, "test", 1, new[] { Flat() });
            var host = new GolfRound(course, new[] { new GolfTeam(new[] { 1, 3, 5 }) });
            var mirror = GolfRound.CreateMirror(course, new[] { new GolfTeam(new[] { 1, 3, 5 }) });

            host.Tick(3f);
            mirror.ApplySnapshot(GolfSnapshot.Capture(host, default, new GolfNetEvent[0]), -1);
            Assert.That(mirror.World.Time, Is.EqualTo(3f).Within(1e-4f));

            host.Tick(0.2f);
            mirror.ApplySnapshot(GolfSnapshot.Capture(host, default, new GolfNetEvent[0]), -1);
            for (var i = 0; i < 10; i++)
            {
                mirror.TickMirror(0.02f, GolfNetRules.SnapshotSeconds);
            }

            Assert.That(mirror.World.Time, Is.EqualTo(3.2f + 0.2f).Within(0.02f));
        }

        // ---- コースの確認 ----

        /// <summary>池に囲まれた小島のティーは、どう打っても落ちて戻るので「詰む所」になる</summary>
        [Test]
        public void CourseCheck_FindsStuckIsland()
        {
            var ground = GolfGeometry.Rect(0f, 0f, 10f, 10f);
            // 外の四角から、まん中の小さな四角（0.8m 四方）をくり抜いた池（いちばん弱く打っても島から出る）
            var water = GolfArea.Water(new[]
            {
                new Vec2(0f, 0f), new Vec2(10f, 0f), new Vec2(10f, 10f), new Vec2(0f, 10f), new Vec2(0f, 5.4f),
                new Vec2(5.4f, 5.4f), new Vec2(5.4f, 4.6f), new Vec2(4.6f, 4.6f), new Vec2(4.6f, 5.4f), new Vec2(0f, 5.4f),
            });
            var island = new GolfHoleDef("island", 2, new Vec2(5f, 5f), new Vec2(1f, 9f), ground, GolfHoleDef.WallsAround(ground), new[] { water });

            var check = GolfCourseCheck.CheckHole(island);

            Assert.That(check.StuckCells, Is.GreaterThan(0));
            Assert.That(check.Passed, Is.False);
        }

        // ---- 道具 ----

        private static GolfHoleDef Flat(GolfWall[] walls = null, GolfArea[] areas = null, GolfMover[] movers = null, GolfWarp[] warps = null)
        {
            return new GolfHoleDef("test", 3, Vec2.Zero, FarCup, GolfGeometry.Rect(-50f, -50f, 50f, 50f), walls ?? new GolfWall[0], areas ?? new GolfArea[0], movers, warps);
        }

        private static void Run(GolfHoleDef hole, params GolfBall[] balls) => RunCounting(hole, balls);

        private static int RunCounting(GolfHoleDef hole, params GolfBall[] balls)
        {
            var sim = new GolfBallSim(hole);
            var events = new List<GolfEvent>();
            for (var i = 0; i < MaxSteps; i++)
            {
                sim.Step(balls, events, i * Dt);
                if (sim.AllStopped(balls, (i + 1) * Dt))
                {
                    return i;
                }
            }

            Assert.Fail("球が止まらない");
            return MaxSteps;
        }

    }
}
