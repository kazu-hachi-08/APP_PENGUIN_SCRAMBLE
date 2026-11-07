using System;
using PenmonScramble.Core;
using PenmonScramble.Game;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 打順のカード1枚: 属性の色の板に顔と名前、右に「魔球」「必殺打」の札（残っていれば明るく、使ったら暗く）。
    /// 今の打者・投げる子は明るい縁と「▶」で目立たせる。自分のチームのカードは押せる（打席の始めに投げる子を選ぶ）
    /// </summary>
    public class BaseballTeamCard : MonoBehaviour
    {
        private static readonly Color UsedColor = new Color(0.2f, 0.21f, 0.26f);
        private static readonly Color UsedTextColor = new Color(0.5f, 0.52f, 0.58f);
        private static readonly Color MagicColor = new Color(1f, 0.55f, 0.25f);
        private static readonly Color HitColor = new Color(1f, 0.82f, 0.25f);
        private static readonly Color RimOff = new Color(0f, 0f, 0f, 0f);
        private static readonly Color RimOn = new Color(1f, 0.92f, 0.45f);

        [SerializeField] private Image _back;
        [Tooltip("今の打者・投げる子のときだけ光らせる縁")]
        [SerializeField] private Image _rim;
        [SerializeField] private Image _face;
        [SerializeField] private Text _name;
        [SerializeField] private Image _magicBadge;
        [SerializeField] private Text _magicText;
        [SerializeField] private Image _hitBadge;
        [SerializeField] private Text _hitText;
        [Tooltip("自分のチームのカードだけ（押すと投げる子を選ぶ）。CPU のカードは無し")]
        [SerializeField] private Button _button;
        [Tooltip("板の属性の色を、どれだけ暗くするか（字を読みやすく）")]
        [SerializeField, Range(0f, 1f)] private float _backDarken = 0.55f;
        [Tooltip("今の子の板は、ここまで明るくする")]
        [SerializeField, Range(0f, 1f)] private float _currentDarken = 0.3f;

        /// <summary>カードが押された</summary>
        public event Action Clicked;

        private void Awake()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(() => Clicked?.Invoke());
            }
        }

        /// <param name="current">今の打者・投げる子</param>
        public void Show(int species, bool magicUsed, bool hitUsed, bool current)
        {
            gameObject.SetActive(true);
            var def = SpeciesTable.Get(species);
            var element = def != null ? PenguinPalette.ElementColor(def.Element) : Color.gray;
            _back.color = Color.Lerp(element, Color.black, current ? _currentDarken : _backDarken);
            _rim.color = current ? RimOn : RimOff;
            CreatureArt.ShowFace(_face, species);
            var name = PenguinPalette.SpeciesName(species);
            _name.text = current ? $"▶ {name}" : name;
            ShowBadge(_magicBadge, _magicText, !magicUsed, MagicColor);
            ShowBadge(_hitBadge, _hitText, !hitUsed, HitColor);
        }

        public void Hide() => gameObject.SetActive(false);

        /// <summary>押せるか（投げる子を選べる間だけ押せるように見せる）</summary>
        public void SetSelectable(bool selectable)
        {
            if (_button != null)
            {
                _button.interactable = selectable;
            }
        }

        private static void ShowBadge(Image badge, Text text, bool left, Color color)
        {
            badge.color = left ? color : UsedColor;
            text.color = left ? Color.black : UsedTextColor;
        }
    }
}
