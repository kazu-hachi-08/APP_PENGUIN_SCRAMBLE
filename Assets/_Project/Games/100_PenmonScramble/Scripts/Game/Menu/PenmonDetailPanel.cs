using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ペンモン画面の右側の、選んだ子の図鑑カード:顔・★・名前・属性・個性・パワー（上限）と HP・上限解放の◆・体力／攻撃／速さの棒・
    /// 技の実演の小窓・技2つと必殺技のカード。カードを押すと、小窓の実演がその技に替わる
    /// </summary>
    public class PenmonDetailPanel : MonoBehaviour
    {
        // 上限はパワーより小さく添える（主役はパワーの数字）
        private const int CapFontSize = 24;

        [SerializeField] private GameObject _content;
        [SerializeField] private Text _emptyText;
        [SerializeField] private Image _face;
        [SerializeField] private Text _titleText;
        [SerializeField] private Image _elementChip;
        [SerializeField] private Text _elementText;
        [SerializeField] private Text _personalityText;
        [SerializeField] private Text _statsText;
        [SerializeField] private Text _limitText;
        [SerializeField] private Image _hpBar;
        [SerializeField] private Image _attackBar;
        [SerializeField] private Image _speedBar;
        [SerializeField] private SkillCardView[] _skillCards;
        [SerializeField] private SkillDemoView _demo;

        private OwnedPenguin _penguin;
        private SpeciesDef _species;
        // 別の子を選んでも、見ている技（技1・技2・必殺技のどれか）はそのままにする（技2どうしを見比べやすいように）。
        // カードの並びの番号が、そのまま SkillPractice の slot（最後が SpecialSlot）になる
        private int _slot;
        // 強化でパワーが変わったら、実演のダメージの数字も新しい強さで出し直す
        private int _demoPower;

        private void Awake()
        {
            foreach (var card in _skillCards)
            {
                card.OnClick(SelectSkill);
            }
        }

        /// <summary>選んだ子を出す。null（何も選んでいない・知らない種類）なら案内だけ出す</summary>
        public void Show(OwnedPenguin penguin)
        {
            var species = penguin != null ? SpeciesTable.Get(penguin.speciesNo) : null;
            var changed = penguin != _penguin || (penguin != null && penguin.Power != _demoPower);
            _penguin = species != null ? penguin : null;
            _species = species;
            _content.SetActive(_species != null);
            _emptyText.gameObject.SetActive(_species == null);
            if (_species == null)
            {
                _demo.Stop();
                return;
            }

            ShowProfile();
            ShowSkills();
            if (changed)
            {
                PlayDemo();
            }
        }

        public void Hide()
        {
            _penguin = null;
            _demo.Stop();
        }

        private void ShowProfile()
        {
            CreatureArt.ShowFace(_face, _species.No);
            _titleText.text = TitleOf(_species);
            _elementChip.color = PenguinPalette.ElementColor(_species.Element);
            _elementText.text = PenguinLabels.ElementName(_species.Element);
            _personalityText.text = SpeciesTexts.Personality(_species.No);
            var stats = PlayerRecord.StatsOf(_penguin);
            _statsText.text = StatsLine(_penguin.Power, PlayerRecord.PowerCap(_penguin), stats.MaxHp);
            _limitText.text = PenguinLabels.LimitPips(_penguin.limitBreak);
            _hpBar.fillAmount = SpeciesProfile.HpRatio(_species);
            _attackBar.fillAmount = SpeciesProfile.AttackRatio(_species);
            _speedBar.fillAmount = SpeciesProfile.SpeedRatio(_species);
        }

        // 文の作り方は、エディタの UiLayoutCheck が全種類の文を枠に当てて測るときにも使う
        public static string TitleOf(SpeciesDef species) => $"{PenguinLabels.ColoredStars(species.Stars)} {species.Name}";

        public static string StatsLine(int power, int cap, int hp) =>
            $"パワー <b>{power}</b><size={CapFontSize}> / 上限 {cap}</size>　HP <b>{hp}</b>";

        private void ShowSkills()
        {
            for (var i = 0; i < _skillCards.Length; i++)
            {
                _skillCards[i].Show(_species, i, i == _slot);
            }
        }

        private void SelectSkill(SkillCardView card)
        {
            var slot = System.Array.IndexOf(_skillCards, card);
            if (slot < 0 || _species == null)
            {
                return;
            }

            _slot = slot;
            ShowSkills();
            PlayDemo();
        }

        private void PlayDemo()
        {
            _demoPower = _penguin.Power;
            _demo.Play(PlayerRecord.StatsOf(_penguin), _slot);
        }
    }
}
