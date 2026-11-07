using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Tests
{
    /// <summary>野球の盤の上の計算（Docs/MINIGAME_BASEBALL_PLAN.md フェーズ 1）</summary>
    public class BaseballTests
    {
        private const float Dt = BaseballRules.StepSeconds;
        private const int MaxSteps = 1000;

        private static BaseballFieldDef Field => BaseballFieldTable.Test();

        /// <summary>投げて、入力を毎歩 input(歩の番号) で決めながら1球の結果が出るまで進め、起きたことを返す</summary>
        private static List<BaseballEvent> PlayPitch(BaseballBoard board, BaseballPitchType type, Func<int, BaseballInput> input)
        {
            var events = new List<BaseballEvent>();
            board.Throw(type, events);
            for (var step = 0; step < MaxSteps && board.Phase != BaseballBoardPhase.Done; step++)
            {
                board.Step(input(step), events);
            }

            Assert.That(board.Phase, Is.EqualTo(BaseballBoardPhase.Done), "1球の結果が出ない");
            return events;
        }

        private static List<BaseballEvent> PlayPitch(BaseballBoard board, BaseballPitchType type, float bend = 0f)
        {
            return PlayPitch(board, type, _ => new BaseballInput(bend, false, false));
        }

        /// <summary>swingStep 歩目に振る（まっすぐの球）</summary>
        private static BaseballBoard SwingAt(BaseballPitchType type, int swingStep)
        {
            var board = new BaseballBoard(Field);
            PlayPitch(board, type, step => new BaseballInput(0f, step == swingStep, false));
            return board;
        }

        /// <summary>振る歩を早い方から順に試し、当たったものだけ（振った歩, 盤）を返す</summary>
        private static List<(int Step, BaseballBoard Board)> ContactSweep(BaseballPitchType type)
        {
            var hits = new List<(int, BaseballBoard)>();
            for (var step = 0; step < 80; step++)
            {
                var board = SwingAt(type, step);
                if (board.LastHit.HasValue)
                {
                    hits.Add((step, board));
                }
            }

            return hits;
        }

        /// <summary>打球を盤の上で結果が出るまで転がす</summary>
        private static List<BaseballEvent> Roll(BaseballBallSim sim, Vec2 from, BaseballHit hit)
        {
            var events = new List<BaseballEvent>();
            sim.Launch(from, hit);
            for (var i = 0; i < MaxSteps && sim.IsActive; i++)
            {
                sim.Step(Dt, events);
            }

            Assert.That(sim.IsActive, Is.False, "打球の結果が出ない");
            return events;
        }

        /// <summary>転がる球がおよそ distance で止まる速さ（摩擦だけで見積もる。すり鉢の引きは小さいので入れない）</summary>
        private static float SpeedToStopAt(float distance) => MathF.Sqrt(2f * BaseballBalance.RollFriction * distance);

        private static BaseballHit Grounder(float degrees, float speed)
        {
            return new BaseballHit(BaseballFieldDef.At(degrees, 1f), speed, 0f, speed / BaseballBalance.MaxHitSpeed, false, false);
        }

        private static BaseballHit Fly(float degrees, float speed, float verticalSpeed)
        {
            return new BaseballHit(BaseballFieldDef.At(degrees, 1f), speed, verticalSpeed, 1f, true, false);
        }

        // ---- 定義表 ----

        [Test]
        public void Table_HasBaseballWithFiveFields()
        {
            var game = MiniGameTable.Get(MiniGameTable.BaseballId);
            Assert.That(game, Is.Not.Null);
            Assert.That(game.Id, Is.EqualTo("baseball"));
            Assert.That(game.SceneName, Is.EqualTo("BaseballScene"));
            Assert.That(game.CourseWord, Is.EqualTo("球場"));
            Assert.That(game.LowerIsBetter, Is.False);
            Assert.That(game.MaxPlayers, Is.EqualTo(4));
            Assert.That(game.Courses.Select(c => c.No), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
            Assert.That(game.Courses.Select(c => c.World), Is.EqualTo(new[] { 1, 2, 3, 4, 5 }));
        }

        [Test]
        public void Guide_AllStadiumsAreReady()
        {
            var guide = new BaseballCourseGuide();
            for (var no = 1; no <= 5; no++)
            {
                Assert.That(guide.IsReady(no), Is.True, $"球場{no} はフェーズ 6 で遊べる");
            }

            Assert.That(guide.Layout(1), Does.Contain("★☆☆☆☆"));
            Assert.That(guide.Layout(5), Does.Contain("★★★★★"));
            Assert.That(guide.Reward(1, true), Does.Contain("ボーナス"));

            Assert.That(guide.FormatBest(1, 3), Is.EqualTo("+3"));
            Assert.That(guide.FormatBest(1, 0), Is.EqualTo("±0"));
            Assert.That(guide.FormatBest(1, -1), Is.EqualTo("−1"));
        }

        [Test]
        public void Field_HolesAreFairAndDoNotOverlap()
        {
            var field = Field;
            foreach (var hole in field.Holes)
            {
                Assert.That(field.IsFair(hole.Center), Is.True, $"{hole.Result} の穴がファウルの溝にある");
                Assert.That(hole.Center.Length + hole.Radius, Is.LessThan(field.FenceRadius), $"{hole.Result} の穴がフェンスにかかる");
            }

            for (var i = 0; i < field.Holes.Count; i++)
            {
                for (var j = i + 1; j < field.Holes.Count; j++)
                {
                    var a = field.Holes[i];
                    var b = field.Holes[j];
                    Assert.That(Vec2.Distance(a.Center, b.Center), Is.GreaterThan(a.Radius + b.Radius), $"穴 {i} と {j} が重なる");
                }
            }
        }

        [Test]
        public void Field_StopZonesByDistance()
        {
            var field = Field;
            Assert.That(field.StopResult(new Vec2(0f, 3f)), Is.EqualTo(BaseballHitResult.GroundOut));
            Assert.That(field.StopResult(new Vec2(0f, 10f)), Is.EqualTo(BaseballHitResult.Single));
            Assert.That(field.StopResult(new Vec2(0f, 13.5f)), Is.EqualTo(BaseballHitResult.Double));
        }

        [Test]
        public void Field_FairWedge()
        {
            var field = Field;
            Assert.That(field.IsFair(new Vec2(0f, 5f)), Is.True);
            Assert.That(field.IsFair(BaseballFieldDef.At(40f, 5f)), Is.True);
            Assert.That(field.IsFair(BaseballFieldDef.At(50f, 5f)), Is.False);
            Assert.That(field.IsFair(BaseballFieldDef.At(-50f, 5f)), Is.False);
            Assert.That(field.IsFair(new Vec2(0f, -1f)), Is.False);
        }

        // ---- 投げる ----

        [TestCase(BaseballPitchType.Straight)]
        [TestCase(BaseballPitchType.Curve)]
        [TestCase(BaseballPitchType.Changeup)]
        public void Pitch_ReachesPlateInSpecSeconds(BaseballPitchType type)
        {
            var board = new BaseballBoard(Field);
            var events = new List<BaseballEvent>();
            board.Throw(type, events);
            var steps = 0;
            while (!events.Any(e => e.Kind == BaseballEventKind.CrossPlate) && steps < MaxSteps)
            {
                board.Step(BaseballInput.None, events);
                steps++;
            }

            var expected = BaseballBalance.Spec(type).PlateSeconds / Dt;
            Assert.That(steps, Is.EqualTo(expected).Within(1.01f));
        }

        [Test]
        public void Pitch_SpeedsDifferByType()
        {
            var straight = BaseballBalance.Spec(BaseballPitchType.Straight).PlateSeconds;
            var curve = BaseballBalance.Spec(BaseballPitchType.Curve).PlateSeconds;
            var changeup = BaseballBalance.Spec(BaseballPitchType.Changeup).PlateSeconds;
            Assert.That(straight, Is.LessThan(curve));
            Assert.That(curve, Is.LessThan(changeup));
        }

        private static float PlateX(BaseballPitchType type, float bend)
        {
            var board = new BaseballBoard(Field);
            var events = PlayPitch(board, type, bend);
            return events.Single(e => e.Kind == BaseballEventKind.CrossPlate).Position.X;
        }

        [Test]
        public void Pitch_BendMovesWhereItCrosses()
        {
            Assert.That(PlateX(BaseballPitchType.Curve, 0f), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(PlateX(BaseballPitchType.Curve, 1f), Is.GreaterThan(0.3f));
            Assert.That(PlateX(BaseballPitchType.Curve, -1f), Is.LessThan(-0.3f));
            // カーブはストレートより大きく曲がる
            Assert.That(PlateX(BaseballPitchType.Curve, 1f), Is.GreaterThan(PlateX(BaseballPitchType.Straight, 1f)));
        }

        [Test]
        public void Pitch_GoesStraightAfterBendableSection()
        {
            var board = new BaseballBoard(Field);
            var events = new List<BaseballEvent>();
            board.Throw(BaseballPitchType.Curve, events);
            while (!events.Any(e => e.Kind == BaseballEventKind.BendEnd))
            {
                board.Step(new BaseballInput(1f, false, false), events);
            }

            Assert.That(board.Pitch.CanBend, Is.False);
            var x = board.Pitch.Position.X;
            Assert.That(x, Is.GreaterThan(0f));
            // 曲げられる区間の後は、反対へ入力しても横へは動かない
            while (board.Phase == BaseballBoardPhase.Pitching)
            {
                board.Step(new BaseballInput(-1f, false, false), events);
                Assert.That(board.Pitch.Position.X, Is.EqualTo(x).Within(1e-5f));
            }
        }

        [Test]
        public void Pitch_BendEndsAtBendableFraction()
        {
            var field = Field;
            var pitch = new BaseballPitch(field);
            pitch.Start(BaseballPitchType.Straight);
            var expected = field.Mound.Y * (1f - BaseballBalance.BendableFraction);
            Assert.That(pitch.BendEndY, Is.EqualTo(expected).Within(1e-4f));
        }

        [Test]
        public void Pitch_StaysInsideGroove()
        {
            Assert.That(MathF.Abs(PlateX(BaseballPitchType.Curve, 1f)), Is.LessThanOrEqualTo(BaseballBalance.PitchMaxOffset));
        }

        // ---- ストライク・ボール ----

        [Test]
        public void Take_InZoneIsCalledStrike()
        {
            var board = new BaseballBoard(Field);
            var events = PlayPitch(board, BaseballPitchType.Straight);
            Assert.That(board.Outcome.Kind, Is.EqualTo(BaseballPitchOutcomeKind.CalledStrike));
            Assert.That(events.Single(e => e.Kind == BaseballEventKind.CrossPlate).Flag, Is.True);
            Assert.That(events.Any(e => e.Kind == BaseballEventKind.Catch), Is.True);
        }

        [Test]
        public void Take_OutsideZoneIsBall()
        {
            var board = new BaseballBoard(Field);
            var events = PlayPitch(board, BaseballPitchType.Curve, 1f);
            Assert.That(board.Outcome.Kind, Is.EqualTo(BaseballPitchOutcomeKind.Ball));
            Assert.That(events.Single(e => e.Kind == BaseballEventKind.CrossPlate).Flag, Is.False);
        }

        [Test]
        public void Swing_WayTooEarlyIsSwingingStrike()
        {
            var board = SwingAt(BaseballPitchType.Changeup, 0);
            Assert.That(board.LastHit, Is.Null);
            Assert.That(board.Outcome.Kind, Is.EqualTo(BaseballPitchOutcomeKind.SwingingStrike));
        }

        [Test]
        public void Swing_UnreachablePitchIsMissed()
        {
            // 外へ大きく曲げた球は、ミットに収まるまでのどの時刻に振ってもバットが届かない
            var catchStep = (int)(BaseballBalance.Spec(BaseballPitchType.Curve).PlateSeconds / Dt);
            for (var step = 0; step < catchStep; step++)
            {
                var board = new BaseballBoard(Field);
                PlayPitch(board, BaseballPitchType.Curve, s => new BaseballInput(1f, s == step, false));
                Assert.That(board.LastHit, Is.Null, $"{step} 歩目に振ったら当たった");
                Assert.That(board.Outcome.Kind, Is.EqualTo(BaseballPitchOutcomeKind.SwingingStrike));
            }
        }

        [Test]
        public void Swing_OnlyOncePerPitch()
        {
            var board = new BaseballBoard(Field);
            var events = PlayPitch(board, BaseballPitchType.Changeup, _ => new BaseballInput(0f, true, false));
            Assert.That(events.Count(e => e.Kind == BaseballEventKind.Swing), Is.EqualTo(1));
        }

        // ---- 振る ----

        [Test]
        public void Swing_EarlyPullsLateGoesOpposite()
        {
            var hits = ContactSweep(BaseballPitchType.Straight);
            Assert.That(hits.Count, Is.GreaterThanOrEqualTo(3), "当たる時刻がほとんど無い");
            var earliest = hits.First().Board.LastHit.Value;
            var latest = hits.Last().Board.LastHit.Value;
            // 右打ち: 早い＝引っ張り（左）、遅い＝流し（右）
            Assert.That(earliest.Direction.X, Is.LessThan(0f));
            Assert.That(latest.Direction.X, Is.GreaterThan(0f));
        }

        [Test]
        public void SwingHit_DirectionFollowsContactAngle()
        {
            Assert.That(BaseballBat.SwingHit(20f, BaseballBalance.SweetSpot).Direction.X, Is.LessThan(0f));
            Assert.That(BaseballBat.SwingHit(-20f, BaseballBalance.SweetSpot).Direction.X, Is.GreaterThan(0f));
            Assert.That(BaseballBat.SwingHit(0f, BaseballBalance.SweetSpot).Direction.X, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void SwingHit_SweetSpotIsStrongest()
        {
            var sweet = BaseballBat.SwingHit(0f, BaseballBalance.SweetSpot).Power;
            var handle = BaseballBat.SwingHit(0f, BaseballBalance.BatInner).Power;
            var tip = BaseballBat.SwingHit(0f, BaseballBalance.BatLength).Power;
            Assert.That(sweet, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(handle, Is.LessThan(sweet * 0.5f));
            Assert.That(tip, Is.LessThan(sweet * 0.5f));
        }

        [Test]
        public void SwingHit_JustAndSweetLofts()
        {
            Assert.That(BaseballBat.SwingHit(0f, BaseballBalance.SweetSpot).Lofted, Is.True);
            Assert.That(BaseballBat.SwingHit(0f, BaseballBalance.SweetSpot).VerticalSpeed, Is.GreaterThan(0f));
            // 遅れた・早すぎた・芯を外した当たりは転がる
            Assert.That(BaseballBat.SwingHit(BaseballBalance.JustDegrees + 5f, BaseballBalance.SweetSpot).Lofted, Is.False);
            Assert.That(BaseballBat.SwingHit(-BaseballBalance.JustDegrees - 5f, BaseballBalance.SweetSpot).Lofted, Is.False);
            Assert.That(BaseballBat.SwingHit(0f, BaseballBalance.BatLength).Lofted, Is.False);
            Assert.That(BaseballBat.SwingHit(0f, BaseballBalance.BatLength).VerticalSpeed, Is.EqualTo(0f));
        }

        [Test]
        public void SwingHit_JustIsStrongerThanLate()
        {
            Assert.That(BaseballBat.SwingHit(0f, BaseballBalance.SweetSpot).Speed,
                Is.GreaterThan(BaseballBat.SwingHit(-40f, BaseballBalance.SweetSpot).Speed));
        }

        [Test]
        public void Swing_JustDownTheMiddleCanHitHomeRun()
        {
            var hits = ContactSweep(BaseballPitchType.Changeup);
            Assert.That(hits.Any(h => h.Board.Outcome.Kind == BaseballPitchOutcomeKind.InPlay && h.Board.Outcome.Hit == BaseballHitResult.HomeRun), Is.True);
        }

        // ---- バント ----

        [Test]
        public void Bunt_RollsSoftlyAndCounts()
        {
            var board = new BaseballBoard(Field);
            var events = PlayPitch(board, BaseballPitchType.Straight, _ => new BaseballInput(0f, false, true));
            Assert.That(events.Count(e => e.Kind == BaseballEventKind.BuntSquare), Is.EqualTo(1));
            Assert.That(board.LastHit, Is.Not.Null);
            Assert.That(board.LastHit.Value.Bunt, Is.True);
            Assert.That(board.LastHit.Value.Lofted, Is.False);
            Assert.That(board.LastHit.Value.Speed, Is.LessThanOrEqualTo(BaseballBalance.BuntSpeed));
            Assert.That(board.Outcome.Bunt, Is.True);
            // まっすぐ当てたバントは内野の手前で止まる（ゴロ扱い）
            Assert.That(board.Outcome.Kind, Is.EqualTo(BaseballPitchOutcomeKind.InPlay));
            Assert.That(board.Outcome.Hit, Is.EqualTo(BaseballHitResult.GroundOut));
            Assert.That(board.Ball.Position.Length, Is.LessThan(4f));
        }

        [Test]
        public void Bunt_ReleasedBeforeBallIsTaken()
        {
            var board = new BaseballBoard(Field);
            PlayPitch(board, BaseballPitchType.Changeup, step => new BaseballInput(0f, false, step < 5));
            Assert.That(board.LastHit, Is.Null);
            Assert.That(board.Outcome.Kind, Is.EqualTo(BaseballPitchOutcomeKind.CalledStrike));
        }

        [Test]
        public void BuntHit_TipSendsRightHandleSendsLeft()
        {
            Assert.That(BaseballBat.BuntHit(BaseballBalance.BatLength).Direction.X, Is.GreaterThan(0f));
            Assert.That(BaseballBat.BuntHit(BaseballBalance.BatInner).Direction.X, Is.LessThan(0f));
            Assert.That(BaseballBat.BuntHit(BaseballBalance.BatLength).Speed, Is.LessThan(BaseballBat.BuntHit(BaseballBalance.SweetSpot).Speed));
        }

        // ---- 打球 ----

        [Test]
        public void Ball_HighFlyOverFenceIsHomeRun()
        {
            var sim = new BaseballBallSim(Field);
            var events = Roll(sim, Vec2.Zero, Fly(0f, BaseballBalance.LoftHorizontalSpeed, BaseballBalance.LoftVerticalSpeed));
            Assert.That(sim.Result, Is.EqualTo(BaseballHitResult.HomeRun));
            Assert.That(events.Any(e => e.Kind == BaseballEventKind.HomeRun), Is.True);
        }

        [Test]
        public void Ball_LowFlyBouncesOffFence()
        {
            var sim = new BaseballBallSim(Field);
            var events = Roll(sim, Vec2.Zero, Fly(10f, 14f, 6.2f));
            Assert.That(events.Any(e => e.Kind == BaseballEventKind.FenceHit), Is.True);
            Assert.That(sim.Result, Is.Not.EqualTo(BaseballHitResult.HomeRun));
            Assert.That(sim.Position.Length, Is.LessThan(Field.FenceRadius));
        }

        [Test]
        public void Ball_FlyingBallDoesNotFallIntoHoles()
        {
            // 内野の穴の上を越えていく高い球は、穴に落ちない
            var sim = new BaseballBallSim(Field);
            var events = new List<BaseballEvent>();
            sim.Launch(Vec2.Zero, Fly(0f, 6f, 9f));
            var passedOver = false;
            var holeCenter = Field.Holes.First(h => h.Result == BaseballHitResult.DoublePlay).Center;
            while (sim.IsActive && sim.IsFlying)
            {
                sim.Step(Dt, events);
                passedOver |= sim.Position.Y > holeCenter.Y;
            }

            Assert.That(passedOver, Is.True);
            Assert.That(events.Any(e => e.Kind == BaseballEventKind.HoleIn && e.Position.Y < holeCenter.Y + 0.01f && e.Result == BaseballHitResult.DoublePlay), Is.False);
        }

        [Test]
        public void Ball_SlowGrounderFallsIntoHole()
        {
            var sim = new BaseballBallSim(Field);
            var events = Roll(sim, Vec2.Zero, Grounder(0f, SpeedToStopAt(Field.Base(2).Y + 0.4f)));
            Assert.That(sim.Result, Is.EqualTo(BaseballHitResult.DoublePlay));
            Assert.That(sim.HoleIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(events.Single(e => e.Kind == BaseballEventKind.HoleIn).Index, Is.EqualTo(sim.HoleIndex));
        }

        [Test]
        public void Ball_FastGrounderSkipsOverHole()
        {
            var sim = new BaseballBallSim(Field);
            var dp = Field.Holes.ToList().FindIndex(h => h.Result == BaseballHitResult.DoublePlay);
            var events = Roll(sim, Vec2.Zero, Grounder(0f, SpeedToStopAt(Field.Base(2).Y + 4f)));
            Assert.That(events.Any(e => e.Kind == BaseballEventKind.LipOut && e.Index == dp), Is.True);
            Assert.That(sim.Result, Is.Not.EqualTo(BaseballHitResult.DoublePlay));
        }

        [Test]
        public void Ball_StopsAndTakesZoneResult()
        {
            var sim = new BaseballBallSim(Field);
            var events = Roll(sim, Vec2.Zero, Grounder(0f, 3f));
            Assert.That(sim.Result, Is.EqualTo(BaseballHitResult.GroundOut));
            Assert.That(events.Single(e => e.Kind == BaseballEventKind.Stop).Result, Is.EqualTo(BaseballHitResult.GroundOut));
        }

        [Test]
        public void Ball_StoppedInOutfieldIsSingle()
        {
            // 外野の穴の無い向き（フライの穴の間）へ、外野の芝で止まる強さ
            var sim = new BaseballBallSim(Field);
            Roll(sim, Vec2.Zero, Grounder(-8f, SpeedToStopAt(9.8f)));
            Assert.That(sim.Result, Is.EqualTo(BaseballHitResult.Single));
            Assert.That(sim.HoleIndex, Is.EqualTo(-1));
        }

        [Test]
        public void Ball_GrounderOutsideFoulLineIsFoul()
        {
            var sim = new BaseballBallSim(Field);
            var events = Roll(sim, Vec2.Zero, Grounder(60f, 8f));
            Assert.That(sim.Result, Is.EqualTo(BaseballHitResult.Foul));
            Assert.That(events.Any(e => e.Kind == BaseballEventKind.Foul), Is.True);
        }

        [Test]
        public void Ball_BackwardIsFoul()
        {
            var sim = new BaseballBallSim(Field);
            Roll(sim, Vec2.Zero, Grounder(180f, 6f));
            Assert.That(sim.Result, Is.EqualTo(BaseballHitResult.Foul));
        }

        [Test]
        public void Ball_FlyLandingOutsideFoulLineIsFoul()
        {
            var sim = new BaseballBallSim(Field);
            Roll(sim, Vec2.Zero, Fly(55f, 8f, 5f));
            Assert.That(sim.Result, Is.EqualTo(BaseballHitResult.Foul));
        }

        [Test]
        public void Ball_FairGrounderIsNotFoul()
        {
            var sim = new BaseballBallSim(Field);
            Roll(sim, Vec2.Zero, Grounder(38f, 6f));
            Assert.That(sim.Result, Is.Not.EqualTo(BaseballHitResult.Foul));
        }

        [Test]
        public void Board_FoulOutcomeFromHit()
        {
            // 早すぎる振りは引っ張りすぎてファウル（左の溝）になる時刻がある
            var hits = ContactSweep(BaseballPitchType.Straight);
            Assert.That(hits.Any(h => h.Board.Outcome.Kind == BaseballPitchOutcomeKind.Foul), Is.True);
            Assert.That(hits.All(h => h.Board.Outcome.Kind == BaseballPitchOutcomeKind.Foul || h.Board.Outcome.Kind == BaseballPitchOutcomeKind.InPlay), Is.True);
        }

        [Test]
        public void Board_ResetAllowsNextPitch()
        {
            var board = new BaseballBoard(Field);
            PlayPitch(board, BaseballPitchType.Straight);
            board.Reset();
            Assert.That(board.Phase, Is.EqualTo(BaseballBoardPhase.Ready));
            Assert.That(board.Outcome.IsDecided, Is.False);
            Assert.That(board.Bat.State, Is.EqualTo(BaseballBatState.Ready));
            PlayPitch(board, BaseballPitchType.Curve);
            Assert.That(board.Outcome.IsDecided, Is.True);
        }

        [Test]
        public void Board_SameInputsGiveSameResult()
        {
            // オンラインでホストとゲストが同じ道のりを描けるよう、同じ入力なら同じ結果
            BaseballInput Input(int step) => new BaseballInput(MathF.Sin(step * 0.3f), step == 30, false);
            var a = new BaseballBoard(Field);
            var b = new BaseballBoard(Field);
            PlayPitch(a, BaseballPitchType.Changeup, Input);
            PlayPitch(b, BaseballPitchType.Changeup, Input);
            Assert.That(a.Outcome.Kind, Is.EqualTo(b.Outcome.Kind));
            Assert.That(a.Outcome.Hit, Is.EqualTo(b.Outcome.Hit));
            Assert.That(a.BallPosition, Is.EqualTo(b.BallPosition));
        }
    }
}
