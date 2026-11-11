using System;
using System.Collections.Generic;
using NUnit.Framework;
using PenmonScramble.Baseball.Core;

namespace PenmonScramble.Tests
{
    /// <summary>野球のカウント・振った時刻・CPU</summary>
    public class BaseballCountAiTests
    {
        private const float Dt = BaseballRules.StepSeconds;
        private const int MaxSteps = 1000;

        private static BaseballFieldDef Field => BaseballFieldTable.Test();

        private static BaseballPitchOutcome Pitch(BaseballPitchOutcomeKind kind) => new BaseballPitchOutcome(kind, BaseballHitResult.Single, false);

        private static BaseballPitchOutcome InPlay(BaseballHitResult hit) => new BaseballPitchOutcome(BaseballPitchOutcomeKind.InPlay, hit, false);

        // ---- カウント ----

        [Test]
        public void Count_ThreeStrikesIsStrikeout()
        {
            var count = new BaseballCount();
            Assert.That(count.Apply(Pitch(BaseballPitchOutcomeKind.CalledStrike)), Is.EqualTo(BaseballPlateResult.Continue));
            Assert.That(count.Apply(Pitch(BaseballPitchOutcomeKind.SwingingStrike)), Is.EqualTo(BaseballPlateResult.Continue));
            Assert.That(count.Strikes, Is.EqualTo(2));
            Assert.That(count.Apply(Pitch(BaseballPitchOutcomeKind.CalledStrike)), Is.EqualTo(BaseballPlateResult.Strikeout));
            Assert.That(count.Outs, Is.EqualTo(1));
            Assert.That(count.Strikes, Is.EqualTo(0));
            Assert.That(count.Balls, Is.EqualTo(0));
        }

        [Test]
        public void Count_FourBallsIsWalk()
        {
            var count = new BaseballCount();
            for (var i = 0; i < BaseballRules.BallsForWalk - 1; i++)
            {
                Assert.That(count.Apply(Pitch(BaseballPitchOutcomeKind.Ball)), Is.EqualTo(BaseballPlateResult.Continue));
            }

            count.Apply(Pitch(BaseballPitchOutcomeKind.CalledStrike));
            Assert.That(count.Apply(Pitch(BaseballPitchOutcomeKind.Ball)), Is.EqualTo(BaseballPlateResult.Walk));
            Assert.That(count.Outs, Is.EqualTo(0));
            Assert.That(count.Balls, Is.EqualTo(0));
            Assert.That(count.Strikes, Is.EqualTo(0));
        }

        [Test]
        public void Count_FoulAddsStrikeOnlyBelowTwo()
        {
            var count = new BaseballCount();
            count.Apply(Pitch(BaseballPitchOutcomeKind.Foul));
            Assert.That(count.Strikes, Is.EqualTo(1));
            count.Apply(Pitch(BaseballPitchOutcomeKind.Foul));
            Assert.That(count.Strikes, Is.EqualTo(2));
            for (var i = 0; i < 5; i++)
            {
                Assert.That(count.Apply(Pitch(BaseballPitchOutcomeKind.Foul)), Is.EqualTo(BaseballPlateResult.Continue));
            }

            Assert.That(count.Strikes, Is.EqualTo(2));
            Assert.That(count.Outs, Is.EqualTo(0));
        }

        [Test]
        public void Count_InPlayHitAndOut()
        {
            var count = new BaseballCount();
            count.Apply(Pitch(BaseballPitchOutcomeKind.Ball));
            Assert.That(count.Apply(InPlay(BaseballHitResult.Double)), Is.EqualTo(BaseballPlateResult.Hit));
            Assert.That(count.Balls, Is.EqualTo(0));
            Assert.That(count.Outs, Is.EqualTo(0));
            Assert.That(count.Apply(InPlay(BaseballHitResult.HomeRun)), Is.EqualTo(BaseballPlateResult.Hit));
            Assert.That(count.Apply(InPlay(BaseballHitResult.FlyOut)), Is.EqualTo(BaseballPlateResult.Out));
            Assert.That(count.Apply(InPlay(BaseballHitResult.DoublePlay)), Is.EqualTo(BaseballPlateResult.Out));
            Assert.That(count.Outs, Is.EqualTo(2));
        }

        [Test]
        public void Count_ThreeOutsEndsInningAndClears()
        {
            var count = new BaseballCount();
            count.Apply(InPlay(BaseballHitResult.GroundOut));
            count.Apply(InPlay(BaseballHitResult.GroundOut));
            Assert.That(count.IsInningOver, Is.False);
            count.Apply(InPlay(BaseballHitResult.GroundOut));
            Assert.That(count.IsInningOver, Is.True);
            count.ClearOuts();
            Assert.That(count.Outs, Is.EqualTo(0));
            Assert.That(count.IsInningOver, Is.False);
        }

