using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PenmonScramble.Core;
using PenmonScramble.Soccer.Core;

namespace PenmonScramble.Tests
{
    /// <summary>
    /// サッカーのオンライン（Docs/MINIGAME_SOCCER_PLAN.md フェーズ 5）: チームの組み方・席ごとの操作・PK戦の番・
    /// 入力の受け方・スナップショットとバイト列・写しの試合・待ちの窓の選び・MVP
    /// </summary>
    public class SoccerNetTests
    {
        private const float Dt = SoccerRules.StepSeconds;
        private static readonly int[] PartyA = { 11, 12, 13 };
        private static readonly int[] PartyB = { 21, 22, 23 };
        private static readonly int[] PartyC = { 31, 32, 33 };
        private static readonly int[] PartyD = { 41, 42, 43 };
        private static readonly int[] Wild = { 51, 52, 53, 54 };

        private static int Steps(float seconds) => (int)MathF.Ceiling(seconds / Dt);

        private static SoccerTeamPlan Plan(SoccerOnlineMode mode, int[] teams, params int[][] parties)
        {
            return SoccerTeamPlan.Create(mode, teams, parties, Wild);
        }

        // ---- チームの組み方 ----

        [Test]
        public void Plan_Solo_IsTheWholePartyAgainstTheCpu()
        {
            var plan = SoccerTeamPlan.Solo(PartyA, Wild);

            Assert.That(plan.PerTeam, Is.EqualTo(3));
            Assert.That(plan.Species, Is.EqualTo(new[] { 11, 12, 13, 51, 52, 53 }));
            Assert.That(plan.Owner, Is.EqualTo(new[] { 0, -1, -1, -1, -1, -1 }));
            Assert.That(plan.CpuTeam, Is.EqualTo(SoccerRules.Blue));
            Assert.That(plan.LeaderOf(0), Is.EqualTo(0));
        }

        [Test]
        public void Plan_TwoHumansOnATeam_AreBothLeadersPlusTheYoungerSeatsSecond()
        {
            // 初めの赤・青（P1・P3 が赤、P2 が青）
            var plan = Plan(SoccerOnlineMode.Versus, null, PartyA, PartyB, PartyC);

            Assert.That(plan.PerTeam, Is.EqualTo(3));
            Assert.That(plan.Species, Is.EqualTo(new[] { 11, 31, 12, 21, 22, 23 }));
            Assert.That(plan.Owner, Is.EqualTo(new[] { 0, 2, -1, 1, -1, -1 }));
            Assert.That(plan.CpuTeam, Is.EqualTo(-1));
            Assert.That(plan.LeaderOf(2), Is.EqualTo(1));
            Assert.That(plan.LeaderOf(1), Is.EqualTo(3));
        }

        [Test]
        public void Plan_ThreeHumansOnATeam_AreTheirLeaders()
        {
            var teams = new[] { SoccerRules.Red, SoccerRules.Red, SoccerRules.Red, SoccerRules.Blue };
            var plan = Plan(SoccerOnlineMode.Versus, teams, PartyA, PartyB, PartyC, PartyD);

            Assert.That(plan.PerTeam, Is.EqualTo(3));
            Assert.That(plan.Species.Take(3), Is.EqualTo(new[] { 11, 21, 31 }));
            Assert.That(plan.Species.Skip(3), Is.EqualTo(new[] { 41, 42, 43 }));
        }

        [Test]
        public void Plan_CoopWithFour_IsFourAgainstFourCpu()
        {
            var plan = Plan(SoccerOnlineMode.Coop, new[] { SoccerRules.Blue, SoccerRules.Blue, SoccerRules.Blue, SoccerRules.Blue }, PartyA, PartyB, PartyC, PartyD);

            Assert.That(plan.PerTeam, Is.EqualTo(4));
            Assert.That(plan.Species, Is.EqualTo(new[] { 11, 21, 31, 41, 51, 52, 53, 54 }));
            Assert.That(plan.Owner, Is.EqualTo(new[] { 0, 1, 2, 3, -1, -1, -1, -1 }));
            Assert.That(plan.CpuTeam, Is.EqualTo(SoccerRules.Blue));
            Assert.That(Enumerable.Range(0, 4).Select(plan.TeamOf), Is.All.EqualTo(SoccerRules.Red), "協力は全員赤");
        }

