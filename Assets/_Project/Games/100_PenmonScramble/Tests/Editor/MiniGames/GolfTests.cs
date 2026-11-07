using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PenmonScramble.Core;
using PenmonScramble.Golf.Core;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Tests
{
    /// <summary>ゴルフの球の計算・ホールの進行・ガイド線（Docs/MINIGAME_GOLF_PLAN.md フェーズ 2）</summary>
    public class GolfTests
    {
        private const int MaxSteps = 20000;
        private static readonly Vec2 FarCup = new Vec2(40f, 40f);

        // ---- 球の計算 ----

        /// <summary>平らな地面では一定の減り方で止まる: 距離 ＝ 速さ² ÷（2 × 摩擦）</summary>
        [TestCase(3f)]
        [TestCase(6f)]
        [TestCase(9f)]
        public void Ball_StraightShotStopsAtExpectedDistance(float speed)
        {
            var ball = new GolfBall(0, Vec2.Zero);
            ball.Launch(new Vec2(speed, 0f));

            Run(Flat(), ball);

            var expected = speed * speed / (2f * GolfBalance.RollDecel);
            Assert.That(ball.Position.X, Is.EqualTo(expected).Within(0.1f));
            Assert.That(ball.Position.Y, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void Ball_StrongerShotGoesFarther()
        {
            Assert.That(GolfHoleWorld.ShotSpeed(1f), Is.GreaterThan(GolfHoleWorld.ShotSpeed(0.5f)));
            Assert.That(GolfHoleWorld.ShotSpeed(0f), Is.EqualTo(GolfBalance.MinShotSpeed));
            Assert.That(GolfHoleWorld.ShotSpeed(2f), Is.EqualTo(GolfBalance.MaxShotSpeed));
        }

        /// <summary>1歩で進む距離が球の半径より短い（薄い壁をすり抜けない）</summary>
        [Test]
        public void Balance_BallCannotTunnelThroughWalls()
        {
            Assert.That(GolfBalance.MaxBallSpeed * GolfBalance.StepSeconds, Is.LessThan(GolfBalance.BallRadius));
            Assert.That(GolfBalance.MaxShotSpeed, Is.LessThanOrEqualTo(GolfBalance.MaxBallSpeed));
        }

        /// <summary>壁に斜めに当たると、壁に沿う速さはそのまま、壁に向かう速さは跳ね返りの強さ倍で返る</summary>
        [Test]
        public void Ball_WallReflectsWithBounce()
        {
            var hole = Flat(walls: new[] { new GolfWall(new Vec2(5f, -20f), new Vec2(5f, 20f)) });
            var ball = new GolfBall(0, Vec2.Zero);
            ball.Launch(new Vec2(4f, 4f));
            var sim = new GolfBallSim(hole);
            var events = new List<GolfEvent>();

            for (var i = 0; i < MaxSteps && ball.Velocity.X >= 0f; i++)
            {
                sim.Step(new[] { ball }, events);
            }

            Assert.That(ball.Velocity.X, Is.LessThan(0f));
            Assert.That(-ball.Velocity.X / ball.Velocity.Y, Is.EqualTo(GolfBalance.WallBounce).Within(0.01f));
            Assert.That(ball.Position.X, Is.LessThanOrEqualTo(5f - GolfBalance.BallRadius + 1e-4f));
            Assert.That(events.Any(e => e.Kind == GolfEventKind.WallHit), Is.True);
        }

        [Test]
        public void Ball_SlowOverCupDropsIn()
        {
            var cup = new Vec2(5f, 0f);
            var hole = Flat(cup: cup);
            var ball = new GolfBall(0, new Vec2(4f, 0f));
            ball.Launch(new Vec2(2.5f, 0f));

            var events = Run(hole, ball);

            Assert.That(ball.InCup, Is.True);
            Assert.That(ball.Position, Is.EqualTo(cup));
            Assert.That(events.Count(e => e.Kind == GolfEventKind.CupIn), Is.EqualTo(1));
        }

        /// <summary>速すぎると縁で曲がって通り過ぎる</summary>
        [Test]
        public void Ball_FastOverCupLipsOut()
        {
            var cup = new Vec2(5f, 0f);
            var hole = Flat(cup: cup);
            var ball = new GolfBall(0, new Vec2(0f, 0.12f));
            ball.Launch(new Vec2(9f, 0f));

            var events = Run(hole, ball);

            Assert.That(ball.InCup, Is.False);
            Assert.That(ball.Position.X, Is.GreaterThan(cup.X + 1f));
            Assert.That(ball.Position.Y, Is.LessThan(0.12f), "縁でカップの方へ曲がる");
            Assert.That(events.Count(e => e.Kind == GolfEventKind.LipOut), Is.EqualTo(1));
        }

        [Test]
        public void Ball_AirborneBallDoesNotDropIn()
        {
            var cup = new Vec2(5f, 0f);
            var hole = Flat(cup: cup, areas: new[] { GolfArea.Jump(MiniGameGeometry.Rect(4f, -1f, 4.6f, 1f), 3f) });
            var ball = new GolfBall(0, new Vec2(3f, 0f));
            ball.Launch(new Vec2(3.5f, 0f));

            Run(hole, ball);

            Assert.That(ball.InCup, Is.False);
            Assert.That(ball.Position.X, Is.GreaterThan(cup.X));
        }

        /// <summary>低い壁は、ジャンプ台で跳べば越えられる。跳ばなければ跳ね返る</summary>
        [TestCase(true)]
        [TestCase(false)]
        public void Ball_JumpClearsLowWall(bool withJump)
        {
            var walls = new[] { GolfWall.Low(new Vec2(3f, -5f), new Vec2(3f, 5f)) };
            var areas = withJump ? new[] { GolfArea.Jump(MiniGameGeometry.Rect(1.5f, -1f, 2.2f, 1f), 3.2f) } : Array.Empty<GolfArea>();
            var ball = new GolfBall(0, Vec2.Zero);
            ball.Launch(new Vec2(6f, 0f));

            var events = Run(Flat(walls: walls, areas: areas), ball);

            Assert.That(ball.Position.X > 3f, Is.EqualTo(withJump));
            Assert.That(events.Any(e => e.Kind == GolfEventKind.Jump), Is.EqualTo(withJump));
        }

        /// <summary>止まっている球は、摩擦より強い坂でだけ転がり出す</summary>
        [TestCase(GolfBalance.RollDecel * 0.5f, false)]
        [TestCase(GolfBalance.RollDecel * 1.5f, true)]
        public void Ball_SlopeMovesRestingBallOnlyWhenSteep(float push, bool moves)
        {
            var hole = Flat(areas: new[] { GolfArea.Slope(MiniGameGeometry.Rect(-2f, -2f, 2f, 2f), new Vec2(push, 0f)) });
            var ball = new GolfBall(0, Vec2.Zero);
            var sim = new GolfBallSim(hole);

            Assert.That(sim.AllStopped(new[] { ball }), Is.EqualTo(!moves));
            Run(hole, ball);
            Assert.That(ball.Position.X > 1f, Is.EqualTo(moves));
        }

        [Test]
        public void Ball_SandStopsSooner()
        {
            var sand = Flat(areas: new[] { GolfArea.Sand(MiniGameGeometry.Rect(-50f, -50f, 50f, 50f)) });
            var onSand = new GolfBall(0, Vec2.Zero);
            onSand.Launch(new Vec2(6f, 0f));
            var onGrass = new GolfBall(0, Vec2.Zero);
            onGrass.Launch(new Vec2(6f, 0f));

            Run(sand, onSand);
            Run(Flat(), onGrass);

            Assert.That(onSand.Position.X, Is.LessThan(onGrass.Position.X * 0.5f));
        }

        [Test]
        public void Ball_WaterReturnsToShotPosition()
        {
            var hole = Flat(areas: new[] { GolfArea.Water(MiniGameGeometry.Rect(3f, -2f, 6f, 2f)) });
            var ball = new GolfBall(0, new Vec2(1f, 0f));
            ball.Launch(new Vec2(5f, 0f));

            var events = Run(hole, ball);

            Assert.That(ball.Position, Is.EqualTo(new Vec2(1f, 0f)));
            Assert.That(events.Count(e => e.Kind == GolfEventKind.Fell), Is.EqualTo(1));
        }

        [Test]
        public void Ball_LeavingGroundCountsAsFall()
        {
            var hole = new GolfHoleDef("t", 3, Vec2.Zero, FarCup, MiniGameGeometry.Rect(-1f, -1f, 3f, 1f), new GolfWall[0], new GolfArea[0]);
            var ball = new GolfBall(0, Vec2.Zero);
            ball.Launch(new Vec2(5f, 0f));

            var events = Run(hole, ball);

            Assert.That(events.Any(e => e.Kind == GolfEventKind.Fell), Is.True);
            Assert.That(ball.Position, Is.EqualTo(Vec2.Zero));
        }

        /// <summary>まっすぐ当てると、当てた球はほぼ止まり、当てられた球が前へ飛ぶ</summary>
        [Test]
        public void Ball_HeadOnCollisionPassesSpeed()
        {
            var shooter = new GolfBall(0, Vec2.Zero);
            var target = new GolfBall(1, new Vec2(1f, 0f));
            shooter.Launch(new Vec2(5f, 0f));

            var events = Run(Flat(), shooter, target);

            Assert.That(target.Position.X, Is.GreaterThan(4f));
            Assert.That(shooter.Position.X, Is.LessThan(1.5f));
            Assert.That(events.Any(e => e.Kind == GolfEventKind.BallHit && e.Other == 1), Is.True);
        }

        [Test]
        public void Ball_InCupIsNotHit()
        {
            var cup = new Vec2(2f, 0f);
            var shooter = new GolfBall(0, Vec2.Zero);
            var holed = new GolfBall(1, cup) { InCup = true };
            shooter.Launch(new Vec2(8f, 0f));

            var events = Run(Flat(cup: cup), shooter, holed);

            Assert.That(events.Any(e => e.Kind == GolfEventKind.BallHit), Is.False);
            Assert.That(holed.Position, Is.EqualTo(cup));
        }

        // ---- ホールの進行 ----

        [Test]
        public void World_TeeSpreadsBallsSideways()
        {
            var world = new GolfHoleWorld(Flat(), GolfTurnOrder.SeatOrder(4));

            for (var i = 0; i < 4; i++)
            {
                for (var j = i + 1; j < 4; j++)
                {
                    Assert.That(Vec2.Distance(world.Balls[i].Position, world.Balls[j].Position), Is.GreaterThan(GolfBalance.BallRadius * 2f));
                }
            }
        }

        [Test]
        public void World_StartsAimingAtCup()
        {
            var world = new GolfHoleWorld(Flat(cup: new Vec2(0f, 10f)), GolfTurnOrder.SeatOrder(1));

            Assert.That(world.Phase, Is.EqualTo(GolfHolePhase.Aiming));
            Assert.That(world.AimAngle, Is.EqualTo(MathF.PI / 2f).Within(1e-4f));
            Assert.That(world.ShotTimeLeft, Is.EqualTo(GolfRules.ShotSeconds));
        }

        [Test]
        public void World_OnlyCurrentPlayerCanShoot()
        {
            var world = new GolfHoleWorld(Flat(), GolfTurnOrder.SeatOrder(2));

            Assert.That(world.Shoot(1, 0f, 0.5f), Is.False);
            Assert.That(world.Shoot(0, 0f, 0.5f), Is.True);
            Assert.That(world.Shoot(0, 0f, 0.5f), Is.False, "転がっている間は打てない");
        }

        [Test]
        public void World_TurnsAlternate()
        {
            var world = new GolfHoleWorld(Flat(), new[] { 1, 0 });
            Assert.That(world.CurrentPlayer, Is.EqualTo(1));

            ShootAndSettle(world, 0.1f);
            Assert.That(world.CurrentPlayer, Is.EqualTo(0));

            ShootAndSettle(world, 0.1f);
            Assert.That(world.CurrentPlayer, Is.EqualTo(1));
            Assert.That(world.Strokes(0), Is.EqualTo(1));
            Assert.That(world.Strokes(1), Is.EqualTo(1));
        }

        /// <summary>カップに入った人は抜け、残りの人だけで続ける</summary>
        [Test]
        public void World_HoledPlayerIsSkipped()
        {
            var world = new GolfHoleWorld(Flat(cup: new Vec2(1.5f, 0f)), GolfTurnOrder.SeatOrder(2));

            world.Shoot(0, world.AngleToCup(0), PowerFor(3f));
            Settle(world);
            Assert.That(world.HoledOut(0), Is.True);
            Assert.That(world.IsDone(0), Is.True);
            Assert.That(world.CurrentPlayer, Is.EqualTo(1));

            ShootAndSettle(world, 0f, -MathF.PI / 2f);
            Assert.That(world.CurrentPlayer, Is.EqualTo(1));
        }

        [Test]
        public void World_FinishesWhenEveryoneHoledOut()
        {
            var world = new GolfHoleWorld(Flat(cup: new Vec2(1.5f, 0f)), GolfTurnOrder.SeatOrder(1));
            var finished = 0;
            world.Happened += e => finished += e.Kind == GolfEventKind.HoleFinished ? 1 : 0;

            world.Shoot(0, world.AngleToCup(0), PowerFor(3f));
            Settle(world);

            Assert.That(world.Phase, Is.EqualTo(GolfHolePhase.Finished));
            Assert.That(world.Strokes(0), Is.EqualTo(1));
            Assert.That(finished, Is.EqualTo(1));
        }

        /// <summary>打った本人が池に落ちたら 1 打足して、打つ前の位置へ</summary>
        [Test]
        public void World_WaterAddsPenaltyStroke()
        {
            var hole = Flat(areas: new[] { GolfArea.Water(MiniGameGeometry.Rect(2f, -2f, 5f, 2f)) });
            var world = new GolfHoleWorld(hole, GolfTurnOrder.SeatOrder(1));
            var start = world.Balls[0].Position;

            ShootAndSettle(world, 0.5f, 0f);

            Assert.That(world.Strokes(0), Is.EqualTo(2));
            Assert.That(world.Balls[0].Position, Is.EqualTo(start));
            Assert.That(world.Phase, Is.EqualTo(GolfHolePhase.Aiming));
        }

        /// <summary>弾かれて池に落ちた人は戻すだけ（自分で打っていないので打数は増えない）</summary>
        [Test]
        public void World_KnockedIntoWaterHasNoPenalty()
        {
            var hole = Flat(areas: new[] { GolfArea.Water(MiniGameGeometry.Rect(3f, -3f, 6f, 3f)) });
            var world = new GolfHoleWorld(hole, GolfTurnOrder.SeatOrder(2));
            world.Balls[0].Place(Vec2.Zero);
            world.Balls[1].Place(new Vec2(1f, 0f));

            world.Shoot(0, 0f, PowerFor(5f));
            Settle(world);

            // 戻した所が打った球のすぐ横なので、重ならないよう少しだけ押し出されることがある
            Assert.That(Vec2.Distance(world.Balls[1].Position, new Vec2(1f, 0f)), Is.LessThan(0.05f));
            Assert.That(world.Strokes(1), Is.Zero);
            Assert.That(world.Strokes(0), Is.EqualTo(1));
        }

        /// <summary>上限（パー＋4）まで打っても入らなければ「上限＋1」打で終わり</summary>
        [Test]
        public void World_GivesUpAtStrokeLimit()
        {
            var hole = Flat();
            var world = new GolfHoleWorld(hole, GolfTurnOrder.SeatOrder(1));
            var gaveUp = 0;
            world.Happened += e => gaveUp += e.Kind == GolfEventKind.GaveUp ? 1 : 0;

            for (var i = 0; i < hole.StrokeLimit; i++)
            {
                Assert.That(world.Phase, Is.EqualTo(GolfHolePhase.Aiming));
                ShootAndSettle(world, 0f, 0f);
            }

            Assert.That(world.Phase, Is.EqualTo(GolfHolePhase.Finished));
            Assert.That(world.Strokes(0), Is.EqualTo(hole.StrokeLimit + 1));
            Assert.That(world.Balls[0].Active, Is.False);
            Assert.That(gaveUp, Is.EqualTo(1));
        }

        /// <summary>持ち時間が過ぎたら、いまの向き・ゲージ 50% で自動で打つ</summary>
        [Test]
        public void World_AutoShotWhenTimeRunsOut()
        {
            var world = new GolfHoleWorld(Flat(), GolfTurnOrder.SeatOrder(1));
            world.SetAim(MathF.PI);
            Vec2 shotFrom = default;
            var shotPower = -1f;
            world.Happened += e =>
            {
                if (e.Kind == GolfEventKind.Shot)
                {
                    shotFrom = e.Position;
                    shotPower = e.Strength;
                }
            };

            world.Tick(GolfRules.ShotSeconds - 0.5f);
            Assert.That(world.Phase, Is.EqualTo(GolfHolePhase.Aiming));
            world.Tick(1f);

            Assert.That(world.Phase, Is.EqualTo(GolfHolePhase.Rolling));
            Assert.That(world.Strokes(0), Is.EqualTo(1));
            Assert.That(shotPower, Is.EqualTo(GolfRules.AutoShotPower));
            Settle(world);
            Assert.That(world.Balls[0].Position.X, Is.LessThan(shotFrom.X));
        }

        [Test]
        public void World_RejectsBadTurnOrder()
        {
            Assert.Throws<ArgumentException>(() => new GolfHoleWorld(Flat(), new[] { 0, 0 }));
            Assert.Throws<ArgumentException>(() => new GolfHoleWorld(Flat(), new[] { 0, 2 }));
            Assert.Throws<ArgumentException>(() => new GolfHoleWorld(Flat(), new int[0]));
            Assert.Throws<ArgumentException>(() => new GolfHoleWorld(Flat(), GolfTurnOrder.SeatOrder(GolfRules.MaxPlayers + 1)));
        }

        [Test]
        public void TurnOrder_FewerStrokesFirstThenSeat()
        {
            Assert.That(GolfTurnOrder.SeatOrder(3), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(GolfTurnOrder.ByTotals(new[] { 5, 3, 5, 2 }), Is.EqualTo(new[] { 3, 1, 0, 2 }));
        }

        // ---- ガイド線 ----

        [Test]
        public void Guide_StopsAtWallAndShowsBounce()
        {
            var hole = Flat(walls: new[] { new GolfWall(new Vec2(5f, -20f), new Vec2(5f, 20f)) });
            var balls = new[] { new GolfBall(0, Vec2.Zero) };

            var straight = GolfGuide.Compute(hole, balls, 0, 0f);
            Assert.That(straight.Stop, Is.EqualTo(GolfGuideStop.Wall));
            Assert.That(straight.End.X, Is.EqualTo(5f - GolfBalance.BallRadius).Within(0.01f));
            Assert.That(straight.BounceEnd.X, Is.LessThan(straight.End.X));

            var angled = GolfGuide.Compute(hole, balls, 0, MathF.PI / 4f);
            var bounce = (angled.BounceEnd - angled.End).Normalized;
            Assert.That(bounce.X, Is.EqualTo(-MathF.Sqrt(0.5f)).Within(0.01f));
            Assert.That(bounce.Y, Is.EqualTo(MathF.Sqrt(0.5f)).Within(0.01f));
        }

        [Test]
        public void Guide_OpenGroundHasMaxLength()
        {
            var line = GolfGuide.Compute(Flat(), new[] { new GolfBall(0, Vec2.Zero) }, 0, 0f);

            Assert.That(line.Stop, Is.EqualTo(GolfGuideStop.None));
            Assert.That(Vec2.Distance(line.Start, line.End), Is.EqualTo(GolfBalance.GuideMaxLength).Within(1e-3f));
        }

        [Test]
        public void Guide_StopsAtOtherBall()
        {
            var balls = new[] { new GolfBall(0, Vec2.Zero), new GolfBall(1, new Vec2(3f, 0f)) };

            var line = GolfGuide.Compute(Flat(), balls, 0, 0f);

            Assert.That(line.Stop, Is.EqualTo(GolfGuideStop.Ball));
            Assert.That(line.End.X, Is.EqualTo(3f - GolfBalance.BallRadius * 2f).Within(0.01f));
        }

        /// <summary>壁にくっついて止まった球から、壁と反対へ打つときは、すぐ後ろの壁で止めない</summary>
        [Test]
        public void Guide_IgnoresWallBehind()
        {
            var hole = Flat(walls: new[] { new GolfWall(new Vec2(0f, -20f), new Vec2(0f, 20f)) });
            var balls = new[] { new GolfBall(0, new Vec2(GolfBalance.BallRadius, 0f)) };

            var line = GolfGuide.Compute(hole, balls, 0, 0f);

            Assert.That(line.Stop, Is.EqualTo(GolfGuideStop.None));
        }

        // ---- 形・試しのホール ----

        [Test]
        public void Geometry_ContainsConcaveShape()
        {
            var shape = LShape();

            Assert.That(MiniGameGeometry.Contains(shape, new Vec2(1f, 1f)), Is.True);
            Assert.That(MiniGameGeometry.Contains(shape, new Vec2(3f, 3f)), Is.False);
            Assert.That(MiniGameGeometry.Contains(shape, new Vec2(-1f, 1f)), Is.False);
        }

        /// <summary>へこんだ形も、向きが逆でも、頂点の数 − 2 枚の三角形に分かれ、面積が合う</summary>
        [TestCase(false)]
        [TestCase(true)]
        public void Geometry_TriangulatesConcaveShape(bool clockwise)
        {
            var shape = LShape();
            if (clockwise)
            {
                Array.Reverse(shape);
            }

            var indices = MiniGameGeometry.Triangulate(shape);

            Assert.That(indices.Count, Is.EqualTo((shape.Length - 2) * 3));
            var area = 0f;
            for (var i = 0; i < indices.Count; i += 3)
            {
                var triangle = new[] { shape[indices[i]], shape[indices[i + 1]], shape[indices[i + 2]] };
                var signed = MiniGameGeometry.SignedArea(triangle);
                Assert.That(signed, Is.GreaterThan(0f), "左回り");
                area += signed;
            }

            Assert.That(area, Is.EqualTo(Math.Abs(MiniGameGeometry.SignedArea(shape))).Within(1e-3f));
        }

        [Test]
        public void TestHole_TeeAndCupAreOnSafeGround()
        {
            var hole = GolfTestHoles.TestHole;

            foreach (var point in new[] { hole.Tee, hole.Cup })
            {
                Assert.That(hole.OnGround(point), Is.True, point.ToString());
                Assert.That(hole.Areas.Any(a => a.IsHazard && a.Contains(point)), Is.False, point.ToString());
            }

            for (var player = 0; player < GolfRules.MaxPlayers; player++)
            {
                Assert.That(hole.OnGround(GolfHoleWorld.TeePosition(hole, player, GolfRules.MaxPlayers)), Is.True);
            }
        }

        /// <summary>試しのホールで1打打つと、どこかで止まって次の番になる（坂で転がり続けない）</summary>
        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(1f)]
        public void TestHole_ShotsSettle(float power)
        {
            var world = new GolfHoleWorld(GolfTestHoles.TestHole, GolfTurnOrder.SeatOrder(2));

            foreach (var angle in new[] { 0f, 1f, 1.57f, 2f, 3f })
            {
                if (world.Phase != GolfHolePhase.Aiming)
                {
                    break;
                }

                world.Shoot(world.CurrentPlayer, angle, power);
                Settle(world);
                Assert.That(world.Phase, Is.Not.EqualTo(GolfHolePhase.Rolling));
            }
        }

        // ---- 道具 ----

        private static GolfHoleDef Flat(Vec2? cup = null, GolfWall[] walls = null, GolfArea[] areas = null)
        {
            return new GolfHoleDef("test", 3, Vec2.Zero, cup ?? FarCup, MiniGameGeometry.Rect(-50f, -50f, 50f, 50f), walls ?? new GolfWall[0], areas ?? new GolfArea[0]);
        }

        private static Vec2[] LShape()
        {
            return new[] { new Vec2(0f, 0f), new Vec2(4f, 0f), new Vec2(4f, 2f), new Vec2(2f, 2f), new Vec2(2f, 4f), new Vec2(0f, 4f) };
        }

        /// <summary>全部の球が止まるまで進める</summary>
        private static List<GolfEvent> Run(GolfHoleDef hole, params GolfBall[] balls)
        {
            var sim = new GolfBallSim(hole);
            var events = new List<GolfEvent>();
            for (var i = 0; i < MaxSteps; i++)
            {
                sim.Step(balls, events);
                if (sim.AllStopped(balls))
                {
                    return events;
                }
            }

            Assert.Fail("球が止まらない");
            return events;
        }

        /// <summary>打ち出しの速さが speed になるゲージの強さ</summary>
        private static float PowerFor(float speed)
        {
            return (speed - GolfBalance.MinShotSpeed) / (GolfBalance.MaxShotSpeed - GolfBalance.MinShotSpeed);
        }

        private static void ShootAndSettle(GolfHoleWorld world, float power, float? angle = null)
        {
            Assert.That(world.Shoot(world.CurrentPlayer, angle ?? world.AimAngle, power), Is.True);
            Settle(world);
        }

        private static void Settle(GolfHoleWorld world)
        {
            for (var i = 0; i < MaxSteps && world.Phase == GolfHolePhase.Rolling; i++)
            {
                world.Tick(0.1f);
            }
        }
    }
}
