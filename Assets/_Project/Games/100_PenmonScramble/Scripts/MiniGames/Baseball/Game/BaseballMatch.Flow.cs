using PenmonScramble.Baseball.Core;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 1球の流れ: 構え（CPU は少し待つ・人は球種の札を選ぶ。持ち時間を過ぎたら前の球種でまっすぐ）→ 振りかぶり → 投げる（ここから盤を進める）
    /// → 魔球・必殺打を出していたらカットイン（盤を止める）→ 結果を見せる → 次の球の構え
    /// （打席が終わっていたら次の打者・3アウトならチェンジ・試合が終わったらゲームセット。.Innings.cs）。
    /// どれも 1/30 秒の歩で進める（描画のフレームに関係なく同じ間になるように）
    /// </summary>
    public partial class BaseballMatch
    {
        private Stage _stage;
        private float _timer;
        private BaseballPitchType _pitchType = BaseballPitchType.Straight;
        private BaseballPitchPlan _plan;

        private enum Stage
        {
            Ready,
            WindUp,
            CutIn,
            Live,
            Result,
            Change,
            Over,
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
                case Stage.CutIn:
                    StepCutIn();
                    break;
                case Stage.Live:
                    StepLive();
                    break;
                case Stage.Result:
                    StepResult();
                    break;
                case Stage.Change:
                    StepChange();
                    break;
            }

            _current = BaseballFrame.Capture(_board);
            foreach (var e in _events)
            {
                BaseballSoundCues.Play(e, _board);
                _view.OnEvent(e, _board);
                OnSpecialEvent(e);
            }

            if (_stage == Stage.Live && _board.Phase == BaseballBoardPhase.Done)
            {
                ShowOutcome(_board.Outcome);
            }
        }

        /// <summary>構え: 人が投げるなら球種の札と持ち時間を出す（打席の始めは投げる子の札も）。CPU が投げるなら少し待つ</summary>
        private void EnterReady()
        {
            _stage = Stage.Ready;
            _chosenPitch = null;
            if (!HumanBats)
            {
                _timer = BaseballRules.PitchClockSeconds;
                _hud.SetPitchCards(true);
                _hud.SetClock(_timer);
                ShowPitcherChoice(_game.IsPlateStart);
            }
            else
            {
                _timer = _cpuReadySeconds;
            }

            RefreshSpecialToggle();
        }

        private void StepReady()
        {
            _timer -= BaseballRules.StepSeconds;
            if (HumanBats)
            {
                if (_timer <= 0f)
                {
                    PlanCpuPitch();
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
            ShowPitcherChoice(false);
            _stage = Stage.WindUp;
            _timer = 0f;
            RefreshSpecialToggle();
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
            Throw();
        }

        /// <summary>
        /// 投げる: 投げる側の魔球と打つ側の必殺打（どちらも投げる前に、相手に見えないまま選んだもの）をここで初めて合わせる。
        /// どちらかが出ていたら、両方のカットインを出して盤を止める
        /// </summary>
        private void Throw()
        {
            var magic = TakeMagic();
            var hitSpecial = TakeHitSpecial();
            _board.Throw(_pitchType, magic, hitSpecial, _events);
            if (magic.IsActive)
            {
                _game.UseMagic();
            }

            // CPU が打つときは、ここでこの球の読み（バントするか）を決める
            _ai.BeginPitch(_game);
            _swingQueued = false;
            _hud.ShowLineups(_game);
            RefreshSpecialToggle();
            if (magic.IsActive || hitSpecial.IsActive)
            {
                PlayCutIns(magic.Special, hitSpecial);
                _stage = Stage.CutIn;
                _timer = BaseballBalance.SpecialCutInSeconds;
                return;
            }

            _stage = Stage.Live;
        }

        private void StepCutIn()
        {
            _timer -= BaseballRules.StepSeconds;
            if (_timer <= 0f)
            {
                _stage = Stage.Live;
            }
        }

        private void StepLive()
        {
            _board.Step(TakeInput(), _events);
        }

        private void StepResult()
        {
            _timer -= BaseballRules.StepSeconds;
            if (_timer > 0f)
            {
                return;
            }

            if (_game.IsOver)
            {
                EnterOver();
            }
            else if (_game.IsHalfOver)
            {
                EnterChange();
            }
            else
            {
                NextPitch();
            }
        }

        /// <summary>次の球へ。打席が終わっていたら次の打者を打席に立たせる。球とバットは一瞬で構えへ戻し、補間で滑らせない</summary>
        private void NextPitch()
        {
            ResetBoard();
            if (_game.IsPlateStart)
            {
                EnterPlate();
            }
            else
            {
                EnterReady();
            }
        }

        private void ResetBoard()
        {
            _board.Reset();
            _view.ResetPitch();
            SnapFrames();
        }
    }
}
