using System;
using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Common.Profile;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// みんなでのホスト: 試合を計算し、ゲストから届いた入力（投げた知らせ・投げる子・必殺打の ON・1球の記録）をその人の番に使う。
    /// 1球の結果は、そこまでの全員の入力が届いてから決める（BaseballPitchSession.InputsComplete。振った時刻はその人の画面の時刻のまま）。
    /// 区切りが変わるたびと、決まった間隔で、試合のスナップショットを BaseballOnlinePlay が送る。
    /// 抜けた人の子は CPU が打ち、投げる番は残っている人で回す（チームに人がいなくなったら CPU）
    /// </summary>
    public partial class BaseballMatch
    {
        private const string LeftMessage = "が抜けました";
        private static readonly Color LeftColor = new Color(1f, 0.85f, 0.6f);
        private const float LeftBannerSeconds = 2f;

        private readonly Dictionary<int, BaseballNetInput> _remote = new Dictionary<int, BaseballNetInput>();
        private BaseballGamePlay _lastPlay;

        /// <summary>ホストのみ: 待ちの窓で選んだ組み方で試合を始める（移っている間に抜けた人は、呼んだ後に RemoveSeat で外す）</summary>
        public void BeginHost(BaseballStadiumDef stadium, BaseballTeamPlan plan, int seed, int cpuStars)
        {
            _mode = BaseballPlayMode.MatchAway;
            _role = BaseballNetRole.Host;
            _localSeat = 0;
            _seed = seed;
            _cpuStars = cpuStars;
            _remote.Clear();
            Build(stadium, record: true);
            StartWithPlan(plan, -1);
            enabled = true;
        }

        /// <summary>ホストのみ: ゲストの入力が届いた（その人の番の物だけ使う）</summary>
        public void ApplyRemoteInput(int seat, BaseballNetInput input)
        {
            if (!IsHost || seat <= 0 || seat >= _plan.SeatCount || _plan.HasLeft(seat))
            {
                return;
            }

            _remote[seat] = input;
            if (seat == _pitcherSeat && _stage == BaseballStage.Ready)
            {
                ApplyRemotePitcher(input);
            }

            if (!_session.IsActive || input.ThrowId != _throw.Id || _stage == BaseballStage.Result)
            {
                return;
            }

            if (seat == _pitcherSeat)
            {
                _session.MergePitcher(input.Log);
            }

            if (seat == BatterSeat)
            {
                _session.MergeBatter(input.Log);
            }
        }

        /// <summary>ゲストが投げる子を選んだ（打席の始めだけ。その人の子だけ）</summary>
        private void ApplyRemotePitcher(BaseballNetInput input)
        {
            var slot = input.PitcherSlot;
            if (!_game.IsPlateStart || input.ThrowId != _throwCount + 1 || slot < 0 || slot == _game.CurrentPitcher
                || !_plan.CanPitch(_pitcherSeat, _game.FieldingTeam, slot))
            {
                return;
            }

            ApplyPitcher(slot);
        }

        /// <summary>
        /// 構え（ゲストが投げる番）: 投げた知らせが届いたら振りかぶる。届かないまま持ち時間を過ぎたら（少し待ってから）、前の球種でまっすぐ投げる
        /// </summary>
        private void StepRemotePitcher()
        {
            if (_remote.TryGetValue(_pitcherSeat, out var input) && input.Thrown && input.ThrowId == _throwCount + 1)
            {
                ApplyRemotePitcher(input);
                BeginWindUp(input.Type);
                return;
            }

            if (_timer <= -BaseballNetRules.PitchClockGraceSeconds)
            {
                BeginWindUp(_pitchType);
            }
        }

        /// <summary>ゲストが投げた球の魔球（ON で、その子がまだ出せるときだけ。乱数はゲストが決めた物を★の幅に収めて使う）</summary>
        private BaseballPitchMagic RemoteMagic()
        {
            if (!_remote.TryGetValue(_pitcherSeat, out var input) || !input.Thrown || input.ThrowId != _throwCount + 1 || !input.MagicOn || !_game.CanUseMagic)
            {
                return BaseballPitchMagic.None;
            }

            var special = BaseballSpecial.Of(PitcherSpecies);
            var freeze = Mathf.Clamp(input.FreezeSeconds, BaseballBalance.FreezeMinSeconds(special.Stars), BaseballBalance.FreezeMaxSeconds(special.Stars));
            return new BaseballPitchMagic(special, freeze, input.WobblePhase);
        }

        private bool RemoteSpecialOn(int seat) => _remote.TryGetValue(seat, out var input) && input.SpecialOn;

        /// <summary>
        /// 席が抜けた（ホストはゲストが抜けたとき、ゲストはホストのスナップショットで知ったとき）。その人の子は CPU が動かす。
        /// 投げている途中・打っている途中なら、その人の記録はそこで終わり（最後の値が続く）にして、結果を待たないようにする
        /// </summary>
        public void RemoveSeat(int seat)
        {
            if (_plan == null || _plan.HasLeft(seat) || seat == _localSeat || seat < 0 || seat >= _plan.SeatCount)
            {
                return;
            }

            var wasPitcher = seat == _pitcherSeat;
            var wasBatter = seat == BatterSeat;
            _plan.RemoveSeat(seat);
            _remote.Remove(seat);
            if (wasPitcher)
            {
                HandOverPitcher();
            }

            if (_session.IsActive && !IsGuest)
            {
                if (wasPitcher)
                {
                    _session.PitcherLog.Seal();
                }

                if (wasBatter)
                {
                    _session.BatterLog.Seal();
                }
            }

            _hud.ShowBanner($"{SeatNames.Get(seat)} {LeftMessage}", LeftColor, LeftBannerSeconds);
            ShowOwners();
            _hud.ShowLineups(_game);
            ShowTurn();
            NotifyChanged();
        }

        /// <summary>
        /// 投げる人が抜けた: CPU が代わりに投げる。構えの間ならすぐ組み立てて投げ、振りかぶっている途中なら、その球種のまま組み立てだけ CPU にする
        /// （投げた後なら、その人の記録はそこまでで終わり）
        /// </summary>
        private void HandOverPitcher()
        {
            _pitcherSeat = -1;
            if (IsGuest)
            {
                return;
            }

            if (_stage == BaseballStage.Ready)
            {
                _timer = Mathf.Min(_timer, _cpuReadySeconds);
                _hud.SetPitchCards(false);
                ShowPitcherChoice(false);
            }
            else if (_stage == BaseballStage.WindUp)
            {
                PlanCpuPitch();
                _cpuPlan = new BaseballPitchPlan(_pitchType, _cpuPlan.Bend, _cpuPlan.TargetX);
            }
        }

        /// <summary>ホストのみ: 今の試合のスナップショット</summary>
        public BaseballSnapshot CaptureSnapshot()
        {
            var seats = _plan.SeatCount;
            var s = new BaseballSnapshot
            {
                Stadium = _stadium != null ? _stadium.No : 0,
                Mode = _plan.Mode,
                CpuStars = _cpuStars,
                SeatTeam = new int[seats],
                SeatLeft = new bool[seats],
                SeatRbi = (int[])_seatRbi.Clone(),
                State = _game.Capture(),
                Stage = _stage,
                ThrowCount = _throwCount,
                PitcherSeat = _pitcherSeat,
                HasThrow = _session.IsActive,
            };
            for (var seat = 0; seat < seats; seat++)
            {
                s.SeatTeam[seat] = _plan.TeamOf(seat);
                s.SeatLeft[seat] = _plan.HasLeft(seat);
            }

            for (var team = 0; team < BaseballRules.TeamCount; team++)
            {
                var count = _teams[team].Count;
                s.Lineup[team] = new int[count];
                s.Owner[team] = new int[count];
                for (var slot = 0; slot < count; slot++)
                {
                    s.Lineup[team][slot] = _teams[team][slot];
                    s.Owner[team][slot] = _plan.OwnerOf(team, slot);
                }
            }

            if (s.HasThrow)
            {
                s.Throw = _throw;
                s.PitcherLog = _session.PitcherLog;
                s.BatterLog = _session.BatterLog;
                s.Committed = _stage == BaseballStage.Result || _stage == BaseballStage.Change || _stage == BaseballStage.Over;
                s.DoneStep = Math.Max(0, _session.DoneStep);
                s.Outcome = _lastPlay.Outcome;
                s.Plate = _lastPlay.Plate;
                s.Runs = _lastPlay.Runs;
            }

            return s;
        }

        /// <summary>球が動いている間か（ホストはスナップショットを、ゲストは入力を、1歩ごとに送る）</summary>
        public bool IsPitchLive => _stage == BaseballStage.Live || _stage == BaseballStage.CutIn || _stage == BaseballStage.WindUp;
    }
}
