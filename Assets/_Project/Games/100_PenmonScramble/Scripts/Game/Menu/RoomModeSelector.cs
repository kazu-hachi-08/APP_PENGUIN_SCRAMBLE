using System.Collections.Generic;
using System.Text;
using PenmonScramble.Common.Online;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 「みんなで遊ぶ」の部屋で遊ぶもの（ボスラッシュ / ゴルフ / …）を1つ決めて持つ。遊びは部屋を作った入口で決まり、待ちの窓では選び直さない
    /// （タイトルの「みんなで遊ぶ」はボスラッシュ、ミニゲーム画面の「みんなで」はそのミニゲーム）。
    /// ホストの遊びはゲストへ届き（scramble.room.choice）、ゲストはどの入口からコードを入れても、ホストの遊びの待ちの窓になる。
    /// 決まった遊びの板（IRoomMode）だけを出し、待ちの窓の上の見出しと「みんなで遊ぶ」の窓の見出しに遊びの名前を出す。
    /// 試合が始まったら、その遊びに始め方を聞く（ミニゲームは自分のシーンへ移る。ボスラッシュは StageRunner が始める）。
    /// 部屋に入っている間は、接続の状態（参加コード・人数）の文字を左の列へ寄せる
    /// </summary>
    public class RoomModeSelector : MonoBehaviour
    {
        private const string OnlineHeadingFormat = "みんなで遊ぶ ― {0}";
        private const string RoomHeadingFormat = "{0}の部屋";

        [SerializeField] private ModeSelectPanel _modeSelect;
        [Tooltip("IRoomMode を持つ部品。先頭が、入口を通らずに部屋を開いたときの遊び")]
        [SerializeField] private MonoBehaviour[] _modes;
        [Tooltip("待ちの窓の上の見出し（遊びの名前）")]
        [SerializeField] private Text _roomHeading;
        [Tooltip("「みんなで遊ぶ」の窓の見出し（どの遊びの部屋を作るか）。窓はロビーを作るときにできるので後からつなぐ")]
        [SerializeField] private Text _onlineHeading;

        [Header("接続の状態の文字")]
        [Tooltip("ModeSelectPanel が文面を入れる。部屋に入っている間は左の列へ寄せる")]
        [SerializeField] private RectTransform _status;
        [SerializeField] private Vector2 _statusAlonePosition;
        [SerializeField] private Vector2 _statusAloneSize;
        [SerializeField] private Vector2 _statusBesidePosition;
        [SerializeField] private Vector2 _statusBesideSize;

        // ホストの遊びは、シーンを読み直しても（遊んで待ちの窓へ戻っても）覚えておく。同じ顔ぶれで続けて遊べるように
        private static string _hostModeId;

        // メッセージの名前は配った版どうしで合わせるので変えない
        private readonly RoomChoiceLink _link = new RoomChoiceLink("scramble.room");
        private readonly List<IRoomMode> _list = new List<IRoomMode>();
        private bool _inRoom;
        private bool _isHost;
        private string _guestModeId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _hostModeId = null;
        }

        /// <summary>ホストの遊び。覚えていた遊びが並んでいなければ先頭</summary>
        private string HostModeId => Find(_hostModeId) != null ? _hostModeId : _list.Count > 0 ? _list[0].Id : null;

        /// <summary>この端末が見ている遊び。ゲストはホストから届いたもの（まだ届いていなければ null）</summary>
        private string CurrentId => _isHost ? HostModeId : _guestModeId;

        private void Awake()
        {
            foreach (var behaviour in _modes)
            {
                if (behaviour is IRoomMode mode)
                {
                    _list.Add(mode);
                }
            }

            _modeSelect.RoomStateChanged += OnRoomStateChanged;
            _link.AskReceived += SendMode;
            _link.ChoiceReceived += OnModeReceived;
            Refresh();
        }

        private void OnDestroy()
        {
            _modeSelect.RoomStateChanged -= OnRoomStateChanged;
            _link.Close();
        }

        /// <summary>試合が始まってタイトルが隠れたら、待ちの窓の通信は終わり（どの遊びかは覚えたまま。StartElsewhere で使う）</summary>
        private void OnDisable()
        {
            _inRoom = false;
            _link.Close();
        }

        /// <summary>その遊びが並んでいるか（ミニゲーム画面が「みんなで」を出すか決める）</summary>
        public bool Has(string modeId) => Find(modeId) != null;

        /// <summary>
        /// 入口から: この遊びの部屋を作ることにする（option はゴルフならコースの番号。待ちの窓の板で最初に選ばれている）。
        /// 部屋に入っている間は変えない（入口は部屋の外にしか無いが、遊びが途中で変わらないことをここで守る）
        /// </summary>
        public void Choose(string modeId, int option = 0)
        {
            var mode = Find(modeId);
            if (mode == null || _inRoom)
            {
                return;
            }

            _hostModeId = modeId;
            mode.Preselect(option);
            Refresh();
        }

        /// <summary>試合が始まった。部屋の遊びが別のシーンで遊ぶなら、そちらへ移り始めて true</summary>
        public bool StartElsewhere(int seat, int playerCount)
        {
            var mode = Find(CurrentId);
            return mode != null && mode.StartMatch(seat, playerCount);
        }

        private void OnRoomStateChanged(bool isHost, bool inRoom)
        {
            _isHost = isHost;
            _inRoom = inRoom;
            _guestModeId = null;
            if (!inRoom)
            {
                _link.Close();
            }
            else if (isHost)
            {
                _link.OpenAsHost();
                SendMode();
            }
            else
            {
                _link.OpenAsGuest();
            }

            Refresh();
        }

        private void SendMode()
        {
            var id = HostModeId;
            if (_inRoom && _isHost && id != null)
            {
                _link.Send(Encoding.UTF8.GetBytes(id));
            }
        }

        /// <summary>ゲストはホストの遊びに合わせる。知らない遊び（相手が新しい版）は選ばない（板を出さずに待つ）</summary>
        private void OnModeReceived(byte[] data)
        {
            if (data == null)
            {
                return;
            }

            var id = Encoding.UTF8.GetString(data);
            _guestModeId = Find(id) != null ? id : null;
            Refresh();
        }

        private void Refresh()
        {
            var current = _inRoom ? CurrentId : null;
            foreach (var mode in _list)
            {
                mode.SetSelected(mode.Id == current);
            }

            var shownMode = Find(current);
            _roomHeading.gameObject.SetActive(shownMode != null);
            _roomHeading.text = shownMode != null ? string.Format(RoomHeadingFormat, shownMode.Label) : string.Empty;
            RefreshOnlineHeading();

            var shown = shownMode != null;
            _status.anchoredPosition = shown ? _statusBesidePosition : _statusAlonePosition;
            _status.sizeDelta = shown ? _statusBesideSize : _statusAloneSize;
        }

        private void RefreshOnlineHeading()
        {
            var host = Find(HostModeId);
            if (_onlineHeading != null && host != null)
            {
                _onlineHeading.text = string.Format(OnlineHeadingFormat, host.Label);
            }
        }

        private IRoomMode Find(string id)
        {
            if (id == null)
            {
                return null;
            }

            foreach (var mode in _list)
            {
                if (mode.Id == id)
                {
                    return mode;
                }
            }

            return null;
        }
    }
}