        [Test]
        public void Plan_MissingParty_FillsWithTheFallbackSpecies()
        {
            var plan = Plan(SoccerOnlineMode.Versus, null, new[] { 11 }, null);

            Assert.That(plan.Species, Is.EqualTo(new[]
            {
                11, SoccerMatchSetup.FallbackSpecies, SoccerMatchSetup.FallbackSpecies,
                SoccerMatchSetup.FallbackSpecies, SoccerMatchSetup.FallbackSpecies, SoccerMatchSetup.FallbackSpecies,
            }));
        }

        [Test]
        public void CanStart_VersusNeedsBothTeams_CoopStartsAlone()
        {
            Assert.That(SoccerTeamPlan.CanStart(SoccerOnlineMode.Versus, null, 1), Is.False, "ひとりは対戦できない");
            Assert.That(SoccerTeamPlan.CanStart(SoccerOnlineMode.Versus, null, 2), Is.True);
            Assert.That(SoccerTeamPlan.CanStart(SoccerOnlineMode.Versus, new[] { SoccerRules.Red, SoccerRules.Red }, 2), Is.False);
            Assert.That(SoccerTeamPlan.CanStart(SoccerOnlineMode.Coop, null, 1), Is.True);
        }

        [Test]
        public void CoopCpuStars_RiseWithHumans_UpToFive()
        {
            Assert.That(SoccerBalance.CoopCpuStars(1, 1), Is.EqualTo(1));
            Assert.That(SoccerBalance.CoopCpuStars(1, 3), Is.EqualTo(1 + 2 * SoccerBalance.CoopStarsPerExtraHuman));
            Assert.That(SoccerBalance.CoopCpuStars(5, 4), Is.EqualTo(SoccerBalance.AiMaxStars));
        }

        // ---- 席ごとの操作 ----

        private static SoccerMatch PlayingMatch(SoccerTeamPlan plan, int seed = 3)
        {
            var world = SoccerMatchSetup.CreateWorld(SoccerPitchTable.Standard(), plan.Species, seed, plan.PerTeam);
            var match = new SoccerMatch(world, seed);
            var events = new List<SoccerEvent>();
            match.Begin(events);
            for (var i = 0; i < Steps(SoccerRules.KickoffWaitSeconds) + 3 && match.Phase == SoccerMatchPhase.Kickoff; i++)
            {
                match.Step(null, events);
            }

            Assert.That(match.Phase, Is.EqualTo(SoccerMatchPhase.Playing));
            return match;
        }

        private static void GiveBall(SoccerWorld world, int index)
        {
            var player = world.Players[index];
            world.Ball.Place(player.Position + player.Facing * SoccerBalance.DribbleDistance);
            world.Step(null, new List<SoccerEvent>());
            Assert.That(world.Ball.Holder, Is.EqualTo(index));
        }

        [Test]
        public void Humans_StartOnTheirLeaders_AndDriveOnlyThem()
        {
            var plan = Plan(SoccerOnlineMode.Versus, null, PartyA, PartyB, PartyC);
            var match = PlayingMatch(plan);
            var humans = new SoccerHumans(plan, match.World.Players.Count);
            var inputs = new SoccerInput[match.World.Players.Count];

            humans.Drive(match, new SoccerInput[3], new bool[3], null, inputs);

            Assert.That(humans.ControlledBy(0), Is.EqualTo(0));
            Assert.That(humans.ControlledBy(1), Is.EqualTo(3));
            Assert.That(humans.ControlledBy(2), Is.EqualTo(1));
            Assert.That(Enumerable.Range(0, inputs.Length).Where(humans.IsDriven), Is.EquivalentTo(new[] { 0, 1, 3 }));
        }

