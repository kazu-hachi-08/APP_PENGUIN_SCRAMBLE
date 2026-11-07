using PenmonScramble.Common.Online;
using PenmonScramble.Core;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.MiniGames.Game
{
    /// <summary>
    /// 「みんなで遊ぶ」の部屋の待ちの窓に出す、チームで戦うミニゲーム（サッカー・野球）の選択（遊ぶものの札でそのミニゲームが選ばれている間だけ）。
    /// ホストが ◀ ▶ でコース、札で 対戦 / 協力 を選び、対戦では席（P1 = ホスト、P2〜 は入った順）の札を押して赤・青を入れ替える。
    /// 選び直すたびにゲストへ送る（「scramble.soccer.choice」など。ゲストは見るだけ）。
    /// 開いていない・準備中のコースや、対戦で片方に人がいない間は「開始」を押せない。試合が始まったら、部屋を引き継いでミニゲームのシーンへ移る。
    /// ミニゲームごとに違う所（名前・メッセージの名前・CPU の強さの一言・作ったコース）だけを子のクラスが決める
    /// </summary>
    public abstract class MiniGameTeamRoomPanel : MonoBehaviour, IRoomMode
    {
        private const string HostTeamNote = "席の札を押すと 赤・青 が入れ替わります";
        private const string NeedBothTeamsNote = "対戦は 赤と青の両方に人が必要です";
        private const string AloneNote = "ひとりのときは「協力」で CPU と遊べます";
        private const string GuestNote = "ホストが選んでいます";
        private const float UnselectedAlpha = 0.55f;
        private const float SelectedScale = 1.08f;
        private static readonly Color CoopSeatColor = new Color(0.55f, 0.58f, 0.66f);
        private const float LabelOutlineDarken = 0.5f;

        [SerializeField] private ModeSelectPanel _modeSelect;
        [SerializeField] private OnlineSession _session;
        [SerializeField] private GameObject _card;
        [SerializeField] private Text _stadiumNoText;
        [SerializeField] private Text _stadiumNameText;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;

        [Header("対戦 / 協力（並びは MiniGameOnlineMode の番号）")]
        [SerializeField] private Button[] _modeButtons;
        [SerializeField] private CanvasGroup[] _modeSlots;
        [SerializeField] private GameObject[] _modeMarks;

        [Header("席（P1〜P4）")]
        [SerializeField] private Button[] _seatButtons;
        [SerializeField] private Text[] _seatTexts;

        [Tooltip("CPU の強さ・仕掛けの一言")]
        [SerializeField] private Text _layoutText;
        [SerializeField] private GameObject _lock;
        [SerializeField] private Text _lockText;
        [SerializeField] private Text _rewardText;
        [SerializeField] private Text _noteText;

        private RoomChoiceLink _link;
        private bool _inRoom;
        private bool _isHost;
        private bool _selected;
        private int _memberCount = 1;
        private MiniGameRoomChoice _guestChoice;

        private static PlayerRecord Record => GameSave.Record;

        private MiniGameDef Game => MiniGameTable.Get(Id);

        private MiniGameRoomSetup Setup => MiniGameRoomSetup.For(Id);

        /// <summary>MiniGameTable の Id</summary>
        public abstract string Id { get; }

        public abstract string Label { get; }

        /// <summary>待ちの窓のメッセージの名前のもと（「scramble.soccer」なら scramble.soccer.choice / .ask）。配った版どうしで合わせるので変えない</summary>
        protected abstract string LinkName { get; }

        /// <summary>球場選びと同じ中身（準備中か・報酬）</summary>
        protected abstract IMiniGameCourseGuide Guide { get; }

        /// <summary>みんなでの中身の一言（協力は人数で上がった CPU の強さ、対戦は人どうし）</summary>
        protected abstract string OnlineLayout(int courseNo, MiniGameOnlineMode mode, int humans);

        /// <summary>そのコースをこの版で遊べるか（作ってある）</summary>
        protected abstract bool IsBuilt(int courseNo);

        /// <summary>ホストが選んでいるコース。まだ選んでいなければ、開いていて遊べる中で一番先のコース</summary>
        private int HostCourse => Game.Course(Setup.Course) != null ? Setup.Course : LatestPlayable();

        private MiniGameRoomChoice HostChoice => new MiniGameRoomChoice(HostCourse, Setup.Mode, _memberCount, Setup.Teams);

        /// <summary>画面に出す選び。ゲストはホストから届いたもの（まだ届いていなければ null）</summary>
        private MiniGameRoomChoice Shown => _isHost ? HostChoice : _guestChoice;

        private void Awake()
        {
            _link = new RoomChoiceLink(LinkName);
            _modeSelect.RoomStateChanged += OnRoomStateChanged;
            _session.OnMemberCountChanged += OnMemberCountChanged;
            _link.AskReceived += SendChoice;
            _link.ChoiceReceived += OnChoiceReceived;
            _prevButton.onClick.AddListener(() => MoveCourse(-1));
            _nextButton.onClick.AddListener(() => MoveCourse(1));
            for (var i = 0; i < _modeButtons.Length; i++)
            {
                var mode = (MiniGameOnlineMode)i;
                _modeButtons[i].onClick.AddListener(() => SelectMode(mode));
            }

            for (var i = 0; i < _seatButtons.Length; i++)
            {
                var seat = i;
                _seatButtons[i].onClick.AddListener(() => ToggleTeam(seat));
            }

            Refresh();
        }

        private void OnDestroy()
        {
            _modeSelect.RoomStateChanged -= OnRoomStateChanged;
            _session.OnMemberCountChanged -= OnMemberCountChanged;
            _link?.Close();
        }

        /// <summary>試合が始まってタイトルが隠れたら、待ちの窓の通信は終わり</summary>
        private void OnDisable()
        {
            _inRoom = false;
            _link?.Close();
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
                Setup.Course = option;
            }

            SendChoice();
            Refresh();
        }

        /// <summary>全員がミニゲームのシーンへ移る（部屋は試合ごと引き継ぐ。遊び方とチームはシーンに入ってからホストが送る）</summary>
        public bool StartMatch(int seat, int playerCount)
        {
            if (_isHost)
            {
                // 押した時の選びを、シーンに入ったホストが読む
                Setup.Course = HostCourse;
            }

            MiniGameLaunch.StartOnline(Game, Shown?.Course ?? 0, _session);
            return true;
        }

        private void OnRoomStateChanged(bool isHost, bool inRoom)
        {
            _isHost = isHost;
            _inRoom = inRoom;
            _guestChoice = null;
            _memberCount = isHost ? Mathf.Max(1, _session.MemberCount) : 1;
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

        /// <summary>ホストのみ届く: 人が入った・抜けた（席の札を増やし減らしする）</summary>
        private void OnMemberCountChanged(int count)
        {
            if (!_isHost)
            {
                return;
            }

            _memberCount = Mathf.Clamp(count, 1, MiniGameRoomChoice.MaxSeats);
            SendChoice();
            Refresh();
        }

        private void OnChoiceReceived(byte[] data)
        {
            var choice = MiniGameRoomChoice.Decode(data);
            if (choice == null || Game.Course(choice.Course) == null)
            {
                return;
            }

            _guestChoice = choice;
            Refresh();
        }

        /// <summary>鍵の掛かったコースも見られる（どこまで進めば開くかがわかるように）。開始できないだけ</summary>
        private void MoveCourse(int step)
        {
            var next = Game.Course(HostCourse + step);
            if (!_isHost || next == null)
            {
                return;
            }

            Setup.Course = next.No;
            SendChoice();
            Refresh();
        }

        private void SelectMode(MiniGameOnlineMode mode)
        {
            if (!_isHost)
            {
                return;
            }

            Setup.Mode = mode;
            SendChoice();
            Refresh();
        }

        /// <summary>対戦の間だけ、押した席の赤・青を入れ替える</summary>
        private void ToggleTeam(int seat)
        {
            if (!_isHost || Setup.Mode != MiniGameOnlineMode.Versus || seat >= _memberCount)
            {
                return;
            }

            Setup.SetTeam(seat, MiniGameTeams.Opponent(Setup.Teams[seat]));
            SendChoice();
            Refresh();
        }

        private void SendChoice()
        {
            if (_inRoom && _isHost)
            {
                _link.Send(HostChoice.Encode());
            }
        }

        private void Refresh()
        {
            var choice = Shown;
            var course = choice != null ? Game.Course(choice.Course) : null;
            var show = _inRoom && _selected && course != null;
            _card.SetActive(show);
            if (!show)
            {
                return;
            }

            var playable = IsPlayable(course);
            _stadiumNoText.text = $"{Game.CourseWord} {course.No}";
            _stadiumNameText.text = course.Name;
            _prevButton.gameObject.SetActive(_isHost);
            _nextButton.gameObject.SetActive(_isHost);
            _prevButton.interactable = Game.Course(course.No - 1) != null;
            _nextButton.interactable = Game.Course(course.No + 1) != null;
            ShowModes(choice);
            ShowSeats(choice);
            _layoutText.text = OnlineLayout(course.No, choice.Mode, choice.SeatCount);
            ShowLock(course);
            ShowReward(course, playable || !_isHost);
            _noteText.text = Note(choice);
            if (_isHost)
            {
                _modeSelect.SetStartAllowed(playable && choice.CanStart);
            }
        }

        private void ShowModes(MiniGameRoomChoice choice)
        {
            for (var i = 0; i < _modeButtons.Length; i++)
            {
                var selected = (int)choice.Mode == i;
                _modeButtons[i].interactable = _isHost;
                _modeSlots[i].alpha = selected ? 1f : UnselectedAlpha;
                _modeSlots[i].transform.localScale = Vector3.one * (selected ? SelectedScale : 1f);
                _modeMarks[i].SetActive(selected);
            }
        }

        /// <summary>部屋にいる人数ぶんの席の札。対戦はチームの色、協力は全員がそろって CPU と戦うので灰色</summary>
        private void ShowSeats(MiniGameRoomChoice choice)
        {
            var versus = choice.Mode == MiniGameOnlineMode.Versus;
            for (var seat = 0; seat < _seatButtons.Length; seat++)
            {
                var here = seat < choice.SeatCount;
                _seatButtons[seat].gameObject.SetActive(here);
                if (!here)
                {
                    continue;
                }

                var team = choice.TeamOf(seat);
                var color = versus ? MiniGameTeamColors.Team(team) : CoopSeatColor;
                _seatButtons[seat].interactable = _isHost && versus;
                _seatButtons[seat].image.color = color;
                _seatTexts[seat].text = versus ? $"P{seat + 1}  {MiniGameTeamColors.TeamName(team)}" : $"P{seat + 1}";
                // 字の縁取りも札の色に合わせる（作ったときの色のままだと、赤の札に青の縁が残る）
                var outline = _seatTexts[seat].GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = Color.Lerp(color, Color.black, LabelOutlineDarken);
                }
            }
        }

        private string Note(MiniGameRoomChoice choice)
        {
            if (!_isHost)
            {
                return GuestNote;
            }

            if (choice.Mode == MiniGameOnlineMode.Coop)
            {
                return $"{Game.CourseWord}と遊び方を選んで「開始」";
            }

            if (choice.SeatCount < 2)
            {
                return AloneNote;
            }

            return choice.CanStart ? HostTeamNote : NeedBothTeamsNote;
        }

        /// <summary>鍵はホストだけ（ゲストは自分が開いていなくても、ホストの選んだコースで遊べる）</summary>
        private void ShowLock(MiniGameCourseDef course)
        {
            var unlocked = MiniGameUnlock.IsUnlocked(course, Record);
            var ready = Guide.IsReady(course.No);
            var locked = _isHost && (!unlocked || !ready);
            _lock.SetActive(locked);
            if (locked)
            {
                _lockText.text = !unlocked ? $"ワールド {course.World} に入ると解放" : $"この{Game.CourseWord}は準備中";
            }
        }

        /// <summary>報酬は見ている人の記録で決まる（初めて勝ったボーナスは、自分がまだそのコースで勝っていなければ付く）</summary>
        private void ShowReward(MiniGameCourseDef course, bool shown)
        {
            if (!shown)
            {
                _rewardText.text = string.Empty;
                return;
            }

            var wins = new MiniGameRecords(Record.Data).Find(Game.Id, course.No)?.special ?? 0;
            _rewardText.text = Guide.Reward(course.No, wins == 0);
        }

        private bool IsPlayable(MiniGameCourseDef course)
        {
            return MiniGameUnlock.IsUnlocked(course, Record) && IsBuilt(course.No);
        }

        private int LatestPlayable()
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
