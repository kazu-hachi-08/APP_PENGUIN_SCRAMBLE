using System;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ペンモン画面の技1つ分のカード: 形のアイコン・名前・［形］と一言の説明・威力／出しやすさ／範囲の3段階。
    /// 必殺技のカードは、ステージの必殺技ボタンと同じ金の面にし、出しやすさの代わりに［ゲージ満タンで1回］と出す。
    /// 押すと、その技を実演の小窓で見せる（選んでいるカードは縁を光らせる）
    /// </summary>
    public class SkillCardView : MonoBehaviour
    {
        private const string ShapeColor = "#FFD24D";
        private const string SkillRatingLabels = "威力\n出しやすさ\n範囲";
        private const string SpecialRatingLabels = "威力\n範囲";

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

        /// <summary>カードに入る文。エディタの UiLayoutCheck も、全種類の文がカードに収まるかをこれで測る</summary>
        public readonly struct Texts
        {
            public readonly string Name;
            public readonly string Description;
            public readonly string RatingLabels;
            public readonly string Ratings;

            public Texts(string name, string description, string ratingLabels, string ratings)
            {
                Name = name;
                Description = description;
                RatingLabels = ratingLabels;
                Ratings = ratings;
            }
        }

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
            var special = slot == SkillPractice.SpecialSlot;
            var texts = TextsFor(species, slot);
            _nameText.text = texts.Name;
            _descriptionText.text = texts.Description;
            _ratingLabelsText.text = texts.RatingLabels;
            _ratingsText.text = texts.Ratings;
            _icon.sprite = SkillShapeIcons.Get(special ? SpecialMoves.For(species.Element).Shape : species.GetSkill(slot).Shape);
            _icon.color = special ? _specialIconColor : PenguinPalette.ElementColor(species.Element);
            _frame.color = special
                ? selected ? _specialSelectedColor : _specialNormalColor
                : selected ? _selectedColor : _normalColor;
        }

        public static Texts TextsFor(SpeciesDef species, int slot)
        {
            if (slot == SkillPractice.SpecialSlot)
            {
                return SpecialTexts(species.Element);
            }

            var skill = species.GetSkill(slot);
            return new Texts(
                $"技{slot + 1}　<b>{skill.Name}</b>",
                $"<color={ShapeColor}>［{SkillProfile.ShapeLabel(skill)}］</color>{SpeciesTexts.Skill(species.No, slot)}",
                SkillRatingLabels,
                $"{PenguinLabels.Pips(SkillProfile.PowerRating(skill), SkillProfile.MaxRating)}\n" +
                $"{PenguinLabels.Pips(SkillProfile.QuicknessRating(skill), SkillProfile.MaxRating)}\n" +
                $"{PenguinLabels.Pips(SkillProfile.AreaRating(skill), SkillProfile.MaxRating)}");
        }

        private static Texts SpecialTexts(Element element)
        {
            var special = SpecialMoves.For(element);
            // 必殺技はゲージで出すので「出しやすさ」は比べられない。範囲は画面全体に効くものがあるので言葉で出す
            return new Texts(
                $"必殺　<b>{special.Name}</b>",
                $"<color={ShapeColor}>［ゲージ満タンで1回］</color>{SpeciesTexts.Special(element)}",
                SpecialRatingLabels,
                $"{PenguinLabels.Pips(SkillProfile.PowerRating(special), SkillProfile.MaxRating)}\n" +
                SkillProfile.SpecialReach(element));
        }
    }
}