        [Test]
        public void Humans_OnTheSameTeam_NeverTakeTheSamePlayer()
        {
            var plan = Plan(SoccerOnlineMode.Versus, null, PartyA, PartyB, PartyC);
            var match = PlayingMatch(plan);
            var humans = new SoccerHumans(plan, match.World.Players.Count);
            var inputs = new SoccerInput[match.World.Players.Count];
            // 赤の AI の子（席 0 の 2番目）が持つと、誰か1人だけがその子へ替わる
            GiveBall(match.World, 2);

            humans.Drive(match, new SoccerInput[3], new bool[3], null, inputs);

            var red = new[] { humans.ControlledBy(0), humans.ControlledBy(2) };
            Assert.That(red, Does.Contain(2));
            Assert.That(red.Distinct().Count(), Is.EqualTo(2));
        }

        [Test]
        public void Humans_DoNotSwitchToAPlayerAnotherHumanControls()
        {
            var plan = Plan(SoccerOnlineMode.Versus, null, PartyA, PartyB, PartyC);
            var match = PlayingMatch(plan);
            var humans = new SoccerHumans(plan, match.World.Players.Count);
            var inputs = new SoccerInput[match.World.Players.Count];
            // 席 2 の子（1番）が持っても、席 0 はその子へ替わらない
            GiveBall(match.World, 1);

            humans.Drive(match, new SoccerInput[3], new[] { true, false, false }, null, inputs);

            Assert.That(humans.ControlledBy(2), Is.EqualTo(1));
            Assert.That(humans.ControlledBy(0), Is.Not.EqualTo(1));
        }

        [Test]
        public void Humans_RemovedSeat_LeavesItsPlayerToTheAi()
        {
            var plan = Plan(SoccerOnlineMode.Versus, null, PartyA, PartyB, PartyC);
            var match = PlayingMatch(plan);
            var humans = new SoccerHumans(plan, match.World.Players.Count);
            var inputs = new SoccerInput[match.World.Players.Count];

            humans.Remove(2);
            humans.Drive(match, new SoccerInput[3], new bool[3], null, inputs);

            Assert.That(humans.Has(2), Is.False);
            Assert.That(humans.IsDriven(1), Is.False);
            Assert.That(humans.SeatTeams(), Is.EqualTo(new[] { SoccerRules.Red, SoccerRules.Blue, -1 }));
        }

        [Test]
        public void Humans_PassTheSpecialButtonThrough()
        {
            var plan = SoccerTeamPlan.Solo(PartyA, Wild);
            var match = PlayingMatch(plan);
            var humans = new SoccerHumans(plan, match.World.Players.Count);
            var inputs = new SoccerInput[match.World.Players.Count];

            humans.Drive(match, new[] { new SoccerInput(Vec2.Zero, false, false, true) }, new bool[1], null, inputs);

            Assert.That(inputs[humans.ControlledBy(0)].Special, Is.True, "必殺シュートのボタンが動かす子へ届く");
        }

        // ---- PK戦の番 ----

