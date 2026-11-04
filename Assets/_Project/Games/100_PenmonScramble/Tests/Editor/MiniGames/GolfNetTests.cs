using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PenmonScramble.Core;
using PenmonScramble.Golf.Core;

namespace PenmonScramble.Tests
{
    /// <summary>ゴルフのオンライン（Docs/MINIGAME_GOLF_PLAN.md フェーズ 5）: 送る中身の読み書き・ゲストの写し・抜けた人・順位と報酬</summary>
    public class GolfNetTests
    {
        private const int MaxTicks = 20000;
        private static readonly Vec2 FarCup = new Vec2(40f, 40f);

        // ---- 読み書き ----

        [Test]
        public void Snapshot_RoundTrips()
        {
            var round = TwoHoleRound(3);
            round.Teams[1].Select(2);
            round.Shoot(0, new GolfShot(1, 0.3f, 0.6f));
            round.Tick(0.2f);
            var events = new[] { new GolfNetEvent(0, new GolfEvent(GolfEventKind.WallHit, 0, new Vec2(1f, 2f), 3.5f, 2)) };
            var aim = new GolfAim(2, 1.25f, 0.4f, true, false, true, -1);

            var sent = GolfSnapshot.Capture(round, aim, events);
            var got = GolfCodec.DecodeSnapshot(GolfCodec.EncodeSnapshot(sent));

            Assert.That(got, Is.Not.Null);
            Assert.That(got.CourseNo, Is.EqualTo(sent.CourseNo));
            Assert.That(got.HoleIndex, Is.EqualTo(sent.HoleIndex));
            Assert.That(got.Phase, Is.EqualTo(GolfHolePhase.Rolling));
            Assert.That(got.CurrentPlayer, Is.EqualTo(0));
            Assert.That(got.TurnOrder, Is.EqualTo(sent.TurnOrder));
            Assert.That(got.PlayerCount, Is.EqualTo(3));
            Assert.That(got.Balls[0].Position.X, Is.EqualTo(sent.Balls[0].Position.X));
            Assert.That(got.Balls[0].Velocity.Y, Is.EqualTo(sent.Balls[0].Velocity.Y));
            Assert.That(got.Strokes, Is.EqualTo(sent.Strokes));
            Assert.That(got.Teams[1].Selected, Is.EqualTo(2));
            Assert.That(got.Teams[0].Species, Is.EqualTo(sent.Teams[0].Species));
            Assert.That(got.Aim.Power, Is.EqualTo(0.4f));
            Assert.That(got.Aim.GaugeRunning && got.Aim.Special && got.Aim.Curve == -1, Is.True);
            Assert.That(got.Events.Count, Is.EqualTo(1));
            Assert.That(got.Events[0].Event.Kind, Is.EqualTo(GolfEventKind.WallHit));
            Assert.That(got.Events[0].Event.Other, Is.EqualTo(2));
            Assert.That(got.Events[0].Event.Strength, Is.EqualTo(3.5f));
        }

        [Test]
        public void Snapshot_BrokenData_IsNull()
        {
            var data = GolfCodec.EncodeSnapshot(GolfSnapshot.Capture(TwoHoleRound(2), default, new GolfNetEvent[0]));

            Assert.That(GolfCodec.DecodeSnapshot(data.Take(data.Length / 2).ToArray()), Is.Null);
            data[0] = (byte)(GolfCodec.FormatVersion + 1);
            Assert.That(GolfCodec.DecodeSnapshot(data), Is.Null);
            Assert.That(GolfCodec.DecodeSnapshot(null), Is.Null);
        }

        [Test]
        public void Input_RoundTrips()
        {
            var aim = new GolfAim(1, -0.5f, 0.75f, false, true, true, 1);

            Assert.That(GolfCodec.TryDecodeInput(GolfCodec.EncodeInput(GolfInput.ForStrike(aim)), out var strike), Is.True);
            Assert.That(strike.Kind, Is.EqualTo(GolfInputKind.Strike));
            var shot = strike.Aim.ToShot();
            Assert.That(shot.Member, Is.EqualTo(1));
            Assert.That(shot.Angle, Is.EqualTo(-0.5f));
            Assert.That(shot.Power, Is.EqualTo(0.75f));
            Assert.That(shot.Special, Is.True);

            Assert.That(GolfCodec.TryDecodeInput(GolfCodec.EncodeInput(GolfInput.ForBoost(0.3f)), out var boost), Is.True);
            Assert.That(boost.Kind, Is.EqualTo(GolfInputKind.Boost));
            Assert.That(boost.BoostTurn, Is.EqualTo(0.3f));
        }

