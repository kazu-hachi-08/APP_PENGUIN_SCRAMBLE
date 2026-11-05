using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PenmonScramble.Core;
using PenmonScramble.Golf.Core;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Tests
{
    /// <summary>ゴルフの特殊ショット・1コースのラウンド・報酬・コース1（Docs/MINIGAME_GOLF_PLAN.md フェーズ 4）</summary>
    public class GolfRoundTests
    {
        private const int MaxSteps = 20000;
        private static readonly Vec2 FarCup = new Vec2(40f, 40f);

        // ---- 特殊ショット ----

        [Test]
        public void Special_KindFollowsElement()
        {
            Assert.That(GolfSpecial.KindOf(Element.Ice), Is.EqualTo(GolfSpecialKind.IceRoad));
            Assert.That(GolfSpecial.KindOf(Element.Water), Is.EqualTo(GolfSpecialKind.SkipBounce));
            Assert.That(GolfSpecial.KindOf(Element.Wind), Is.EqualTo(GolfSpecialKind.WindCurve));
            Assert.That(GolfSpecial.KindOf(Element.Thunder), Is.EqualTo(GolfSpecialKind.ThunderBoost));
        }

        [Test]
        public void Special_MoreStarsAreStronger()
        {
            for (var stars = 1; stars < GolfSpecialBalance.MaxStars; stars++)
            {
                Assert.That(GolfSpecialBalance.IceLength(stars + 1), Is.GreaterThan(GolfSpecialBalance.IceLength(stars)));
                Assert.That(GolfSpecialBalance.SkipBounces(stars + 1), Is.GreaterThan(GolfSpecialBalance.SkipBounces(stars)));
                Assert.That(GolfSpecialBalance.Curvature(stars + 1), Is.GreaterThan(GolfSpecialBalance.Curvature(stars)));
                Assert.That(GolfSpecialBalance.BoostSpeed(stars + 1), Is.GreaterThan(GolfSpecialBalance.BoostSpeed(stars)));
                Assert.That(GolfSpecialBalance.BoostMaxTurn(stars + 1), Is.GreaterThan(GolfSpecialBalance.BoostMaxTurn(stars)));
            }
        }

        /// <summary>水切りの1回目の跳びは、低い壁を越えて高い壁は越えない高さ</summary>
        [Test]
        public void Special_SkipHeightBetweenWalls()
        {
            var peak = GolfSpecialBalance.SkipLaunch * GolfSpecialBalance.SkipLaunch / (2f * GolfBalance.Gravity);
            Assert.That(peak, Is.GreaterThan(GolfBalance.LowWallHeight + GolfBalance.BallRadius));
            Assert.That(peak, Is.LessThan(GolfBalance.TallWallHeight));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Ice_CrossesWater(bool iced)
        {
            var hole = Flat(areas: new[] { GolfArea.Water(MiniGameGeometry.Rect(2f, -2f, 4f, 2f)) });
            var world = Solo(hole);

            world.Shoot(0, 0f, PowerForDistance(8f), iced ? new GolfSpecial(GolfSpecialKind.IceRoad, 3) : GolfSpecial.None);
            var events = Settle(world);

            Assert.That(events.Any(e => e.Kind == GolfEventKind.Fell), Is.EqualTo(!iced));
            Assert.That(world.Balls[0].Position.X, iced ? Is.GreaterThan(4f) : Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void Ice_RollsFarther()
        {
            var plain = Solo(Flat());
            plain.Shoot(0, 0f, 0.4f);
            Settle(plain);
            var iced = Solo(Flat());
            iced.Shoot(0, 0f, 0.4f, new GolfSpecial(GolfSpecialKind.IceRoad, 1));
            Settle(iced);

            Assert.That(iced.Balls[0].Position.X, Is.GreaterThan(plain.Balls[0].Position.X + 1f));
        }

        /// <summary>凍った道の上で池の上に止まると、氷が消えて落ちる（打った本人なので 1 打足す）</summary>
        [Test]
        public void Ice_StoppingOnWaterFallsAfterShot()
        {
            var hole = Flat(areas: new[] { GolfArea.Water(MiniGameGeometry.Rect(1f, -2f, 30f, 2f)) });
            var world = Solo(hole);

            world.Shoot(0, 0f, PowerForDistance(3f), new GolfSpecial(GolfSpecialKind.IceRoad, 3));
            var events = Settle(world);

            Assert.That(events.Any(e => e.Kind == GolfEventKind.Fell), Is.True);
            Assert.That(world.Balls[0].Position, Is.EqualTo(Vec2.Zero));
            Assert.That(world.Strokes(0), Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Skip_ClearsLowWall(bool skip)
        {
            var hole = Flat(walls: new[] { GolfWall.Low(new Vec2(1.2f, -5f), new Vec2(1.2f, 5f)) });
            var world = Solo(hole);

            world.Shoot(0, 0f, 0.6f, skip ? new GolfSpecial(GolfSpecialKind.SkipBounce, 1) : GolfSpecial.None);
            Settle(world);

            Assert.That(world.Balls[0].Position.X > 1.2f, Is.EqualTo(skip));
        }

        [Test]
        public void Skip_BouncesStarsTimesAndSkimsWater()
        {
            var hole = Flat(areas: new[] { GolfArea.Water(MiniGameGeometry.Rect(1.5f, -3f, 8f, 3f)) });
            var world = Solo(hole);

            world.Shoot(0, 0f, 1f, new GolfSpecial(GolfSpecialKind.SkipBounce, 3));
            var events = Settle(world);

            Assert.That(events.Count(e => e.Kind == GolfEventKind.Skip), Is.EqualTo(GolfSpecialBalance.SkipBounces(3)));
            Assert.That(events.Any(e => e.Kind == GolfEventKind.Skip && e.Strength > 0f), Is.True, "池の上で跳ねる");
            Assert.That(world.Balls[0].Position.X, Is.GreaterThan(8f));
        }

        /// <summary>右（+1）は時計回りに曲がる＝ +X へ打つと −Y 側へそれる。左は反対</summary>
        [TestCase(1)]
        [TestCase(-1)]
        public void Wind_CurvesToChosenSide(int curve)
        {
            var world = Solo(Flat());

            world.Shoot(0, 0f, 0.6f, new GolfSpecial(GolfSpecialKind.WindCurve, 2, curve));
            Settle(world);

            Assert.That(Math.Sign(world.Balls[0].Position.Y), Is.EqualTo(-curve));
            Assert.That(Math.Abs(world.Balls[0].Position.Y), Is.GreaterThan(1f));
        }

        /// <summary>風のガイド線は、球が実際に通る道筋に沿って曲がる（強さによらない）</summary>
        [Test]
        public void Wind_GuideFollowsBallPath()
        {
            var hole = Flat(walls: new[] { new GolfWall(new Vec2(6f, -20f), new Vec2(6f, 20f)) });
            var special = new GolfSpecial(GolfSpecialKind.WindCurve, 1, 1);
            var straight = GolfGuide.Compute(hole, new[] { new GolfBall(0, Vec2.Zero) }, 0, 0f);
            var curved = GolfGuide.Compute(hole, new[] { new GolfBall(0, Vec2.Zero) }, 0, 0f, special);

            Assert.That(curved.Path.Count, Is.GreaterThan(2));
            Assert.That(curved.End.Y, Is.LessThan(straight.End.Y - 0.5f));

            foreach (var power in new[] { 0.7f, 1f })
            {
                var ball = new GolfBall(0, Vec2.Zero);
                ball.Launch(GolfHoleWorld.Direction(0f) * GolfHoleWorld.ShotSpeed(power));
                ball.ApplySpecial(special);
                var sim = new GolfBallSim(hole);
                var events = new List<GolfEvent>();
                for (var i = 0; i < MaxSteps && !events.Any(e => e.Kind == GolfEventKind.WallHit); i++)
                {
                    sim.Step(new[] { ball }, events);
                }

                var hit = events.First(e => e.Kind == GolfEventKind.WallHit);
                Assert.That(hit.Position.Y, Is.EqualTo(curved.End.Y).Within(0.15f), $"power {power}");
            }
        }

        [Test]
        public void Thunder_BoostSpeedsUpAndTurnsWithinLimit()
        {
            var world = Solo(Flat());
            world.Shoot(0, 0f, 0.3f, new GolfSpecial(GolfSpecialKind.ThunderBoost, 1));
            world.Tick(0.2f);
            var before = world.Balls[0].Velocity.Length;

            Assert.That(world.CanBoost(0), Is.True);
            Assert.That(world.Boost(0, MathF.PI), Is.True);

            var velocity = world.Balls[0].Velocity;
            Assert.That(velocity.Length, Is.EqualTo(before + GolfSpecialBalance.BoostSpeed(1)).Within(0.01f));
            Assert.That(MathF.Atan2(velocity.Y, velocity.X), Is.EqualTo(GolfSpecialBalance.BoostMaxTurn(1)).Within(0.01f));
            Assert.That(world.Boost(0, 0f), Is.False, "1打に1回だけ");
        }

        [Test]
        public void Thunder_NoBoostWithoutSpecial()
        {
            var world = Solo(Flat());
            world.Shoot(0, 0f, 0.3f);
            world.Tick(0.2f);

            Assert.That(world.CanBoost(0), Is.False);
            Assert.That(world.Boost(0, 0f), Is.False);
        }

        // ---- ラウンド ----

        [Test]
        public void Round_SpecialOncePerMemberPerCourse()
        {
            var round = TwoHoleRound(1);
            var team = round.Teams[0];

            Assert.That(round.Shoot(0, new GolfShot(0, 0f, 0.3f, special: true)), Is.True);
            Assert.That(team.IsSpecialUsed(0), Is.True);
            SettleRound(round);

            var events = new List<GolfEvent>();
            round.Happened += events.Add;
            round.Shoot(0, new GolfShot(0, 0f, 0.3f, special: true));
            Assert.That(events.Any(e => e.Kind == GolfEventKind.Special), Is.False, "使った子はもう特殊にならない");
        }

        [Test]
        public void Round_RestartHoleGivesBackSpecialsUsedOnIt()
        {
            var round = TwoHoleRound(1);
            round.Shoot(0, new GolfShot(1, 0f, 0.3f, special: true));
            SettleRound(round);

            round.RestartHole();

            Assert.That(round.Teams[0].IsSpecialUsed(1), Is.False);
            Assert.That(round.World.Strokes(0), Is.EqualTo(0));
        }

        [Test]
        public void Round_RecordsHolesAndMovesOn()
        {
            var round = TwoHoleRound(2);
            var completed = new List<int>();
            round.HoleCompleted += completed.Add;

            PlayOutHole(round);

            Assert.That(completed, Is.EqualTo(new[] { 0 }));
            Assert.That(round.IsRecorded(0), Is.True);
            Assert.That(round.Total(0), Is.EqualTo(round.Strokes(0, 0)));
            Assert.That(round.NextHole(), Is.True);
            Assert.That(round.HoleIndex, Is.EqualTo(1));
            PlayOutHole(round);
            Assert.That(round.IsFinished, Is.True);
            Assert.That(round.NextHole(), Is.False);
        }

        /// <summary>2ホール目は前のホールの打数が少ない人から</summary>
        [Test]
        public void Round_SecondHoleOrderByFewerStrokes()
        {
            // カップがティーのすぐ先にあるコース。席 1 だけがカップへ打ち、席 0 は反対へ弱く打ち続けて上限で終わる
            var cup = new Vec2(1.5f, 0f);
            var hole = new GolfHoleDef("near", 2, Vec2.Zero, cup, MiniGameGeometry.Rect(-50f, -50f, 50f, 50f), new GolfWall[0], new GolfArea[0]);
            var teams = Enumerable.Range(0, 2).Select(_ => new GolfTeam(new[] { 1, 3, 5 })).ToList();
            var round = new GolfRound(new GolfCourseDef(1, "test", 1, new[] { hole, hole }), teams);

            for (var i = 0; i < 100 && round.World.Phase != GolfHolePhase.Finished; i++)
            {
                var player = round.World.CurrentPlayer;
                var distance = Vec2.Distance(round.World.Balls[player].Position, cup);
                var shot = player == 1
                    ? new GolfShot(0, round.World.AngleToCup(player), PowerForDistance(distance))
                    : new GolfShot(0, MathF.PI, 0f);
                round.Shoot(player, shot);
                SettleRound(round);
            }

            Assert.That(round.Strokes(0, 1), Is.LessThan(round.Strokes(0, 0)));
            round.NextHole();

            Assert.That(round.World.CurrentPlayer, Is.EqualTo(1));
        }

        [Test]
        public void Round_RestartClearsScorecard()
        {
            var round = TwoHoleRound(1);
            round.Shoot(0, new GolfShot(2, 0f, 0.3f, special: true));
            PlayOutHole(round);

            round.Restart();

            Assert.That(round.HoleIndex, Is.EqualTo(0));
            Assert.That(round.IsRecorded(0), Is.False);
            Assert.That(round.Total(0), Is.EqualTo(0));
            Assert.That(round.Teams[0].IsSpecialUsed(2), Is.False);
        }

        // ---- 報酬・コース ----

        [Test]
        public void Reward_CountsUnderParHoleInOnesAndFirstPlay()
        {
            var reward = GolfReward.Compute(7, 9, 1, firstPlay: true);

            Assert.That(reward.UnderParStrokes, Is.EqualTo(2));
            Assert.That(reward.Total, Is.EqualTo(GolfReward.BaseCoins + 2 * GolfReward.CoinsPerStrokeUnderPar + GolfReward.CoinsPerHoleInOne + GolfReward.FirstPlayBonus));
        }

        [Test]
        public void Reward_OverParStillGetsBase()
        {
            var reward = GolfReward.Compute(20, 9, 0, firstPlay: false);

            Assert.That(reward.Total, Is.EqualTo(GolfReward.BaseCoins));
        }

        /// <summary>1コースの報酬は、いつも本編の1ステージのクリアより控えめ（初回ボーナスを除く・ホールインワンが無いとき）</summary>
        [Test]
        public void Reward_BelowStageClearWithoutHoleInOne()
        {
            var course = GolfCourseTable.Get(1);
            var best = GolfReward.Compute(course.Holes.Count * 2, course.TotalPar, 0, firstPlay: false);

            Assert.That(best.Total, Is.LessThanOrEqualTo(Balance.ClearCoins));
        }

        [Test]
        public void MiniGameReward_AddsCoinsAndRecords()
        {
            var data = new SaveData { coins = 10 };
            var game = MiniGameTable.Get(MiniGameTable.GolfId);

            var result = MiniGameReward.Apply(data, game, 1, 9, 1, 140);

            Assert.That(data.coins, Is.EqualTo(150));
            Assert.That(result.FirstPlay, Is.True);
            Assert.That(new MiniGameRecords(data).TryGetBest(game.Id, 1, out var best) && best == 9, Is.True);
        }

        /// <summary>表の5コースは全部作ってあり、どれも3ホール・パー 2〜4</summary>
        [Test]
        public void CourseTable_AllCoursesAreReady()
        {
            foreach (var row in MiniGameTable.Get(MiniGameTable.GolfId).Courses)
            {
                var course = GolfCourseTable.Get(row.No);

                Assert.That(course, Is.Not.Null, row.Name);
                Assert.That(course.World, Is.EqualTo(row.World));
                Assert.That(course.Holes.Count, Is.EqualTo(GolfRules.HolesPerCourse), row.Name);
                Assert.That(course.Holes.Select(h => h.Par), Is.All.InRange(2, 4), row.Name);
                Assert.That(new GolfCourseGuide().IsReady(row.No), Is.True);
                Assert.That(new GolfCourseGuide().Layout(row.No), Does.Contain($"合計 {course.TotalPar}"));
            }

            Assert.That(GolfCourseTable.Get(99), Is.Null);
        }

        /// <summary>どのホールも、4人分のティーとカップが池・壁・動く物・ワープに掛かっていない</summary>
        [TestCaseSource(nameof(AllHoles))]
        public void Holes_TeesAndCupsAreOnSafeGround(GolfHoleDef hole)
        {
            var sim = new GolfBallSim(hole);
            Assert.That(sim.IsHazard(hole.Cup), Is.False, hole.Name);
            for (var player = 0; player < GolfRules.MaxPlayers; player++)
            {
                var tee = GolfHoleWorld.TeePosition(hole, player, GolfRules.MaxPlayers);
                Assert.That(sim.IsHazard(tee), Is.False, $"{hole.Name} {player}");
                Assert.That(hole.Walls.All(w => Vec2.Distance(MiniGameGeometry.ClosestOnSegment(w.A, w.B, tee), tee) > GolfBalance.BallRadius), Is.True, $"{hole.Name} {player}");
                Assert.That(hole.Movers.All(m => !m.TryPushOutOfSweep(tee, out _)), Is.True, $"{hole.Name} {player}");
                Assert.That(hole.Warps.All(w => !w.Catches(tee)), Is.True, $"{hole.Name} {player}");
            }
        }

        /// <summary>どのホールも、どう打っても止まって次の番になる</summary>
        [TestCaseSource(nameof(AllHoles))]
        public void Holes_ShotsSettle(GolfHoleDef hole)
        {
            {
                var world = new GolfHoleWorld(hole, GolfTurnOrder.SeatOrder(1));
                foreach (var angle in new[] { 0f, 0.8f, 1.57f, 2.4f, 4f })
                {
                    if (world.Phase != GolfHolePhase.Aiming)
                    {
                        break;
                    }

                    world.Shoot(0, angle, 1f);
                    Settle(world);
                    Assert.That(world.Phase, Is.Not.EqualTo(GolfHolePhase.Rolling), hole.Name);
                }
            }
        }

        // ---- 道具 ----

        private static IEnumerable<GolfHoleDef> AllHoles()
        {
            foreach (var row in MiniGameTable.Get(MiniGameTable.GolfId).Courses)
            {
                foreach (var hole in GolfCourseTable.Get(row.No)?.Holes ?? new GolfHoleDef[0])
                {
                    yield return hole;
                }
            }
        }

        private static GolfHoleDef Flat(GolfWall[] walls = null, GolfArea[] areas = null)
        {
            return new GolfHoleDef("test", 3, Vec2.Zero, FarCup, MiniGameGeometry.Rect(-50f, -50f, 50f, 50f), walls ?? new GolfWall[0], areas ?? new GolfArea[0]);
        }

        private static GolfHoleWorld Solo(GolfHoleDef hole) => new GolfHoleWorld(hole, GolfTurnOrder.SeatOrder(1));

        /// <summary>2ホールの平らなコース（カップは遠くて入らない。上限まで打って終わる）</summary>
        private static GolfRound TwoHoleRound(int players)
        {
            var hole = new GolfHoleDef("flat", 2, Vec2.Zero, FarCup, MiniGameGeometry.Rect(-50f, -50f, 50f, 50f), new GolfWall[0], new GolfArea[0]);
            var course = new GolfCourseDef(1, "test", 1, new[] { hole, hole });
            var teams = Enumerable.Range(0, players).Select(_ => new GolfTeam(new[] { 1, 3, 5 })).ToList();
            return new GolfRound(course, teams);
        }

        private static void PlayOutHole(GolfRound round)
        {
            for (var i = 0; i < 100 && round.World.Phase != GolfHolePhase.Finished; i++)
            {
                var player = round.World.CurrentPlayer;
                round.Shoot(player, new GolfShot(0, 0f, 0.2f));
                SettleRound(round);
            }

            Assert.That(round.World.Phase, Is.EqualTo(GolfHolePhase.Finished));
        }

        private static void SettleRound(GolfRound round)
        {
            for (var i = 0; i < MaxSteps && round.World.Phase == GolfHolePhase.Rolling; i++)
            {
                round.Tick(0.1f);
            }
        }

        private static List<GolfEvent> Settle(GolfHoleWorld world)
        {
            var events = new List<GolfEvent>();
            world.Happened += events.Add;
            for (var i = 0; i < MaxSteps && world.Phase == GolfHolePhase.Rolling; i++)
            {
                world.Tick(0.1f);
            }

            world.Happened -= events.Add;
            return events;
        }

        /// <summary>平らな地面でちょうど distance 転がる強さ</summary>
        private static float PowerForDistance(float distance)
        {
            var speed = MathF.Sqrt(2f * GolfBalance.RollDecel * distance);
            return (speed - GolfBalance.MinShotSpeed) / (GolfBalance.MaxShotSpeed - GolfBalance.MinShotSpeed);
        }
    }
}
