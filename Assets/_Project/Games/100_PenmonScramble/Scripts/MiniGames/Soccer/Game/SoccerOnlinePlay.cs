using System;
using System.Collections.Generic;
using PenmonScramble.Common.Online;
using PenmonScramble.MiniGames.Game;
using PenmonScramble.Soccer.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// みんなでのサッカーの通信の段取り。部屋は本編のシーンから試合ごと引き継いでいる（OnlineSession.CarryMatchToNextScene）。
    /// 1. ゲストはシーンに入ったら「準備できた（編成）」をホストへ送る（最初のスナップショットが届くまで送り直す）
    /// 2. ホストは全員の準備がそろうか待つ上限が過ぎたら、待ちの窓で選んだ遊び方と赤・青で、各自の編成からチームを組んで始める
    ///    （届いていない人は仮の3体。移っている間に抜けた人の子は、はじめから AI）。スナップショットを毎秒15回配る
    /// 3. ゲストは届いたスナップショットを写しの試合に書き込む。自分の入力はホストへ送る（ISoccerInputSender）
    /// ゲストが抜けたら、その人の子を AI が動かして続ける。ホストが抜けたら、ゲストはロビーへ戻る（本編・ゴルフと同じ）
    /// </summary>
    public class SoccerOnlinePlay : MonoBehaviour, ISoccerInputSender
    {
        [SerializeField] private OnlineSession _session;
        [SerializeField] private SoccerMatchPlay _play;
        [Tooltip("始まるまで（全員の準備・ホストのスナップショットを待つ間）出す")]
        [SerializeField] private GameObject _waiting;
        [SerializeField] private Text _waitingText;

        private readonly Dictionary<int, int[]> _readyParty = new Dictionary<int, int[]>();
        private readonly List<SoccerEvent> _pending = new List<SoccerEvent>();
        private MiniGameLink _link;
        private SoccerStadiumDef _stadium;
        private bool _online;
        private bool _started;
        private bool _leaving;
        private float _waited;
        private float _timer;

        // ゲストの入力の送り方: ボタン・交代は変わったらすぐ、スティックだけの変わりは間隔をあけて
        private SoccerNetInput _lastSent;
        private SoccerNetInput _wanted;
        private bool _inputSent;
        private bool _inputDirty;
        private float _inputTimer;

        private bool IsHost => _session.IsHost;

        /// <summary>みんなで遊び始める。ゲストの stadiumNo は待ちの窓で届いたもの（使うのはホストが送るスタジアム）</summary>
        public void Begin(int stadiumNo)
        {
            _online = true;
            _session.ContinueWhenMemberLeaves = true;
            _session.OnPeerDisconnected += Leave;
            _session.OnConnectionLost += Leave;
            // メッセージの名前は配った版どうしで合わせるので変えない
            _link = new MiniGameLink("scramble.soccer");
            _link.Open();
            _play.RoomRequested += ReturnToRoomAsHost;
            if (IsHost)
            {
                _stadium = SoccerStadiumTable.Get(stadiumNo) ?? SoccerStadiumTable.Stadiums[0];
                _link.ReadyReceived += OnReady;
                _link.InputReceived += OnInput;
                _session.OnMemberLeft += OnMemberLeft;
                ShowWaiting("みんなの準備を待っています…");
                return;
            }

            _link.SnapshotReceived += OnSnapshot;
            // ホストが結果で「スタジアムを選ぶ」を押したら、部屋をつないだまま待ちの窓へ（OnlineSession が部屋を引き継ぐ）
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

            _session.OnPeerDisconnected -= Leave;
            _session.OnConnectionLost -= Leave;
            _session.OnMemberLeft -= OnMemberLeft;
            _session.OnReturnToRoom -= MiniGameLaunch.ReturnToRoom;
            _link?.Close();
        }

        private void Update()
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
                if (AllReady() || _waited >= SoccerNetRules.ReadyTimeoutSeconds)
                {
                    StartAsHost();
                }

                return;
            }

            _timer += dt;
            if (_timer >= SoccerNetRules.SnapshotSeconds)
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
            if (!_started && _session.TryGetSeat(clientId, out var seat) && SoccerCodec.TryDecodeParty(data, out var species))
            {
                _readyParty[seat] = species;
            }
        }

        /// <summary>
        /// 待ちの窓で選んだ遊び方と赤・青、各自の編成でチームを組んで始める。準備が届かなかった人は仮の3体、
        /// 移っている間に抜けた人の子ははじめから AI
        /// </summary>
        private void StartAsHost()
        {
            _started = true;
            _waiting.SetActive(false);
            var players = Mathf.Clamp(_session.MatchPlayerCount, 1, SoccerRoomChoice.MaxSeats);
            var parties = new List<IReadOnlyList<int>> { SoccerMatchPlay.PartySpecies() };
            for (var seat = 1; seat < players; seat++)
            {
                parties.Add(_readyParty.TryGetValue(seat, out var species) ? species : null);
            }

            var mode = SoccerRoomSetup.Mode;
            var teams = SoccerRoomSetup.Teams;
            var seed = Environment.TickCount;
            var wild = _stadium.PickWild(SoccerTeamPlan.PlayersPerTeam(mode, teams, players), seed);
            var plan = SoccerTeamPlan.Create(mode, teams, parties, wild);
            _play.Happened += _pending.Add;
            _play.BeginHost(_stadium, plan, seed);

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
            var snapshot = _play.CaptureSnapshot(_pending);
            _pending.Clear();
            _timer = 0f;
            _link.SendSnapshot(SoccerCodec.EncodeSnapshot(snapshot));
        }

        private void OnInput(ulong clientId, byte[] data)
        {
            if (_started && _session.TryGetSeat(clientId, out var seat) && SoccerCodec.TryDecodeInput(data, out var input))
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

        /// <summary>結果で「スタジアムを選ぶ」: 全員で同じ顔ぶれのまま待ちの窓へ</summary>
        private void ReturnToRoomAsHost()
        {
            if (_leaving || !IsHost)
            {
                return;
            }

            _leaving = true;
            _session.ReturnToRoom();
            MiniGameLaunch.ReturnToRoom();
        }

        // ---- ゲスト ----

        private void TickGuest(float dt)
        {
            _timer += dt;
            _inputTimer += dt;
            if (!_started)
            {
                _waited += dt;
                if (_waited >= SoccerNetRules.GuestGiveUpSeconds)
                {
                    Debug.LogWarning("[SoccerOnlinePlay] ホストのスナップショットが届かないのでロビーへ戻ります");
                    Leave();
                    return;
                }

                if (_timer >= SoccerNetRules.ReadyResendSeconds)
                {
                    SendReady();
                }

                return;
            }

            if (_inputDirty && _inputTimer >= SoccerNetRules.MoveSendSeconds)
            {
                SendInputNow(_wanted);
            }
        }

        private void SendReady()
        {
            _timer = 0f;
            _link.SendReady(SoccerCodec.EncodeParty(SoccerMatchPlay.PartySpecies()));
        }

        private void OnSnapshot(byte[] data)
        {
            var snapshot = SoccerCodec.DecodeSnapshot(data);
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

        /// <summary>最初のスナップショットで、ホストのスタジアムとチームから写しの試合を作る</summary>
        private void StartAsGuest(SoccerSnapshot snapshot)
        {
            var mirror = SoccerMatch.CreateMirror(snapshot);
            if (mirror == null || _session.Seat >= snapshot.SeatCount)
            {
                Debug.LogWarning($"[SoccerOnlinePlay] ホストのスタジアム {snapshot.Stadium} がこの版で遊べないのでロビーへ戻ります");
                Leave();
                return;
            }

            MiniGameLaunch.SetCourse(snapshot.Stadium);
            _started = true;
            _waiting.SetActive(false);
            _play.BeginGuest(mirror, snapshot, _session.Seat, this);
        }

        /// <summary>ボタン・交代が変わったらすぐ、スティックだけの変わりは間隔をあけて送る（送る数を抑える）</summary>
        public void SendInput(SoccerNetInput input)
        {
            if (!_started)
            {
                return;
            }

            if (!_inputSent || !input.SameButtons(_lastSent))
            {
                SendInputNow(input);
                return;
            }

            if (input.SameMove(_lastSent))
            {
                _inputDirty = false;
                return;
            }

            _wanted = input;
            _inputDirty = true;
            if (_inputTimer >= SoccerNetRules.MoveSendSeconds)
            {
                SendInputNow(input);
            }
        }

        private void SendInputNow(SoccerNetInput input)
        {
            _inputTimer = 0f;
            _inputDirty = false;
            _inputSent = true;
            _lastSent = input;
            _link.SendInput(SoccerCodec.EncodeInput(input));
        }
    }
}
