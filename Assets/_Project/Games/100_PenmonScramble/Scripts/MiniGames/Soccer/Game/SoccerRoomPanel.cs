using PenmonScramble.Common.Online;
using PenmonScramble.Core;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using PenmonScramble.Soccer.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// 「みんなで遊ぶ」の部屋の待ちの窓に出す、サッカーの選択（遊ぶものの札でサッカーが選ばれている間だけ）。
    /// ホストが ◀ ▶ でスタジアム、札で 対戦 / 協力 を選び、対戦では席（P1 = ホスト、P2〜 は入った順）の札を押して赤・青を入れ替える。
    /// 選び直すたびにゲストへ送る（scramble.soccer.choice。ゲストは見るだけ）。
    /// 開いていない・準備中のスタジアムや、対戦で片方に人がいない間は「開始」を押せない。試合が始まったら、部屋を引き継いでサッカーのシーンへ移る
    /// </summary>
    public class SoccerRoomPanel : MonoBehaviour, IRoomMode
    {
        private const string HostNote = "スタジアムと遊び方を選んで「開始」";
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

        [Header("対戦 / 協力（並びは SoccerOnlineMode の番号）")]
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

        // メッセージの名前は配った版どうしで合わせるので変えない
        private readonly RoomChoiceLink _link = new RoomChoiceLink("scramble.soccer");
        private readonly SoccerCourseGuide _guide = new SoccerCourseGuide();
        private bool _inRoom;
        private bool _isHost;
        private bool _selected;
        private int _memberCount = 1;
        private SoccerRoomChoice _guestChoice;

        private static PlayerRecord Record => GameSave.Record;

        private static MiniGameDef Game => MiniGameTable.Get(MiniGameTable.SoccerId);

        public string Id => MiniGameTable.SoccerId;

        public string Label => "サッカー";

        /// <summary>ホストが選んでいるスタジアム。まだ選んでいなければ、開いていて遊べる中で一番先のスタジアム</summary>
        private static int HostStadium => Game.Course(SoccerRoomSetup.Stadium) != null ? SoccerRoomSetup.Stadium : LatestPlayable();

        private SoccerRoomChoice HostChoice => new SoccerRoomChoice(HostStadium, SoccerRoomSetup.Mode, _memberCount, SoccerRoomSetup.Teams);

        /// <summary>画面に出す選び。ゲストはホストから届いたもの（まだ届いていなければ null）</summary>
        private SoccerRoomChoice Shown => _isHost ? HostChoice : _guestChoice;

        private void Awake()
        {
            _modeSelect.RoomStateChanged += OnRoomStateChanged;
            _session.OnMemberCountChanged += OnMemberCountChanged;
            _link.AskReceived += SendChoice;
            _link.ChoiceReceived += OnChoiceReceived;
            _prevButton.onClick.AddListener(() => MoveStadium(-1));
            _nextButton.onClick.AddListener(() => MoveStadium(1));
            for (var i = 0; i < _modeButtons.Length; i++)
            {
                var mode = (SoccerOnlineMode)i;
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

        /// <summary>ミニゲーム画面の「みんなで」から: 見ていたスタジアムを選んでおく</summary>
        public void Preselect(int option)
        {
            if (Game.Course(option) != null)
            {
                SoccerRoomSetup.Stadium = option;
            }

            SendChoice();
            Refresh();
        }

        /// <summary>全員がサッカーのシーンへ移る（部屋は試合ごと引き継ぐ。遊び方とチームはシーンに入ってからホストが送る）</summary>
        public bool StartMatch(int seat, int playerCount)
        {
            if (_isHost)
            {
                // 押した時の選びを、シーンに入ったホストが読む
                SoccerRoomSetup.Stadium = HostStadium;
            }

            MiniGameLaunch.StartOnline(Game, Shown?.Stadium ?? 0, _session, seat, playerCount);
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

            _memberCount = Mathf.Clamp(count, 1, SoccerRoomChoice.MaxSeats);
            SendChoice();
            Refresh();
        }

        private void OnChoiceReceived(byte[] data)
        {
            var choice = SoccerRoomChoice.Decode(data);
            if (choice == null || Game.Course(choice.Stadium) == null)
            {
                return;
            }

            _guestChoice = choice;
            Refresh();
        }

        /// <summary>鍵の掛かったスタジアムも見られる（どこまで進めば開くかがわかるように）。開始できないだけ</summary>
        private void MoveStadium(int step)
        {
            var next = Game.Course(HostStadium + step);
            if (!_isHost || next == null)
            {
                return;
            }

            SoccerRoomSetup.Stadium = next.No;
            SendChoice();
            Refresh();
        }

        private void SelectMode(SoccerOnlineMode mode)
        {
            if (!_isHost)
            {
                return;
            }

            SoccerRoomSetup.Mode = mode;
            SendChoice();
            Refresh();
        }

        /// <summary>対戦の間だけ、押した席の赤・青を入れ替える</summary>
        private void ToggleTeam(int seat)
        {
            if (!_isHost || SoccerRoomSetup.Mode != SoccerOnlineMode.Versus || seat >= _memberCount)
            {
                return;
            }

            var team = SoccerRoomSetup.Teams[seat];
            SoccerRoomSetup.SetTeam(seat, SoccerRules.Opponent(team));
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
            var stadium = choice != null ? Game.Course(choice.Stadium) : null;
            var show = _inRoom && _selected && stadium != null;
            _card.SetActive(show);
            if (!show)
            {
                return;
            }

            var playable = IsPlayable(stadium);
            _stadiumNoText.text = $"スタジアム {stadium.No}";
            _stadiumNameText.text = stadium.Name;
            _prevButton.gameObject.SetActive(_isHost);
            _nextButton.gameObject.SetActive(_isHost);
            _prevButton.interactable = Game.Course(stadium.No - 1) != null;
            _nextButton.interactable = Game.Course(stadium.No + 1) != null;
            ShowModes(choice);
            ShowSeats(choice);
            _layoutText.text = _guide.OnlineLayout(stadium.No, choice.Mode, choice.SeatCount);
            ShowLock(stadium);
            ShowReward(stadium, playable || !_isHost);
            _noteText.text = Note(choice);
            if (_isHost)
            {
                _modeSelect.SetStartAllowed(playable && choice.CanStart);
            }
        }

        private void ShowModes(SoccerRoomChoice choice)
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

        /// <summary>部屋にいる人数ぶんの席の札。対戦はチームの色、協力は全員がそろって CPU と戦うので灰色で「みんな」</summary>
        private void ShowSeats(SoccerRoomChoice choice)
        {
            var versus = choice.Mode == SoccerOnlineMode.Versus;
            for (var seat = 0; seat < _seatButtons.Length; seat++)
            {
                var here = seat < choice.SeatCount;
                _seatButtons[seat].gameObject.SetActive(here);
                if (!here)
                {
                    continue;
                }

                var team = choice.TeamOf(seat);
                var color = versus ? SoccerColors.Team(team) : CoopSeatColor;
                _seatButtons[seat].interactable = _isHost && versus;
                _seatButtons[seat].image.color = color;
                _seatTexts[seat].text = versus ? $"P{seat + 1}  {SoccerColors.TeamName(team)}" : $"P{seat + 1}";
                // 字の縁取りも札の色に合わせる（作ったときの色のままだと、赤の札に青の縁が残る）
                var outline = _seatTexts[seat].GetComponent<Outline>();
                if (outline != null)
                {
                    outline.effectColor = Color.Lerp(color, Color.black, LabelOutlineDarken);
                }
            }
        }

        private string Note(SoccerRoomChoice choice)
        {
            if (!_isHost)
            {
                return GuestNote;
            }

            if (choice.Mode == SoccerOnlineMode.Coop)
            {
                return HostNote;
            }

            if (choice.SeatCount < 2)
            {
                return AloneNote;
            }

            return choice.CanStart ? HostTeamNote : NeedBothTeamsNote;
        }

        /// <summary>鍵はホストだけ（ゲストは自分が開いていなくても、ホストの選んだスタジアムで遊べる）</summary>
        private void ShowLock(MiniGameCourseDef stadium)
        {
            var unlocked = MiniGameUnlock.IsUnlocked(stadium, Record);
            var ready = _guide.IsReady(stadium.No);
            var locked = _isHost && (!unlocked || !ready);
            _lock.SetActive(locked);
            if (locked)
            {
                _lockText.text = !unlocked ? $"ワールド {stadium.World} に入ると解放" : "このスタジアムは準備中";
            }
        }

        /// <summary>報酬は見ている人の記録で決まる（初めて勝ったボーナスは、自分がまだそのスタジアムで勝っていなければ付く）</summary>
        private void ShowReward(MiniGameCourseDef stadium, bool shown)
        {
            if (!shown)
            {
                _rewardText.text = string.Empty;
                return;
            }

            var wins = new MiniGameRecords(Record.Data).Find(Game.Id, stadium.No)?.special ?? 0;
            _rewardText.text = _guide.Reward(stadium.No, wins == 0);
        }

        private static bool IsPlayable(MiniGameCourseDef stadium)
        {
            return MiniGameUnlock.IsUnlocked(stadium, Record) && SoccerStadiumTable.Get(stadium.No) != null;
        }

        private static int LatestPlayable()
        {
            var latest = Game.Courses[0].No;
            foreach (var stadium in Game.Courses)
            {
                if (IsPlayable(stadium))
                {
                    latest = stadium.No;
                }
            }

            return latest;
        }
    }
}
