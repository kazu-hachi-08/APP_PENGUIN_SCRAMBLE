using System;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ステージ中の画面表示: 左上の自分のプレート（場に出ている子の顔・必殺技のゲージ・名札・HP・3体の小さな顔）と結果。
    /// エリアの進み具合・ボスの HP・画面の真ん中の知らせは StageHud.Progress.cs
    /// </summary>
    public partial class StageHud : MonoBehaviour
    {
        private const string DownMessage = "ダウン… 仲間が近くにいると復活";
        private static readonly Color DownDiscColor = new Color(0.35f, 0.37f, 0.42f);
        private static readonly Color DownFaceColor = new Color(0.4f, 0.4f, 0.4f);

        [Tooltip("プレートをまとめた入れ物。タイトルの間は隠す")]
        [SerializeField] private GameObject _plates;
        [SerializeField] private Image _namePlate;
        [SerializeField] private Text _nameText;
        [SerializeField] private HudBar _hpBar;
        [SerializeField] private Text _hpText;
        [SerializeField] private Image _activeDisc;
        [SerializeField] private Image _activeFace;
        [SerializeField] private HudSpecialRing _special;
        [SerializeField] private HudMemberSlot[] _slots;
        [SerializeField] private GameObject _resultPanel;
        [SerializeField] private Text _resultText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _titleButton;

        private string _playerLabel = string.Empty;
        private int _shownIndex = -1;
        private int _shownSpecies = -1;

        public event Action RetryRequested;
        public event Action TitleRequested;

        private void Awake()
        {
            _retryButton.onClick.AddListener(() => RetryRequested?.Invoke());
            _titleButton.onClick.AddListener(() => TitleRequested?.Invoke());
        }

        /// <summary>タイトルの間（ステージが始まる前）は出さない。ステージから戻るときはシーンを読み直すので、隠し直さなくてよい</summary>
        public void SetShown(bool shown)
        {
            if (_plates.activeSelf != shown)
            {
                _plates.SetActive(shown);
            }
        }

        /// <summary>名札を自分のペンギンの足元の輪と同じ色にして、協力のときにどれが自分か分かるようにする</summary>
        public void SetPlayer(string label, Color color)
        {
            _namePlate.color = color;
            _playerLabel = label;
        }

        /// <param name="fallen">3体とも倒れて場から消えた。最後に見た3体を HP 0 で出す</param>
        public void SetParty(PlayerState state, bool fallen)
        {
            var active = state.Members[state.ActiveIndex];
            var hp = fallen ? 0 : active.Hp;
            ShowActive(state.ActiveIndex, active, hp);
            _special.SetGauge(fallen ? 0f : state.SpecialGauge);
            ShowSlots(state, fallen);
        }

        private void ShowActive(int index, MemberState active, int hp)
        {
            var ratio = active.MaxHp > 0 ? (float)hp / active.MaxHp : 0f;
            if (index != _shownIndex || active.SpeciesNo != _shownSpecies)
            {
                // 交代した: 前の子の HP から減ったように見せない
                _shownIndex = index;
                _shownSpecies = active.SpeciesNo;
                CreatureArt.ShowFace(_activeFace, active.SpeciesNo);
                _hpBar.Snap(ratio);
            }
            else
            {
                _hpBar.SetRatio(ratio);
            }

            var down = hp <= 0;
            _activeDisc.color = down ? DownDiscColor : PenguinPalette.SpeciesElementColor(active.SpeciesNo);
            _activeFace.color = down ? DownFaceColor : Color.white;
            _nameText.text = $"{_playerLabel}  {PenguinPalette.SpeciesName(active.SpeciesNo)}";
            // 3体とも倒れた（ダウン）ときは、何をすれば戻れるかを出す
            _hpText.text = down ? DownMessage : $"{hp} / {active.MaxHp}";
        }

        private void ShowSlots(PlayerState state, bool fallen)
        {
            var next = fallen ? -1 : state.NextAliveIndex();
            var cooldown = state.SwitchCooldown / Balance.SwitchCooldownSeconds;
            for (var i = 0; i < _slots.Length; i++)
            {
                if (i >= state.Members.Count)
                {
                    _slots[i].Hide();
                    continue;
                }

                var member = state.Members[i];
                var hp = fallen ? 0 : member.Hp;
                var ratio = member.MaxHp > 0 ? (float)hp / member.MaxHp : 0f;
                _slots[i].Show(member.SpeciesNo, ratio, i == state.ActiveIndex && !fallen, i == next, cooldown);
            }
        }

        private void Update()
        {
            UpdateBanner();
        }

        /// <param name="canRetry">「もう一度」を押せるのはホストだけ（ゲストはホストが始め直すのを待つ）</param>
        public void ShowResult(bool cleared, bool canRetry, int enemiesDefeated, int coins)
        {
            var title = cleared ? "クリア！" : "やられた…";
            title = $"{title}\n<size=56>倒した数 {enemiesDefeated} 体　コイン +{coins}</size>";
            _resultText.text = canRetry ? title : $"{title}\n<size=48>ホストが「もう一度」を押すと始まります</size>";
            _retryButton.gameObject.SetActive(canRetry);
            _resultPanel.SetActive(true);
        }

        public void HideResult()
        {
            _resultPanel.SetActive(false);
        }
    }
}
