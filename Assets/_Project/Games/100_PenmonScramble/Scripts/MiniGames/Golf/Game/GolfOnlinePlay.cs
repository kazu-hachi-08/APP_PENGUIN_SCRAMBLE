using System.Collections.Generic;
using PenmonScramble.Common.Online;
using PenmonScramble.Golf.Core;
using PenmonScramble.MiniGames.Game;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// みんなで回るゴルフの通信の段取り。部屋は本編のシーンから試合ごと引き継いでいる（OnlineSession.CarryMatchToNextScene）。
    /// 1. ゲストはシーンに入ったら「準備できた（編成の3体）」をホストへ送る（最初のスナップショットが届くまで送り直す）
    /// 2. ホストは全員の準備がそろうか待つ上限が過ぎたら、各自の3体でラウンドを作って始め、スナップショットを配り続ける
    ///    （届いていない人は仮の3体。移っている間に抜けた人は、はじめから外す）
    /// 3. ゲストは届いたスナップショットを写しのラウンドに写す。自分の番の入力はホストへ送る（IGolfInputSender）
    /// ゲストが抜けたら、その人の球を消して残りで続ける。ホストが抜けたら、ゲストはロビーへ戻る（本編と同じ）
    /// </summary>
    public class GolfOnlinePlay : MonoBehaviour, IGolfInputSender
    {
        // ゲストがホストを待つ上限（ホストのシーンが始まらないまま、切断も届かないとき）
        private const float GuestGiveUpSeconds = 20f;
        // 狙いの向きがこれより変わったら送る（ラジアン）
        private const float AimAngleEpsilon = 0.002f;
        private const float AimPowerEpsilon = 0.01f;

        [SerializeField] private OnlineSession _session;
        [SerializeField] private GolfRoundPlay _play;
        [Tooltip("始まるまで（全員の準備・ホストのスナップショットを待つ間）出す")]
        [SerializeField] private GameObject _waiting;
        [SerializeField] private Text _waitingText;

        private readonly Dictionary<int, int[]> _readyParty = new Dictionary<int, int[]>();
        private readonly List<GolfNetEvent> _pending = new List<GolfNetEvent>();
        private MiniGameLink _link;
        private GolfCourseDef _course;
        private GolfRound _round;
        private bool _online;
        private bool _started;
        private bool _leaving;
        private float _waited;
        private float _timer;
        private GolfAim _lastAim;
        private GolfAim _wantedAim;
        private bool _aimDirty;
        private bool _aimSent;

        private bool IsHost => _session.IsHost;

        /// <summary>みんなで回り始める。ゲストの courseNo は待ちの窓で届いたもの（使うのはホストが送るコース）</summary>
        public void Begin(int courseNo)
        {
            _online = true;
            MiniGameLaunch.BindRoom(_session, Leave);
            // メッセージの名前は配った版どうしで合わせるので変えない
            _link = new MiniGameLink("scramble.golf");
            _link.Open();
            _play.RoomRequested += ReturnToRoomAsHost;
            if (IsHost)
            {
                _course = GolfCourseTable.Get(courseNo) ?? GolfCourseTable.Get(1);
                _link.ReadyReceived += OnReady;
                _link.InputReceived += OnInput;
                _session.OnMemberLeft += OnMemberLeft;
                ShowWaiting("みんなの準備を待っています…");
                return;
            }

            _link.SnapshotReceived += OnSnapshot;
            // ホストが結果で「コースを選ぶ」を押したら、部屋をつないだまま待ちの窓へ（OnlineSession が部屋を引き継ぐ）
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
                if (AllReady() || _waited >= GolfNetRules.ReadyTimeoutSeconds)
                {
                    StartAsHost();
                }

                return;
            }

            _timer += dt;
            if (_pending.Count > 0 || _timer >= GolfNetRules.SnapshotSeconds)
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
            if (!_started && _session.TryGetSeat(clientId, out var seat) && GolfCodec.TryDecodeParty(data, out var species))
            {
                _readyParty[seat] = species;
            }
        }

        /// <summary>各自の3体でラウンドを作る。準備が届かなかった人は仮の3体、移っている間に抜けた人ははじめから外す</summary>
        private void StartAsHost()
        {
            _started = true;
            _waiting.SetActive(false);
            var players = Mathf.Clamp(_session.MatchPlayerCount, 1, GolfRules.MaxPlayers);
            var teams = new List<GolfTeam> { new GolfTeam(GolfRoundPlay.PartySpecies()) };
            for (var seat = 1; seat < players; seat++)
            {
                teams.Add(new GolfTeam(_readyParty.TryGetValue(seat, out var species) ? species : null));
            }

            var round = new GolfRound(_course, teams);
            var connected = new HashSet<int>(_session.ConnectedSeats);
            for (var seat = 1; seat < players; seat++)
            {
                if (!connected.Contains(seat))
                {
                    round.RemovePlayer(seat);
                }
            }

            round.Happened += e => _pending.Add(new GolfNetEvent(round.HoleIndex, e));
            _round = round;
            _play.Begin(round, true, GolfPlayMode.Host());
            SendSnapshot();
        }

        private void SendSnapshot()
        {
            var snapshot = GolfSnapshot.Capture(_round, _play.CurrentAim, _pending);
            _pending.Clear();
            _timer = 0f;
            _link.SendSnapshot(GolfCodec.EncodeSnapshot(snapshot));
        }

        private void OnInput(ulong clientId, byte[] data)
        {
            if (_started && _session.TryGetSeat(clientId, out var seat) && GolfCodec.TryDecodeInput(data, out var input))
            {
                _play.ApplyRemoteInput(seat, input);
            }
        }

        private void OnMemberLeft(int seat)
        {
            _readyParty.Remove(seat);
            if (_started)
            {
                _play.RemovePlayer(seat);
            }
        }

        /// <summary>結果で「コースを選ぶ」: 全員で同じ顔ぶれのまま待ちの窓へ</summary>
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
                if (_waited >= GuestGiveUpSeconds)
                {
                    Debug.LogWarning("[GolfOnlinePlay] ホストのスナップショットが届かないのでロビーへ戻ります");
                    Leave();
                    return;
                }

                if (_timer >= GolfNetRules.ReadyResendSeconds)
                {
                    SendReady();
                }

                return;
            }

            if (_aimDirty && _timer >= GolfNetRules.AimSendSeconds)
            {
                SendAimNow(_wantedAim);
            }
        }

        private void SendReady()
        {
            _timer = 0f;
            _link.SendReady(GolfCodec.EncodeParty(GolfRoundPlay.PartySpecies()));
        }

        private void OnSnapshot(byte[] data)
        {
            var snapshot = GolfCodec.DecodeSnapshot(data);
            if (snapshot == null || _leaving)
            {
                return;
            }

            if (_round == null && !StartAsGuest(snapshot))
            {
                return;
            }

            _round.ApplySnapshot(snapshot, _session.Seat);
        }

        /// <summary>最初のスナップショットで、ホストのコースと各自の3体から写しのラウンドを作る</summary>
        private bool StartAsGuest(GolfSnapshot snapshot)
        {
            var course = GolfCourseTable.Get(snapshot.CourseNo);
            if (course == null || course.Holes.Count != snapshot.Recorded.Length)
            {
                Debug.LogWarning($"[GolfOnlinePlay] ホストのコース {snapshot.CourseNo} がこの版に無いのでロビーへ戻ります");
                Leave();
                return false;
            }

            var teams = new List<GolfTeam>();
            foreach (var team in snapshot.Teams)
            {
                teams.Add(new GolfTeam(team.Species));
            }

            MiniGameLaunch.SetCourse(course.No);
            _round = GolfRound.CreateMirror(course, teams);
            _started = true;
            _waiting.SetActive(false);
            _play.Begin(_round, true, GolfPlayMode.Guest(_session.Seat, this));
            return true;
        }

        /// <summary>変わったときだけ、間隔をあけて送る（ゲージは動き続けるので、送る数を抑える）</summary>
        public void SendAim(GolfAim aim)
        {
            if (_aimSent && !Changed(_lastAim, aim))
            {
                _aimDirty = false;
                return;
            }

            _wantedAim = aim;
            _aimDirty = true;
            if (_timer >= GolfNetRules.AimSendSeconds)
            {
                SendAimNow(aim);
            }
        }

        public void Send(GolfInput input)
        {
            _link.SendInput(GolfCodec.EncodeInput(input));
        }

        private void SendAimNow(GolfAim aim)
        {
            _timer = 0f;
            _aimDirty = false;
            _aimSent = true;
            _lastAim = aim;
            Send(GolfInput.ForAim(aim));
        }

        private static bool Changed(GolfAim a, GolfAim b)
        {
            return a.Member != b.Member || a.GaugeRunning != b.GaugeRunning || a.Special != b.Special || a.Curve != b.Curve
                || Mathf.Abs(a.Angle - b.Angle) > AimAngleEpsilon || Mathf.Abs(a.Power - b.Power) > AimPowerEpsilon;
        }
    }
}
