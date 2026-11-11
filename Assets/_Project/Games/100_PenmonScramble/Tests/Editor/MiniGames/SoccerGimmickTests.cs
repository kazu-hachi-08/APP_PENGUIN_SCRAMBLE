using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.Soccer.Core;

namespace PenmonScramble.Tests
{
    /// <summary>サッカーのスタジアム2〜5と仕掛け</summary>
    public class SoccerGimmickTests
    {
        private const float Dt = SoccerRules.StepSeconds;
        private const float Near = 1e-3f;

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

        private static void Throw(SoccerWorld world, Vec2 position, Vec2 velocity, float height = 0f, float verticalSpeed = 0f)
        {
            world.Ball.Place(position);
            world.Ball.Velocity = velocity;
            world.Ball.Height = height;
            world.Ball.VerticalSpeed = verticalSpeed;
        }

        private static SoccerPitchDef PitchOf(int stadium) => SoccerStadiumTable.Get(stadium).CreatePitch();

        /// <summary>子を置かないピッチ（ボールの動きだけを見る）</summary>
        private static SoccerWorld Empty(SoccerPitchDef pitch) => new SoccerWorld(pitch, 0, 0);

        /// <summary>1体だけの赤（キックオフの並びから好きな所へ動かして使う）</summary>
        private static SoccerWorld Single(SoccerPitchDef pitch, Vec2 at)
        {
            var world = new SoccerWorld(pitch, 1, 0);
            world.Players[0].Position = at;
            world.Ball.Place(new Vec2(0f, -7f));
            return world;
        }

        // ---- 定義表 ----

        [Test]
        public void Stadiums_AllFiveArePlayable_AndTheCpuGetsStrongerFurtherIn()
        {
            var soccer = MiniGameTable.Get(MiniGameTable.SoccerId);
            var guide = new SoccerCourseGuide();

            Assert.That(SoccerStadiumTable.Stadiums.Count, Is.EqualTo(soccer.Courses.Count));
            for (var i = 0; i < soccer.Courses.Count; i++)
            {
                var course = soccer.Courses[i];
                var stadium = SoccerStadiumTable.Get(course.No);
                Assert.That(stadium, Is.Not.Null, $"スタジアム {course.No}");
                Assert.That(guide.IsReady(course.No), Is.True);
                Assert.That(stadium.Name, Is.EqualTo(course.Name), "名前はミニゲームの表と同じ");
                Assert.That(stadium.World, Is.EqualTo(course.World), "景色と解放はストーリーのワールド");
                Assert.That(stadium.CpuStars, Is.EqualTo(i + 1), "奥ほど CPU が強い");
                Assert.That(stadium.WildSpecies.All(no => SpeciesTable.Get(no) != null), Is.True, "CPU の種類は表にある子だけ");
            }
        }

        [Test]
        public void Stadiums_EachHasItsOwnGimmick()
        {
            Assert.That(PitchOf(1).Grounds.Count + PitchOf(1).Flows.Count + PitchOf(1).Geysers.Count + PitchOf(1).Posts.Count, Is.Zero, "海岸は仕掛けなし");
            Assert.That(PitchOf(2).Grounds.Any(z => z.Ground == SoccerGround.Ice), Is.True);
            Assert.That(PitchOf(3).Flows.All(f => f.Kind == SoccerFlowKind.Current) && PitchOf(3).Flows.Count > 0, Is.True);
            Assert.That(PitchOf(4).Flows.Any(f => f.Kind == SoccerFlowKind.Wind), Is.True);
            Assert.That(PitchOf(4).Walls.Count(w => w.IsBumper), Is.GreaterThan(0));
            Assert.That(PitchOf(5).Geysers.Count, Is.GreaterThan(0));
        }

