using System.Collections.Generic;
using System.Text;
using PenmonScramble.Common.Online;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 「みんなで遊ぶ」の部屋の待ちの窓の右上に並べる「遊ぶもの」の札（ボスラッシュ / ゴルフ / …）。
    /// ホストが札で切り替えると、ゲストへ届く（scramble.room.choice。ゲストは見るだけ）。選ばれた遊びの板（IRoomMode）だけを出す。
    /// 試合が始まったら、選ばれた遊びに始め方を聞く（ミニゲームは自分のシーンへ移る。ボスラッシュは StageRunner が始める）。
    /// 部屋に入っている間は、接続の状態（参加コード・人数）の文字を左の列へ寄せる
    /// </summary>
    public class RoomModeSelector : MonoBehaviour
    {
        private const float UnselectedAlpha = 0.55f;
        private const float SelectedScale = 1.08f;

        [SerializeField] private ModeSelectPanel _modeSelect;
        [Tooltip("IRoomMode を持つ部品。並びが札の並び（先頭が最初に選ばれている遊び）")]
        [SerializeField] private MonoBehaviour[] _modes;
        [Tooltip("添字は _modes と同じ")]
        [SerializeField] private Button[] _tabs;
        [Tooltip("札の入れ物（選んでいる札を大きく濃く、ほかを薄くする。ボタン自身の動きとぶつからないよう外側で）")]
        [SerializeField] private CanvasGroup[] _tabSlots;
        [Tooltip("選んでいる札の後ろに光る白い縁")]
        [SerializeField] private GameObject[] _tabMarks;
        [SerializeField] private GameObject _tabRow;

        [Header("接続の状態の文字")]
        [Tooltip("ModeSelectPanel が文面を入れる。部屋に入っている間は左の列へ寄せる")]
        [SerializeField] private RectTransform _status;
        [SerializeField] private Vector2 _statusAlonePosition;
        [SerializeField] private Vector2 _statusAloneSize;
        [SerializeField] private Vector2 _statusBesidePosition;
        [SerializeField] private Vector2 _statusBesideSize;

        // ホストの選んだ遊びは、シーンを読み直しても（遊んで待ちの窓へ戻っても）覚えておく。続けて同じ遊びをしやすいように
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

        /// <summary>ホストが選んでいる遊び。覚えていた遊びが並んでいなければ先頭</summary>
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

            for (var i = 0; i < _tabs.Length && i < _list.Count; i++)
            {
                var index = i;
                _tabs[i].onClick.AddListener(() => Select(index));
                var label = _tabs[i].GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = _list[i].Label;
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

        /// <summary>ミニゲーム画面の「みんなで」から: この遊びを選んでおく（部屋を作ったら、これを遊ぶ）</summary>
        public void Preselect(string modeId, int option)
        {
            var mode = Find(modeId);
            if (mode == null)
            {
                return;
            }

            _hostModeId = modeId;
            mode.Preselect(option);
            Refresh();
        }

        /// <summary>試合が始まった。選ばれた遊びが別のシーンで遊ぶなら、そちらへ移り始めて true</summary>
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

        private void Select(int index)
        {
            if (!_isHost || !_inRoom || index < 0 || index >= _list.Count)
            {
                return;
            }

            _hostModeId = _list[index].Id;
            SendMode();
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

        /// <summary>知らない遊び（相手が新しい版）は選ばない（板を出さずに待つ）</summary>
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
            for (var i = 0; i < _list.Count; i++)
            {
                _list[i].SetSelected(_list[i].Id == current);
            }

            var shown = current != null;
            _tabRow.SetActive(shown);
            for (var i = 0; i < _tabs.Length && i < _list.Count; i++)
            {
                var selected = _list[i].Id == current;
                _tabs[i].interactable = _isHost;
                _tabSlots[i].alpha = selected ? 1f : UnselectedAlpha;
                _tabSlots[i].transform.localScale = Vector3.one * (selected ? SelectedScale : 1f);
                _tabMarks[i].SetActive(selected);
            }

            _status.anchoredPosition = shown ? _statusBesidePosition : _statusAlonePosition;
            _status.sizeDelta = shown ? _statusBesideSize : _statusAloneSize;
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
