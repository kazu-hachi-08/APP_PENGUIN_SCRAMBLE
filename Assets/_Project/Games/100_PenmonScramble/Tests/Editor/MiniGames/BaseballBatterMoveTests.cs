using System;
using System.Collections.Generic;
using NUnit.Framework;
using PenmonScramble.Baseball.Core;

namespace PenmonScramble.Tests
{
    /// <summary>打つ子が打席の中を左右へ動く（内角・外角の球に芯を合わせる）</summary>
    public class BaseballBatterMoveTests
    {
        private const int MaxSteps = 1000;

        private static BaseballFieldDef Field => BaseballFieldTable.Test();

        [Test]
        public void Bat_Move_StaysInsideTheRange()
        {
            var bat = new BaseballBat();
            bat.Move(1f, 10f);
            Assert.That(bat.Offset, Is.EqualTo(BaseballBalance.BatterMoveRange).Within(1e-5f));
            bat.Move(-1f, 10f);
            Assert.That(bat.Offset, Is.EqualTo(-BaseballBalance.BatterMoveRange).Within(1e-5f));
        }

        [Test]
        public void Bat_Move_StopsOnceSwinging_AndResetsForTheNextPitch()
        {
            var bat = new BaseballBat();
            bat.Move(1f, 0.1f);
            var before = bat.Offset;
            Assert.That(before, Is.EqualTo(BaseballBalance.BatterMoveSpeed * 0.1f).Within(1e-5f));

            bat.Swing();
            bat.Move(1f, 0.1f);
            Assert.That(bat.Offset, Is.EqualTo(before), "振り始めたら足は止まる");

            bat.Reset();
            Assert.That(bat.Offset, Is.EqualTo(0f), "次の球は構えの所から");
        }

        [Test]
        public void Bat_Move_AllowedWhileBunting()
        {
            var bat = new BaseballBat();
            bat.SetBunt(true);
            bat.Move(-1f, 0.1f);
            Assert.That(bat.Offset, Is.LessThan(0f));
        }

        /// <summary>外へ曲げた球（右へ）を、振る歩を全部試して、いちばん強く打てた強さを返す</summary>
        private static float BestPowerOnOutsidePitch(float move)
        {
            var best = 0f;
            for (var swingStep = 0; swingStep < 80; swingStep++)
            {
                var board = new BaseballBoard(Field);
                var events = new List<BaseballEvent>();
                board.Throw(BaseballPitchType.Curve, events);
                for (var step = 0; step < MaxSteps && board.Phase != BaseballBoardPhase.Done; step++)
                {
                    board.Step(new BaseballInput(1f, step == swingStep, false, 0f, move), events);
                }

                if (board.LastHit is { } hit)
                {
                    best = Math.Max(best, hit.Power);
                }
            }

            return best;
        }

        [Test]
        public void Board_MovingOut_HitsAnOutsidePitchHarder()
        {
            var still = BestPowerOnOutsidePitch(0f);
            var moved = BestPowerOnOutsidePitch(1f);
            Assert.That(moved, Is.GreaterThan(still + 0.1f), "外の球へ寄れば芯に近い所で打てる");
        }

        [Test]
        public void Board_SwingEvent_IsAtTheMovedPivot()
        {
            var board = new BaseballBoard(Field);
            var events = new List<BaseballEvent>();
            board.Throw(BaseballPitchType.Changeup, events);
            for (var step = 0; step < 10; step++)
            {
                board.Step(new BaseballInput(0f, step == 9, false, 0f, 1f), events);
            }

            var swing = events.Find(e => e.Kind == BaseballEventKind.Swing);
            Assert.That(swing.Position.X, Is.GreaterThan(Field.BatPivot.X + 0.1f));
            Assert.That(board.BatPivot.X, Is.EqualTo(Field.BatPivot.X + board.Bat.Offset).Within(1e-5f));
        }

        [Test]
        public void Session_BatterMove_IsReadFromTheBatterLog()
        {
            var session = new BaseballPitchSession(Field);
            var events = new List<BaseballEvent>();
            session.Begin(new BaseballThrow(1, BaseballPitchType.Changeup, BaseballPitchMagic.None, BaseballSpecial.None, null), events);
            for (var step = 0; step < 10; step++)
            {
                session.PitcherLog.Record(0f, false, false, 0f);
                session.BatterLog.Record(-1f, false, false, 0f);
                session.Advance(events);
            }

            Assert.That(session.Board.Bat.Offset, Is.LessThan(-0.1f), "打つ人の記録の曲げる入力の欄で、内へ動く");
        }

        [Test]
        public void Session_BatterMoveArrivingLate_Replays()
        {
            var session = new BaseballPitchSession(Field);
            var events = new List<BaseballEvent>();
            session.Begin(new BaseballThrow(1, BaseballPitchType.Changeup, BaseballPitchMagic.None, BaseballSpecial.None, null), events);
            for (var step = 0; step < 10; step++)
            {
                session.PitcherLog.Record(0f, false, false, 0f);
                session.Advance(events);
            }

            Assert.That(session.Board.Bat.Offset, Is.EqualTo(0f), "届くまでは動いていないと見なす");

            var late = new BaseballRoleLog();
            for (var step = 0; step < 10; step++)
            {
                late.Record(1f, false, false, 0f);
            }

            session.MergeBatter(late);
            session.PitcherLog.Record(0f, false, false, 0f);
            session.Advance(events);
            Assert.That(session.Replays, Is.GreaterThan(0));
            Assert.That(session.Board.Bat.Offset, Is.GreaterThan(0.1f), "届いた左右で計算し直す");
        }

        [Test]
        public void Ai_MovesTowardWhereTheBallCrosses()
        {
            var field = Field;
            var total = 0f;
            const int pitches = 20;
            for (var seed = 0; seed < pitches; seed++)
            {
                var board = new BaseballBoard(field);
                var ai = new BaseballAi(seed, BaseballBalance.AiSkill(BaseballBalance.AiMaxStars));
                ai.ResetBatter();
                var events = new List<BaseballEvent>();
                var plan = new BaseballPitchPlan(BaseballPitchType.Changeup, BaseballBendPlan.Away, field.ZoneHalfWidth * BaseballBalance.AiEdgeAim);
                board.Throw(plan.Type, events);
                var offset = 0f;
                for (var step = 0; step < MaxSteps && board.Phase == BaseballBoardPhase.Pitching; step++)
                {
                    offset = board.Bat.Offset;
                    var bat = ai.BatInput(board);
                    board.Step(new BaseballInput(BaseballAi.Bend(board.Pitch, plan, field), bat.Swing, bat.Bunt, bat.SwingTime, bat.Move), events);
                }

                total += offset;
            }

            // 外角（右）の球には、芯が届くよう右へ寄る（1球ごとの合わせ損ねがあるので、平均で見る）
            Assert.That(total / pitches, Is.GreaterThan(0.2f));
        }
    }
}
