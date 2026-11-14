using PenmonScramble.Common.Online;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 「みんなで遊ぶ」の部屋の待ちの窓に出す、ボスラッシュのステージと難易度の選択。
    /// ホストが ◀ ▶ と難易度の札で選び、選び直すたびにゲストへ送る（ゲストは見るだけ）。解放はホストの記録で決め、
    /// 開いていないものを選んでいる間は「開始」を押せない。ボスラッシュの部屋に入っている間だけ出す
    /// （ModeSelectPanel.RoomStateChanged・RoomModeSelector）。ボスラッシュは本編のシーンの中で遊ぶ（StageRunner が始める）。
    /// 見た目の書き換えは RushLobby.View.cs
    /// </summary>
    public partial class RushLobby : MonoBehaviour, IRoomMode
    {
        public const string ModeId = "rush";

        [SerializeField] private ModeSelectPanel _modeSelect;
        [SerializeField] private Button _prevButton;
        [SerializeField] private Button _nextButton;
        [Tooltip("添字は Difficulty（ふつう・むずかしい・げきむず）")]
        [SerializeField] private Button[] _difficultyButtons = new Button[Balance.DifficultyCount];

        // ホストの選択は、シーンを読み直しても（ステージから戻っても）覚えておく。続けて同じものに挑みやすいように
        private static RushChoice? _hostChoice;

        private readonly RushLobbyLink _link = new RushLobbyLink();
        private bool _inRoom;
        private bool _isHost;
        private bool _selected;
        private RushChoice? _guestChoice;

        public string Id => ModeId;

        public string Label => "ボスラッシュ";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _hostChoice = null;
        }

        private static PlayerRecord Record => GameSave.Record;

        /// <summary>ホストが選んでいるステージと難易度。ホストが「開始」を押したら、これを遊ぶ</summary>
        public RushChoice Choice => _hostChoice ?? DefaultChoice();

        /// <summary>画面に出す選択。ゲストはホストから届いたもの（まだ届いていなければ null）</summary>
        private RushChoice? Shown => _isHost ? Choice : _guestChoice;

        private void Awake()
        {
            _modeSelect.RoomStateChanged += OnRoomStateChanged;
            _link.AskReceived += SendChoice;
            _link.ChoiceReceived += OnChoiceReceived;
            _prevButton.onClick.AddListener(() => MoveStage(-1));
            _nextButton.onClick.AddListener(() => MoveStage(1));
            for (var i = 0; i < _difficultyButtons.Length; i++)
            {
                var difficulty = (Difficulty)i;
                _difficultyButtons[i].onClick.AddListener(() => SelectDifficulty(difficulty));
            }

            Refresh();
        }

        private void OnDestroy()
        {
            _modeSelect.RoomStateChanged -= OnRoomStateChanged;
            _link.Close();
        }

        /// <summary>試合が始まってタイトルが隠れたら、待ちの窓の通信は終わり（ステージの通信は StageLink）</summary>
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

        /// <summary>ボスラッシュはミニゲーム画面から頼まれることがない（選ぶものはステージのカードで選ぶ）</summary>
        public void Preselect(int option)
        {
        }

        /// <summary>本編のシーンの中で遊ぶので、StageRunner に任せる</summary>
        public bool StartMatch(int seat, int playerCount) => false;

        private void OnRoomStateChanged(bool isHost, bool inRoom)
        {
            _isHost = isHost;
            _inRoom = inRoom;
            _guestChoice = null;
            if (!inRoom)
            {
                _link.Close();
            }
            else if (isHost)
            {
                _hostChoice = Validated(Choice);
                _link.OpenAsHost();
                SendChoice();
            }
            else
            {
                _link.OpenAsGuest();
            }

            Refresh();
        }

        private void OnChoiceReceived(RushChoice choice)
        {
            _guestChoice = choice;
            Refresh();
        }

        private void MoveStage(int step)
        {
            var current = Choice;
            var no = current.StageNo + step;
            if (!_isHost || !RushTable.IsRush(no))
            {
                return;
            }

            // 難易度はそのまま持ち越す。次のステージでまだ開いていなければ、開いている中で一番上にする
            _hostChoice = new RushChoice(no, HighestUnlocked(no, current.Difficulty));
            OnChoiceChanged();
        }

        private void SelectDifficulty(Difficulty difficulty)
        {
            var current = Choice;
            if (!_isHost || !Record.IsRushUnlocked(current.StageNo, difficulty))
            {
                return;
            }

            _hostChoice = new RushChoice(current.StageNo, difficulty);
            OnChoiceChanged();
        }

        private void OnChoiceChanged()
        {
            SendChoice();
            Refresh();
        }

        private void SendChoice()
        {
            if (_inRoom && _isHost)
            {
                _link.SendChoice(Choice);
            }
        }

        /// <summary>最初は、解放した中で一番先のステージの「ふつう」</summary>
        private static RushChoice DefaultChoice()
        {
            var no = RushTable.FirstNo;
            while (no < RushTable.LastNo && Record.IsRushUnlocked(no + 1, Difficulty.Normal))
            {
                no++;
            }

            return new RushChoice(no, Difficulty.Normal);
        }

        /// <summary>記録を初期化した後などで、覚えていた選択が開いていなければ、最初の選択に戻す</summary>
        private static RushChoice Validated(RushChoice choice)
        {
            return Record.IsRushUnlocked(choice.StageNo, choice.Difficulty) ? choice : DefaultChoice();
        }

        /// <summary>wanted から下へ、開いている難易度を探す。ステージ自体が閉じていれば「ふつう」（鍵を見せる）</summary>
        private static Difficulty HighestUnlocked(int stageNo, Difficulty wanted)
        {
            for (var d = wanted; d > Difficulty.Normal; d--)
            {
                if (Record.IsRushUnlocked(stageNo, d))
                {
                    return d;
                }
            }

            return Difficulty.Normal;
        }
    }
}