        /// <summary>キックオフの並びに柱・間欠泉がかからない（並んだ途端に押し出されたり跳ね上げられたりしない）</summary>
        [Test]
        public void Stadiums_KickoffSpotsAreClearOfPostsAndGeysers()
        {
            foreach (var stadium in SoccerStadiumTable.Stadiums)
            {
                var pitch = stadium.CreatePitch();
                for (var team = 0; team < SoccerRules.TeamCount; team++)
                {
                    for (var index = 0; index < SoccerRules.TeamCount * 2; index++)
                    {
                        foreach (var kicking in new[] { true, false })
                        {
                            var spot = pitch.KickoffSpot(team, index, kicking);
                            Assert.That(pitch.Posts.All(p => Vec2.Distance(p.Center, spot) > p.Radius + SoccerBalance.PlayerRadius), Is.True, $"{stadium.Name} {spot}");
                            Assert.That(pitch.Geysers.All(g => !g.Contains(spot)), Is.True, $"{stadium.Name} {spot}");
                        }
                    }
                }

                Assert.That(pitch.Geysers.All(g => !g.Contains(Vec2.Zero)), Is.True, "キックオフのボールは間欠泉の上に置かない");
            }
        }

        /// <summary>仕掛けは真ん中を中心に 180° 回すか、真ん中の線で左右を裏返すと重なる（赤と青で有利・不利が出ない）</summary>
        [Test]
        public void Stadiums_GimmicksAreTheSameForBothTeams()
        {
            foreach (var stadium in SoccerStadiumTable.Stadiums)
            {
                var pitch = stadium.CreatePitch();
                foreach (var zone in pitch.Grounds)
                {
                    var rotated = pitch.Grounds.Any(z => z.Ground == zone.Ground && Same(z.Min, zone.Max * -1f) && Same(z.Max, zone.Min * -1f));
                    var mirrored = pitch.Grounds.Any(z => z.Ground == zone.Ground && Same(z.Min, new Vec2(-zone.Max.X, zone.Min.Y)) && Same(z.Max, new Vec2(-zone.Min.X, zone.Max.Y)));
                    Assert.That(rotated || mirrored, Is.True, stadium.Name);
                }

                foreach (var flow in pitch.Flows)
                {
                    var d = flow.DirectionAt(1f);
                    var rotated = pitch.Flows.Any(f => Same(f.Center, flow.Center * -1f) && Same(f.DirectionAt(1f), d * -1f));
                    var mirrored = pitch.Flows.Any(f => Same(f.Center, Mirror(flow.Center)) && Same(f.DirectionAt(1f), Mirror(d)));
                    Assert.That(rotated || mirrored, Is.True, stadium.Name);
                }

                foreach (var geyser in pitch.Geysers)
                {
                    Assert.That(pitch.Geysers.Any(g => Same(g.Center, geyser.Center * -1f)), Is.True, stadium.Name);
                }

                foreach (var post in pitch.Posts)
                {
                    Assert.That(pitch.Posts.Any(p => Same(p.Center, post.Center * -1f)), Is.True, stadium.Name);
                }
            }
        }

        /// <summary>ゴールの前（キーパーの動く所）には仕掛けを置かない（キーパーの止める割合をどのスタジアムでも同じにする）</summary>
        [Test]
        public void Stadiums_KeepGimmicksAwayFromTheGoals()
        {
            foreach (var stadium in SoccerStadiumTable.Stadiums)
            {
                var pitch = stadium.CreatePitch();
                var limit = pitch.HalfLength - 2f;
                Assert.That(pitch.Grounds.All(z => Math.Abs(z.Min.X) <= limit && Math.Abs(z.Max.X) <= limit), Is.True, stadium.Name);
                Assert.That(pitch.Flows.All(f => Math.Abs(f.Min.X) <= limit && Math.Abs(f.Max.X) <= limit), Is.True, stadium.Name);
                Assert.That(pitch.Geysers.All(g => Math.Abs(g.Center.X) + g.Radius <= limit), Is.True, stadium.Name);
                Assert.That(pitch.Posts.All(p => Math.Abs(p.Center.X) + p.Radius <= limit), Is.True, stadium.Name);
            }
        }

