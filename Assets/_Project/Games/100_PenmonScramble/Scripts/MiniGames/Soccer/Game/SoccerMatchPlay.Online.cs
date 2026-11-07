using PenmonScramble.MiniGames.Core;
using System;
using System.Collections.Generic;
using PenmonScramble.Common.Profile;
using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// みんなでの所。ホストは試合を計算し、ゲストから届いた入力をその人の子へ渡す（届いた入力は SoccerRemoteInput で歩ごとに）。
    /// 点を入れた人（MVP）を数え、起きたことを SoccerOnlinePlay へ渡してスナップショットに載せる。
    /// ゲストは計算せず、届いたスナップショットを写しの試合に書き込み、前に描いていた所から届いた所までを、
    /// 次が届くまでの間（SoccerNetRules.SnapshotSeconds）をかけて寄せて描く。自分の入力はホストへ送る
    /// </summary>
    public partial class SoccerMatchPlay
    {
        private const string LeftMessage = "が抜けました";

        private SoccerTeamPlan _plan;
        private SoccerRemoteInput[] _remote;
        // 席ごとの入れた点（3分の間だけ。MVP に使う）と、子ごとの最後に蹴ったときに動かしていた人の席
        private int[] _seatGoals;
        private int[] _kickSeat;

        // ゲストのみ
        private SoccerControl _guestControl;
        private int[] _guestShown;
        private float _guestBlend;
        private byte _switchCount;

        /// <summary>ホストのみ: 試合で起きたこと（SoccerOnlinePlay がスナップショットに載せる）</summary>
        public event Action<SoccerEvent> Happened;

        /// <summary>オンラインのホストが結果で「スタジアムを選ぶ」を押した（全員で待ちの窓へ戻る。SoccerOnlinePlay が受ける）</summary>
        public event Action RoomRequested;

        /// <summary>この端末の人の切り替え（スマホのボタンの中身に使う）。ゲストはホストの決めた子を写した物</summary>
        private SoccerControl LocalControl => _mode.IsGuest ? _guestControl : _humans?.Control(_mode.LocalSeat);

        /// <summary>ゲストのみ: 前に描いていた所から届いた所まで、どこまで寄せたか（0〜1）</summary>
        private float GuestBlend => Mathf.Clamp01(_guestBlend / SoccerNetRules.SnapshotSeconds);

        /// <summary>
        /// ホストのみ: 各自の編成で組んだチームで試合を始める（移っている間に抜けた人は、呼んだ後に RemoveSeat で外す）
        /// </summary>
        public void BeginHost(SoccerStadiumDef stadium, SoccerTeamPlan plan, int seed)
        {
            _stadium = stadium;
            _record = true;
            _shootoutOnly = false;
            _mode = SoccerPlayMode.Host();
            _remote = new SoccerRemoteInput[plan.SeatCount];
            for (var seat = 0; seat < _remote.Length; seat++)
            {
                _remote[seat] = new SoccerRemoteInput();
            }

            StartComputed(plan, seed);
        }

        /// <summary>ホストのみ: ゲストの入力が届いた</summary>
        public void ApplyRemoteInput(int seat, SoccerNetInput input)
        {
            if (_remote != null && seat > 0 && seat < _remote.Length)
            {
                _remote[seat].Receive(input);
            }
        }

        /// <summary>ホストのみ: ゲストが抜けた。その人の子は AI が動かす（3対3 のまま続ける）</summary>
        public void RemoveSeat(int seat)
        {
            if (_humans == null || !_humans.Has(seat) || seat == _mode.LocalSeat)
            {
                return;
            }

            _humans.Remove(seat);
            _seatTeams = _humans.SeatTeams();
            _hud.ShowBanner($"{SeatNames.Get(seat)} {LeftMessage}", BannerSeconds);
        }

        /// <summary>ホストのみ: 今の試合のスナップショット（events は前に送ってから起きたこと）</summary>
        public SoccerSnapshot CaptureSnapshot(IReadOnlyList<SoccerEvent> events)
        {
            return SoccerSnapshot.Capture(_stadium.No, _match, _plan, _humans, _seatGoals, events);
        }

        private SoccerInput TakeRemoteInput(int seat, out bool switchPressed)
        {
            switchPressed = false;
            if (_remote == null || seat >= _remote.Length || !_humans.Has(seat))
            {
                return SoccerInput.None;
            }

            return _remote[seat].Take(out switchPressed);
        }

        /// <summary>
        /// 計算する側（ひとり・ホスト）: 蹴った子を動かしていた人を覚えておき、点が入ったらその人の点にする
        /// （蹴った後に動かす子が替わっても、蹴った人の点になるように）。自分のゴールへ入れた点は数えない
        /// </summary>
        private void CountScorer(SoccerEvent e)
        {
            if (_mode.IsGuest)
            {
                return;
            }

            Happened?.Invoke(e);
            if (e.Player < 0 || e.Player >= _kickSeat.Length)
            {
                return;
            }

            switch (e.Kind)
            {
                case SoccerEventKind.Kick:
                case SoccerEventKind.Pass:
                    _kickSeat[e.Player] = _humans.SeatOf(e.Player);
                    break;
                case SoccerEventKind.Goal:
                    var seat = _kickSeat[e.Player];
                    if (seat >= 0 && seat < _seatGoals.Length && _world.Players[e.Player].Team == e.Team)
                    {
                        _seatGoals[seat]++;
                    }

                    break;
            }
        }

        // ---- ゲスト ----

        /// <summary>ゲストのみ: 最初のスナップショットで作った写しの試合で始める</summary>
        public void BeginGuest(SoccerMatch mirror, SoccerSnapshot first, int seat, ISoccerInputSender sender)
        {
            _stadium = SoccerStadiumTable.Get(first.Stadium);
            _record = true;
            _shootoutOnly = false;
            _mode = SoccerPlayMode.Guest(seat, sender);
            _seatTeams = (int[])first.SeatTeam.Clone();
            _guestShown = (int[])first.SeatControlled.Clone();
            SetupMatch(mirror, first.Species, first.Mode == MiniGameOnlineMode.Coop ? SoccerRules.Blue : -1);
            var controlled = seat < first.SeatControlled.Length ? first.SeatControlled[seat] : SoccerBall.NoPlayer;
            _guestControl = new SoccerControl(OwnTeam, Math.Max(0, controlled), IsTakenByOtherSeat);
            ApplyGuestSnapshot(first);
            _previous = _current;
            enabled = true;
        }

        /// <summary>ゲストのみ: 次のスナップショット。席の数が違う物は無視する</summary>
        public void ApplySnapshot(SoccerSnapshot snapshot)
        {
            if (!_mode.IsGuest || _match == null || !_match.Fits(snapshot) || snapshot.SeatCount != _seatTeams.Length)
            {
                return;
            }

            // いま描いている所から寄せ始める（届くたびに前の所へ跳ばないように）
            _previous = SoccerFrame.Blend(_previous, _current, GuestBlend);
            ApplyGuestSnapshot(snapshot);
        }

        private void ApplyGuestSnapshot(SoccerSnapshot snapshot)
        {
            _match.ApplySnapshot(snapshot);
            Array.Copy(snapshot.SeatTeam, _seatTeams, _seatTeams.Length);
            Array.Copy(snapshot.SeatControlled, _guestShown, _guestShown.Length);
            Array.Copy(snapshot.SeatGoals, _seatGoals, _seatGoals.Length);
            _guestControl.Follow(_guestShown[_mode.LocalSeat]);
            _hud.SetScore(_world.Score(SoccerRules.Red), _world.Score(SoccerRules.Blue));
            _events.Clear();
            _events.AddRange(snapshot.Events);
            HandleEvents();
            _current = SoccerFrame.Capture(_world);
            _guestBlend = 0f;
            if (_snap)
            {
                _previous = _current;
                _snap = false;
            }
        }

        /// <summary>ゲストのみ: 手元の入力をホストへ（送るかどうかは送る側が決める）。描く所を寄せる</summary>
        private void TickGuest(SoccerInput raw, bool switchDown, float dt)
        {
            if (switchDown)
            {
                _switchCount++;
            }

            _mode.Sender?.SendInput(new SoccerNetInput(raw.Move, raw.Shoot, raw.Pass, raw.Special, _switchCount));
            _guestBlend += dt;
        }

        private int GuestShown(int seat) => _guestShown != null && seat < _guestShown.Length ? _guestShown[seat] : SoccerBall.NoPlayer;

        /// <summary>ゲストの交代の顔: ほかの人が動かしている子へは替わらない</summary>
        private bool IsTakenByOtherSeat(int player)
        {
            for (var seat = 0; seat < _guestShown.Length; seat++)
            {
                if (seat != _mode.LocalSeat && _guestShown[seat] == player)
                {
                    return true;
                }
            }

            return false;
        }

        private void RequestRoom() => RoomRequested?.Invoke();
    }
}
