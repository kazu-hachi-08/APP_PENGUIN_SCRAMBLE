using System;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ペンモン画面の技のタブ（技1・技2・必殺を横に3つ）: 形のアイコン・見出し・名前だけを出す。
    /// 説明と3段階は、選んでいる技の分だけ下の SkillDetailView に大きく出す。
    /// 必殺技のタブは、ステージの必殺技ボタンと同じ金の面にする
    /// </summary>
    public class SkillTabView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _frame;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _headerText;
        [SerializeField] private Text _nameText;
        [SerializeField] private Color _normalColor = new Color(0.16f, 0.21f, 0.34f);
        // 選んでいるタブは、下の詳しい欄（SkillDetailView）と同じ色にして、つながって見せる
        [SerializeField] private Color _selectedColor = new Color(0.28f, 0.36f, 0.56f);
        [SerializeField] private Color _specialNormalColor = new Color(0.36f, 0.29f, 0.10f);
        [SerializeField] private Color _specialSelectedColor = new Color(0.60f, 0.47f, 0.13f);
        // ステージの必殺技ボタン（TouchSpecialButton）の金と同じ
        [SerializeField] private Color _specialIconColor = new Color(1f, 0.78f, 0.1f);
        [SerializeField] private Color _headerColor = new Color(0.78f, 0.84f, 0.95f);
        [SerializeField] private Color _selectedHeaderColor = new Color(1f, 0.82f, 0.30f);

        private Action<SkillTabView> _clicked;

        private void Awake()
        {
            _button.onClick.AddListener(() => _clicked?.Invoke(this));
        }

        public void OnClick(Action<SkillTabView> clicked)
        {
            _clicked = clicked;
        }

        /// <param name="slot">0 が技1、SkillPractice.SpecialSlot が必殺技</param>
        public void Show(SpeciesDef species, int slot, bool selected)
        {
            var lines = SkillTexts.For(species, slot);
            _headerText.text = lines.Header;
            _headerText.color = selected ? _selectedHeaderColor : _headerColor;
            _nameText.text = lines.Name;

            var special = SkillTexts.IsSpecial(slot);
            _icon.sprite = SkillShapeIcons.Get(SkillTexts.SkillOf(species, slot).Shape);
            _icon.color = special ? _specialIconColor : PenguinPalette.ElementColor(species.Element);
            _frame.color = special
                ? selected ? _specialSelectedColor : _specialNormalColor
                : selected ? _selectedColor : _normalColor;
        }
    }
}
