using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ペンモン画面の右側の、選んだ子の図鑑カード:顔・★・名前・属性・個性・パワーと HP・体力／攻撃／速さの棒・
    /// 技の実演の小窓・技2つのカード。技のカードを押すと、小窓の実演がその技に替わる
    /// </summary>
    public class PenmonDetailPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _content;
        [SerializeField] private Text _emptyText;
        [SerializeField] private Image _face;
        [SerializeField] private Text _titleText;
        [SerializeField] private Image _elementChip;
        [SerializeField] private Text _elementText;
        [SerializeField] private Text _personalityText;
        [SerializeField] private Text _statsText;
        [SerializeField] private Image _hpBar;
        [SerializeField] private Image _attackBar;
        [SerializeField] private Image _speedBar;
        [SerializeField] private SkillCardView[] _skillCards;
        [SerializeField] private SkillDemoView _demo;

        private OwnedPenguin _penguin;
        private SpeciesDef _species;
        // 別の子を選んでも、見ている技（技1か技2か）はそのままにする（技2どうしを見比べやすいように）
        private int _slot;

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
            var changed = penguin != _penguin;
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
            _titleText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(PenguinLabels.StarColor(_species.Stars))}>{PenguinLabels.Stars(_species.Stars)}</color> {_species.Name}";
            _elementChip.color = PenguinPalette.ElementColor(_species.Element);
            _elementText.text = PenguinLabels.ElementName(_species.Element);
            _personalityText.text = SpeciesTexts.Personality(_species.No);
            var stats = PlayerRecord.StatsOf(_penguin);
            _statsText.text = $"パワー <b>{_penguin.power}</b>　　HP <b>{stats.MaxHp}</b>";
            _hpBar.fillAmount = SpeciesProfile.HpRatio(_species);
            _attackBar.fillAmount = SpeciesProfile.AttackRatio(_species);
            _speedBar.fillAmount = SpeciesProfile.SpeedRatio(_species);
        }

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
            _demo.Play(PlayerRecord.StatsOf(_penguin), _slot);
        }
    }
}
