using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using PenguinScramble.Common.Profile;
using UnityEngine;

namespace PenguinScramble.Common.Online
{
    /// <summary>
    /// オンライン対戦の接続だけを担当する（部屋を作る／参加コードで入る／抜ける）。
    /// 通信経路は Unity Relay に任せ、NAT越えやIP入力をプレイヤーにさせない。
    /// 試合中にやり取りする内容はゲーム側の通信クラスが扱う。
    ///
    /// 3人以上の部屋（HostAsync の maxPlayers が3以上）では、相手が来ても自動では始めず、
    /// ホストが StartMatch を呼んだときに参加順で席番号を配る。
    /// </summary>
    public class OnlineSession : MonoBehaviour
    {
        /// <summary>1対1の対戦の定員。引数を省略したときは2人部屋</summary>
        public const int DefaultMaxPlayers = 2;

        private const string StartMessage = "session.start";
        private const string NameMessage = "session.name";

        // 名前は可変長なので書き込み時に伸ばす。6文字×最大4人でも MaxMessageSize には遠く届かない
        private const int InitialMessageSize = 64;
        private const int MaxMessageSize = 1024;

        /// <summary>
        /// 1対1で相手の名前を待つ上限。古いビルドの相手は名前を送ってこないので、
        /// 待ち続けると永遠に始まらない。過ぎたら名前なし（「P2」等）で始める
        /// </summary>
        private const float NameWaitSeconds = 3f;

        /// <summary>
        /// Relay はこの秒数だけ通信が無いと切断する。スマホでアプリを裏に回す・ブラウザのタブを隠すと
        /// Unity ごと止まって通信も止まるので、これ以上止まっていたら戻ってきても続けられない
        /// </summary>
        private const double RelayIdleSeconds = 10;

        /// <summary>
        /// 相手が落ちたと気づくまでの時間。既定の30秒だと「固まった」と思われてアプリを落とされるので、
        /// Relay の無通信切断と同じくらいにする
        /// </summary>
        private const int DisconnectTimeoutMs = 10000;

        /// <summary>接続完了（相手が揃い、1対1では名前交換も済んだ）。引数は自分がホストかどうか</summary>
        public event Action<bool> OnPeerConnected;

        /// <summary>試合中に相手との接続が切れた</summary>
        public event Action OnPeerDisconnected;

        /// <summary>自分の端末が裏に回って止まっていたため、部屋から抜けた（相手には切断が届く）</summary>
        public event Action OnConnectionLost;

        /// <summary>3人以上の部屋のホストのみ：参加人数（ホスト自身を含む）が変わった</summary>
        public event Action<int> OnMemberCountChanged;

        /// <summary>3人以上の部屋のみ：試合開始。引数は自分の席番号（ホスト=0）と総人数</summary>
        public event Action<int, int> OnMatchStarted;

        public bool IsHost { get; private set; }

        /// <summary>ホストから見た参加者（自分以外）。席番号を参加順で配るため順番を保つ</summary>
        private readonly List<ulong> _memberIds = new List<ulong>();

        /// <summary>3人以上の部屋のホストのみ：参加者から届いた名前。開始メッセージで全員に配る</summary>
        private readonly Dictionary<ulong, string> _memberNames = new Dictionary<ulong, string>();

        /// <summary>1対1で相手の名前を待っている間だけ動く（届くかタイムアウトで止まる）</summary>
        private Coroutine _nameWait;

        private ISession _session;
        private bool _peerConnected;
        private int _maxPlayers = DefaultMaxPlayers;
        private bool _matchStarted;

        /// <summary>前のフレームの時刻。止まっていた間も進む壁時計で測る（Time は止まっている間を数えない端末があるため）</summary>
        private DateTime _lastFrameUtc = DateTime.UtcNow;

        private bool IsMultiRoom => _maxPlayers > DefaultMaxPlayers;

        private static bool IsBrowser => Application.platform == RuntimePlatform.WebGLPlayer;

        /// <summary>
        /// ブラウザはUDP(dtls)を使えないので、Relayとの通信をWebSocket(wss)にする。
        /// 相手の端末とは独立に選べるので、スマホ版とブラウザ版でも対戦できる
        /// </summary>
        private static NetworkOptions PlatformNetworkOptions => new NetworkOptions
        {
            RelayProtocol = IsBrowser ? RelayProtocol.WSS : RelayProtocol.DTLS
        };

