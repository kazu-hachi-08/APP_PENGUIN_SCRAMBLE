using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ボスラッシュの選択の見た目: ステージのカード（番号・名前・3戦の顔・鍵）、難易度の3つの札、報酬のコイン
    /// </summary>
    public partial class RushLobby
    {
        private const float UnselectedAlpha = 0.55f;
        private const float LockedAlpha = 0.35f;
        private const float SelectedScale = 1.1f;
        private static readonly Color WildFaceColor = Color.Lerp(Color.white, Color.black, UnitView.EnemyDarkness);

        [Header("見た目")]
        [SerializeField] private GameObject _card;
        [SerializeField] private Text _stageNoText;
        [SerializeField] private Text _stageNameText;
        [Tooltip("1戦目の巨大ペンモン2体・2戦目の大ボス・3戦目の本気の大ボス")]
        [SerializeField] private Image[] _faces = new Image[4];
        [SerializeField] private GameObject _lock;
        [SerializeField] private Text _lockText;
        [Tooltip("添字は Difficulty。札の入れ物（選んでいる札を大きく濃く、ほかを薄くする。ボタン自身の動きとぶつからないよう外側で）")]
        [SerializeField] private CanvasGroup[] _difficultySlots = new CanvasGroup[Balance.DifficultyCount];
        [Tooltip("添字は Difficulty。選んでいる札の後ろに光る白い縁")]
        [SerializeField] private GameObject[] _difficultyMarks = new GameObject[Balance.DifficultyCount];
        [Tooltip("添字は Difficulty。開いていない難易度に重ねる鍵")]
        [SerializeField] private GameObject[] _difficultyLocks = new GameObject[Balance.DifficultyCount];
        [SerializeField] private Text _rewardText;
        [SerializeField] private Text _noteText;

        private const string HostNote = "ステージと難易度を選んで「開始」";
        private const string GuestNote = "ホストが選んでいます";

        private void Refresh()
        {
            var shown = Shown;
            var showCard = _inRoom && _selected && shown.HasValue;
            _card.SetActive(showCard);
            if (!showCard)
            {
                return;
            }

            var choice = shown.Value;
            var stage = RushTable.Get(choice.StageNo);
            ShowStage(stage, choice);
            ShowDifficulties(choice);
            ShowReward(choice);
            _noteText.text = _isHost ? HostNote : GuestNote;
            if (_isHost)
            {
                _modeSelect.SetStartAllowed(Record.IsRushUnlocked(choice.StageNo, choice.Difficulty));
            }
        }

        private void ShowStage(StageDef stage, RushChoice choice)
        {
            _stageNoText.text = $"ステージ {choice.StageNo - RushTable.FirstNo + 1}";
            _stageNameText.text = stage?.Name;
            _prevButton.gameObject.SetActive(_isHost);
            _nextButton.gameObject.SetActive(_isHost);
            _prevButton.interactable = RushTable.IsRush(choice.StageNo - 1);
            _nextButton.interactable = RushTable.IsRush(choice.StageNo + 1);

            var giants = RushTable.BossesOf(stage, 1);
            ShowFace(_faces[0], giants.Count > 0 ? giants[0].SpeciesNo : 0);
            ShowFace(_faces[1], giants.Count > 1 ? giants[1].SpeciesNo : 0);
            ShowFace(_faces[2], FirstBoss(stage, 2));
            ShowFace(_faces[3], FirstBoss(stage, 3));

            // 鍵はホストだけ（ゲストは自分が開いていなくても、ホストの選んだものを遊べる）
            var locked = _isHost && !Record.IsRushUnlocked(choice.StageNo, Difficulty.Normal);
            _lock.SetActive(locked);
            if (locked)
            {
                var previous = RushTable.Get(choice.StageNo - 1);
                _lockText.text = $"「{previous?.Name}」を\nふつうでクリアすると解放";
            }
        }

        private static int FirstBoss(StageDef stage, int round)
        {
            var bosses = RushTable.BossesOf(stage, round);
            return bosses.Count > 0 ? bosses[0].SpeciesNo : 0;
        }

        /// <summary>巨大ペンモンは野生のペンモンなので、ステージの中と同じく暗く。大ボスはそのままの色</summary>
        private static void ShowFace(Image image, int speciesNo)
        {
            image.gameObject.SetActive(speciesNo > 0);
            if (speciesNo <= 0)
            {
                return;
            }

            CreatureArt.ShowFace(image, speciesNo);
            image.color = BigBossTable.Get(speciesNo) == null ? WildFaceColor : Color.white;
        }

        /// <summary>選んでいる札は大きく濃く、ほかは薄く。開いていない札は、ホストにだけ鍵を重ねて押せなくする</summary>
        private void ShowDifficulties(RushChoice choice)
        {
            for (var i = 0; i < _difficultyButtons.Length; i++)
            {
                var difficulty = (Difficulty)i;
                var unlocked = !_isHost || Record.IsRushUnlocked(choice.StageNo, difficulty);
                var selected = difficulty == choice.Difficulty;
                _difficultyButtons[i].interactable = _isHost && unlocked;
                var slot = _difficultySlots[i];
                slot.transform.localScale = Vector3.one * (selected ? SelectedScale : 1f);
                slot.alpha = !unlocked ? LockedAlpha : selected ? 1f : UnselectedAlpha;
                _difficultyMarks[i].SetActive(selected);
                _difficultyLocks[i].SetActive(!unlocked);
            }
        }

        /// <summary>報酬は見ている人の記録で決まる（初クリアのボーナスは、自分がまだクリアしていなければ付く）</summary>
        private void ShowReward(RushChoice choice)
        {
            var clear = Balance.RushClearCoins(choice.StageNo, choice.Difficulty);
            var text = $"クリア報酬  {clear:N0}";
            if (!Record.IsRushCleared(choice.StageNo, choice.Difficulty))
            {
                text += $"   初クリア +{Balance.RushFirstClearBonus(choice.StageNo, choice.Difficulty):N0}";
            }

            _rewardText.text = text;
        }
    }
}
