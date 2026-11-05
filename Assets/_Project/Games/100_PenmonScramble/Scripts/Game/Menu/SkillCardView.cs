using System;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ガチャの結果に出す技1つ分のカード: 形のアイコン・名前・［形］と一言の説明・威力／出しやすさ／範囲の3段階。
    /// 必殺技のカードは、ステージの必殺技ボタンと同じ金の面にする。文は SkillTexts
    /// </summary>
    public class SkillCardView : MonoBehaviour
    {
        private const string ShapeColor = "#FFD24D";

        [SerializeField] private Button _button;
        [SerializeField] private Image _frame;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private Text _ratingLabelsText;
        [SerializeField] private Text _ratingsText;
        [SerializeField] private Color _normalColor = new Color(0.16f, 0.21f, 0.34f);
        [SerializeField] private Color _selectedColor = new Color(0.28f, 0.36f, 0.56f);
        [SerializeField] private Color _specialNormalColor = new Color(0.36f, 0.29f, 0.10f);
        [SerializeField] private Color _specialSelectedColor = new Color(0.60f, 0.47f, 0.13f);
        // ステージの必殺技ボタン（TouchSpecialButton）の金と同じ
        [SerializeField] private Color _specialIconColor = new Color(1f, 0.78f, 0.1f);

        private Action<SkillCardView> _clicked;

        private void Awake()
        {
            _button.onClick.AddListener(() => _clicked?.Invoke(this));
        }

        public void OnClick(Action<SkillCardView> clicked)
        {
            _clicked = clicked;
        }

        /// <param name="slot">0 が技1、SkillPractice.SpecialSlot が必殺技</param>
        public void Show(SpeciesDef species, int slot, bool selected)
        {
            var lines = SkillTexts.For(species, slot);
            _nameText.text = NameLine(lines);
            _descriptionText.text = DescriptionLine(lines);
            _ratingLabelsText.text = lines.RatingLabels;
            _ratingsText.text = lines.Ratings;

            var special = SkillTexts.IsSpecial(slot);
            _icon.sprite = SkillShapeIcons.Get(SkillTexts.SkillOf(species, slot).Shape);
            _icon.color = special ? _specialIconColor : PenguinPalette.ElementColor(species.Element);
            _frame.color = special
                ? selected ? _specialSelectedColor : _specialNormalColor
                : selected ? _selectedColor : _normalColor;
        }

        // カードの1行目と2行目の作り方。エディタの UiLayoutCheck も同じ文で測る
        public static string NameLine(SkillTexts.Lines lines) => $"{lines.Header}　<b>{lines.Name}</b>";

        public static string DescriptionLine(SkillTexts.Lines lines) => $"<color={ShapeColor}>{lines.Shape}</color>{lines.Description}";
    }
}
