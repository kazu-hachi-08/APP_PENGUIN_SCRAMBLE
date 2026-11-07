using System;
using PenmonScramble.Baseball.Core;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// みんなでのゲスト: 試合は計算せず、ホストから届いたスナップショットで試合の様子を写し、区切りが変わったら同じ演出を出す。
    /// 球が動いている間だけは、ホストから届いた投げ方と2人の入力の記録から、自分の端末で1球を計算する
    /// （自分の番の入力はその場で記録に書くので、振ったバット・曲げた球がすぐ動く。相手の入力は届いたら計算し直す）。
    /// 自分が投げるときは、投げた知らせをホストへ送ってから先回りして振りかぶって投げる（ホストより少し先に球が出るので、曲げる入力が遅れずに届く）。
    /// 結果はホストが決めた物を使う（自分の計算も、ホストの決めた記録でそろえ直す）
    /// </summary>
    public partial class BaseballMatch
    {
        private readonly BaseballNetInput _netInput = new BaseballNetInput();
        private readonly BaseballRoleLog _emptyLog = new BaseballRoleLog();
        private Random _guestRandom;
        private bool _predicting;
        private BaseballPitchMagic _predictedMagic;
        private bool _committed;
        private int _plateKey = -1;

        /// <summary>ゲストのみ: 送る入力が変わった（投げた・投げる子を選んだ・必殺打の ON。BaseballOnlinePlay がすぐ送る）</summary>
        public event Action InputChanged;

        /// <summary>ゲストのみ: 最初のスナップショットで、ホストの球場と組み方から試合を作って始める</summary>
        public void BeginGuest(BaseballStadiumDef stadium, BaseballSnapshot first, int seat)
        {
            _mode = BaseballPlayMode.MatchAway;
            _role = BaseballNetRole.Guest;
            _localSeat = seat;
            _seed = Environment.TickCount;
            _cpuStars = first.CpuStars;
            _guestRandom = new Random(_seed + 7);
            _plateKey = -1;
            Build(stadium, record: true);
            var plan = BaseballTeamPlan.FromParts(first.Mode, first.SeatTeam, first.Lineup, first.Owner);
            StartWithPlan(plan, -1);
            for (var s = 0; s < first.SeatLeft.Length; s++)
            {
                if (first.SeatLeft[s])
                {
                    plan.RemoveSeat(s);
                }
            }

            _game.Restore(first.State);
            _throwCount = first.ThrowCount;
            _committed = true;
            ApplySnapshot(first);
            enabled = true;
        }

        /// <summary>ゲストのみ: 届いたスナップショット（組み方の違う物は無視する）</summary>
        public void ApplySnapshot(BaseballSnapshot s)
        {
            if (!IsGuest || _plan == null || s.SeatCount != _plan.SeatCount)
            {
                return;
            }

            ApplySeats(s);
            SyncPlate(s);
            ApplyThrow(s);
            SyncStage(s);
            PlayEvents();
        }

        /// <summary>抜けた人と打点</summary>
        private void ApplySeats(BaseballSnapshot s)
        {
            for (var seat = 0; seat < s.SeatLeft.Length; seat++)
            {
                if (s.SeatLeft[seat] && !_plan.HasLeft(seat))
                {
                    RemoveSeat(seat);
                }
            }

            Array.Copy(s.SeatRbi, _seatRbi, Math.Min(s.SeatRbi.Length, _seatRbi.Length));
        }

        /// <summary>
        /// 打席が替わった・攻守が替わった（1球の途中でなければ）: 様子を写して、子を並べ直し、打席の始めの構えにする
        /// </summary>
        private void SyncPlate(BaseballSnapshot s)
        {
            var midPitch = (_session.IsActive && !_committed) || _predicting;
            if (midPitch || s.Stage == BaseballStage.Over || s.Stage == BaseballStage.Change || s.Stage == BaseballStage.Result)
            {
                return;
            }

            var key = s.State.Pitches - s.State.PitchesThisPlate;
            var newHalf = s.State.Inning != _game.Inning || s.State.IsTop != _game.IsTop;
            if (!newHalf && key == _plateKey)
            {
                return;
            }

            _plateKey = key;
            _game.Restore(s.State);
            _pitcherSeat = s.PitcherSeat;
            if (newHalf || _stage == BaseballStage.Change)
            {
                SetUpHalf();
            }

            ResetBoard();
            ShowPlate();
            EnterReady();
        }

        /// <summary>投げた球（新しい球・自分が先回りして投げた球の答え合わせ）、相手の入力の記録、決まった結果</summary>
        private void ApplyThrow(BaseballSnapshot s)
        {
            if (!s.HasThrow)
            {
                return;
            }

            if (s.Throw.Id == _throwCount && _predicting)
            {
                ConfirmPrediction(s);
            }
            else if (s.Throw.Id > _throwCount)
            {
                StartHostPitch(s);
            }

            if (s.Throw.Id != _throwCount || !_session.IsActive)
            {
                return;
            }

            if (!LocalPitches)
            {
                _session.MergePitcher(s.PitcherLog);
            }

            if (!LocalBats)
            {
                _session.MergeBatter(s.BatterLog);
            }

            if (s.Committed && !_committed)
            {
                CommitFromHost(s);
            }
        }

        /// <summary>ほかの人（ホスト・CPU・ほかのゲスト）が投げた球を、自分の端末でも投げる</summary>
        private void StartHostPitch(BaseballSnapshot s)
        {
            _predicting = false;
            _game.Restore(s.State);
            _pitcherSeat = s.PitcherSeat;
            _throwCount = s.Throw.Id;
            _pitchType = s.Throw.Type;
            _committed = false;
            _hud.SetPitchCards(false);
            ShowPitcherChoice(false);
            ShowPitcher();
            StartPitch(s.Throw);
        }

        /// <summary>
        /// 自分が先回りして投げた球に、ホストの「投げた」が届いた。同じ球なら打つ人の必殺打だけ足し（出ていれば、まだならカットインを出して少し止める）、
        /// 違えば（持ち時間切れでホストが先に投げたなど）ホストの球で投げ直す
        /// </summary>
        private void ConfirmPrediction(BaseballSnapshot s)
        {
            _predicting = false;
            _committed = false;
            if (!_throw.SamePitch(s.Throw))
            {
                StartHostPitch(s);
                return;
            }

            _game.Restore(s.State);
            _hud.ShowLineups(_game);
            if (!s.Throw.HitSpecial.IsActive || _throw.HitSpecial.IsActive)
            {
                return;
            }

            _throw = _throw.WithHitSpecial(s.Throw.HitSpecial);
            _session.SetHitSpecial(s.Throw.HitSpecial);
            PlayCutIns(BaseballSpecial.None, s.Throw.HitSpecial);
            // ホストはカットインの間、球を止めている。投げてから進めた分を差し引いて、残りだけ止める
            var left = BaseballBalance.SpecialCutInSeconds - _session.Step * BaseballRules.StepSeconds;
            if (_stage == BaseballStage.Live && left > 0f)
            {
                _stage = BaseballStage.CutIn;
                _timer = left;
            }
        }

        /// <summary>ホストが結果を決めた: 自分の計算をホストの記録でそろえ直し、ホストの結果と試合の様子で演出を出す</summary>
        private void CommitFromHost(BaseballSnapshot s)
        {
            _committed = true;
            var before = _current;
            _session.Adopt(s.PitcherLog, s.BatterLog, s.DoneStep);
            _session.Settle(_events);
            _current = BaseballFrame.Capture(Board);
            CarryBallOffset(before);
            PlayEvents();
            _game.Restore(s.State);
            if (LocalBats && Board.SpecialHitUsed)
            {
                _specialOn = false;
            }

            ShowPlay(s.Outcome, new BaseballGamePlay(s.Outcome, s.Plate, s.Runs, s.State.IsHalfOver, s.State.IsOver));
        }

        /// <summary>結果を見せた後の区切り（次の球の構え・チェンジ・ゲームセット）と、投げる子の替わり・ほかの人の振りかぶり</summary>
        private void SyncStage(BaseballSnapshot s)
        {
            if (_predicting || (_session.IsActive && !_committed))
            {
                return;
            }

            switch (s.Stage)
            {
                case BaseballStage.Over:
                    if (_stage != BaseballStage.Over)
                    {
                        _game.Restore(s.State);
                        EnterOver();
                    }

                    break;
                case BaseballStage.Change:
                    if (_stage == BaseballStage.Result)
                    {
                        _game.Restore(s.State);
                        EnterChange();
                    }

                    break;
                case BaseballStage.Ready:
                case BaseballStage.WindUp:
                    SyncReady(s);
                    break;
            }
        }

        /// <summary>同じ打席の次の球の構え（結果を見せていたら構えに戻す）、投げる子の替わり、ほかの人の振りかぶり</summary>
        private void SyncReady(BaseballSnapshot s)
        {
            if (_stage == BaseballStage.Result || _stage == BaseballStage.Live || _stage == BaseballStage.CutIn)
            {
                _game.Restore(s.State);
                ResetBoard();
                EnterReady();
            }

            if (_stage != BaseballStage.Ready && _stage != BaseballStage.WindUp)
            {
                return;
            }

            var pitcher = _game.CurrentPitcher;
            _game.Restore(s.State);
            if (pitcher != _game.CurrentPitcher)
            {
                ShowPitcher();
                _hud.ShowLineups(_game);
                RefreshSpecialToggle();
            }

            if (s.Stage == BaseballStage.WindUp && _stage == BaseballStage.Ready && !LocalPitches)
            {
                BeginWindUp(_pitchType);
            }
        }

        /// <summary>自分が投げる: 投げた知らせ（球種・魔球の ON と中身の乱数）をホストへ送り、先回りして振りかぶる</summary>
        private void PredictPitch(BaseballPitchType type)
        {
            var magicOn = _magicOn && _game.CanUseMagic;
            _predictedMagic = magicOn ? BaseballPitchMagic.Roll(BaseballSpecial.Of(PitcherSpecies), _guestRandom) : BaseballPitchMagic.None;
            _predicting = true;
            _netInput.PitcherSlot = _game.CurrentPitcher;
            _netInput.Thrown = true;
            _netInput.Type = type;
            _netInput.MagicOn = magicOn;
            _netInput.FreezeSeconds = _predictedMagic.FreezeSeconds;
            _netInput.WobblePhase = _predictedMagic.WobblePhase;
            InputChanged?.Invoke();
        }

        /// <summary>投げる子を替えたい（ホストが替えて、スナップショットで届く）</summary>
        private void RequestPitcher(int slot)
        {
            _netInput.PitcherSlot = slot;
            InputChanged?.Invoke();
        }

        /// <summary>必殺打の ON を切り替えた（ゲストはホストへ知らせる）</summary>
        private void NotifySpecialChanged()
        {
            if (IsGuest)
            {
                InputChanged?.Invoke();
            }
        }

        /// <summary>ゲストのみ: ホストへ送る入力（自分の番の物）</summary>
        public BaseballNetInput CaptureInput()
        {
            var midPitch = _session.IsActive && !_committed;
            _netInput.ThrowId = midPitch ? _throw.Id : _throwCount + 1;
            _netInput.SpecialOn = LocalBats && _specialOn;
            _netInput.Log = !midPitch ? _emptyLog : LocalPitches ? _session.PitcherLog : LocalBats ? _session.BatterLog : _emptyLog;
            return _netInput;
        }

        /// <summary>次の球の構えに入ったら、前の球の投げた知らせ・投げる子の選びを消す</summary>
        private void ClearNetInput()
        {
            _netInput.Thrown = false;
            _netInput.MagicOn = false;
            _netInput.PitcherSlot = -1;
        }
    }
}
