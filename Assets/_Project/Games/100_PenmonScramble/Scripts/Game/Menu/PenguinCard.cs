using System;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 一覧と編成の枠に並べる1体分のカード。左に顔、右に★・名前・パワー、下に属性の色の帯。
    /// 編成にいる子は左上に編成の番号（1〜3）のバッジを出す。編成の枠は、入れる枠を選ぶ間だけ光って脈打つ
    /// </summary>
    public class PenguinCard : MonoBehaviour
    {
        // 入れる枠を選ぶ間の光り方（1秒に何回脈打つか）
        private const float PulseSpeed = 2.5f;

        [SerializeField] private Button _button;
        [SerializeField] private Image _frame;
        [SerializeField] private Image _body;
        [SerializeField] private Text _label;
        [SerializeField] private Image _elementStrip;
        [SerializeField] private GameObject _badge;
        [SerializeField] private Text _badgeText;
        [SerializeField] private Color _normalColor = new Color(0.20f, 0.26f, 0.40f);
        [SerializeField] private Color _partyColor = new Color(0.36f, 0.30f, 0.18f);
        [SerializeField] private Color _selectedColor = new Color(1.00f, 0.60f, 0.25f);
        [SerializeField] private Color _pickColor = new Color(1.00f, 0.85f, 0.35f);
        [SerializeField] private Color _emptyStripColor = new Color(1f, 1f, 1f, 0.15f);

        private Action<PenguinCard> _clicked;
        private Color _restColor;
        private bool _pickable;

        /// <summary>空の枠なら 0</summary>
        public int PenguinId { get; private set; }

        private void Awake()
        {
            _button.onClick.AddListener(() => _clicked?.Invoke(this));
        }

        public void OnClick(Action<PenguinCard> clicked)
        {
            _clicked = clicked;
        }

        /// <param name="partyIndex">編成の何番目か（0〜）。編成にいなければ -1</param>
        public void Show(OwnedPenguin penguin, int partyIndex, bool selected)
        {
            PenguinId = penguin?.id ?? 0;
            _body.enabled = penguin != null;
            if (penguin != null)
            {
                CreatureArt.ShowFace(_body, penguin.speciesNo);
            }

            var species = penguin != null ? SpeciesTable.Get(penguin.speciesNo) : null;
            _label.text = penguin != null ? PenguinLabels.Card(penguin) : "（空き）";
            _elementStrip.color = species != null ? PenguinPalette.ElementColor(species.Element) : _emptyStripColor;
            _badge.SetActive(partyIndex >= 0);
            _badgeText.text = (partyIndex + 1).ToString();
            _restColor = selected ? _selectedColor : partyIndex >= 0 ? _partyColor : _normalColor;
            _frame.color = _restColor;
        }

        /// <summary>入れる枠を選んでいる間は光らせる（どこを押せばいいか分かるように）</summary>
        public void SetPickable(bool pickable)
        {
            _pickable = pickable;
            if (!pickable)
            {
                _frame.color = _restColor;
            }
        }

        private void Update()
        {
            if (_pickable)
            {
                var t = (Mathf.Sin(Time.unscaledTime * PulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
                _frame.color = Color.Lerp(_restColor, _pickColor, t);
            }
        }
    }
}
