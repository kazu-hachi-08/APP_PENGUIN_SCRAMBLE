using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Tests
{
    /// <summary>野球の球場と仕掛け: 盤の時計・氷の床・水流・風・動く穴・間欠泉・5球場の盤</summary>
    public class BaseballGimmickTests
    {
        private const float Dt = BaseballRules.StepSeconds;
        private const int MaxSteps = 1000;

        /// <summary>穴の無い盤（仕掛けの効き方だけを見る）</summary>
        private static BaseballFieldDef Plain(BaseballGimmicks gimmicks, params BaseballHole[] holes)
        {
            var stopZones = new[]
            {
                new BaseballStopZone(7.5f, BaseballHitResult.GroundOut),
                new BaseballStopZone(12.2f, BaseballHitResult.Single),
                new BaseballStopZone(14f, BaseballHitResult.Double),
            };
            return new BaseballFieldDef("仕掛けの試し", 14f, 1f, 45f, holes, stopZones, 0.55f, -0.8f,
                new Vec2(0f, 5.5f), new Vec2(-1.15f, 0f), 4.6f, new Vec2(0f, 7f), gimmicks);
        }

        private static BaseballHit Grounder(Vec2 direction, float speed) => new BaseballHit(direction.Normalized, speed, 0f, 0.5f, false, false);

        private static BaseballHit Fly(float speed, float up) => new BaseballHit(new Vec2(0f, 1f), speed, up, 1f, true, false);

        /// <summary>打球を結果が出るまで進め、起きたことを返す</summary>
        private static List<BaseballEvent> Run(BaseballBallSim sim, Vec2 from, BaseballHit hit, float clock)
        {
            var events = new List<BaseballEvent>();
            sim.Launch(from, hit, BaseballSpecial.None, clock);
            for (var i = 0; i < MaxSteps && sim.IsActive; i++)
            {
                sim.Step(Dt, events);
            }

            Assert.That(sim.IsActive, Is.False, "打球の結果が出ない");
            return events;
        }

        // ---- 盤の時計 ----

        [Test]
        public void Clock_DependsOnlyOnThrowNumber()
        {
            Assert.That(BaseballClock.AtThrow(1), Is.EqualTo(BaseballClock.SecondsPerThrow));
            Assert.That(BaseballClock.AtThrow(2), Is.Not.EqualTo(BaseballClock.AtThrow(1)));
            Assert.That(BaseballClock.AtThrow(0), Is.EqualTo(0f));
        }

        [Test]
        public void Clock_BoardStartsAtThrowAndAdvancesWhileBallMoves()
        {
            var board = new BaseballBoard(BaseballFieldTable.Test());
            var events = new List<BaseballEvent>();
            board.Throw(BaseballPitchType.Changeup, BaseballPitchMagic.None, BaseballSpecial.None, events, 5f);
            Assert.That(board.Clock, Is.EqualTo(5f));
            for (var i = 0; i < 10; i++)
            {
                board.Step(BaseballInput.None, events);
            }

            Assert.That(board.Clock, Is.EqualTo(5f + 10 * Dt).Within(1e-4f));
        }

        [Test]
        public void Clock_SessionUsesThrowNumber()
        {
            var session = new BaseballPitchSession(BaseballFieldTable.StormCliff());
            session.Begin(new BaseballThrow(3, BaseballPitchType.Curve, BaseballPitchMagic.None, BaseballSpecial.None, null), new List<BaseballEvent>());
            Assert.That(session.Board.Clock, Is.EqualTo(BaseballClock.AtThrow(3)));
        }

        // ---- 氷の床 ----

        [Test]
        public void Ice_BallRollsFartherOnIce()
        {
            var ice = new BaseballGimmicks(ice: new[] { new BaseballArcBand(5f, 14f, 45f) });
            var plain = new BaseballBallSim(Plain(BaseballGimmicks.None));
            var icy = new BaseballBallSim(Plain(ice));
            Run(plain, new Vec2(0f, 0.3f), Grounder(new Vec2(0f, 1f), 10f), 0f);
            Run(icy, new Vec2(0f, 0.3f), Grounder(new Vec2(0f, 1f), 10f), 0f);
            Assert.That(icy.Position.Length, Is.GreaterThan(plain.Position.Length + 1f));
        }

        // ---- 水流 ----

        [Test]
        public void Current_PushesRollingBallSidewaysAndFlipsWithClock()
        {
            var cycle = new MiniGameCycle(5f, 5f);
            var current = new BaseballCurrent(new BaseballArcBand(5f, 8f, 40f), 14f, 3.2f, cycle);
            var field = Plain(new BaseballGimmicks(currents: new[] { current }));
            Assert.That(current.SideAt(1f), Is.EqualTo(1f));
            Assert.That(current.SideAt(6f), Is.EqualTo(-1f));

            var right = new BaseballBallSim(field);
            Run(right, new Vec2(0f, 0.3f), Grounder(new Vec2(0f, 1f), 9f), 0.5f);
            Assert.That(right.Position.X, Is.GreaterThan(0.3f), "1回り目は一塁側へ流れる");

            var left = new BaseballBallSim(field);
            Run(left, new Vec2(0f, 0.3f), Grounder(new Vec2(0f, 1f), 9f), 5.5f);
            Assert.That(left.Position.X, Is.LessThan(-0.3f), "2回り目は三塁側へ流れる");
        }

        [Test]
        public void Current_DoesNotPushFlyingBall()
        {
            var current = new BaseballCurrent(new BaseballArcBand(2f, 10f, 40f), 14f, 3.2f, new MiniGameCycle(5f, 5f));
            var sim = new BaseballBallSim(Plain(new BaseballGimmicks(currents: new[] { current })));
            var events = Run(sim, Vec2.Zero, Fly(8f, 7f), 0.5f);
            var land = events.First(e => e.Kind == BaseballEventKind.Land);
            Assert.That(MathF.Abs(land.Position.X), Is.LessThan(0.01f));
        }

        // ---- 風 ----

        [Test]
        public void Wind_DriftsFlyingBallOnlyWhileBlowing()
        {
            var wind = new BaseballWind(3f, new MiniGameCycle(6f, 4f));
            var field = Plain(new BaseballGimmicks(wind: wind));
            Assert.That(wind.SideAt(1f), Is.EqualTo(1f));
            Assert.That(wind.SideAt(5f), Is.EqualTo(0f));
            Assert.That(wind.NextSideAt(5f), Is.EqualTo(-1f), "止んでいる間は、次に吹く向きが分かる");
            Assert.That(wind.SideAt(7f), Is.EqualTo(-1f));

            float LandX(float clock)
            {
                var sim = new BaseballBallSim(field);
                return Run(sim, Vec2.Zero, Fly(8f, 7f), clock).First(e => e.Kind == BaseballEventKind.Land).Position.X;
            }

            Assert.That(LandX(0f), Is.GreaterThan(0.5f));
            Assert.That(LandX(6.1f), Is.LessThan(-0.5f));
            Assert.That(MathF.Abs(LandX(4.1f)), Is.LessThan(0.3f), "止んでいる間（1.2秒ほど）はほとんど流れない");
        }

        [Test]
        public void Wind_DoesNotPushRollingBall()
        {
            var wind = new BaseballWind(3f, new MiniGameCycle(6f, 4f));
            var sim = new BaseballBallSim(Plain(new BaseballGimmicks(wind: wind)));
            Run(sim, new Vec2(0f, 0.3f), Grounder(new Vec2(0f, 1f), 8f), 0f);
            Assert.That(MathF.Abs(sim.Position.X), Is.LessThan(0.01f));
        }

        // ---- 動く穴 ----

        [Test]
        public void MovingHole_FollowsClockAndCatchesAtMovedPlace()
        {
            // 40 秒で1往復（10 秒でいちばん右。球が転がる間はほとんど動かない）
            var sway = new BaseballHoleSway(20f, 40f, 0f);
            var hole = new BaseballHole(BaseballHitResult.FlyOut, BaseballFieldDef.At(0f, 6f), 0.5f, sway);
            Assert.That(hole.CenterAt(0f).X, Is.EqualTo(hole.Center.X).Within(1e-4f));
            Assert.That(BaseballFieldDef.DegreesOf(hole.CenterAt(10f)), Is.EqualTo(20f).Within(0.01f));
            Assert.That(hole.CenterAt(10f).Length, Is.EqualTo(6f).Within(1e-3f), "ホームからの距離は変わらない");

            var field = Plain(BaseballGimmicks.None, hole);
            var empty = new BaseballBallSim(field);
            Run(empty, BaseballFieldDef.At(0f, 5.3f), Grounder(BaseballFieldDef.At(0f, 1f), 2f), 10f);
            Assert.That(empty.HoleIndex, Is.EqualTo(-1), "穴が動いて行った後の所には落ちない");

            var moved = new BaseballBallSim(field);
            Run(moved, BaseballFieldDef.At(20f, 5.3f), Grounder(BaseballFieldDef.At(20f, 1f), 2f), 10f);
            Assert.That(moved.HoleIndex, Is.EqualTo(0));
            Assert.That(moved.Result, Is.EqualTo(BaseballHitResult.FlyOut));
        }

        [Test]
        public void FixedHole_CenterNeverMoves()
        {
            var hole = new BaseballHole(BaseballHitResult.Single, new Vec2(1f, 5f), 0.4f);
            Assert.That(hole.Moves, Is.False);
            Assert.That(hole.CenterAt(123f), Is.EqualTo(hole.Center));
        }

        // ---- 間欠泉 ----

        [Test]
        public void Geyser_LaunchesRollingBallOnlyWhileErupting()
        {
            var geyser = new BaseballGeyser(new Vec2(0f, 5f), 0.55f, new MiniGameCycle(3f, 1.2f));
            var field = Plain(new BaseballGimmicks(geysers: new[] { geyser }));

            var erupting = new BaseballBallSim(field);
            var events = Run(erupting, new Vec2(0f, 3f), Grounder(new Vec2(0f, 1f), 6f), 0f);
            Assert.That(events.Count(e => e.Kind == BaseballEventKind.GeyserLaunch), Is.EqualTo(1), "1回噴く間に跳ね上げるのは1度だけ");
            Assert.That(events.Any(e => e.Kind == BaseballEventKind.Land), Is.True, "跳ね上がった球はまた盤に着く");

            var quiet = new BaseballBallSim(field);
            var calm = Run(quiet, new Vec2(0f, 3f), Grounder(new Vec2(0f, 1f), 6f), 1.5f);
            Assert.That(calm.Any(e => e.Kind == BaseballEventKind.GeyserLaunch), Is.False);
        }

        // ---- 5球場 ----

        [Test]
        public void Stadiums_FiveWithRisingStrengthAndMatchingTable()
        {
            var courses = MiniGameTable.Get(MiniGameTable.BaseballId).Courses;
            Assert.That(BaseballStadiumTable.Stadiums.Count, Is.EqualTo(5));
            foreach (var stadium in BaseballStadiumTable.Stadiums)
            {
                var course = courses.First(c => c.No == stadium.No);
                Assert.That(stadium.Name, Is.EqualTo(course.Name));
                Assert.That(stadium.World, Is.EqualTo(course.World));
                Assert.That(stadium.CpuStars, Is.EqualTo(stadium.No), "奥の球場ほど CPU が強い（★1〜★5）");
                var picked = stadium.PickWild(BaseballStadiumTable.CpuLineup, 7);
                Assert.That(picked.Distinct().Count(), Is.EqualTo(picked.Length));
                Assert.That(picked.All(s => stadium.WildSpecies.Contains(s)), Is.True);
            }
        }

        [Test]
        public void Stadiums_EachHasItsGimmick()
        {
            Assert.That(BaseballFieldTable.Beach().Gimmicks, Is.SameAs(BaseballGimmicks.None));
            Assert.That(BaseballFieldTable.Iceberg().Gimmicks.Ice.Count, Is.GreaterThan(0));
            Assert.That(BaseballFieldTable.DeepSea().Gimmicks.Currents.Count, Is.GreaterThan(0));
            var storm = BaseballFieldTable.StormCliff();
            Assert.That(storm.Gimmicks.Wind, Is.Not.Null);
            Assert.That(storm.Holes.Count(h => h.Moves), Is.GreaterThan(0));
            Assert.That(storm.Holes.Where(h => h.Moves).All(h => h.IsOut), Is.True, "動くのはアウトの穴");
            var volcano = BaseballFieldTable.VolcanoCove();
            Assert.That(volcano.Gimmicks.Geysers.Count, Is.GreaterThan(0));
            Assert.That(volcano.FenceRadius, Is.LessThan(BaseballFieldTable.Beach().FenceRadius), "近いフェンス");
            Assert.That(volcano.FenceHeight, Is.GreaterThan(BaseballFieldTable.Beach().FenceHeight), "高いフェンス");
        }

        /// <summary>どの球場の盤も、穴がフェアの中でフェンスにかからず、動く穴が動いても重ならない。仕掛けは穴と重ならない</summary>
        [Test]
        public void Stadiums_HolesAndGimmicksFitOnBoard()
        {
            foreach (var stadium in BaseballStadiumTable.Stadiums)
            {
                var field = stadium.CreateField();
                for (var clock = 0f; clock < 8f; clock += 0.2f)
                {
                    var centers = field.Holes.Select(h => h.CenterAt(clock)).ToArray();
                    for (var i = 0; i < field.Holes.Count; i++)
                    {
                        var hole = field.Holes[i];
                        Assert.That(field.IsFair(centers[i]), Is.True, $"{stadium.Name}: 穴 {i} がファウルの溝にある（{clock:0.0}秒）");
                        Assert.That(centers[i].Length + hole.Radius, Is.LessThan(field.FenceRadius), $"{stadium.Name}: 穴 {i} がフェンスにかかる");
                        for (var j = i + 1; j < field.Holes.Count; j++)
                        {
                            Assert.That(Vec2.Distance(centers[i], centers[j]), Is.GreaterThan(hole.Radius + field.Holes[j].Radius),
                                $"{stadium.Name}: 穴 {i} と {j} が重なる（{clock:0.0}秒）");
                        }
                    }
                }

                foreach (var geyser in field.Gimmicks.Geysers)
                {
                    Assert.That(field.IsFair(geyser.Center), Is.True, $"{stadium.Name}: 間欠泉がファウルの溝にある");
                    foreach (var hole in field.Holes)
                    {
                        Assert.That(Vec2.Distance(geyser.Center, hole.Center), Is.GreaterThan(geyser.Radius + hole.Radius), $"{stadium.Name}: 間欠泉が穴と重なる");
                    }
                }

                foreach (var current in field.Gimmicks.Currents)
                {
                    Assert.That(current.Band.HalfDegrees, Is.LessThan(field.FoulDegrees), $"{stadium.Name}: 水流がファウルラインまで届くと、流された球がみなファウルになる");
                }
            }
        }

        /// <summary>どの球場でも CPU 同士の試合が最後まで終わる（仕掛けで球が詰まって打ち切りにならない）</summary>
        [Test]
        public void Stadiums_CpuMatchesFinish()
        {
            foreach (var stadium in BaseballStadiumTable.Stadiums)
            {
                var field = stadium.CreateField();
                for (var seed = 0; seed < 3; seed++)
                {
                    var stats = BaseballMatchCheck.Play(BaseballBalance.AiSkill(stadium.CpuStars), BaseballBalance.AiSkill(stadium.CpuStars), seed, field);
                    Assert.That(stats.Pitches, Is.LessThan(1000), $"{stadium.Name}: 試合が終わらない");
                    Assert.That(stats.Innings, Is.GreaterThanOrEqualTo(2), $"{stadium.Name}: 試合が早すぎる");
                }
            }
        }

        /// <summary>同じ投げた番号・同じ入力なら、仕掛けのある盤でも同じ1球になる（オンラインでどの端末も同じ計算になる）</summary>
        [Test]
        public void Stadiums_SamePitchIsDeterministic()
        {
            foreach (var make in new Func<BaseballFieldDef>[] { BaseballFieldTable.DeepSea, BaseballFieldTable.StormCliff, BaseballFieldTable.VolcanoCove })
            {
                BaseballBoard Play(int swingStep)
                {
                    var session = new BaseballPitchSession(make());
                    var events = new List<BaseballEvent>();
                    session.Begin(new BaseballThrow(9, BaseballPitchType.Changeup, BaseballPitchMagic.None, BaseballSpecial.None, null), events);
                    for (var step = 0; step < MaxSteps && !session.IsDone; step++)
                    {
                        session.PitcherLog.Record(0f, false, false, 0f);
                        session.BatterLog.Record(0f, false, step == swingStep, 0f);
                        session.Advance(events);
                    }

                    Assert.That(session.IsDone, Is.True, "1球の結果が出ない");
                    return session.Board;
                }

                var contacts = 0;
                for (var swing = 20; swing < 60; swing++)
                {
                    var a = Play(swing);
                    var b = Play(swing);
                    Assert.That(b.Outcome.Kind, Is.EqualTo(a.Outcome.Kind));
                    Assert.That(b.Outcome.Hit, Is.EqualTo(a.Outcome.Hit));
                    Assert.That(b.Ball.Position, Is.EqualTo(a.Ball.Position));
                    contacts += a.LastHit.HasValue ? 1 : 0;
                }

                Assert.That(contacts, Is.GreaterThan(0), "当たる球が無いと仕掛けを通らない");
            }
        }
    }
}