        [Test]
        public void ShootoutSeat_TwoHumansOnATeam_TakeTurns()
        {
            var plan = Plan(SoccerOnlineMode.Versus, null, PartyA, PartyB, PartyC);
            var world = SoccerMatchSetup.CreateWorld(SoccerPitchTable.Standard(), plan.Species, 1, plan.PerTeam);
            var shootout = new SoccerShootout(world, 1);
            var events = new List<SoccerEvent>();
            shootout.Begin(events);
            var humans = new SoccerHumans(plan, world.Players.Count);

            var redTurns = new List<int>();
            var blueKeepers = new List<int>();
            var seen = -1;
            for (var i = 0; i < Steps(60f) && redTurns.Count < 3 && !shootout.IsFinished; i++)
            {
                if (shootout.Phase == SoccerShootoutPhase.Aim && shootout.TotalKicks != seen)
                {
                    seen = shootout.TotalKicks;
                    if (shootout.KickingTeam == SoccerRules.Red)
                    {
                        redTurns.Add(humans.ShootoutSeat(shootout, SoccerRules.Red));
                    }
                    else
                    {
                        blueKeepers.Add(humans.ShootoutSeat(shootout, SoccerRules.Red));
                    }
                }

                // 誰も押さない（持ち時間で弱く蹴る）
                shootout.Step(null, events);
            }

            Assert.That(redTurns, Is.EqualTo(new[] { 0, 2, 0 }).Or.EqualTo(new[] { 0, 2 }), "蹴る番は 席0 → 席2 → 席0");
            Assert.That(blueKeepers, Is.Not.Empty);
            Assert.That(humans.ShootoutSeat(shootout, SoccerRules.Blue), Is.EqualTo(1), "1人のチームはいつもその人");
        }

        // ---- 入力の受け方 ----

        [Test]
        public void RemoteInput_ShortPressBetweenSteps_IsNotLost()
        {
            var remote = new SoccerRemoteInput();
            remote.Receive(new SoccerNetInput(Vec2.Zero, true, false, false, 0));
            remote.Receive(new SoccerNetInput(Vec2.Zero, false, false, false, 0));

            Assert.That(remote.Take(out _).Shoot, Is.True, "押して離したのが1歩の間に届いても、その歩は押している");
            Assert.That(remote.Take(out _).Shoot, Is.False, "次の歩で離す（蹴る）");
        }

        [Test]
        public void RemoteInput_SwitchCountsUp_PressesOnce()
        {
            var remote = new SoccerRemoteInput();
            remote.Receive(new SoccerNetInput(Vec2.Zero, false, false, false, 7));
            remote.Take(out var first);
            remote.Receive(new SoccerNetInput(new Vec2(1f, 0f), false, false, false, 8));
            var input = remote.Take(out var second);
            remote.Take(out var third);

            Assert.That(first, Is.False, "初めて届いた回数は押したと数えない");
            Assert.That(second, Is.True);
            Assert.That(third, Is.False);
            Assert.That(input.Move.X, Is.EqualTo(1f));
        }

        // ---- バイト列 ----

        [Test]
        public void Codec_Input_RoundTrips()
        {
            var input = new SoccerNetInput(new Vec2(0.5f, -0.5f), true, false, true, 200);

            Assert.That(SoccerCodec.TryDecodeInput(SoccerCodec.EncodeInput(input), out var read), Is.True);
            Assert.That(read.Move.X, Is.EqualTo(0.5f).Within(0.01f));
            Assert.That(read.Move.Y, Is.EqualTo(-0.5f).Within(0.01f));
            Assert.That(read.SameButtons(input), Is.True);
        }

        [Test]
        public void Codec_Party_RoundTrips_AndOtherVersionsAreIgnored()
        {
            Assert.That(SoccerCodec.TryDecodeParty(SoccerCodec.EncodeParty(PartyB), out var party), Is.True);
            Assert.That(party, Is.EqualTo(PartyB));

            var data = SoccerCodec.EncodeParty(PartyB);
            data[0] = (byte)(SoccerCodec.FormatVersion + 1);
            Assert.That(SoccerCodec.TryDecodeParty(data, out _), Is.False);
            Assert.That(SoccerCodec.DecodeSnapshot(new byte[] { SoccerCodec.FormatVersion, 1 }), Is.Null, "途中で切れた物は読まない");
        }

