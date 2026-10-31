using PenmonScramble.Common.Input;
using PenmonScramble.Common.Profile;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// タイトルのロビー（§2）。接続の流れ（ModeSelectPanel）以外の所: 上の名前の札・コイン・設定、左の列（ガチャ・ペンモン・遊び方）、
    /// 右下のステージのカードと「ひとりで遊ぶ」「みんなで遊ぶ」。「みんなで遊ぶ」は部屋を作る / コードで参加の窓を開く。
    /// ステージはソロとホストが使う（ゲストはホストの選んだステージを遊ぶ）
    /// </summary>
    public class TitleMenu : MonoBehaviour
    {
        private const string ClearedBadge = "★ クリア済み";
        private const string BigBossBadge = "大ボス出現";
        // 名前を決める前（初回の入力の後ろ）に札に出す。ステージで名前の無い席に出るのと同じ
        private const string UnnamedLabel = "P1";

        [Header("上の札")]
        [SerializeField] private Button _nameButton;
        [SerializeField] private Text _nameText;
        [SerializeField] private Image _leaderFace;
        [SerializeField] private Text _coinText;
        [SerializeField] private Button _settingsButton;

        [Header("左の列")]
        [SerializeField] private Button _gachaButton;
        [SerializeField] private Button _penguinsButton;
        [SerializeField] private Button _howToPlayButton;

        [Header("ステージのカード")]
        [SerializeField] private Text _stageNoText;
        [SerializeField] private Text _stageNameText;
        [SerializeField] private Text _stageBadgeText;
        [SerializeField] private Button _prevStageButton;
        [SerializeField] private Button _nextStageButton;

        [Header("みんなで遊ぶ")]
        [SerializeField] private Button _onlineButton;
        [SerializeField] private GameObject _onlinePanel;
        [SerializeField] private Button _onlineCloseButton;

        [Header("ほかの画面")]
        [SerializeField] private GachaScreen _gacha;
        [SerializeField] private PenguinListScreen _penguins;
        [SerializeField] private HowToPlayScreen _howToPlay;
        [SerializeField] private SettingsScreen _settings;
        [SerializeField] private NameEntryDialog _nameEntry;

        private static PlayerRecord Record => GameSave.Record;

        /// <summary>遊ぶステージの番号。最初は解放した中で一番先のステージ</summary>
        public int SelectedStageNo { get; private set; } = 1;

        private void Awake()
        {
            _prevStageButton.onClick.AddListener(() => MoveStage(-1));
            _nextStageButton.onClick.AddListener(() => MoveStage(1));
            _gachaButton.onClick.AddListener(_gacha.Open);
            _penguinsButton.onClick.AddListener(_penguins.Open);
            _howToPlayButton.onClick.AddListener(_howToPlay.Open);
            _settingsButton.onClick.AddListener(_settings.Open);
            _nameButton.onClick.AddListener(() => _nameEntry.Show(Refresh, Refresh));
            _onlineButton.onClick.AddListener(() => _onlinePanel.SetActive(true));
            _onlineCloseButton.onClick.AddListener(() => _onlinePanel.SetActive(false));
            _gacha.Closed += Refresh;
            _penguins.Closed += Refresh;
            _settings.Closed += Refresh;
            _onlinePanel.SetActive(false);
        }

        private void OnDestroy()
        {
            _gacha.Closed -= Refresh;
            _penguins.Closed -= Refresh;
            _settings.Closed -= Refresh;
        }

        private void Start()
        {
            SelectedStageNo = LatestUnlockedStage();
            Refresh();
        }

        /// <summary>Esc / OPTIONS で、いちばん上に開いている窓だけを閉じる（初回の名前の入力は閉じない）</summary>
        private void Update()
        {
            if (!InputManager.HasInstance || !InputManager.Instance.IsPauseDown)
            {
                return;
            }

            if (_nameEntry.IsOpen)
            {
                _nameEntry.Cancel();
            }
            else if (_settings.IsOpen)
            {
                _settings.Close();
            }
            else if (_onlinePanel.activeSelf)
            {
                _onlinePanel.SetActive(false);
            }
        }

        private static int LatestUnlockedStage()
        {
            var no = 1;
            while (no < StageTable.Count && Record.IsUnlocked(no + 1))
            {
                no++;
            }

            return no;
        }

        private void MoveStage(int step)
        {
            var next = SelectedStageNo + step;
            if (StageTable.Get(next) != null && Record.IsUnlocked(next))
            {
                SelectedStageNo = next;
            }

            Refresh();
        }

        public void Refresh()
        {
            // SeatNames は直前の協力の名前が残っていることがあるので、自分の名前を直接読む
            _nameText.text = UserProfile.HasName ? UserProfile.Name : UnnamedLabel;
            RefreshLeader();
            _coinText.text = Record.Coins.ToString("N0");
            RefreshStage();
        }

        /// <summary>名前の札の顔は編成の先頭の子。編成が空（初回のガチャの前）なら出さない</summary>
        private void RefreshLeader()
        {
            var party = Record.PartyPenguins();
            if (party.Count == 0)
            {
                _leaderFace.enabled = false;
                return;
            }

            CreatureArt.ShowFace(_leaderFace, party[0].speciesNo);
        }

        private void RefreshStage()
        {
            var no = SelectedStageNo;
            var stage = StageTable.Get(no);
            var world = StageTable.WorldOf(no);
            var inWorld = (no - 1) % StageTable.StagesPerWorld + 1;
            _stageNoText.text = $"ワールド {world} - {inWorld}";
            _stageNameText.text = stage?.Name;
            // ワールドの最後のステージには大ボスが出る（§6.2）
            _stageBadgeText.text = Record.IsCleared(no) ? ClearedBadge : inWorld == StageTable.StagesPerWorld ? BigBossBadge : string.Empty;
            _prevStageButton.interactable = no > 1;
            _nextStageButton.interactable = StageTable.Get(no + 1) != null && Record.IsUnlocked(no + 1);
        }
    }
}