        private static bool Same(Vec2 a, Vec2 b) => Vec2.Distance(a, b) < Near;

        /// <summary>真ん中の線（X = 0）で左右を裏返す</summary>
        private static Vec2 Mirror(Vec2 p) => new Vec2(-p.X, p.Y);

        // ---- 氷の床 ----

        [Test]
        public void Ice_BallRollsFartherThanOnGrass()
        {
            var grass = Empty(PitchOf(1));
            var ice = Empty(PitchOf(2));
            foreach (var world in new[] { grass, ice })
            {
                Throw(world, new Vec2(-3f, 0f), new Vec2(3f, 0f));
                Run(world, Steps(3f));
            }

            Assert.That(grass.Ball.Velocity.IsNearlyZero, Is.True);
            Assert.That(ice.Ball.Position.X, Is.GreaterThan(grass.Ball.Position.X + 1f), "氷の上はよく転がる");
        }

        [Test]
        public void Ice_PlayerSlidesOnWhenLettingGo()
        {
            var stopped = new List<float>();
            foreach (var stadium in new[] { 1, 2 })
            {
                var world = Single(PitchOf(stadium), new Vec2(-3f, 0f));
                Run(world, Steps(1f), new SoccerInput(new Vec2(1f, 0f), false));
                var from = world.Players[0].Position.X;
                Run(world, Steps(0.5f), SoccerInput.None);
                stopped.Add(world.Players[0].Position.X - from);
            }

            Assert.That(stopped[1], Is.GreaterThan(stopped[0] * 3f), "氷の上は止まれずにすべる");
        }

        // ---- 押し流す帯 ----

        [Test]
        public void Current_FlowsTheOtherWayEveryCycle()
        {
            var left = PitchOf(3).Flows.First(f => f.Center.X < 0f);

            Assert.That(left.DirectionAt(1f).Y, Is.EqualTo(1f).Within(Near));
            Assert.That(left.DirectionAt(left.Cycle.Period + 1f).Y, Is.EqualTo(-1f).Within(Near), "向きが替わる");
            Assert.That(left.DirectionAt(left.Cycle.Period * 2f + 1f).Y, Is.EqualTo(1f).Within(Near));
        }

        [Test]
        public void Current_CarriesAStillBallDownTheBand()
        {
            var world = Empty(PitchOf(3));
            var left = world.Pitch.Flows.First(f => f.Center.X < 0f);
            Throw(world, new Vec2(left.Center.X, -3f), Vec2.Zero);

            Run(world, Steps(1f));

            Assert.That(world.Ball.Position.Y, Is.GreaterThan(-3f + 0.5f), "止まった球も流される");
            Assert.That(Vec2.Dot(world.Ball.Velocity, left.DirectionAt(world.Time)), Is.LessThanOrEqualTo(left.MaxSpeed + Near), "いつまでも速くならない");
        }

        [Test]
        public void Current_LeavesAHighBallAlone()
        {
            var world = Empty(PitchOf(3));
            var left = world.Pitch.Flows.First(f => f.Center.X < 0f);
            Throw(world, new Vec2(left.Center.X, 0f), Vec2.Zero, 1.2f, 2f);

            Run(world, 3);

            Assert.That(world.Ball.Height, Is.GreaterThan(SoccerBalance.CurrentMaxHeight));
            Assert.That(world.Ball.Velocity.IsNearlyZero, Is.True, "水の上を飛ぶ球は流されない");
        }

        [Test]
        public void Current_DriftsAPlayerStandingInIt()
        {
            var pitch = PitchOf(3);
            var left = pitch.Flows.First(f => f.Center.X < 0f);
            var world = Single(pitch, new Vec2(left.Center.X, -2f));

            Run(world, Steps(1f), SoccerInput.None);

            Assert.That(world.Players[0].Position.Y - -2f, Is.EqualTo(SoccerBalance.CurrentPlayerDrift).Within(0.1f), "立っているだけで流される");
        }