        /// <summary>CPU どうしで少し試合を進めたホストの試合（ボールが動いて、点やゲージが入っている所）</summary>
        private static (SoccerMatch Match, SoccerTeamPlan Plan, SoccerHumans Humans, List<SoccerEvent> Events) HostMatch(float seconds)
        {
            var plan = Plan(SoccerOnlineMode.Coop, null, PartyA, PartyB);
            var match = PlayingMatch(plan, 5);
            var humans = new SoccerHumans(plan, match.World.Players.Count);
            var ai = new SoccerAi(match.World, 9);
            var inputs = new SoccerInput[match.World.Players.Count];
            var events = new List<SoccerEvent>();
            for (var i = 0; i < Steps(seconds); i++)
            {
                events.Clear();
                ai.ThinkMatch(match, inputs, _ => false);
                match.Step(inputs, events);
            }

            match.World.SetGauge(SoccerRules.Red, 0.4f);
            return (match, plan, humans, events);
        }

        [Test]
        public void Codec_Snapshot_RoundTrips()
        {
            var (match, plan, humans, events) = HostMatch(8f);
            events.Add(new SoccerEvent(SoccerEventKind.Kick, 1, SoccerRules.Red, new Vec2(1f, 2f), 0.7f, true, 3));
            var snapshot = SoccerSnapshot.Capture(1, match, plan, humans, new[] { 2, 0 }, events);

            var read = SoccerCodec.DecodeSnapshot(SoccerCodec.EncodeSnapshot(snapshot));

            Assert.That(read, Is.Not.Null);
            Assert.That(read.Stadium, Is.EqualTo(1));
            Assert.That(read.Mode, Is.EqualTo(SoccerOnlineMode.Coop));
            Assert.That(read.PerTeam, Is.EqualTo(3));
            Assert.That(read.SeatTeam, Is.EqualTo(snapshot.SeatTeam));
            Assert.That(read.SeatControlled, Is.EqualTo(snapshot.SeatControlled));
            Assert.That(read.SeatGoals, Is.EqualTo(new[] { 2, 0 }));
            Assert.That(read.Species, Is.EqualTo(plan.Species));
            Assert.That(read.Phase, Is.EqualTo(match.Phase));
            Assert.That(read.TimeLeft, Is.EqualTo(match.TimeLeft));
            Assert.That(read.WorldTime, Is.EqualTo(match.World.Time), "仕掛けの様子はピッチの時刻で決まる");
            Assert.That(read.Gauge[SoccerRules.Red], Is.EqualTo(0.4f));
            Assert.That(read.Ball.Position, Is.EqualTo(snapshot.Ball.Position));
            Assert.That(read.Ball.Holder, Is.EqualTo(snapshot.Ball.Holder));
            for (var i = 0; i < snapshot.Players.Length; i++)
            {
                Assert.That(read.Players[i].Position, Is.EqualTo(snapshot.Players[i].Position));
                Assert.That(read.Players[i].State, Is.EqualTo(snapshot.Players[i].State));
            }

            Assert.That(read.Keepers.Length, Is.EqualTo(2));
            Assert.That(read.Keepers[1].Position, Is.EqualTo(snapshot.Keepers[1].Position));
            var kick = read.Events.Last();
            Assert.That(kick.Kind, Is.EqualTo(SoccerEventKind.Kick));
            Assert.That(kick.Player, Is.EqualTo(1));
            Assert.That(kick.Other, Is.EqualTo(3));
            Assert.That(kick.Lofted, Is.True);
            Assert.That(kick.Strength, Is.EqualTo(0.7f));
        }

        // ---- 写しの試合 ----

        [Test]
        public void Mirror_FollowsTheHostMatch()
        {
            var (match, plan, humans, events) = HostMatch(6f);
            var first = SoccerCodec.DecodeSnapshot(SoccerCodec.EncodeSnapshot(SoccerSnapshot.Capture(1, match, plan, humans, null, events)));
            var mirror = SoccerMatch.CreateMirror(first);

            Assert.That(mirror, Is.Not.Null);
            Assert.That(mirror.IsMirror, Is.True);
            Assert.That(mirror.World.Players.Count, Is.EqualTo(6));
            Assert.That(mirror.World.Players[4].Element, Is.EqualTo(match.World.Players[4].Element), "種類から必殺シュートの属性を作る");
            Assert.That(mirror.World.Time, Is.EqualTo(match.World.Time), "ゲストも同じ時刻の仕掛けを描く");
            AssertSame(match, mirror);
        }

