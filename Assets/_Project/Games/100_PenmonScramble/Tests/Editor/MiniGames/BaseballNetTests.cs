using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Tests
{
    /// <summary>野球のみんなで（Docs/MINIGAME_BASEBALL_PLAN.md フェーズ 5）: 打順の組み方・1球の記録と計算し直し・試合の写し・通信の中身</summary>
    public class BaseballNetTests
    {
        private const int MaxSteps = 600;
        private static readonly int[] PartyA = { 1, 2, 3 };
        private static readonly int[] PartyB = { 6, 7, 8 };
        private static readonly int[] PartyC = { 11, 12, 13 };
        private static readonly int[] PartyD = { 16, 17 };
        private static readonly int[] Wild = { 1, 6, 11 };

        private static BaseballFieldDef Field => BaseballFieldTable.Test();

        private static BaseballTeamPlan Plan(MiniGameOnlineMode mode, int[] teams, params int[][] parties)
        {
            return BaseballTeamPlan.Create(mode, teams, parties.Select(p => (IReadOnlyList<int>)p).ToList(), Wild);
        }

        private static BaseballThrow Pitch(BaseballPitchType type = BaseballPitchType.Curve, BaseballPitchPlan? plan = null) =>
            new BaseballThrow(1, type, BaseballPitchMagic.None, BaseballSpecial.None, plan);

        /// <summary>投げる人の曲げは bend、打つ人は swingStep 歩目に振る記録で、1球を結果まで進める</summary>
        private static BaseballPitchSession Play(BaseballThrow pitch, float bend, int swingStep)
        {
            var session = new BaseballPitchSession(Field);
            var events = new List<BaseballEvent>();
            session.Begin(pitch, events);
            for (var step = 0; step < MaxSteps && !session.IsDone; step++)
            {
                session.PitcherLog.Record(bend, false, false, 0f);
                session.BatterLog.Record(0f, false, step == swingStep, 0f);
                session.Advance(events);
            }

            Assert.That(session.IsDone, Is.True, "1球の結果が出ない");
            return session;
        }

        /// <summary>振ると当たる歩（曲げは bend）</summary>
        private static int ContactStep(BaseballThrow pitch, float bend)
        {
            for (var step = 0; step < 80; step++)
            {
                if (Play(pitch, bend, step).Board.LastHit.HasValue)
                {
                    return step;
                }
            }

            Assert.Fail("当たる歩が無い");
            return -1;
        }

        // ---- 打順の組み方 ----

        [Test]
        public void Plan_Solo_IsThePartyAgainstTheWild()
        {
            var plan = BaseballTeamPlan.Solo(PartyA, Wild, BaseballRules.Home);

            Assert.That(plan.Lineup(BaseballRules.Home), Is.EqualTo(PartyA));
            Assert.That(plan.Lineup(BaseballRules.Away), Is.EqualTo(Wild));
            Assert.That(plan.OwnerOf(BaseballRules.Home, 2), Is.EqualTo(0));
            Assert.That(plan.OwnerOf(BaseballRules.Away, 0), Is.EqualTo(-1));
            Assert.That(plan.IsCpuTeam(BaseballRules.Away), Is.True);
            Assert.That(plan.TeamOf(0), Is.EqualTo(BaseballRules.Home));
        }

        [Test]
        public void Plan_TwoHumanTeam_AlternatesA1B1A2B2()
        {
            // P1・P3 が赤（先攻）、P2 が青
            var plan = Plan(MiniGameOnlineMode.Versus, null, PartyA, PartyB, PartyC);

            Assert.That(plan.Lineup(MiniGameTeams.Red), Is.EqualTo(new[] { 1, 11, 2, 12 }));
            Assert.That(Enumerable.Range(0, 4).Select(i => plan.OwnerOf(MiniGameTeams.Red, i)), Is.EqualTo(new[] { 0, 2, 0, 2 }));
            Assert.That(plan.Lineup(MiniGameTeams.Blue), Is.EqualTo(PartyB), "1人のチームは編成の3体");
        }

        [Test]
        public void Plan_ThreeOrMoreHumans_UseEachLeader()
        {
            var plan = Plan(MiniGameOnlineMode.Coop, null, PartyA, PartyB, PartyC, PartyD);

            Assert.That(plan.Lineup(MiniGameTeams.Red), Is.EqualTo(new[] { 1, 6, 11, 16 }));
            Assert.That(plan.Lineup(MiniGameTeams.Blue), Is.EqualTo(Wild), "協力の相手は CPU");
            Assert.That(plan.IsCpuTeam(MiniGameTeams.Blue), Is.True);
        }

        [Test]
        public void Plan_MissingPartyMembers_AreSkipped_EmptyPartyGetsAStandIn()
        {
            var plan = Plan(MiniGameOnlineMode.Versus, new[] { 0, 0, 1 }, new[] { 6 }, null, PartyC);

            Assert.That(plan.Lineup(MiniGameTeams.Red), Is.EqualTo(new[] { 6, BaseballTeamPlan.FallbackSpecies }), "A1・B1 だけ（A2・B2 はいない）");
        }

        [Test]
        public void Plan_PitchingSeat_RotatesAmongTheTeamPerPlate_AndPicksOwnSlots()
        {
            var plan = Plan(MiniGameOnlineMode.Versus, null, PartyA, PartyB, PartyC);

            Assert.That(plan.PitchingSeat(MiniGameTeams.Red, 0), Is.EqualTo(0));
            Assert.That(plan.PitchingSeat(MiniGameTeams.Red, 1), Is.EqualTo(2));
            Assert.That(plan.PitchingSeat(MiniGameTeams.Red, 2), Is.EqualTo(0));
            Assert.That(plan.PitchingSeat(MiniGameTeams.Blue, 5), Is.EqualTo(1));
            Assert.That(plan.CanPitch(2, MiniGameTeams.Red, 1), Is.True);
            Assert.That(plan.CanPitch(2, MiniGameTeams.Red, 0), Is.False, "なかまの子は選べない");
            Assert.That(plan.FirstPitchable(2, MiniGameTeams.Red), Is.EqualTo(1));
        }

        [Test]
        public void Plan_RemoveSeat_GivesTheirSlotsToTheCpu()
        {
            var plan = Plan(MiniGameOnlineMode.Versus, null, PartyA, PartyB, PartyC);

            plan.RemoveSeat(2);
            Assert.That(plan.OwnerOf(MiniGameTeams.Red, 1), Is.EqualTo(-1));
            Assert.That(plan.PitchingSeat(MiniGameTeams.Red, 1), Is.EqualTo(0), "投げる番は残っている人で回す");

            plan.RemoveSeat(1);
            Assert.That(plan.IsCpuTeam(MiniGameTeams.Blue), Is.True);
            Assert.That(plan.PitchingSeat(MiniGameTeams.Blue, 0), Is.EqualTo(-1));
            Assert.That(plan.CanPitch(-1, MiniGameTeams.Blue, 2), Is.True);
        }

        [Test]
        public void CoopCpuStars_AddOnePerExtraHuman()
        {
            Assert.That(BaseballBalance.CoopCpuStars(1, 1), Is.EqualTo(1));
            Assert.That(BaseballBalance.CoopCpuStars(1, 3), Is.EqualTo(3));
            Assert.That(BaseballBalance.CoopCpuStars(4, 4), Is.EqualTo(BaseballBalance.AiMaxStars));
        }

        // ---- 入力の記録 ----

        [Test]
        public void RoleLog_RoundsValues_AndHoldsTheLastValueBeyondProgress()
        {
            var log = new BaseballRoleLog();
            log.Record(0.5f, false, false, 0f);
            log.Record(-1f, true, true, BaseballRules.StepSeconds * 0.5f);
            log.Record(-1f, true, true, 0f);

            Assert.That(log.Progress, Is.EqualTo(3));
            Assert.That(log.BendAt(0), Is.EqualTo(BaseballRoleLog.DequantizeBend(BaseballRoleLog.QuantizeBend(0.5f))));
            Assert.That(log.BendAt(10), Is.EqualTo(-1f), "先の歩は最後の値が続く");
            Assert.That(log.BuntAt(10), Is.True);
            Assert.That(log.SwingStep, Is.EqualTo(1), "振るのは1球に1回");
            Assert.That(log.SwingTimeAt(1), Is.EqualTo(BaseballRules.StepSeconds * 0.5f).Within(BaseballRules.StepSeconds / 100f));
            Assert.That(log.SwingAt(5), Is.False);
        }

        [Test]
        public void RoleLog_MergesOnlyLongerLogs_AndSealFillsToTheEnd()
        {
            var log = new BaseballRoleLog();
            log.Record(0.2f, false, false, 0f);
            log.Record(0.2f, false, false, 0f);
            var shorter = new BaseballRoleLog();
            shorter.Record(1f, false, false, 0f);

            Assert.That(log.Merge(shorter), Is.False, "古い記録で戻さない");
            log.Seal();
            Assert.That(log.Progress, Is.EqualTo(BaseballRoleLog.MaxSteps));
            Assert.That(log.BendAt(BaseballRoleLog.MaxSteps - 1), Is.EqualTo(log.BendAt(0)));
        }

        // ---- 1球の計算 ----

        [Test]
        public void Session_SameThrowAndLogs_GiveTheSamePitch()
        {
            var pitch = Pitch();
            var swing = ContactStep(pitch, 0.15f);

            var a = Play(pitch, 0.15f, swing);
            var b = Play(pitch, 0.15f, swing);

            Assert.That(a.Board.Outcome.Kind, Is.EqualTo(b.Board.Outcome.Kind));
            Assert.That(a.Board.Outcome.Hit, Is.EqualTo(b.Board.Outcome.Hit));
            Assert.That(a.Board.Ball.Position.X, Is.EqualTo(b.Board.Ball.Position.X));
            Assert.That(a.DoneStep, Is.EqualTo(b.DoneStep));
        }

        [Test]
        public void Session_CpuPitch_BendsFromThePlan_WithoutAPitcherLog()
        {
            var plan = new BaseballPitchPlan(BaseballPitchType.Curve, BaseballBendPlan.Away, 0.6f);
            var session = Play(Pitch(BaseballPitchType.Curve, plan), 0f, -1);

            Assert.That(session.PitcherNeeded, Is.EqualTo(0), "CPU の曲げは送らない");
            Assert.That(session.Board.Pitch.Position.X, Is.GreaterThan(0.3f), "組み立てどおり外へ曲がる");
        }

        [Test]
        public void Session_LateSwing_ReplaysToTheSameResult_WithoutRepeatingEvents()
        {
            var pitch = Pitch();
            var swing = ContactStep(pitch, 0f);
            var expected = Play(pitch, 0f, swing);

            // 打つ人の記録が届かないまま進め（振っていないと見なす）、あとから振った記録が届く
            var late = new BaseballPitchSession(Field);
            var events = new List<BaseballEvent>();
            late.Begin(pitch, events);
            while (late.Step < swing + 8)
            {
                late.PitcherLog.Record(0f, false, false, 0f);
                late.Advance(events);
            }

            Assert.That(late.InputsComplete, Is.False);
            var remote = new BaseballRoleLog();
            for (var step = 0; step < swing + 8; step++)
            {
                remote.Record(0f, false, step == swing, 0f);
            }

            late.MergeBatter(remote);
            for (var step = 0; step < MaxSteps && !late.IsDone; step++)
            {
                late.PitcherLog.Record(0f, false, false, 0f);
                remote.Record(0f, false, false, 0f);
                late.MergeBatter(remote);
                late.Advance(events);
            }

            Assert.That(late.Board.Outcome.Hit, Is.EqualTo(expected.Board.Outcome.Hit));
            Assert.That(late.DoneStep, Is.EqualTo(expected.DoneStep));
            Assert.That(events.Count(e => e.Kind == BaseballEventKind.Pitch), Is.EqualTo(1), "計算し直しても投げた音は1回");
            Assert.That(events.Count(e => e.Kind == BaseballEventKind.Contact), Is.EqualTo(1));
            Assert.That(late.Replays, Is.GreaterThan(0));
        }

        [Test]
        public void Session_InputsComplete_WaitsForTheBatterUpToTheResult()
        {
            var session = new BaseballPitchSession(Field);
            var events = new List<BaseballEvent>();
            session.Begin(Pitch(BaseballPitchType.Straight), events);
            for (var step = 0; step < MaxSteps && !session.IsDone; step++)
            {
                session.PitcherLog.Record(0f, false, false, 0f);
                session.Advance(events);
            }

            Assert.That(session.IsDone, Is.True);
            Assert.That(session.InputsComplete, Is.False, "打つ人がまだ振っていないと知らせてこない");

            var batter = new BaseballRoleLog();
            for (var step = 0; step < session.BatterNeeded; step++)
            {
                batter.Record(0f, false, false, 0f);
            }

            session.MergeBatter(batter);
            Assert.That(session.InputsComplete, Is.True);
        }

        [Test]
        public void Session_PitcherBendArrivingLate_ChangesWhereTheBallCrosses()
        {
            var session = new BaseballPitchSession(Field);
            var events = new List<BaseballEvent>();
            session.Begin(Pitch(), events);
            for (var step = 0; step < 10; step++)
            {
                session.BatterLog.Record(0f, false, false, 0f);
                session.Advance(events);
            }

            var straightX = session.Board.Pitch.Position.X;
            var bent = new BaseballRoleLog();
            for (var step = 0; step < 10; step++)
            {
                bent.Record(1f, false, false, 0f);
            }

            session.MergePitcher(bent);
            session.BatterLog.Record(0f, false, false, 0f);
            session.Advance(events);

            Assert.That(session.Board.Pitch.Position.X, Is.GreaterThan(straightX + 0.05f), "届いた曲げで計算し直す");
        }

        // ---- 試合の写し ----

        [Test]
        public void GameState_RestoresIntoAFreshGame()
        {
            var game = new BaseballGame(4, 3);
            game.ChoosePitcher(2);
            game.UseMagic();
            game.Apply(new BaseballPitchOutcome(BaseballPitchOutcomeKind.InPlay, BaseballHitResult.Double, false));
            game.Apply(new BaseballPitchOutcome(BaseballPitchOutcomeKind.Ball, BaseballHitResult.Single, false));
            game.Apply(new BaseballPitchOutcome(BaseballPitchOutcomeKind.InPlay, BaseballHitResult.Single, false));

            var copy = new BaseballGame(4, 3);
            copy.Restore(game.Capture());

            Assert.That(copy.Score(BaseballRules.Away), Is.EqualTo(game.Score(BaseballRules.Away)));
            Assert.That(copy.CurrentBatter, Is.EqualTo(game.CurrentBatter));
            Assert.That(copy.PitcherSlot(BaseballRules.Home), Is.EqualTo(2));
            Assert.That(copy.IsMagicUsed(BaseballRules.Home, 2), Is.True);
            Assert.That(copy.Bases.RunnerOn(1), Is.EqualTo(game.Bases.RunnerOn(1)));
            Assert.That(copy.Bases.RunnerOn(3), Is.EqualTo(game.Bases.RunnerOn(3)));
            Assert.That(copy.LastMoves.Count, Is.EqualTo(game.LastMoves.Count));
            Assert.That(copy.IsPlateStart, Is.EqualTo(game.IsPlateStart));
        }

        // ---- 通信の中身 ----

        [Test]
        public void Snapshot_RoundTrips()
        {
            var game = new BaseballGame(3, 4);
            game.Apply(new BaseballPitchOutcome(BaseballPitchOutcomeKind.InPlay, BaseballHitResult.Triple, false));
            var pitcherLog = new BaseballRoleLog();
            pitcherLog.Record(0.3f, false, false, 0f);
            pitcherLog.Record(-0.7f, false, false, 0f);
            var batterLog = new BaseballRoleLog();
            batterLog.Record(0f, true, false, 0f);
            batterLog.Record(0.4f, true, true, -0.01f);
            var magic = new BaseballPitchMagic(new BaseballSpecial(Element.Water, 2), 0.4f, 1.25f);
            var s = new BaseballSnapshot
            {
                Stadium = 1,
                Mode = MiniGameOnlineMode.Coop,
                CpuStars = 3,
                SeatTeam = new[] { 0, 0 },
                SeatLeft = new[] { false, true },
                SeatRbi = new[] { 2, 0 },
                Lineup = new[] { new[] { 1, 6, 2 }, new[] { 11, 12, 13, 14 } },
                Owner = new[] { new[] { 0, 1, 0 }, new[] { -1, -1, -1, -1 } },
                State = game.Capture(),
                Stage = BaseballStage.Result,
                ThrowCount = 7,
                PitcherSeat = 1,
                HasThrow = true,
                Throw = new BaseballThrow(7, BaseballPitchType.Changeup, magic, new BaseballSpecial(Element.Wind, 3),
                    new BaseballPitchPlan(BaseballPitchType.Changeup, BaseballBendPlan.Late, -0.25f)),
                PitcherLog = pitcherLog,
                BatterLog = batterLog,
                Committed = true,
                DoneStep = 42,
                Outcome = new BaseballPitchOutcome(BaseballPitchOutcomeKind.InPlay, BaseballHitResult.Triple, false),
                Plate = BaseballPlateResult.Hit,
                Runs = 1,
            };

            var read = BaseballCodec.DecodeSnapshot(BaseballCodec.EncodeSnapshot(s));

            Assert.That(read, Is.Not.Null);
            Assert.That(read.Mode, Is.EqualTo(MiniGameOnlineMode.Coop));
            Assert.That(read.SeatLeft, Is.EqualTo(s.SeatLeft));
            Assert.That(read.SeatRbi, Is.EqualTo(s.SeatRbi));
            Assert.That(read.Lineup[1], Is.EqualTo(s.Lineup[1]));
            Assert.That(read.Owner[0], Is.EqualTo(s.Owner[0]));
            Assert.That(read.State.Runners, Is.EqualTo(s.State.Runners));
            Assert.That(read.State.InningRuns[0], Is.EqualTo(s.State.InningRuns[0]));
            Assert.That(read.State.LastMoves.Count, Is.EqualTo(s.State.LastMoves.Count));
            Assert.That(read.Stage, Is.EqualTo(BaseballStage.Result));
            Assert.That(read.Throw.SamePitch(s.Throw), Is.True);
            Assert.That(read.Throw.HitSpecial.Element, Is.EqualTo(Element.Wind));
            Assert.That(read.Throw.Plan.Value.Bend, Is.EqualTo(BaseballBendPlan.Late));
            Assert.That(read.PitcherLog.Bends, Is.EqualTo(pitcherLog.Bends));
            Assert.That(read.BatterLog.Bunts, Is.EqualTo(batterLog.Bunts));
            Assert.That(read.BatterLog.SwingStep, Is.EqualTo(1));
            Assert.That(read.BatterLog.SwingTime, Is.EqualTo(batterLog.SwingTime));
            Assert.That(read.DoneStep, Is.EqualTo(42));
            Assert.That(read.Outcome.Hit, Is.EqualTo(BaseballHitResult.Triple));
            Assert.That(read.Plate, Is.EqualTo(BaseballPlateResult.Hit));
        }

        [Test]
        public void Input_AndParty_RoundTrip_BadDataIsIgnored()
        {
            var log = new BaseballRoleLog();
            for (var i = 0; i < 300; i++)
            {
                log.Record(i < 200 ? 0f : 0.5f, false, false, 0f);
            }

            var input = new BaseballNetInput { ThrowId = 12, PitcherSlot = 2, SpecialOn = true, Thrown = true, Type = BaseballPitchType.Curve, MagicOn = true, FreezeSeconds = 0.5f, WobblePhase = 2f, Log = log };
            var bytes = BaseballCodec.EncodeInput(input);

            Assert.That(BaseballCodec.TryDecodeInput(bytes, out var read), Is.True);
            Assert.That(read.ThrowId, Is.EqualTo(12));
            Assert.That(read.PitcherSlot, Is.EqualTo(2));
            Assert.That(read.Type, Is.EqualTo(BaseballPitchType.Curve));
            Assert.That(read.Log.Bends, Is.EqualTo(log.Bends));
            Assert.That(bytes.Length, Is.LessThan(64), "同じ値が続く記録は詰めて送る");

            Assert.That(BaseballCodec.TryDecodeParty(BaseballCodec.EncodeParty(PartyD), out var party), Is.True);
            Assert.That(party, Is.EqualTo(PartyD));

            bytes[0] = (byte)(BaseballCodec.FormatVersion + 1);
            Assert.That(BaseballCodec.TryDecodeInput(bytes, out _), Is.False, "違う版は読まない");
            Assert.That(BaseballCodec.DecodeSnapshot(new byte[] { BaseballCodec.FormatVersion, 1 }), Is.Null, "短い物は読まない");
        }
    }
}