        [Test]
        public void Party_RoundTrips_AndKeepsTeamSize()
        {
            Assert.That(GolfCodec.TryDecodeParty(GolfCodec.EncodeParty(new[] { 1, 3, 5, 7 }), out var species), Is.True);
            Assert.That(species, Is.EqualTo(new[] { 1, 3, 5 }));
        }

        // ---- ゲストの写し ----

        /// <summary>ホストのラウンドを送り続けると、写しも同じ打数・同じホール・同じ位置になり、同じ出来事が届く</summary>
        [Test]
        public void Mirror_FollowsHostThroughHoles()
        {
            var host = TwoHoleRound(2);
            var link = new Link(host);
            var mirror = GolfRound.CreateMirror(host.Course, Teams(2));
            var mirrorEvents = new List<GolfEvent>();
            var started = new List<int>();
            var completed = new List<int>();
            mirror.Happened += mirrorEvents.Add;
            mirror.HoleStarted += started.Add;
            mirror.HoleCompleted += completed.Add;

            PlayOutHole(host, () => link.Sync(mirror));
            Assert.That(host.NextHole(), Is.True);
            link.Sync(mirror);
            PlayOutHole(host, () => link.Sync(mirror));
            link.Sync(mirror);
            mirror.TickMirror(1f, GolfNetRules.SnapshotSeconds);

            Assert.That(mirror.HoleIndex, Is.EqualTo(1));
            Assert.That(mirror.IsFinished, Is.True);
            Assert.That(completed, Is.EqualTo(new[] { 0, 1 }));
            Assert.That(started, Does.Contain(1));
            for (var player = 0; player < 2; player++)
            {
                Assert.That(mirror.Total(player), Is.EqualTo(host.Total(player)));
                Assert.That(mirror.World.Balls[player].Position.X, Is.EqualTo(host.World.Balls[player].Position.X).Within(1e-4f));
            }

            Assert.That(mirrorEvents.Count(e => e.Kind == GolfEventKind.Shot), Is.EqualTo(link.Sent.Count(e => e.Kind == GolfEventKind.Shot)));
            Assert.That(mirrorEvents.Count(e => e.Kind == GolfEventKind.HoleFinished), Is.EqualTo(2));
        }

        /// <summary>出来事を見せる時点では、その出来事が起きたときの打数と番の人になっている（カップインの名前・喜ぶ子を合わせるため）</summary>
        [Test]
        public void Mirror_EventSeesStrokesAtThatMoment()
        {
            var hole = new GolfHoleDef("cup", 3, Vec2.Zero, new Vec2(3f, 0f), GolfGeometry.Rect(-50f, -50f, 50f, 50f), new GolfWall[0], new GolfArea[0]);
            var course = new GolfCourseDef(1, "test", 1, new[] { hole });
            var host = new GolfRound(course, Teams(2));
            var link = new Link(host);
            var mirror = GolfRound.CreateMirror(course, Teams(2));
            var seen = new List<(int strokes, int current)>();
            mirror.Happened += e =>
            {
                if (e.Kind == GolfEventKind.CupIn)
                {
                    seen.Add((mirror.World.Strokes(e.Player), mirror.World.CurrentPlayer));
                }
            };

            // 席 0 がまっすぐカップへ。止まる（番が替わる）まで1度に進めて、まとめて送る
            host.Shoot(0, new GolfShot(0, host.World.AngleToCup(0), PowerForDistance(3.05f)));
            for (var i = 0; i < MaxTicks && host.World.Phase == GolfHolePhase.Rolling; i++)
            {
                host.Tick(0.1f);
            }

            Assert.That(host.World.HoledOut(0), Is.True, "テストのホールでカップに入ること");
            link.Sync(mirror);

            Assert.That(seen, Is.EqualTo(new[] { (1, 0) }));
            Assert.That(mirror.World.CurrentPlayer, Is.EqualTo(1));
        }