        /// <summary>
        /// 部屋を作り、参加コードを発行する。
        /// 相手の参加は OnPeerConnected（3人以上の部屋では OnMemberCountChanged）で通知する
        /// </summary>
        public async Task<string> HostAsync(int maxPlayers = DefaultMaxPlayers)
        {
            await PrepareAsync();

            // 部屋の作成完了より先に相手が接続してきても取りこぼさないよう、先にホストとして扱う
            IsHost = true;
            _maxPlayers = maxPlayers;
            var options = new SessionOptions { MaxPlayers = maxPlayers }.WithRelayNetwork().WithNetworkOptions(PlatformNetworkOptions);
            _session = await MultiplayerService.Instance.CreateSessionAsync(options);

            // CustomMessagingManager は接続開始後にしか無いので、部屋を作ってから登録する。
            // ゲストは参加コードを受け取ってから入ってくるため、ここより先に名前が届くことはない
            NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(NameMessage, ReceiveName);
            return _session.Code;
        }

        /// <summary>参加コードで部屋に入る。接続できたら OnPeerConnected で通知する</summary>
        public async Task JoinAsync(string code)
        {
            await PrepareAsync();

            IsHost = false;
            var options = new JoinSessionOptions().WithNetworkOptions(PlatformNetworkOptions);
            _session = await MultiplayerService.Instance.JoinSessionByCodeAsync(code.Trim().ToUpperInvariant(), options);

            // 部屋の定員はホストが決めるので、参加側は部屋の情報から知る
            _maxPlayers = _session.MaxPlayers;

            var messaging = NetworkManager.Singleton.CustomMessagingManager;
            messaging.RegisterNamedMessageHandler(StartMessage, ReceiveStart);
            messaging.RegisterNamedMessageHandler(NameMessage, ReceiveName);

            // ホストからは送らず、ゲストが名乗ったら返事をもらう形にする。
            // ホストが先に送ると、ゲスト側でハンドラを登録する前に届いて捨てられることがあるため
            SendName(NetworkManager.ServerClientId);

            if (IsMultiRoom)
            {
                // 3人以上の部屋では全員の名前は開始メッセージで届くので、ここでは待たない
                NotifyPeerConnected();
                return;
            }

            StartNameWait();
        }

        /// <summary>
        /// 3人以上の部屋のホストが、集まったメンバーで試合を始める。席番号は参加順（ホスト=0）。
        /// 以降に入ってきた人は AddMember で切断する
        /// </summary>
        public void StartMatch()
        {
            if (!IsHost || _matchStarted || _memberIds.Count == 0) return;

            _matchStarted = true;
            int count = _memberIds.Count + 1;
            string[] names = CollectMemberNames();
            for (int i = 0; i < _memberIds.Count; i++)
            {
                SendStart(_memberIds[i], i + 1, names);
            }

            SeatNames.UseOnline(names);
            OnMatchStarted?.Invoke(0, count);
        }

        /// <summary>部屋から抜ける。待機中のキャンセルとシーン離脱の両方で使う</summary>
        public void Leave()
        {
            // オフラインでは端末のスリープ設定に戻す（電池を無駄に使わないため）
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
            UnsubscribeNetworkEvents();
            StopNameWait();
            _memberIds.Clear();
            _memberNames.Clear();
            _matchStarted = false;
            _maxPlayers = DefaultMaxPlayers;

            if (_session == null) return;

            // 抜ける処理の完了は待たない（シーン遷移を止めないため）
            _ = _session.LeaveAsync();
            _session = null;
            _peerConnected = false;
        }

        private void OnDestroy()
        {
            Leave();
        }

        /// <summary>
        /// 裏から戻った最初のフレームで、止まっていた時間を見る。
        /// OnApplicationPause / Focus は機種やブラウザで来方が違い、Windows では裏でも動き続けるので、
        /// 実際にフレームが止まっていた時間で判断する
        /// </summary>
        private void Update()
        {
            DateTime now = DateTime.UtcNow;
            double frozenSeconds = (now - _lastFrameUtc).TotalSeconds;
            _lastFrameUtc = now;
            if (_session == null || frozenSeconds < RelayIdleSeconds) return;

            Debug.LogWarning($"[OnlineSession] {frozenSeconds:F0}秒止まっていたので Relay に切られたとみなして抜けます");
            Leave();
            OnConnectionLost?.Invoke();
        }

