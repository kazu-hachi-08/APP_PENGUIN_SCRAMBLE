using PenmonScramble.Baseball.Core;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 1球の流れ: 構え（CPU は少し待つ・人は球種の札を選ぶ。持ち時間を過ぎたら前の球種でまっすぐ）→ 振りかぶり → 投げる（ここから盤を進める）
    /// → 結果を見せる → 次の球の構え。どれも 1/30 秒の歩で進める（描画のフレームに関係なく同じ間になるように）
    /// </summary>
    public partial class BaseballTestPlay
    {
        private static readonly Color HomeRunColor = new Color(1f, 0.82f, 0.2f);
        private static readonly Color HitColor = new Color(1f, 0.95f, 0.55f);
        private static readonly Color OutColor = new Color(0.62f, 0.82f, 1f);
        private static readonly Color WalkColor = new Color(0.6f, 1f, 0.6f);

        private Stage _stage;
        private float _timer;
        private BaseballPitchType _pitchType = BaseballPitchType.Straight;
        private BaseballPitchPlan _plan;
        private BaseballPlateResult _plate;

        private enum Stage
        {
            Ready,
            WindUp,
            Live,
            Result,
        }

        private void Step()
        {
            _previous = _current;
            _events.Clear();
            switch (_stage)
            {
                case Stage.Ready:
                    StepReady();
                    break;
                case Stage.WindUp:
                    StepWindUp();
                    break;
                case Stage.Live:
                    StepLive();
                    break;
                case Stage.Result:
                    StepResult();
                    break;
            }

            _current = BaseballFrame.Capture(_board);
            foreach (var e in _events)
            {
                BaseballSoundCues.Play(e, _board);
                _view.OnEvent(e, _board);
            }

            if (_stage == Stage.Live && _board.Phase == BaseballBoardPhase.Done)
            {
                ShowOutcome(_board.Outcome);
            }
        }

        /// <summary>構え: 人が投げるなら球種の札と持ち時間を出す。CPU が投げるなら少し待つ</summary>
        private void EnterReady()
        {
            _stage = Stage.Ready;
            _chosenPitch = null;
            if (_side == BaseballTestSide.Pitch)
            {
                _timer = BaseballRules.PitchClockSeconds;
                _hud.SetPitchCards(true);
                _hud.SetClock(_timer);
            }
            else
            {
                _timer = _cpuReadySeconds;
            }
        }

        private void StepReady()
        {
            _timer -= BaseballRules.StepSeconds;
            if (_side == BaseballTestSide.Bat)
            {
                if (_timer <= 0f)
                {
                    _plan = _ai.PlanPitch(_board.Field);
                    BeginWindUp(_plan.Type);
                }

                return;
            }

            _hud.SetClock(Mathf.Max(0f, _timer));
            if (_chosenPitch.HasValue)
            {
                BeginWindUp(_chosenPitch.Value);
            }
            else if (_timer <= 0f)
            {
                // 持ち時間切れは、前に投げた球種でまっすぐ投げる
                BeginWindUp(_pitchType);
            }
        }

        private void BeginWindUp(BaseballPitchType type)
        {
            _pitchType = type;
            _chosenPitch = null;
            _hud.SetPitchCards(false);
            _stage = Stage.WindUp;
            _timer = 0f;
        }

        /// <summary>振りかぶりきったら投げる（球が手を離れる瞬間に Core で投げる）</summary>
        private void StepWindUp()
        {
            _timer += BaseballRules.StepSeconds;
            _view.Figures.WindUp(Mathf.Clamp01(_timer / _windUpSeconds));
            if (_timer < _windUpSeconds)
            {
                return;
            }

            _view.Figures.Release();
            _board.Throw(_pitchType, _events);
            _ai.ResetBatter();
            _swingQueued = false;
            _stage = Stage.Live;
        }

        private void StepLive()
        {
            _board.Step(TakeInput(), _events);
        }

        /// <summary>1球の結果をカウントに入れて、大きな文字・音・子の反応を出す</summary>
        private void ShowOutcome(BaseballPitchOutcome outcome)
        {
            _plate = _count.Apply(outcome);
            var plateLabel = BaseballResults.Label(_plate);
            var text = plateLabel.Length > 0 ? plateLabel : BaseballResults.Label(outcome);
            var ended = _plate != BaseballPlateResult.Continue;
            _timer = ended ? _plateEndSeconds : _resultSeconds;
            _hud.ShowBanner(text, BannerColor(outcome, _plate), _timer);
            _hud.ShowCount(_count);
            _view.Figures.React(outcome, _plate);
            BaseballSoundCues.PlayOutcome(outcome, _plate);
            _stage = Stage.Result;
        }

        private static Color BannerColor(BaseballPitchOutcome outcome, BaseballPlateResult plate)
        {
            switch (plate)
            {
                case BaseballPlateResult.Hit:
                    return outcome.Hit == BaseballHitResult.HomeRun ? HomeRunColor : HitColor;
                case BaseballPlateResult.Out:
                case BaseballPlateResult.Strikeout:
                    return OutColor;
                case BaseballPlateResult.Walk:
                    return WalkColor;
                default:
                    return Color.white;
            }
        }

        private void StepResult()
        {
            _timer -= BaseballRules.StepSeconds;
            if (_timer <= 0f)
            {
                NextPitch();
            }
        }

        /// <summary>
        /// 次の球へ。打席が終わっていたら次の打者（編成の 1→2→3 をくり返す）に替え、3アウトならアウトを戻す。
        /// 球とバットは一瞬で構えへ戻し、補間で滑らせない
        /// </summary>
        private void NextPitch()
        {
            if (_plate != BaseballPlateResult.Continue)
            {
                _batterIndex = (_batterIndex + 1) % _offense.Count;
                _view.Figures.SetBatter(_offense[_batterIndex], _side == BaseballTestSide.Pitch);
                if (_count.IsInningOver)
                {
                    _count.ClearOuts();
                }
            }

            _plate = BaseballPlateResult.Continue;
            _board.Reset();
            _view.ResetPitch();
            _hud.ShowCount(_count);
            SnapFrames();
            EnterReady();
        }
    }
}