        /// <summary>自分の番で狙っている途中は、届いたスナップショットで手元の向きと選んだ子が戻らない</summary>
        [Test]
        public void Mirror_KeepsLocalAimOnOwnTurn()
        {
            var host = TwoHoleRound(2);
            var link = new Link(host);
            var mirror = GolfRound.CreateMirror(host.Course, Teams(2));
            link.Sync(mirror, localSeat: 0);

            mirror.World.SetAim(2f);
            mirror.Teams[0].Select(2);
            link.Sync(mirror, localSeat: 0);

            Assert.That(mirror.World.AimAngle, Is.EqualTo(2f));
            Assert.That(mirror.Teams[0].Selected, Is.EqualTo(2));

            // ほかの人の席として見ていれば、ホストの向きになる
            link.Sync(mirror, localSeat: 1);
            Assert.That(mirror.World.AimAngle, Is.EqualTo(host.World.AimAngle));
            Assert.That(mirror.Teams[0].Selected, Is.EqualTo(0));
        }

        /// <summary>自分の番が回ってきたときは、前の人の向きを引きずらず、ホストの向き（カップの方）から始める</summary>
        [Test]
        public void Mirror_OwnTurnStartsFromHostAim()
        {
            var host = TwoHoleRound(2);
            var link = new Link(host);
            var mirror = GolfRound.CreateMirror(host.Course, Teams(2));
            link.Sync(mirror, localSeat: 1);

            host.Shoot(0, new GolfShot(0, 1.5f, 0.2f));
            for (var i = 0; i < MaxTicks && host.World.Phase == GolfHolePhase.Rolling; i++)
            {
                host.Tick(0.1f);
            }

            link.Sync(mirror, localSeat: 1);

            Assert.That(mirror.World.CurrentPlayer, Is.EqualTo(1));
            Assert.That(mirror.World.AimAngle, Is.EqualTo(host.World.AimAngle));
        }

        [Test]
        public void Mirror_DoesNotSimulateOrShoot()
        {
            var host = TwoHoleRound(2);
            var mirror = GolfRound.CreateMirror(host.Course, Teams(2));
            var start = mirror.World.Balls[0].Position;

            Assert.That(mirror.Shoot(0, new GolfShot(0, 0f, 1f)), Is.False);
            mirror.Tick(5f);

            Assert.That(mirror.World.Phase, Is.EqualTo(GolfHolePhase.Aiming));
            Assert.That(mirror.World.Balls[0].Position, Is.EqualTo(start));
            Assert.That(mirror.NextHole(), Is.False);
        }

        [Test]
        public void Mirror_RejectsOtherPlayerCount()
        {
            var host = TwoHoleRound(3);
            var mirror = GolfRound.CreateMirror(host.Course, Teams(2));

            Assert.That(mirror.ApplySnapshot(GolfSnapshot.Capture(host, default, new GolfNetEvent[0]), -1), Is.False);
        }

        // ---- 抜けた人 ----

        [Test]
        public void Remove_CurrentPlayerPassesTurn()
        {
            var round = TwoHoleRound(3);
            Assert.That(round.World.CurrentPlayer, Is.EqualTo(0));

            round.RemovePlayer(0);

            Assert.That(round.World.CurrentPlayer, Is.EqualTo(1));
            Assert.That(round.World.Balls[0].Active, Is.False);
            Assert.That(round.IsRemoved(0), Is.True);
        }

        [Test]
        public void Remove_LastWaitingPlayerFinishesHole()
        {
            var round = TwoHoleRound(2);
            round.RemovePlayer(1);
            PlayOutHole(round, null);

            Assert.That(round.IsRecorded(0), Is.True);
        }

        [Test]
        public void Remove_StaysOutOfLaterHolesAndRanks()
        {
            var round = TwoHoleRound(3);
            round.RemovePlayer(2);
            PlayOutHole(round, null);
            round.NextHole();

            Assert.That(round.World.IsDone(2), Is.True);
            Assert.That(round.World.Balls[2].Active, Is.False);
            Assert.That(round.Rank(2), Is.EqualTo(0));
        }

