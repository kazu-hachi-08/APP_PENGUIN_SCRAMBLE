using System;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ペンモン画面の技1つ分のカード: 形のアイコン・名前・［形］と一言の説明・威力／出しやすさ／範囲の3段階。
    /// 押すと、その技を実演の小窓で見せる（選んでいるカードは縁を光らせる）
    /// </summary>
    public class SkillCardView : MonoBehaviour
    {
        private const string ShapeColor = "#FFD24D";

        [SerializeField] private Button _button;
        [SerializeField] private Image _frame;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private Text _ratingsText;
        [SerializeField] private Color _normalColor = new Color(0.16f, 0.21f, 0.34f);
        [SerializeField] private Color _selectedColor = new Color(0.28f, 0.36f, 0.56f);

        private Action<SkillCardView> _clicked;

        private void Awake()
        {
            _button.onClick.AddListener(() => _clicked?.Invoke(this));
        }

        public void OnClick(Action<SkillCardView> clicked)
        {
            _clicked = clicked;
        }

        /// <param name="slot">0 が技1</param>
        public void Show(SpeciesDef species, int slot, bool selected)
        {
            var skill = species.GetSkill(slot);
            _icon.sprite = SkillShapeIcons.Get(skill.Shape);
            _icon.color = PenguinPalette.ElementColor(species.Element);
            _nameText.text = $"技{slot + 1}　<b>{skill.Name}</b>";
            _descriptionText.text = $"<color={ShapeColor}>［{SkillProfile.ShapeLabel(skill)}］</color>{SpeciesTexts.Skill(species.No, slot)}";
            // 見出し（威力・出しやすさ・範囲）は SceneBuilder が左の列に置く。ここは右の列の●だけ
            _ratingsText.text =
                $"{PenguinLabels.Pips(SkillProfile.PowerRating(skill), SkillProfile.MaxRating)}\n" +
                $"{PenguinLabels.Pips(SkillProfile.QuicknessRating(skill), SkillProfile.MaxRating)}\n" +
                $"{PenguinLabels.Pips(SkillProfile.AreaRating(skill), SkillProfile.MaxRating)}";
            _frame.color = selected ? _selectedColor : _normalColor;
        }
    }
}