        /// <summary>1対1なので、ホストから見た相手は唯一の接続クライアント、クライアントから見た相手はホスト</summary>
        public static bool TryGetPeerId(out ulong peerId)
        {
            var network = NetworkManager.Singleton;
            peerId = NetworkManager.ServerClientId;
            if (network == null) return false;
            if (!network.IsServer) return true;

            foreach (ulong id in network.ConnectedClientsIds)
            {
                if (id == network.LocalClientId) continue;

                peerId = id;
                return true;
            }

            return false;
        }

        private async Task PrepareAsync()
        {
            // 相手を待つ間に画面が消えるとアプリが止まり、Relay の無通信タイムアウト（約10秒）で切断されるため
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            // 直前にオフラインで遊んでいると席0がユーザー名のままなので、名前交換が済むまでは「P1」「P2」に戻しておく
            SeatNames.UseOnline(null);
            EnsureNetworkManager();
            await SignInAsync();
            SubscribeNetworkEvents();
        }

        private static async Task SignInAsync()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                var options = new InitializationOptions();
#if UNITY_EDITOR
                // エディタを2つ立ち上げて試すと同じ匿名IDになり、自分の部屋に参加できなくなるため
                options.SetProfile("editor" + UnityEngine.Random.Range(0, 100000));
#endif
                await UnityServices.InitializeAsync(options);
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }

        /// <summary>
        /// NetworkManager はシーンに置かず実行時に1つだけ作る。
        /// Scene に置くとオフライン（NPC戦）でも常に存在してしまい、2人開発での Scene 差分も増えるため。
        /// NetworkManager 自身が DontDestroyOnLoad になるので、再戦時は作り直さず使い回す。
        /// </summary>
        private static void EnsureNetworkManager()
        {
            if (NetworkManager.Singleton != null) return;

            var obj = new GameObject("NetworkManager");
            var transport = obj.AddComponent<UnityTransport>();
            transport.UseWebSockets = IsBrowser;
            transport.DisconnectTimeoutMS = DisconnectTimeoutMs;
            var manager = obj.AddComponent<NetworkManager>();
            manager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,

                // 両者とも同じシーンを自分で読み込んでいるので、NGOのシーン同期は使わない
                EnableSceneManagement = false
            };
        }

        private void SubscribeNetworkEvents()
        {
            UnsubscribeNetworkEvents();
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
        }

        private void UnsubscribeNetworkEvents()
        {
            if (NetworkManager.Singleton == null) return;

            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
            NetworkManager.Singleton.CustomMessagingManager?.UnregisterNamedMessageHandler(StartMessage);
            NetworkManager.Singleton.CustomMessagingManager?.UnregisterNamedMessageHandler(NameMessage);
        }

        private void HandleClientConnected(ulong clientId)
        {
            // ホスト自身の接続は無視し、相手が入ってきたときだけ試合を始める
            if (!IsHost || clientId == NetworkManager.Singleton.LocalClientId) return;

            if (IsMultiRoom)
            {
                AddMember(clientId);
                return;
            }

            // 接続通知より先に名前が届いて開始済みなら、待つものはない
            if (_peerConnected) return;

            // ゲストの名前が届いたら ReceiveName から開始する。届かなければタイムアウトで開始
            StartNameWait();
        }

        private void AddMember(ulong clientId)
        {
            // 試合中に途中参加されると手番の数が合わなくなるので断る
            if (_matchStarted)
            {
                NetworkManager.Singleton.DisconnectClient(clientId);
                return;
            }

            _memberIds.Add(clientId);
            _peerConnected = true;
            OnMemberCountChanged?.Invoke(_memberIds.Count + 1);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (IsHost && IsMultiRoom)
            {
                HandleMemberDisconnected(clientId);
                return;
            }

            if (_nameWait != null)
            {
                HandleDisconnectedWhileNaming();
                return;
            }

            if (!_peerConnected) return;

            _peerConnected = false;
            OnPeerDisconnected?.Invoke();
        }

        /// <summary>
        /// 開始前に抜けた人は人数を減らすだけ。試合中に1人でも抜けたら全員の試合を終える（途中離脱の継続・再接続はしない）。
        /// ホストが部屋を閉じると残りのクライアントにも切断が届き、それぞれの端末で試合が終わる
        /// </summary>
        private void HandleMemberDisconnected(ulong clientId)
        {
            if (!_memberIds.Remove(clientId)) return;

            _memberNames.Remove(clientId);

            if (!_matchStarted)
            {
                _peerConnected = _memberIds.Count > 0;
                OnMemberCountChanged?.Invoke(_memberIds.Count + 1);
                return;
            }

            OnPeerDisconnected?.Invoke();
            Leave();
        }

        /// <summary>席順（ホスト=0、以降は参加順）の名前。名前が届いていない人は空欄にして「P{n}」表示に任せる</summary>
        private string[] CollectMemberNames()
        {
            var names = new string[_memberIds.Count + 1];
            names[0] = UserProfile.Name;
            for (int i = 0; i < _memberIds.Count; i++)
            {
                names[i + 1] = _memberNames.TryGetValue(_memberIds[i], out string name) ? name : string.Empty;
            }

            return names;
        }

        private static void SendStart(ulong clientId, int seat, string[] names)
        {
            using var writer = new FastBufferWriter(InitialMessageSize, Allocator.Temp, MaxMessageSize);
            writer.WriteValueSafe(seat);
            writer.WriteValueSafe(names.Length);
            foreach (string name in names) writer.WriteValueSafe(name);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(StartMessage, clientId, writer,
                NetworkDelivery.ReliableSequenced);
        }

        private void ReceiveStart(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int seat);
            reader.ReadValueSafe(out int count);
            var names = new string[count];
            for (int i = 0; i < count; i++)
            {
                reader.ReadValueSafe(out string name);
                names[i] = UserProfile.Sanitize(name);
            }

            _matchStarted = true;
            SeatNames.UseOnline(names);
            OnMatchStarted?.Invoke(seat, count);
        }

        // ---- 名前交換 ----

        private static void SendName(ulong clientId)
        {
            using var writer = new FastBufferWriter(InitialMessageSize, Allocator.Temp, MaxMessageSize);
            writer.WriteValueSafe(UserProfile.Name);
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(NameMessage, clientId, writer,
                NetworkDelivery.ReliableSequenced);
        }

        private void ReceiveName(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out string raw);
            // 相手が別ビルドでも表示を壊さないよう、自分で保存するときと同じ整形をかける
            string name = UserProfile.Sanitize(raw);

            if (!IsHost)
            {
                // 1対1でホストから返事が来た。ホスト=席0、自分=席1
                FinishNameExchange(new[] { name, UserProfile.Name });
                return;
            }

            if (IsMultiRoom)
            {
                _memberNames[senderId] = name;
                return;
            }

            // 1対1のホスト：返事として自分の名前を返してから始める
            SendName(senderId);
            FinishNameExchange(new[] { UserProfile.Name, name });
        }

        private void StartNameWait()
        {
            StopNameWait();
            _nameWait = StartCoroutine(WaitNameOrTimeout());
        }

        private void StopNameWait()
        {
            if (_nameWait == null) return;

            StopCoroutine(_nameWait);
            _nameWait = null;
        }

        private IEnumerator WaitNameOrTimeout()
        {
            yield return new WaitForSecondsRealtime(NameWaitSeconds);

            _nameWait = null;
            Debug.LogWarning("[OnlineSession] 相手の名前が届かなかったので既定の表示名で開始します");
            FinishNameExchange(IsHost ? new[] { UserProfile.Name, string.Empty } : new[] { string.Empty, UserProfile.Name });
        }

        private void FinishNameExchange(string[] names)
        {
            // 開始済みなら、タイムアウト後に遅れて届いた名前なので無視する（試合中に表示が変わらないように）
            if (_peerConnected) return;

            StopNameWait();
            SeatNames.UseOnline(names);
            Debug.Log($"[OnlineSession] 名前交換完了: {string.Join(" / ", names)}");
            NotifyPeerConnected();
        }

        /// <summary>
        /// 名前を待っている間に相手が抜けた。ホストは次の相手を待ち続け、
        /// ゲストはホストがいなくなったので待機画面に切断を知らせる
        /// </summary>
        private void HandleDisconnectedWhileNaming()
        {
            StopNameWait();
            if (!IsHost) OnPeerDisconnected?.Invoke();
        }

        private void NotifyPeerConnected()
        {
            if (_peerConnected) return;

            _peerConnected = true;
            OnPeerConnected?.Invoke(IsHost);
        }
    }
}