        [Test]
        public void Results_PlateLabels()
        {
            Assert.That(BaseballResults.Label(BaseballPlateResult.Strikeout), Is.EqualTo("三振！"));
            Assert.That(BaseballResults.Label(BaseballPlateResult.Walk), Is.EqualTo("フォアボール"));
            Assert.That(BaseballResults.Label(BaseballPlateResult.Hit), Is.Empty);
        }

        // ---- 振った時刻（歩の中まで） ----

        /// <summary>ちょうど振り始めた瞬間からの秒で振る（歩の刻みに関係なく）。当たった角度を返す（当たらなければ null）</summary>
        private static float? ContactDegreesSwingingAt(BaseballPitchType type, float swingSeconds)
        {
            var board = new BaseballBoard(Field);
            var events = new List<BaseballEvent>();
            board.Throw(type, events);
            var swingStep = (int)MathF.Floor(swingSeconds / Dt);
            var degrees = (float?)null;
            for (var step = 0; step < MaxSteps && board.Phase == BaseballBoardPhase.Pitching; step++)
            {
                var swing = step == swingStep;
                board.Step(new BaseballInput(0f, swing, false, swingSeconds - step * Dt), events);
                if (board.LastHit.HasValue && degrees == null)
                {
                    degrees = board.Bat.Degrees;
                }
            }

            return degrees;
        }

        [Test]
        public void Board_SwingTimeInsideStepChangesContactSmoothly()
        {
            // 歩の中の時刻を変えると当たる角度も細かく変わる（歩の刻みの 36° ずつ飛ばない）
            var seen = new List<float>();
            var plate = BaseballBalance.Spec(BaseballPitchType.Curve).PlateSeconds;
            for (var t = plate - 0.12f; t < plate; t += 0.004f)
            {
                var degrees = ContactDegreesSwingingAt(BaseballPitchType.Curve, t);
                if (degrees.HasValue)
                {
                    seen.Add(degrees.Value);
                }
            }

            Assert.That(seen.Count, Is.GreaterThan(5));
            seen.Sort();
            for (var i = 1; i < seen.Count; i++)
            {
                Assert.That(seen[i] - seen[i - 1], Is.LessThan(12f), "当たる角度が大きく飛んでいる");
            }
        }

        [Test]
        public void Board_NegativeSwingTimeMeansAlreadySwinging()
        {
            // 同じ瞬間に振った扱いなら、前の歩の途中（負の時刻）で渡しても、次の歩の始めで渡しても同じ所で当たる
            var plate = BaseballBalance.Spec(BaseballPitchType.Changeup).PlateSeconds;
            var swingSeconds = plate - BaseballBat.SecondsToSquare - 0.3f / (Field.Mound.Y / plate);
            var a = ContactDegreesSwingingAt(BaseballPitchType.Changeup, swingSeconds);

            var board = new BaseballBoard(Field);
            var events = new List<BaseballEvent>();
            board.Throw(BaseballPitchType.Changeup, events);
            var nextStep = (int)MathF.Floor(swingSeconds / Dt) + 1;
            float? b = null;
            for (var step = 0; step < MaxSteps && board.Phase == BaseballBoardPhase.Pitching; step++)
            {
                board.Step(new BaseballInput(0f, step == nextStep, false, swingSeconds - step * Dt), events);
                if (board.LastHit.HasValue && b == null)
                {
                    b = board.Bat.Degrees;
                }
            }

            Assert.That(a.HasValue && b.HasValue, Is.True);
            Assert.That(b.Value, Is.EqualTo(a.Value).Within(6f));
        }

        // ---- CPU ----

        /// <summary>CPU の組み立てで1球投げ、ホームの線を越えた横の位置を返す</summary>
        private static float PlateXOf(BaseballPitchPlan plan)
        {
            var field = Field;
            var board = new BaseballBoard(field);
            var events = new List<BaseballEvent>();
            board.Throw(plan.Type, events);
            for (var step = 0; step < MaxSteps && board.Phase == BaseballBoardPhase.Pitching; step++)
            {
                board.Step(new BaseballInput(BaseballAi.Bend(board.Pitch, plan, field), false, false), events);
                foreach (var e in events)
                {
                    if (e.Kind == BaseballEventKind.CrossPlate)
                    {
                        return e.Position.X;
                    }
                }
            }

            Assert.Fail("ホームの線を越えない");
            return 0f;
        }