        [Test]
        public void Mirror_CopiesTheShootout_WithoutTheKickersAim()
        {
            var plan = Plan(SoccerOnlineMode.Versus, null, PartyA, PartyB);
            var world = SoccerMatchSetup.CreateWorld(SoccerPitchTable.Standard(), plan.Species, 1, plan.PerTeam);
            var match = new SoccerMatch(world, 1);
            var events = new List<SoccerEvent>();
            match.Begin(events);
            match.StartShootoutNow(events);
            for (var i = 0; i < Steps(30f) && match.Shootout.TotalKicks < 3; i++)
            {
                match.Step(new[] { new SoccerInput(new Vec2(1f, 1f), false) }, events);
            }

            var humans = new SoccerHumans(plan, world.Players.Count);
            var snapshot = SoccerSnapshot.Capture(1, match, plan, humans, null, null);
            var mirror = SoccerMatch.CreateMirror(SoccerCodec.DecodeSnapshot(SoccerCodec.EncodeSnapshot(snapshot)));

            Assert.That(mirror.Phase, Is.EqualTo(SoccerMatchPhase.Shootout));
            Assert.That(mirror.Shootout.TotalKicks, Is.EqualTo(match.Shootout.TotalKicks));
            Assert.That(mirror.Shootout.Results(SoccerRules.Red), Is.EqualTo(match.Shootout.Results(SoccerRules.Red)));
            Assert.That(mirror.Shootout.KickingTeam, Is.EqualTo(match.Shootout.KickingTeam));
            Assert.That(mirror.Shootout.Kicker, Is.EqualTo(match.Shootout.Kicker));
            Assert.That(mirror.Shootout.AimSide, Is.EqualTo(0), "蹴る子の狙いは送らない（守る人に見せない）");
        }

        [Test]
        public void Mirror_IgnoresSnapshotsOfAnotherShape()
        {
            var (match, plan, humans, events) = HostMatch(1f);
            var mirror = SoccerMatch.CreateMirror(SoccerSnapshot.Capture(1, match, plan, humans, null, events));
            var other = HostMatchWithFour();

            mirror.ApplySnapshot(other);

            Assert.That(mirror.Fits(other), Is.False);
            Assert.That(mirror.World.Players.Count, Is.EqualTo(6));
        }

        [Test]
        public void Mirror_FollowsAWholeFourOnFourMatch_ThroughTheCodec()
        {
            var plan = Plan(SoccerOnlineMode.Coop, null, PartyA, PartyB, PartyC, PartyD);
            var world = SoccerMatchSetup.CreateWorld(SoccerPitchTable.Standard(), plan.Species, 11, plan.PerTeam);
            var match = new SoccerMatch(world, 11);
            var ai = new SoccerAi(world, 12);
            ai.SetUsesSpecial(SoccerRules.Blue, true);
            var humans = new SoccerHumans(plan, world.Players.Count);
            var inputs = new SoccerInput[world.Players.Count];
            var events = new List<SoccerEvent>();
            var pending = new List<SoccerEvent>();
            match.Begin(pending);
            SoccerMatch mirror = null;
            var kicks = 0;
            for (var step = 0; step < Steps(SoccerRules.MatchSeconds + 60f) && !match.IsFinished; step++)
            {
                events.Clear();
                ai.ThinkMatch(match, inputs, _ => false);
                match.Step(inputs, events);
                pending.AddRange(events);
                if (step % 2 != 0)
                {
                    continue;
                }

                var snapshot = SoccerCodec.DecodeSnapshot(SoccerCodec.EncodeSnapshot(SoccerSnapshot.Capture(1, match, plan, humans, null, pending)));
                kicks += snapshot.Events.Count(e => e.Kind == SoccerEventKind.Kick);
                pending.Clear();
                if (mirror == null)
                {
                    mirror = SoccerMatch.CreateMirror(snapshot);
                }
                else
                {
                    mirror.ApplySnapshot(snapshot);
                }

                AssertSame(match, mirror);
            }

            Assert.That(world.Players.Count, Is.EqualTo(8));
            Assert.That(match.World.Score(SoccerRules.Red) + match.World.Score(SoccerRules.Blue) + (match.Shootout?.TotalKicks ?? 0), Is.GreaterThan(0), "4対4 でも試合が進む");
            Assert.That(kicks, Is.GreaterThan(10), "起きたことがスナップショットで届く");
        }

