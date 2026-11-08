using System;
using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Common.Online;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// みんなでの野球の通信の段取り。部屋は本編のシーンから試合ごと引き継いでいる（OnlineSession.CarryMatchToNextScene）。
    /// 1. ゲストはシーンに入ったら「準備できた（編成）」をホストへ送る（最初のスナップショットが届くまで送り直す）
    /// 2. ホストは全員の準備がそろうか待つ上限が過ぎたら、待ちの窓で選んだ遊び方と赤・青で、各自の編成から打順を組んで始める
    ///    （届いていない人は仮の子。移っている間に抜けた人の子は、はじめから CPU）
    /// 3. ホストは区切りが変わるたびと決まった間隔（球が動いている間は1歩ごと）でスナップショットを送る。
    ///    ゲストは届いたスナップショットを試合に写し、自分の番の入力をホストへ送る（変わったらすぐ・球が動いている間は1歩ごと）
    /// ゲストが抜けたら、その人の子を CPU が動かして続ける。ホストが抜けたら、ゲストはロビーへ戻る（サッカーと同じ）
    /// </summary>
    public class BaseballOnlinePlay : MonoBehaviour
    {
        [SerializeField] private OnlineSession _session;
        [SerializeField] private BaseballMatch _play;
        [Tooltip("始まるまで（全員の準備・ホストのスナップショットを待つ間）出す")]
        [SerializeField] private GameObject _waiting;
        [SerializeField] private Text _waitingText;

        private readonly Dictionary<int, int[]> _readyParty = new Dictionary<int, int[]>();
        private MiniGameLink _link;
        private BaseballStadiumDef _stadium;
        private bool _online;
        private bool _started;
        private bool _leaving;
        private bool _sendNow;
        private float _waited;
        private float _timer;

        private bool IsHost => _session.IsHost;

        /// <summary>みんなで遊び始める。ゲストの stadiumNo は待ちの窓で届いたもの（使うのはホストが送る球場）</summary>
        public void Begin(int stadiumNo)
        {
            _online = true;
            MiniGameLaunch.BindRoom(_session, Leave);
            // メッセージの名前は配った版どうしで合わせるので変えない
            _link = new MiniGameLink("scramble.baseball");
            _link.Open();
            _play.RoomRequested += ReturnToRoomAsHost;
            if (IsHost)
            {
                _stadium = BaseballStadiumTable.Get(stadiumNo) ?? BaseballStadiumTable.Stadiums[0];
                _link.ReadyReceived += OnReady;
                _link.InputReceived += OnInput;
                _session.OnMemberLeft += OnMemberLeft;
                _play.StageChanged += RequestSend;
                ShowWaiting("みんなの準備を待っています…");
                return;
            }

            _link.SnapshotReceived += OnSnapshot;
            _play.InputChanged += RequestSend;
            // ホストが結果で「球場を選ぶ」を押したら、部屋をつないだまま待ちの窓へ（OnlineSession が部屋を引き継ぐ）
            _session.OnReturnToRoom += MiniGameLaunch.ReturnToRoom;
            ShowWaiting("ホストを待っています…");
            SendReady();
        }

        private void OnDestroy()
        {
            if (!_online)
            {
                return;
            }

            MiniGameLaunch.UnbindRoom(_session, Leave);
            _session.OnMemberLeft -= OnMemberLeft;
            _session.OnReturnToRoom -= MiniGameLaunch.ReturnToRoom;
            _link?.Close();
        }

        private void RequestSend() => _sendNow = true;

        /// <summary>試合の Update（1歩を進める）の後に送る（その歩で変わった区切り・記録を、同じフレームのうちに届ける）</summary>
        private void LateUpdate()
        {
            if (!_online || _leaving)
            {
                return;
            }

            var dt = Time.unscaledDeltaTime;
            if (IsHost)
            {
                TickHost(dt);
            }
            else
            {
                TickGuest(dt);
            }
        }

        private void ShowWaiting(string text)
        {
            _waiting.SetActive(true);
            _waitingText.text = text;
        }

        private void Leave()
        {
            if (_leaving)
            {
                return;
            }

            _leaving = true;
            MiniGameLaunch.ReturnToLobby();
        }

        // ---- ホスト ----

        private void TickHost(float dt)
        {
            if (!_started)
            {
                _waited += dt;
                if (AllReady() || _waited >= BaseballNetRules.ReadyTimeoutSeconds)
                {
                    StartAsHost();
                }

                return;
            }

            _timer += dt;
            var interval = _play.IsPitchLive ? BaseballNetRules.LiveSendSeconds : BaseballNetRules.SnapshotSeconds;
            if (_sendNow || _timer >= interval)
            {
                SendSnapshot();
            }
        }

        private bool AllReady()
        {
            foreach (var seat in _session.ConnectedSeats)
            {
                if (!_readyParty.ContainsKey(seat))
                {
                    return false;
                }
            }

            return true;
        }

        private void OnReady(ulong clientId, byte[] data)
        {
            if (!_started && _session.TryGetSeat(clientId, out var seat) && BaseballCodec.TryDecodeParty(data, out var species))
            {
                _readyParty[seat] = species;
            }
        }

        /// <summary>待ちの窓で選んだ遊び方と赤・青、各自の編成で打順を組んで始める。協力の CPU は人数で強くする</summary>
        private void StartAsHost()
        {
            _started = true;
            _waiting.SetActive(false);
            var players = Mathf.Clamp(_session.MatchPlayerCount, 1, MiniGameRoomChoice.MaxSeats);
            var parties = new List<IReadOnlyList<int>> { BaseballMatch.PartySpecies() };
            for (var seat = 1; seat < players; seat++)
            {
                parties.Add(_readyParty.TryGetValue(seat, out var species) ? species : null);
            }

            var setup = MiniGameRoomSetup.For(MiniGameTable.BaseballId);
            var seed = Environment.TickCount;
            var wild = _stadium.PickWild(BaseballStadiumTable.CpuLineup, seed);
            var plan = BaseballTeamPlan.Create(setup.Mode, setup.Teams, parties, wild);
            var stars = setup.Mode == MiniGameOnlineMode.Coop ? BaseballBalance.CoopCpuStars(_stadium.CpuStars, players) : _stadium.CpuStars;
            _play.BeginHost(_stadium, plan, seed, stars);

            var connected = new HashSet<int>(_session.ConnectedSeats);
            for (var seat = 1; seat < players; seat++)
            {
                if (!connected.Contains(seat))
                {
                    _play.RemoveSeat(seat);
                }
            }

            SendSnapshot();
        }

        private void SendSnapshot()
        {
            _sendNow = false;
            _timer = 0f;
            _link.SendSnapshot(BaseballCodec.EncodeSnapshot(_play.CaptureSnapshot()));
        }

        private void OnInput(ulong clientId, byte[] data)
        {
            if (_started && _session.TryGetSeat(clientId, out var seat) && BaseballCodec.TryDecodeInput(data, out var input))
            {
                _play.ApplyRemoteInput(seat, input);
            }
        }

        private void OnMemberLeft(int seat)
        {
            _readyParty.Remove(seat);
            if (_started)
            {
                _play.RemoveSeat(seat);
            }
        }

        /// <summary>結果で「球場を選ぶ」: 全員で同じ顔ぶれのまま待ちの窓へ</summary>
        private void ReturnToRoomAsHost()
        {
            if (_leaving || !IsHost)
            {
                return;
            }

            _leaving = true;
            MiniGameLaunch.LeaveToRoom(_session);
        }

        // ---- ゲスト ----

        private void TickGuest(float dt)
        {
            _timer += dt;
            if (!_started)
            {
                _waited += dt;
                if (_waited >= BaseballNetRules.GuestGiveUpSeconds)
                {
                    Debug.LogWarning("[BaseballOnlinePlay] ホストのスナップショットが届かないのでロビーへ戻ります");
                    Leave();
                    return;
                }

                if (_timer >= BaseballNetRules.ReadyResendSeconds)
                {
                    SendReady();
                }

                return;
            }

            var interval = _play.IsPitchLive ? BaseballNetRules.LiveSendSeconds : BaseballNetRules.IdleSendSeconds;
            if (_sendNow || _timer >= interval)
            {
                SendInput();
            }
        }

        private void SendReady()
        {
            _timer = 0f;
            _link.SendReady(BaseballCodec.EncodeParty(BaseballMatch.PartySpecies()));
        }

        private void SendInput()
        {
            _sendNow = false;
            _timer = 0f;
            _link.SendInput(BaseballCodec.EncodeInput(_play.CaptureInput()));
        }

        private void OnSnapshot(byte[] data)
        {
            var snapshot = BaseballCodec.DecodeSnapshot(data);
            if (snapshot == null || _leaving)
            {
                return;
            }

            if (!_started)
            {
                StartAsGuest(snapshot);
                return;
            }

            _play.ApplySnapshot(snapshot);
        }

        /// <summary>最初のスナップショットで、ホストの球場と組み方から試合を作る</summary>
        private void StartAsGuest(BaseballSnapshot snapshot)
        {
            var stadium = BaseballStadiumTable.Get(snapshot.Stadium);
            if (stadium == null || _session.Seat >= snapshot.SeatCount)
            {
                Debug.LogWarning($"[BaseballOnlinePlay] ホストの球場 {snapshot.Stadium} がこの版で遊べないのでロビーへ戻ります");
                Leave();
                return;
            }

            MiniGameLaunch.SetCourse(snapshot.Stadium);
            _started = true;
            _waiting.SetActive(false);
            _play.BeginGuest(stadium, snapshot, _session.Seat);
        }
    }
}