        [Test]
        public void Wind_BlowsThenCalms_AndTurnsAroundEachGust()
        {
            var wind = PitchOf(4).Flows.Single(f => f.Kind == SoccerFlowKind.Wind);
            var gust = wind.Cycle.Active;
            var period = wind.Cycle.Period;

            Assert.That(wind.DirectionAt(gust * 0.5f).IsNearlyZero, Is.False);
            Assert.That(wind.DirectionAt((gust + period) * 0.5f).IsNearlyZero, Is.True, "止んでいる間は押さない");
            Assert.That(Vec2.Dot(wind.DirectionAt(period + gust * 0.5f), wind.DirectionAt(gust * 0.5f)), Is.EqualTo(-1f).Within(Near), "吹くたびに向きが替わる");
        }

        [Test]
        public void Wind_PushesAHighBallHarder()
        {
            var low = Empty(PitchOf(4));
            var high = Empty(PitchOf(4));
            Throw(low, new Vec2(0f, -1f), Vec2.Zero);
            Throw(high, new Vec2(0f, -1f), Vec2.Zero, 1.5f, 4f);

            Run(low, 6);
            Run(high, 6);

            Assert.That(high.Ball.IsAirborne, Is.True);
            Assert.That(high.Ball.Velocity.Length, Is.GreaterThan(low.Ball.Velocity.Length + Near), "浮いた球ほど流される");
        }

        // ---- バンパー ----

        [Test]
        public void Pitch_BumpersReplaceOnlyTheirPartOfTheBoards()
        {
            var plain = PitchOf(1);
            var storm = PitchOf(4);

            float Length(SoccerPitchDef pitch, Func<SoccerWall, bool> which) => pitch.Walls.Where(which).Sum(w => Vec2.Distance(w.A, w.B));
            var plainBoards = Length(plain, w => w.Kind == SoccerWallKind.Board);
            var stormBoards = Length(storm, w => w.Kind == SoccerWallKind.Board || w.Kind == SoccerWallKind.Bumper);

            Assert.That(stormBoards, Is.EqualTo(plainBoards).Within(Near), "外周の長さは変わらない（すき間も重なりもない）");
            Assert.That(storm.Walls.Count(w => w.Kind == SoccerWallKind.Bumper), Is.EqualTo(4), "四隅");
            Assert.That(storm.Walls.Count(w => w.Kind == SoccerWallKind.Post), Is.EqualTo(storm.Posts.Count * SoccerPost.Sides));
        }

        [Test]
        public void Bumper_PostKicksTheBallBackHard()
        {
            var world = Empty(PitchOf(4));
            var post = world.Pitch.Posts.First(p => p.Center.Y > 0f);
            Throw(world, post.Center + new Vec2(0f, -2f), new Vec2(0f, 3f));

            var events = Run(world, Steps(0.6f));

            Assert.That(events.Any(e => e.Kind == SoccerEventKind.BumperHit), Is.True);
            Assert.That(world.Ball.Position.Y, Is.LessThan(post.Center.Y - 2f), "弾き返されて来た所より戻る");
        }

        [Test]
        public void Bumper_CornerReturnsTheBallFasterThanABoard()
        {
            var plain = Empty(PitchOf(1));
            var storm = Empty(PitchOf(4));
            var corner = new Vec2(-plain.Pitch.HalfLength + plain.Pitch.CornerCut * 0.5f, plain.Pitch.HalfWidth - plain.Pitch.CornerCut * 0.5f);
            var inward = new Vec2(1f, -1f).Normalized;
            foreach (var world in new[] { plain, storm })
            {
                Throw(world, corner + inward * 1f, inward * -4f);
                Run(world, 9);
            }

            Assert.That(Vec2.Dot(storm.Ball.Velocity, inward), Is.GreaterThan(0f), "跳ね返った後");
            Assert.That(storm.Ball.Velocity.Length, Is.GreaterThanOrEqualTo(SoccerBalance.BumperMinSpeed - 1.5f));
            Assert.That(storm.Ball.Velocity.Length, Is.GreaterThan(plain.Ball.Velocity.Length * 2f), "角の球を弾き出す");
        }