        private static SoccerSnapshot HostMatchWithFour()
        {
            var plan = Plan(SoccerOnlineMode.Coop, null, PartyA, PartyB, PartyC, PartyD);
            var match = PlayingMatch(plan);
            return SoccerSnapshot.Capture(1, match, plan, new SoccerHumans(plan, match.World.Players.Count), null, null);
        }

        private static void AssertSame(SoccerMatch host, SoccerMatch mirror)
        {
            Assert.That(mirror.Phase, Is.EqualTo(host.Phase));
            Assert.That(mirror.TimeLeft, Is.EqualTo(host.TimeLeft));
            Assert.That(mirror.World.Score(SoccerRules.Red), Is.EqualTo(host.World.Score(SoccerRules.Red)));
            Assert.That(mirror.World.Gauge(SoccerRules.Red), Is.EqualTo(host.World.Gauge(SoccerRules.Red)));
            Assert.That(mirror.World.Ball.Position, Is.EqualTo(host.World.Ball.Position));
            Assert.That(mirror.World.Ball.Holder, Is.EqualTo(host.World.Ball.Holder));
            for (var i = 0; i < host.World.Players.Count; i++)
            {
                Assert.That(mirror.World.Players[i].Position, Is.EqualTo(host.World.Players[i].Position));
                Assert.That(mirror.World.Players[i].Facing.X, Is.EqualTo(host.World.Players[i].Facing.X).Within(1e-4f));
                Assert.That(mirror.World.Players[i].State, Is.EqualTo(host.World.Players[i].State));
            }

            for (var i = 0; i < host.World.Keepers.Count; i++)
            {
                Assert.That(mirror.World.Keepers[i].Position, Is.EqualTo(host.World.Keepers[i].Position));
                Assert.That(mirror.World.Keepers[i].State, Is.EqualTo(host.World.Keepers[i].State));
            }
        }

        // ---- 待ちの窓の選び・MVP ----

        [Test]
        public void RoomChoice_RoundTrips()
        {
            var choice = new SoccerRoomChoice(3, SoccerOnlineMode.Versus, 4, new[] { SoccerRules.Blue, SoccerRules.Red, SoccerRules.Red, SoccerRules.Blue });

            var read = SoccerRoomChoice.Decode(choice.Encode());

            Assert.That(read.Stadium, Is.EqualTo(3));
            Assert.That(read.Mode, Is.EqualTo(SoccerOnlineMode.Versus));
            Assert.That(read.SeatCount, Is.EqualTo(4));
            Assert.That(read.Teams, Is.EqualTo(choice.Teams));
            Assert.That(read.CanStart, Is.True);
            Assert.That(SoccerRoomChoice.Decode(new byte[] { 1, 9, 2, 0 }), Is.Null, "知らない遊び方は読まない");
            Assert.That(new SoccerRoomChoice(1, SoccerOnlineMode.Versus, 1).CanStart, Is.False);
        }

        [Test]
        public void Mvp_IsTheTopScorers_OrNobody()
        {
            Assert.That(SoccerMvp.Find(new[] { 1, 3, 0, 2 }), Is.EqualTo(new[] { 1 }));
            Assert.That(SoccerMvp.Find(new[] { 2, 0, 2 }), Is.EqualTo(new[] { 0, 2 }));
            Assert.That(SoccerMvp.Find(new[] { 0, 0 }), Is.Empty);
        }
    }
}
