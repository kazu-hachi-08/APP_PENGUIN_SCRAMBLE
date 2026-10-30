using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// タイトルのうち、接続（ModeSelectPanel）以外の所: コイン・遊ぶステージの選択・ガチャ・ペンモン画面・遊び方（§2）。
    /// ステージはソロとホストが使う（ゲストはホストの選んだステージを遊ぶ）。初回はまず無料ガチャを開く（§7）
    /// </summary>
    public class TitleMenu : MonoBehaviour
    {
        [SerializeField] private Text _coinText;
        [SerializeField] private Text _stageText;
        [SerializeField] private Button _prevStageButton;
        [SerializeField] private Button _nextStageButton;
        [SerializeField] private Button _gachaButton;
        [SerializeField] private Button _penguinsButton;
        [SerializeField] private Button _howToPlayButton;
        [SerializeField] private GachaScreen _gacha;
        [SerializeField] private PenguinListScreen _penguins;
        [SerializeField] private HowToPlayScreen _howToPlay;

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
            _gacha.Closed += Refresh;
            _penguins.Closed += Refresh;
        }

        private void Start()
        {
            SelectedStageNo = LatestUnlockedStage();
            Refresh();
            if (Record.FreePullsLeft > 0)
            {
                _gacha.Open();
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

        private void Refresh()
        {
            _coinText.text = $"コイン {Record.Coins}";
            var stage = StageTable.Get(SelectedStageNo);
            var cleared = Record.IsCleared(SelectedStageNo) ? "　<color=#FFD24D>クリア済み</color>" : string.Empty;
            _stageText.text = $"ステージ {SelectedStageNo}　{stage?.Name}{cleared}";
            _prevStageButton.interactable = SelectedStageNo > 1;
            _nextStageButton.interactable = StageTable.Get(SelectedStageNo + 1) != null && Record.IsUnlocked(SelectedStageNo + 1);
        }
    }
}