        /// <summary>写しは、抜けた人をスナップショットで知る（次のホールでもはじめから外れている）</summary>
        [Test]
        public void Mirror_KnowsRemovedPlayer()
        {
            var host = TwoHoleRound(3);
            var link = new Link(host);
            var mirror = GolfRound.CreateMirror(host.Course, Teams(3));
            host.RemovePlayer(1);
            link.Sync(mirror);

            Assert.That(mirror.IsRemoved(1), Is.True);
            Assert.That(mirror.World.Balls[1].Active, Is.False);
        }

        // ---- 順位と報酬 ----

        [Test]
        public void Rank_SameStrokesShareRank()
        {
            var round = TwoHoleRound(3);
            PlayOutHole(round, null);

            // 遠いカップで全員が上限まで打つので同じ打数
            Assert.That(Enumerable.Range(0, 3).Select(round.Rank), Is.All.EqualTo(1));
        }

        [Test]
        public void Reward_RankBonusOnlyForTopThree()
        {
            Assert.That(GolfReward.Compute(9, 9, 0, false, 1).RankBonus, Is.EqualTo(40));
            Assert.That(GolfReward.Compute(9, 9, 0, false, 2).RankBonus, Is.EqualTo(20));
            Assert.That(GolfReward.Compute(9, 9, 0, false, 3).RankBonus, Is.EqualTo(10));
            Assert.That(GolfReward.Compute(9, 9, 0, false, 4).RankBonus, Is.EqualTo(0));
            Assert.That(GolfReward.Compute(9, 9, 0, false).RankBonus, Is.EqualTo(0));
            Assert.That(GolfReward.Compute(9, 9, 0, false, 1).Total, Is.EqualTo(GolfReward.BaseCoins + 40));
        }

        // ---- 道具 ----

        /// <summary>ホストの出来事を溜め、スナップショットにして（バイト列を通して）写しへ届ける</summary>
        private sealed class Link
        {
            private readonly GolfRound _host;
            private readonly List<GolfNetEvent> _pending = new List<GolfNetEvent>();

            public Link(GolfRound host)
            {
                _host = host;
                host.Happened += e =>
                {
                    _pending.Add(new GolfNetEvent(host.HoleIndex, e));
                    Sent.Add(e);
                };
            }

            public List<GolfEvent> Sent { get; } = new List<GolfEvent>();

            public void Sync(GolfRound mirror, int localSeat = -1)
            {
                var data = GolfCodec.EncodeSnapshot(GolfSnapshot.Capture(_host, default, _pending));
                _pending.Clear();
                Assert.That(mirror.ApplySnapshot(GolfCodec.DecodeSnapshot(data), localSeat), Is.True);
            }
        }

        private static List<GolfTeam> Teams(int players)
        {
            return Enumerable.Range(0, players).Select(_ => new GolfTeam(new[] { 1, 3, 5 })).ToList();
        }

        /// <summary>2ホールの平らなコース（カップは遠くて入らない。上限まで打って終わる）</summary>
        private static GolfRound TwoHoleRound(int players)
        {
            var hole = new GolfHoleDef("flat", 2, Vec2.Zero, FarCup, GolfGeometry.Rect(-50f, -50f, 50f, 50f), new GolfWall[0], new GolfArea[0]);
            var course = new GolfCourseDef(1, "test", 1, new[] { hole, hole });
            return new GolfRound(course, Teams(players));
        }

        /// <summary>全員が弱く打って、ホールが終わるまで進める。afterTick があれば、進めるたびに呼ぶ（送る）</summary>
        private static void PlayOutHole(GolfRound round, System.Action afterTick)
        {
            for (var i = 0; i < 200 && round.World.Phase != GolfHolePhase.Finished; i++)
            {
                round.Shoot(round.World.CurrentPlayer, new GolfShot(0, 0f, 0.2f));
                for (var t = 0; t < MaxTicks && round.World.Phase == GolfHolePhase.Rolling; t++)
                {
                    round.Tick(0.1f);
                    afterTick?.Invoke();
                }
            }

            Assert.That(round.World.Phase, Is.EqualTo(GolfHolePhase.Finished));
        }

        /// <summary>平らな地面でちょうど distance 転がる強さ</summary>
        private static float PowerForDistance(float distance)
        {
            var speed = System.MathF.Sqrt(2f * GolfBalance.RollDecel * distance);
            return (speed - GolfBalance.MinShotSpeed) / (GolfBalance.MaxShotSpeed - GolfBalance.MinShotSpeed);
        }
    }
}
