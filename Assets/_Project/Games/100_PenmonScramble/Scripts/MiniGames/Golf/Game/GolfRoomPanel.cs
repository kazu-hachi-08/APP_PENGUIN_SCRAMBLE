using PenmonScramble.Common.Online;
using PenmonScramble.Core;
using PenmonScramble.Game;
using PenmonScramble.Golf.Core;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// 「みんなで遊ぶ」の部屋の待ちの窓に出す、ゴルフのコースの選択（ゴルフの部屋に入っている間だけ）。
    /// ホストが ◀ ▶ でコースを選び、選び直すたびにゲストへ送る（scramble.golf.choice。ゲストは見るだけ）。
    /// 解放はホストの記録（ストーリー連動）で決め、開いていない・準備中のコースを選んでいる間は「開始」を押せない。
    /// 試合が始まったら、部屋を引き継いでゴルフのシーンへ移る
    /// </summary>
    public class GolfRoomPanel : MonoBehaviour, IRoomMode
    {
        private const string HostNote = "コースを選んで「開始」";
        private const string GuestNote = "ホストが選んでいます";

        [SerializeField] private ModeSelectPanel _modeSelect;
        [SerializeField] private OnlineSession _session;
        [SerializeField] private GameObject _card;
        [SerializeField] private Text _courseNoText;
        [SerializeField] private Text _courseNameText;
        [Tooltip("ホールとパー")]
        [SerializeField] private Text _layoutText;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [SerializeField] private GameObject _lock;
        [SerializeField] private Text _lockText;
        [SerializeField] private Text _rewardText;
        [SerializeField] private Text _noteText;

        // ホストの選んだコースは、シーンを読み直しても（回って待ちの窓へ戻っても）覚えておく
        private static int _hostCourseNo;

        // メッセージの名前は配った版どうしで合わせるので変えない
        private readonly RoomChoiceLink _link = new RoomChoiceLink("scramble.golf");
        private readonly GolfCourseGuide _guide = new GolfCourseGuide();
        private bool _inRoom;
        private bool _isHost;
        private bool _selected;
        private int _guestCourseNo;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _hostCourseNo = 0;
        }

        private static PlayerRecord Record => GameSave.Record;

        private static MiniGameDef Game => MiniGameTable.Get(MiniGameTable.GolfId);

        public string Id => MiniGameTable.GolfId;

        public string Label => "ゴルフ";

        /// <summary>ホストが選んでいるコース。まだ選んでいなければ、開いていて遊べる中で一番先のコース</summary>
        private int HostCourseNo => Game.Course(_hostCourseNo) != null ? _hostCourseNo : LatestPlayable();

        /// <summary>画面に出すコース。ゲストはホストから届いたもの（まだ届いていなければ 0）</summary>
        private int ShownCourseNo => _isHost ? HostCourseNo : _guestCourseNo;

        private void Awake()
        {
            _modeSelect.RoomStateChanged += OnRoomStateChanged;
            _link.AskReceived += SendChoice;
            _link.ChoiceReceived += OnChoiceReceived;
            _prevButton.onClick.AddListener(() => MoveCourse(-1));
            _nextButton.onClick.AddListener(() => MoveCourse(1));
            Refresh();
        }

        private void OnDestroy()
        {
            _modeSelect.RoomStateChanged -= OnRoomStateChanged;
            _link.Close();
        }

        /// <summary>試合が始まってタイトルが隠れたら、待ちの窓の通信は終わり</summary>
        private void OnDisable()
        {
            _inRoom = false;
            _link.Close();
        }

        public void SetSelected(bool selected)
        {
            _selected = selected;
            Refresh();
        }

        /// <summary>ミニゲーム画面の「みんなで」から: 見ていたコースを選んでおく</summary>
        public void Preselect(int option)
        {
            if (Game.Course(option) != null)
            {
                _hostCourseNo = option;
            }

            SendChoice();
            Refresh();
        }

        /// <summary>全員がゴルフのシーンへ移る（部屋は試合ごと引き継ぐ。正しいコースはシーンに入ってからホストが送る）</summary>
        public bool StartMatch(int seat, int playerCount)
        {
            MiniGameLaunch.StartOnline(Game, ShownCourseNo, _session);
            return true;
        }

        private void OnRoomStateChanged(bool isHost, bool inRoom)
        {
            _isHost = isHost;
            _inRoom = inRoom;
            _guestCourseNo = 0;
            if (!inRoom)
            {
                _link.Close();
            }
            else if (isHost)
            {
                _link.OpenAsHost();
                SendChoice();
            }
            else
            {
                _link.OpenAsGuest();
            }

            Refresh();
        }

        private void OnChoiceReceived(byte[] data)
        {
            if (data == null || data.Length < 1 || Game.Course(data[0]) == null)
            {
                return;
            }

            _guestCourseNo = data[0];
            Refresh();
        }

        /// <summary>鍵の掛かったコースも見られる（どこまで進めば開くかがわかるように）。開始できないだけ</summary>
        private void MoveCourse(int step)
        {
            var next = Game.Course(HostCourseNo + step);
            if (!_isHost || next == null)
            {
                return;
            }

            _hostCourseNo = next.No;
            SendChoice();
            Refresh();
        }

        private void SendChoice()
        {
            if (_inRoom && _isHost)
            {
                _link.Send(new[] { (byte)HostCourseNo });
            }
        }

        private void Refresh()
        {
            var course = Game.Course(ShownCourseNo);
            var show = _inRoom && _selected && course != null;
            _card.SetActive(show);
            if (!show)
            {
                return;
            }

            var playable = IsPlayable(course);
            _courseNoText.text = $"コース {course.No}";
            _courseNameText.text = course.Name;
            _prevButton.gameObject.SetActive(_isHost);
            _nextButton.gameObject.SetActive(_isHost);
            _prevButton.interactable = Game.Course(course.No - 1) != null;
            _nextButton.interactable = Game.Course(course.No + 1) != null;
            _layoutText.text = _guide.Layout(course.No);
            ShowLock(course);
            ShowReward(course, playable || !_isHost);
            _noteText.text = _isHost ? HostNote : GuestNote;
            if (_isHost)
            {
                _modeSelect.SetStartAllowed(playable);
            }
        }

        /// <summary>鍵はホストだけ（ゲストは自分が開いていなくても、ホストの選んだコースを回れる）</summary>
        private void ShowLock(MiniGameCourseDef course)
        {
            var unlocked = MiniGameUnlock.IsUnlocked(course, Record);
            var ready = _guide.IsReady(course.No);
            var locked = _isHost && (!unlocked || !ready);
            _lock.SetActive(locked);
            if (locked)
            {
                _lockText.text = !unlocked ? $"ワールド {course.World} に入ると解放" : "このコースは準備中";
            }
        }

        /// <summary>報酬は見ている人の記録で決まる（初回ボーナスは、自分がまだ回っていなければ付く）。順位のボーナスも書く</summary>
        private void ShowReward(MiniGameCourseDef course, bool shown)
        {
            if (!shown)
            {
                _rewardText.text = string.Empty;
                return;
            }

            var firstPlay = new MiniGameRecords(Record.Data).Plays(Game.Id, course.No) == 0;
            var bonus = $"順位ボーナス 1位 +{GolfReward.RankBonusFor(1)}・2位 +{GolfReward.RankBonusFor(2)}・3位 +{GolfReward.RankBonusFor(3)}";
            _rewardText.text = $"{_guide.Reward(course.No, firstPlay)}\n{bonus}";
        }

        private static bool IsPlayable(MiniGameCourseDef course)
        {
            return MiniGameUnlock.IsUnlocked(course, Record) && GolfCourseTable.Get(course.No) != null;
        }

        private static int LatestPlayable()
        {
            var latest = Game.Courses[0].No;
            foreach (var course in Game.Courses)
            {
                if (IsPlayable(course))
                {
                    latest = course.No;
                }
            }

            return latest;
        }
    }
}
