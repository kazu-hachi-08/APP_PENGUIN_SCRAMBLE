using PenmonScramble.Baseball.Core;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 1球の流れ: 構え（CPU は少し待つ・人は球種の札を選ぶ。持ち時間を過ぎたら前の球種でまっすぐ）→ 振りかぶり → 投げる（ここから1球を計算する）
    /// → 魔球・必殺打を出していたらカットイン（盤を止める）→ 結果を見せる → 次の球の構え
    /// （打席が終わっていたら次の打者・3アウトならチェンジ・試合が終わったらゲームセット。.Innings.cs）。
    /// どれも 1/30 秒の歩で進める（描画のフレームに関係なく同じ間になるように）。
    /// 区切りを決めるのはひとり・ホストだけ。ゲストはホストから届いた区切りに合わせる（.Guest.cs）が、
    /// 振りかぶり・カットイン・球が動いている間は自分の端末の時計で進める（自分が投げるときは、投げた瞬間から先回りして振りかぶる）
    /// </summary>
    public partial class BaseballMatch
    {
        private BaseballStage _stage;
        private float _timer;
        private BaseballPitchType _pitchType = BaseballPitchType.Straight;
        private BaseballPitchPlan _cpuPlan;
        private int _throwCount;
        private BaseballThrow _throw;
        private float _commitWait;

        /// <summary>区切りが変わった・結果を決めた・投げる子を替えた（ホストは、すぐにスナップショットを送る）</summary>
        public event System.Action StageChanged;

        private void NotifyChanged() => StageChanged?.Invoke();

        private void Step()
        {
            // 起きたことは PlayEvents で鳴らしてから消す（歩の外で起きたこと＝ゲストがスナップショットで投げた球も、ここで鳴らす）
            _previous = _current;
            var replays = _session.Replays;
            switch (_stage)
            {
                case BaseballStage.Ready:
                    StepReady();
                    break;
                case BaseballStage.WindUp:
                    StepWindUp();
                    break;
                case BaseballStage.CutIn:
                    StepCutIn();
                    break;
                case BaseballStage.Live:
                    StepLive();
                    break;
                case BaseballStage.Result:
                    StepResult();
                    break;
                case BaseballStage.Change:
                    StepChange();
                    break;
            }

            _current = BaseballFrame.Capture(Board);
            if (_session.Replays != replays)
            {
                CarryBallOffset(_previous);
            }

            PlayEvents();
            if (!IsGuest && _stage == BaseballStage.Live && _session.IsDone)
            {
                TryCommit();
            }
        }

        private void PlayEvents()
        {
            foreach (var e in _events)
            {
                BaseballSoundCues.Play(e, Board);
                _view.OnEvent(e, Board);
            }

            _events.Clear();
        }

        /// <summary>構え: 自分が投げるなら球種の札と持ち時間を出す（打席の始めは投げる子の札も）。CPU が投げるなら少し待つ</summary>
        private void EnterReady()
        {
            _stage = BaseballStage.Ready;
            _chosenPitch = null;
            _predicting = false;
            if (IsGuest)
            {
                ClearNetInput();
            }

            _timer = CpuPitches ? _cpuReadySeconds : BaseballRules.PitchClockSeconds;
            _hud.SetPitchCards(LocalPitches);
            if (LocalPitches)
            {
                _hud.SetClock(_timer);
            }

            ShowPitcherChoice(LocalPitches && _game.IsPlateStart);
            RefreshSpecialToggle();
            NotifyChanged();
        }

        private void StepReady()
        {
            _timer -= BaseballRules.StepSeconds;
            if (LocalPitches)
            {
                _hud.SetClock(Mathf.Max(0f, _timer));
                if (_chosenPitch.HasValue)
                {
                    BeginLocalWindUp(_chosenPitch.Value);
                }
                else if (_timer <= 0f)
                {
                    // 持ち時間切れは、前に投げた球種でまっすぐ投げる
                    BeginLocalWindUp(_pitchType);
                }

                return;
            }

            if (IsGuest)
            {
                return;
            }

            if (CpuPitches)
            {
                if (_timer <= 0f)
                {
                    PlanCpuPitch();
                    BeginWindUp(_cpuPlan.Type);
                }

                return;
            }

            StepRemotePitcher();
        }

        /// <summary>この端末の人が投げる（ゲストは、投げた知らせをホストへ送ってから先回りして振りかぶる）</summary>
        private void BeginLocalWindUp(BaseballPitchType type)
        {
            if (IsGuest)
            {
                PredictPitch(type);
            }

            BeginWindUp(type);
        }

        private void BeginWindUp(BaseballPitchType type)
        {
            _pitchType = type;
            _chosenPitch = null;
            _hud.SetPitchCards(false);
            ShowPitcherChoice(false);
            _stage = BaseballStage.WindUp;
            _timer = 0f;
            RefreshSpecialToggle();
            NotifyChanged();
        }

        /// <summary>
        /// 振りかぶりきったら投げる（球が手を離れる瞬間に投げる）。ゲストは、自分が投げた球のときだけ先回りして投げ、
        /// ほかの人の球はホストの「投げた」が届くまで振りかぶったまま待つ
        /// </summary>
        private void StepWindUp()
        {
            _timer += BaseballRules.StepSeconds;
            _view.Figures.WindUp(Mathf.Clamp01(_timer / _windUpSeconds));
            if (_timer < _windUpSeconds || (IsGuest && !_predicting))
            {
                return;
            }

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
            _throwCount++;
            var plan = CpuPitches ? _cpuPlan : (BaseballPitchPlan?)null;
            if (!IsGuest && magic.IsActive)
            {
                _game.UseMagic();
            }

            if (!IsGuest)
            {
                // CPU が打つときは、ここでこの球の読み（バントするか）を決める
                _ai.BeginPitch(_game);
            }

            StartPitch(new BaseballThrow(_throwCount, _pitchType, magic, hitSpecial, plan));
        }

        /// <summary>1球を計算し始める（ひとり・ホストは投げたとき、ゲストはホストの「投げた」が届いたとき）</summary>
        private void StartPitch(BaseballThrow pitch)
        {
            _throw = pitch;
            _committed = false;
            _session.SteerFromLog = BatterSeat >= 0;
            _session.Begin(pitch, _events);
            _swingQueued = false;
            _commitWait = 0f;
            _view.Figures.Release();
            _hud.ShowLineups(_game);
            RefreshSpecialToggle();
            if (pitch.Magic.IsActive || pitch.HitSpecial.IsActive)
            {
                PlayCutIns(pitch.Magic.Special, pitch.HitSpecial);
                _stage = BaseballStage.CutIn;
                _timer = BaseballBalance.SpecialCutInSeconds;
            }
            else
            {
                _stage = BaseballStage.Live;
            }

            NotifyChanged();
        }

        private void StepCutIn()
        {
            _timer -= BaseballRules.StepSeconds;
            if (_timer <= 0f)
            {
                _stage = BaseballStage.Live;
            }
        }

        private void StepLive()
        {
            RecordInputs();
            _session.Advance(_events);
        }

        /// <summary>
        /// 結果が出たら、そこまでの入力が全員ぶん届くのを待って試合に入れる（遅れている人の入力で結果が変わるかもしれないため）。
        /// 待つ上限を過ぎたら、届いた所までで決める
        /// </summary>
        private void TryCommit()
        {
            _commitWait += BaseballRules.StepSeconds;
            if (!_session.InputsComplete && _commitWait < BaseballNetRules.CommitWaitSeconds)
            {
                return;
            }

            _session.Settle(_events);
            PlayEvents();
            _current = BaseballFrame.Capture(Board);
            ShowOutcome(Board.Outcome);
            NotifyChanged();
        }

        private void StepResult()
        {
            if (IsGuest)
            {
                return;
            }

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
            _session.End();
            _view.ResetPitch();
            SnapFrames();
        }
    }
}
