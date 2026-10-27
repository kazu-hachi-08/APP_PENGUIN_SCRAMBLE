using System;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>一覧と編成の枠に並べる1体分のカード。絵ができるまでは種類の色の丸で見せる</summary>
    public class PenguinCard : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _frame;
        [SerializeField] private Image _body;
        [SerializeField] private Text _label;
        [SerializeField] private GameObject _partyMark;
        [SerializeField] private Color _normalColor = new Color(0.20f, 0.26f, 0.40f);
        [SerializeField] private Color _selectedColor = new Color(1.00f, 0.60f, 0.25f);

        private Action<PenguinCard> _clicked;

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

        public void Show(OwnedPenguin penguin, bool inParty, bool selected)
        {
            PenguinId = penguin?.id ?? 0;
            _body.enabled = penguin != null;
            _body.color = penguin != null ? PenguinPalette.SpeciesColor(penguin.speciesNo) : Color.clear;
            _label.text = penguin != null ? PenguinLabels.Card(penguin) : "（空き）";
            if (_partyMark != null)
            {
                _partyMark.SetActive(inParty);
            }

            _frame.color = selected ? _selectedColor : _normalColor;
        }
    }
}
