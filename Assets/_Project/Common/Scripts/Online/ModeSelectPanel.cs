using System;
using System.Collections;
using PenmonScramble.Common.Scene;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Common.Online
{
    /// <summary>
    /// 試合前に「コンピュータと対戦」か「オンライン対戦（部屋を作る／コードで参加）」かを選ばせるパネル。
    /// オンラインを選んだ場合は、相手と接続できるまでここで待たせる。
    /// 3人以上で遊べるゲームでは、ホストが「開始」を押すまでここで参加者を待つ（ロビー）。
    /// </summary>
    public class ModeSelectPanel : MonoBehaviour
    {
        [SerializeField] private OnlineSession _session;

        [Header("Menu")]
        [SerializeField] private GameObject _menuGroup;
        [SerializeField] private Button _npcButton;
        [SerializeField] private Button _hostButton;
        [SerializeField] private Button _joinButton;
        [SerializeField] private InputField _codeInput;
        [Tooltip("ゲームに入った直後に遊ばずに抜けられるようにする。未設定なら出さない（シーンに置いていなくても壊れないように）")]
        [SerializeField] private Button _titleButton;

        [Header("Waiting")]
        [SerializeField] private GameObject _waitingGroup;
        [SerializeField] private Text _statusText;
        [SerializeField] private Button _cancelButton;
        [SerializeField] private Button _copyButton;
        [SerializeField] private Text _copyButtonLabel;
        [Tooltip("3人以上の部屋でホストが試合を始めるボタン。1対1のゲームでは使わないので未設定でよい")]
        [SerializeField] private Button _startButton;

        [Header("Copy")]
        [SerializeField] private string _copyLabel = "コードをコピー";
        [SerializeField] private string _copiedLabel = "コピーしました";
        [SerializeField] private float _copiedLabelSeconds = 1.5f;

        [Header("Error")]
        [SerializeField] private string _connectFailedMessage = "接続できませんでした\nコードや通信状況を確認してください";
        [Tooltip("Unity のサービスの準備ができていない（Project ID 未リンクなど）とき。プレイヤーには直せないので、そう伝える")]
        [SerializeField] private string _servicesNotReadyMessage = "いまはオンラインで遊べません\nオンラインの準備ができていません";

        private Action _onNpcSelected;
        private Action<bool> _onOnlineReady;
        private Action<int, int> _onMatchStarted;
        private Action _onTitle;
        private int _maxPlayers = OnlineSession.DefaultMaxPlayers;
        private string _roomCode;

        private bool IsMultiRoom => _maxPlayers > OnlineSession.DefaultMaxPlayers;

        private void Awake()
        {
            _npcButton.onClick.AddListener(SelectNpc);
            _hostButton.onClick.AddListener(Host);
            _joinButton.onClick.AddListener(Join);
            _cancelButton.onClick.AddListener(CancelWaiting);
            _copyButton.onClick.AddListener(CopyRoomCode);
            if (_startButton != null) _startButton.onClick.AddListener(StartMatch);
            if (_titleButton != null) _titleButton.onClick.AddListener(ReturnToTitle);
        }

        private void OnEnable()
        {
            _session.OnPeerConnected += HandlePeerConnected;
            _session.OnPeerDisconnected += HandleLobbyDisconnected;
            _session.OnConnectionLost += HandleLobbyDisconnected;
            _session.OnMemberCountChanged += HandleMemberCountChanged;
            _session.OnMatchStarted += HandleMatchStarted;
        }

        private void OnDisable()
        {
            _session.OnPeerConnected -= HandlePeerConnected;
            _session.OnPeerDisconnected -= HandleLobbyDisconnected;
            _session.OnConnectionLost -= HandleLobbyDisconnected;
            _session.OnMemberCountChanged -= HandleMemberCountChanged;
            _session.OnMatchStarted -= HandleMatchStarted;
        }

        /// <param name="onNpcSelected">NPC戦を選んだ</param>
        /// <param name="onOnlineReady">相手と接続できた（引数は自分がホストか）</param>
        /// <param name="onTitle">「タイトルへ」の戻り先。未指定ならシーンを読み直してタイトルへ</param>
        public void Show(Action onNpcSelected, Action<bool> onOnlineReady, Action onTitle = null)
        {
            _onOnlineReady = onOnlineReady;
            _onMatchStarted = null;
            Open(onNpcSelected, OnlineSession.DefaultMaxPlayers, onTitle);
        }

        /// <summary>3人以上で遊べるゲーム用。ホストが「開始」を押すと、全員に席番号と人数が届く</summary>
        /// <param name="onNpcSelected">ひとりで遊ぶ（NPC戦）を選んだ</param>
        /// <param name="onMatchStarted">試合開始（引数は自分の席番号（ホスト=0）と総人数）</param>
        public void ShowMultiplayer(Action onNpcSelected, Action<int, int> onMatchStarted, int maxPlayers, Action onTitle = null)
        {
            _onOnlineReady = null;
            _onMatchStarted = onMatchStarted;
            Open(onNpcSelected, maxPlayers, onTitle);
        }

        private void Open(Action onNpcSelected, int maxPlayers, Action onTitle)
        {
            _onNpcSelected = onNpcSelected;
            _onTitle = onTitle;
            _maxPlayers = maxPlayers;
            gameObject.SetActive(true);
            ShowMenu();
        }

        private void SelectNpc()
        {
            gameObject.SetActive(false);
            _onNpcSelected?.Invoke();
        }

        private async void Host()
        {
            ShowWaiting("部屋を作っています…");
            try
            {
                string code = await _session.HostAsync(_maxPlayers);
                ShowCopyButton(code);
                if (IsMultiRoom)
                {
                    RefreshLobby(1);
                }
                else
                {
                    // チャットでコードを送りに行ったまま約10秒戻らないと、Relay に部屋ごと切られるため
                    SetStatus($"参加コード\n<size=64>{code}</size>\n相手の参加を待っています\nコードを送ったら すぐ戻ってきてください");
                }
            }
            catch (Exception e)
            {
                ShowError(e);
            }
        }

        private async void Join()
        {
            // チャットから貼り付けると前後に空白・改行が混ざったり小文字になったりするため整える
            string code = _codeInput.text.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code)) return;

            ShowWaiting("接続しています…");
            try
            {
                await _session.JoinAsync(code);
            }
            catch (Exception e)
            {
                ShowError(e);
            }
        }

        /// <summary>友達にチャットで送れるよう、コードだけをクリップボードに入れる</summary>
        private void CopyRoomCode()
        {
            WebBrowser.CopyText(_roomCode);
            StopAllCoroutines();
            StartCoroutine(ShowCopiedFeedback());
        }

        private IEnumerator ShowCopiedFeedback()
        {
            _copyButtonLabel.text = _copiedLabel;
            yield return new WaitForSecondsRealtime(_copiedLabelSeconds);
            _copyButtonLabel.text = _copyLabel;
        }

        private void ShowCopyButton(string code)
        {
            _roomCode = code;
            _copyButtonLabel.text = _copyLabel;
            _copyButton.gameObject.SetActive(true);
        }

        private void CancelWaiting()
        {
            _session.Leave();
            ShowMenu();
        }

        /// <summary>
        /// メニュー表示中はまだ接続していないので、セッションを切らずにそのまま戻ってよい。
        /// 接続待ち中は「キャンセル」でメニューに戻ってから押してもらう。
        /// ゲーム固有のタイトル画面を持つゲームは、Sceneを読み直さずにそこへ戻れるよう戻り先を渡せる
        /// </summary>
        private void ReturnToTitle()
        {
            if (_onTitle != null)
            {
                gameObject.SetActive(false);
                _onTitle();
                return;
            }

            if (SceneLoader.HasInstance)
            {
                SceneLoader.Instance.LoadTitleScene();
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.Title);
            }
        }

        private void StartMatch()
        {
            _session.StartMatch();
        }

        private void HandlePeerConnected(bool isHost)
        {
            // 3人以上の部屋はホストの開始を待つ。ホスト側の人数表示は HandleMemberCountChanged で行う
            if (IsMultiRoom)
            {
                if (!isHost) SetStatus("接続しました\nホストが開始するのを待っています");
                return;
            }

            gameObject.SetActive(false);
            _onOnlineReady?.Invoke(isHost);
        }

        private void HandleMemberCountChanged(int count)
        {
            RefreshLobby(count);
        }

        private void HandleMatchStarted(int seat, int count)
        {
            gameObject.SetActive(false);
            _onMatchStarted?.Invoke(seat, count);
        }

        /// <summary>ロビーで待っている間にホストが抜けたら、キャンセルでやり直してもらう</summary>
        private void HandleLobbyDisconnected()
        {
            _session.Leave();
            SetStartButtonVisible(false);
            _copyButton.gameObject.SetActive(false);
            SetStatus("接続が切れました\nキャンセルしてやり直してください");
        }

        /// <summary>ホストの待機画面。2人以上そろえば開始できる</summary>
        private void RefreshLobby(int count)
        {
            SetStatus($"参加コード\n<size=64>{_roomCode}</size>\n参加者 {count}/{_maxPlayers}人");
            SetStartButtonVisible(true);
            if (_startButton != null) _startButton.interactable = count >= OnlineSession.DefaultMaxPlayers;
        }

        /// <summary>
        /// 接続失敗の理由（コード違い・通信なし等）は細かく分けず、ログに残して待機画面で止める。
        /// キャンセルでメニューに戻ればやり直せる。
        /// ただしサービスの準備ができていない失敗は、プレイヤーがコードや通信を見直しても直らないので、別の文で伝える
        /// </summary>
        private void ShowError(Exception e)
        {
            Debug.LogWarning($"[ModeSelectPanel] オンライン接続に失敗しました: {e}");
            _session.Leave();
            _copyButton.gameObject.SetActive(false);
            SetStartButtonVisible(false);
            SetStatus(OnlineSession.IsServicesSetupError(e) ? _servicesNotReadyMessage : _connectFailedMessage);
        }

        private void ShowMenu()
        {
            _menuGroup.SetActive(true);
            _waitingGroup.SetActive(false);
        }

        private void ShowWaiting(string status)
        {
            _menuGroup.SetActive(false);
            _waitingGroup.SetActive(true);
            // コードが発行されるまで（部屋作成中・参加側の接続中）はコピーするものがないので隠す
            _copyButton.gameObject.SetActive(false);
            SetStartButtonVisible(false);
            SetStatus(status);
        }

        private void SetStartButtonVisible(bool visible)
        {
            if (_startButton != null) _startButton.gameObject.SetActive(visible);
        }

        private void SetStatus(string status)
        {
            _statusText.text = status;
        }
    }
}
