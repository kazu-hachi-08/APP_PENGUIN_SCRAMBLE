using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ガチャで出てきた子のカード（§7）: ★・名前・属性・個性・パワーと HP・技2つ。初めて手に入れた種類なら「NEW!」の札。
    /// 出すたびに右からすべり込む（UiSlideIn）。技のカードはペンモン画面と同じ見た目（押しても何もしない）
    /// </summary>
    public class GachaResultCard : MonoBehaviour
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Image _elementChip;
        [SerializeField] private Text _elementText;
        [SerializeField] private Text _personalityText;
        [SerializeField] private Text _statsText;
        [SerializeField] private SkillCardView[] _skillCards;
        [SerializeField] private GameObject _newTag;

        public void Show(OwnedPenguin penguin, bool isNew)
        {
            var species = SpeciesTable.Get(penguin.speciesNo);
            if (species == null)
            {
                Hide();
                return;
            }

            _titleText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(PenguinLabels.StarColor(species.Stars))}>{PenguinLabels.Stars(species.Stars)}</color> {species.Name}";
            _elementChip.color = PenguinPalette.ElementColor(species.Element);
            _elementText.text = PenguinLabels.ElementName(species.Element);
            _personalityText.text = SpeciesTexts.Personality(species.No);
            _statsText.text = $"パワー <b>{penguin.power}</b>　　HP <b>{PlayerRecord.StatsOf(penguin).MaxHp}</b>";
            for (var i = 0; i < _skillCards.Length; i++)
            {
                _skillCards[i].Show(species, i, false);
            }

            _newTag.SetActive(isNew);
            // 開き直して、すべり込みを最初から見せる
            gameObject.SetActive(false);
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);
    }
}