        [Test]
        public void Bumper_PlayersWalkAroundThePost()
        {
            var pitch = PitchOf(4);
            var post = pitch.Posts.First(p => p.Center.Y > 0f);
            var world = Single(pitch, post.Center + new Vec2(-2f, 0.1f));

            Run(world, Steps(1.5f), new SoccerInput(new Vec2(1f, 0f), false));

            Assert.That(Vec2.Distance(world.Players[0].Position, post.Center), Is.GreaterThanOrEqualTo(post.Radius + SoccerBalance.PlayerRadius - 0.05f), "柱にめり込まない");
            Assert.That(world.Players[0].Position.X, Is.GreaterThan(post.Center.X), "柱に沿って回り込む");
        }

        // ---- 間欠泉 ----

        private static (SoccerWorld World, SoccerGeyser Erupting, SoccerGeyser Quiet) VolcanoWorld(int red = 0)
        {
            var world = new SoccerWorld(PitchOf(5), red, 0);
            var time = Dt;
            var erupting = world.Pitch.Geysers.First(g => g.IsErupting(time) && g.IsErupting(time + 0.5f));
            var quiet = world.Pitch.Geysers.First(g => !g.IsErupting(time) && !g.IsErupting(time + 0.5f));
            return (world, erupting, quiet);
        }

        [Test]
        public void Geyser_LaunchesTheBallOnlyWhileErupting()
        {
            var (world, erupting, quiet) = VolcanoWorld();
            Throw(world, erupting.Center, Vec2.Zero);

            var events = Run(world, 1);

            Assert.That(events.Any(e => e.Kind == SoccerEventKind.GeyserLaunch), Is.True);
            Assert.That(world.Ball.IsAirborne, Is.True);

            Throw(world, quiet.Center, Vec2.Zero);
            events = Run(world, 1);
            Assert.That(events.Any(e => e.Kind == SoccerEventKind.GeyserLaunch), Is.False, "噴いていない口は何もしない");
            Assert.That(world.Ball.IsAirborne, Is.False);
        }

        [Test]
        public void Geyser_LaunchesOnlyOncePerEruption()
        {
            var (world, erupting, _) = VolcanoWorld();
            Throw(world, erupting.Center, Vec2.Zero);
            Run(world, 1);

            Throw(world, erupting.Center, Vec2.Zero);
            var events = Run(world, 1);

            Assert.That(events.Any(e => e.Kind == SoccerEventKind.GeyserLaunch), Is.False, "落ちてきた球を跳ね上げ続けない");
        }

        [Test]
        public void Geyser_PopsTheBallOffAnAttackersFeet_AndNobodyGrabsItRightAway()
        {
            var (world, erupting, _) = VolcanoWorld(red: 1);
            var player = world.Players[0];
            player.Position = erupting.Center - new Vec2(SoccerBalance.DribbleDistance, 0f);
            world.Ball.Place(erupting.Center);
            world.Ball.Holder = 0;

            var events = Run(world, 1);

            Assert.That(events.Any(e => e.Kind == SoccerEventKind.GeyserLaunch && e.Player == 0), Is.True);
            Assert.That(world.Ball.Holder, Is.EqualTo(SoccerBall.NoPlayer), "足元から取られる");
            Run(world, Steps(SoccerBalance.GeyserNoPickupSeconds * 0.5f));
            Assert.That(world.Ball.Holder, Is.EqualTo(SoccerBall.NoPlayer), "上がりきる前の低い所で取らない");
        }
    }
}
