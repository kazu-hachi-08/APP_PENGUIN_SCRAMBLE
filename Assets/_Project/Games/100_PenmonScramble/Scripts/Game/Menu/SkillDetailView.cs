using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ペンモン画面の技のタブの下の、選んでいる技の詳しい欄: ［形・特徴］・一言の説明・威力／出しやすさ／範囲の3段階（必殺技は威力と範囲）。
    /// 1つの技だけを出すので、3枚並べるより字を大きくできる
    /// </summary>
    public class SkillDetailView : MonoBehaviour
    {
        [SerializeField] private Image _frame;
        [SerializeField] private Text _shapeText;
        [SerializeField] private Text _descriptionText;
        [SerializeField] private Text _ratingLabelsText;
        [SerializeField] private Text _ratingsText;
        // 選んでいるタブ（SkillTabView）と同じ色
        [SerializeField] private Color _color = new Color(0.28f, 0.36f, 0.56f);
        [SerializeField] private Color _specialColor = new Color(0.60f, 0.47f, 0.13f);

        /// <param name="slot">0 が技1、SkillPractice.SpecialSlot が必殺技</param>
        public void Show(SpeciesDef species, int slot)
        {
            var lines = SkillTexts.For(species, slot);
            _shapeText.text = lines.Shape;
            _descriptionText.text = lines.Description;
            _ratingLabelsText.text = lines.RatingLabels;
            _ratingsText.text = lines.Ratings;
            _frame.color = SkillTexts.IsSpecial(slot) ? _specialColor : _color;
        }
    }
}
