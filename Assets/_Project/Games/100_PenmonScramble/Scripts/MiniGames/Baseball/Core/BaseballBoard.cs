using System;
using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Baseball.Core
{
    public enum BaseballBoardPhase
    {
        /// <summary>投げる前</summary>
        Ready,
        /// <summary>投げた球がホームへ来ている（振れる・バントできる）</summary>
        Pitching,
        /// <summary>打球が盤の上を動いている</summary>
        BallInPlay,
        /// <summary>1球の結果が出た（Outcome）</summary>
        Done,
    }

    /// <summary>1歩の入力。投げる人の曲げる入力と、打つ人の振る・バント（オンラインでは別の人の入力をホストがまとめる）</summary>
    public readonly struct BaseballInput
    {
        public static readonly BaseballInput None = default;

        public BaseballInput(float bend, bool swing, bool bunt)
        {
            Bend = bend;
            Swing = swing;
            Bunt = bunt;
        }

        /// <summary>曲げる（−1 で左・1 で右）</summary>
        public float Bend { get; }

        /// <summary>この歩で振るボタンを押した</summary>
        public bool Swing { get; }

        /// <summary>バントのボタンを押している</summary>
        public bool Bunt { get; }
    }

    /// <summary>
    /// 盤の上の1球（投げる → 振る・バント → 打球 → 結果）。1/30 秒の固定の刻みで進め、起きたことを BaseballEvent で返す。
    /// 投げた球・バットは1歩をさらに細かく分けて動かし、バットが球を飛び越えないようにする
    /// </summary>
    public sealed class BaseballBoard
    {
        /// <summary>投げた球とバットを1歩の中で何回に分けて動かすか（振るバットは1歩で 30°以上回るため）</summary>
        private const int PitchSubsteps = 8;

        private readonly BaseballPitch _pitch;
        private readonly BaseballBat _bat = new BaseballBat();
        private readonly BaseballBallSim _ball;
        private bool _crossedPlate;
        private bool _inZone;

        public BaseballBoard(BaseballFieldDef field)
        {
            Field = field;
            _pitch = new BaseballPitch(field);
            _ball = new BaseballBallSim(field);
        }

        public BaseballFieldDef Field { get; }
        public BaseballBoardPhase Phase { get; private set; } = BaseballBoardPhase.Ready;
        public BaseballPitch Pitch => _pitch;
        public BaseballBat Bat => _bat;
        public BaseballBallSim Ball => _ball;

        public BaseballPitchOutcome Outcome { get; private set; } = BaseballPitchOutcome.None;

        /// <summary>当たったときの打球の出だし（当たっていなければ null）</summary>
        public BaseballHit? LastHit { get; private set; }

        /// <summary>今の球の位置（投げた球か打球）</summary>
        public Vec2 BallPosition => Phase == BaseballBoardPhase.Pitching || !LastHit.HasValue ? _pitch.Position : _ball.Position;

        /// <summary>今の球の高さ（転がっていれば 0）</summary>
        public float BallHeight => Phase == BaseballBoardPhase.BallInPlay || (Phase == BaseballBoardPhase.Done && LastHit.HasValue) ? _ball.Height : 0f;

        /// <summary>投げた球がゾーンの中を通ったか（ホームの線を越えた後に決まる）</summary>
        public bool InZone => _inZone;

        /// <summary>次の球のために構えに戻す</summary>
        public void Reset()
        {
            Phase = BaseballBoardPhase.Ready;
            Outcome = BaseballPitchOutcome.None;
            LastHit = null;
            _crossedPlate = false;
            _inZone = false;
            _pitch.Stop();
            _ball.Stop();
            _bat.Reset();
        }

        public void Throw(BaseballPitchType type, List<BaseballEvent> events)
        {
            if (Phase != BaseballBoardPhase.Ready)
            {
                throw new InvalidOperationException("投げられるのは構えのときだけ");
            }

            _pitch.Start(type);
            Phase = BaseballBoardPhase.Pitching;
            events.Add(new BaseballEvent(BaseballEventKind.Pitch, _pitch.Position, (int)type));
        }

        public void Step(BaseballInput input, List<BaseballEvent> events)
        {
            switch (Phase)
            {
                case BaseballBoardPhase.Pitching:
                    StepPitching(input, events);
                    break;
                case BaseballBoardPhase.BallInPlay:
                    StepBallInPlay(events);
                    break;
            }
        }

        private void StepPitching(BaseballInput input, List<BaseballEvent> events)
        {
            if (input.Swing && _bat.Swing())
            {
                events.Add(new BaseballEvent(BaseballEventKind.Swing, Field.BatPivot));
            }

            if (_bat.SetBunt(input.Bunt))
            {
                events.Add(new BaseballEvent(BaseballEventKind.BuntSquare, Field.BatPivot));
            }

            var dt = BaseballRules.StepSeconds / PitchSubsteps;
            for (var i = 0; i < PitchSubsteps && Phase == BaseballBoardPhase.Pitching; i++)
            {
                PitchSubstep(input.Bend, dt, events);
            }
        }

        private void PitchSubstep(float bend, float dt, List<BaseballEvent> events)
        {
            var prevBall = _pitch.Position;
            var prevDegrees = _bat.Degrees;
            var wasLive = _bat.IsLive;
            var bunting = _bat.State == BaseballBatState.Bunting;
            if (_pitch.Step(bend, dt))
            {
                events.Add(new BaseballEvent(BaseballEventKind.BendEnd, _pitch.Position));
            }

            _bat.Step(dt);
            if (wasLive && BaseballBat.TryContact(Field.BatPivot, prevDegrees, _bat.Degrees, prevBall, _pitch.Position, out var along))
            {
                Contact(bunting ? BaseballBat.BuntHit(along) : BaseballBat.SwingHit(_bat.Degrees, along), bunting, events);
                return;
            }

            CheckPlate(events);
        }

        /// <summary>ホームの線を越えたらゾーンの内外を決め、ミットまで来たら1球の結果を出す</summary>
        private void CheckPlate(List<BaseballEvent> events)
        {
            var position = _pitch.Position;
            if (!_crossedPlate && position.Y <= BaseballFieldDef.PlateY)
            {
                _crossedPlate = true;
                _inZone = MathF.Abs(position.X) <= Field.ZoneHalfWidth;
                events.Add(new BaseballEvent(BaseballEventKind.CrossPlate, position, flag: _inZone));
            }

            if (position.Y > Field.CatcherY)
            {
                return;
            }

            _pitch.Stop();
            events.Add(new BaseballEvent(BaseballEventKind.Catch, position));
            var kind = _bat.HasAttempted
                ? BaseballPitchOutcomeKind.SwingingStrike
                : _inZone ? BaseballPitchOutcomeKind.CalledStrike : BaseballPitchOutcomeKind.Ball;
            Decide(new BaseballPitchOutcome(kind, BaseballHitResult.Single, false));
        }

        private void Contact(BaseballHit hit, bool bunt, List<BaseballEvent> events)
        {
            var at = _pitch.Position;
            _pitch.Stop();
            _bat.Finish();
            LastHit = hit;
            _ball.Launch(at, hit);
            Phase = BaseballBoardPhase.BallInPlay;
            events.Add(new BaseballEvent(BaseballEventKind.Contact, at, hit.Power, hit.Lofted));
        }

        private void StepBallInPlay(List<BaseballEvent> events)
        {
            _ball.Step(BaseballRules.StepSeconds, events);
            if (_ball.Result is not { } result)
            {
                return;
            }

            var bunt = LastHit.HasValue && LastHit.Value.Bunt;
            var kind = result == BaseballHitResult.Foul ? BaseballPitchOutcomeKind.Foul : BaseballPitchOutcomeKind.InPlay;
            Decide(new BaseballPitchOutcome(kind, result, bunt));
        }

        private void Decide(BaseballPitchOutcome outcome)
        {
            Outcome = outcome;
            Phase = BaseballBoardPhase.Done;
        }
    }
}