        [TestCase(BaseballPitchType.Straight, 0.3f)]
        [TestCase(BaseballPitchType.Curve, 0.45f)]
        [TestCase(BaseballPitchType.Curve, -0.45f)]
        [TestCase(BaseballPitchType.Changeup, 0.45f)]
        public void Ai_PitchReachesTarget(BaseballPitchType type, float target)
        {
            var bend = target > 0f ? BaseballBendPlan.Away : BaseballBendPlan.Inside;
            Assert.That(PlateXOf(new BaseballPitchPlan(type, bend, target)), Is.EqualTo(target).Within(0.12f));
            // ストレートは曲げにくいので、後半だけでは端まで届かない（届くのはカーブ・チェンジアップ）
            if (type != BaseballPitchType.Straight)
            {
                Assert.That(PlateXOf(new BaseballPitchPlan(type, BaseballBendPlan.Late, target)), Is.EqualTo(target).Within(0.15f));
            }
        }

        [Test]
        public void Ai_StraightPlanDoesNotBend()
        {
            Assert.That(PlateXOf(new BaseballPitchPlan(BaseballPitchType.Curve, BaseballBendPlan.Straight, 0.5f)), Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void Ai_LatePlanWaitsFirstHalf()
        {
            var field = Field;
            var board = new BaseballBoard(field);
            board.Throw(BaseballPitchType.Curve, new List<BaseballEvent>());
            var plan = new BaseballPitchPlan(BaseballPitchType.Curve, BaseballBendPlan.Late, 0.45f);
            Assert.That(BaseballAi.Bend(board.Pitch, plan, field), Is.EqualTo(0f));
        }

        [Test]
        public void Ai_PlanPitchStaysNearZone()
        {
            var field = Field;
            var ai = new BaseballAi(3, BaseballBalance.AiTest);
            for (var i = 0; i < 200; i++)
            {
                var plan = ai.PlanPitch(field);
                Assert.That(MathF.Abs(plan.TargetX), Is.LessThan(field.ZoneHalfWidth + BaseballBalance.AiWasteMargin + 0.6f));
            }
        }

        /// <summary>CPU が打つ側で1球（まっすぐの球を x の所に通す）</summary>
        private static BaseballBoard CpuBats(BaseballAiSkill skill, BaseballPitchType type, float plateX, int seed = 1)
        {
            var field = Field;
            var board = new BaseballBoard(field);
            var ai = new BaseballAi(seed, skill);
            var events = new List<BaseballEvent>();
            var plan = new BaseballPitchPlan(type, plateX > 0f ? BaseballBendPlan.Away : BaseballBendPlan.Inside, plateX);
            board.Throw(type, events);
            for (var step = 0; step < MaxSteps && board.Phase != BaseballBoardPhase.Done; step++)
            {
                var swing = ai.TrySwing(board, out var swingTime);
                board.Step(new BaseballInput(BaseballAi.Bend(board.Pitch, plan, field), swing, false, swingTime), events);
            }

            return board;
        }

        [TestCase(BaseballPitchType.Straight)]
        [TestCase(BaseballPitchType.Curve)]
        [TestCase(BaseballPitchType.Changeup)]
        public void Ai_PerfectBatterHitsJust(BaseballPitchType type)
        {
            // ぶれの無い CPU はゾーンの中の球をジャストで（浮くほど強く）打ち返す
            var board = CpuBats(new BaseballAiSkill(0f, 1f, 0f), type, 0.05f);
            Assert.That(board.LastHit.HasValue, Is.True, "当たらない");
            Assert.That(MathF.Abs(board.Bat.Degrees), Is.LessThan(BaseballBalance.JustDegrees));
        }

        [Test]
        public void Ai_TakesBallOutsideZone()
        {
            var board = CpuBats(new BaseballAiSkill(0f, 1f, 0f), BaseballPitchType.Curve, 1.2f);
            Assert.That(board.Bat.HasAttempted, Is.False);
            Assert.That(board.Outcome.Kind, Is.EqualTo(BaseballPitchOutcomeKind.Ball));
        }

        [Test]
        public void Ai_TimingErrorMissesSometimes()
        {
            // ぶれが大きいと、ジャストを外す球が出る
            var miss = 0;
            for (var seed = 0; seed < 40; seed++)
            {
                var board = CpuBats(new BaseballAiSkill(0.08f, 1f, 0f), BaseballPitchType.Straight, 0f, seed);
                if (!board.LastHit.HasValue || MathF.Abs(board.Bat.Degrees) > BaseballBalance.JustDegrees)
                {
                    miss++;
                }
            }

            Assert.That(miss, Is.GreaterThan(5));
        }
    }
}
