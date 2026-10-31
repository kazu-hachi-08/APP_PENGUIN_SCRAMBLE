using System;
using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ペンモン画面。左上に編成の帯（3枠）、左下に持っている子の一覧、右に選んだ子の図鑑カード（PenmonDetailPanel）。
    /// 一覧か枠の子を選ぶ → 「編成に入れる」 → 光った枠から入れたい所を押す、の順で編成する。
    /// 編成にいない子は「売る」（2回押しで確定）
    /// </summary>
    public class PenguinListScreen : MonoBehaviour
    {
        [SerializeField] private PenguinCard[] _partySlots;
        [SerializeField] private PenguinCard _cardTemplate;
        [SerializeField] private RectTransform _content;
        [SerializeField] private Text _coinText;
        [SerializeField] private Text _countText;
        [SerializeField] private Text _hintText;
        [SerializeField] private PenmonDetailPanel _detail;
        [SerializeField] private Button _setPartyButton;
        [SerializeField] private Text _setPartyLabel;
        [SerializeField] private Button _sellButton;
        [SerializeField] private Text _sellLabel;
        [SerializeField] private Button _sortButton;
        [SerializeField] private Text _sortLabel;
        [SerializeField] private Button _backButton;

        private readonly List<PenguinCard> _cards = new List<PenguinCard>();
        private PenguinSortMode _sort = PenguinSortMode.Power;
        private int _selectedId;
        // 「編成に入れる」を押して、入れる枠を選んでいる間
        private bool _picking;
        private bool _confirmSell;

        public event Action Closed;

        private static PlayerRecord Record => GameSave.Record;

        private void Awake()
        {
            foreach (var slot in _partySlots)
            {
                slot.OnClick(ClickSlot);
            }

            _setPartyButton.onClick.AddListener(TogglePicking);
            _sellButton.onClick.AddListener(Sell);
            _sortButton.onClick.AddListener(ChangeSort);
            _backButton.onClick.AddListener(Close);
            _cardTemplate.gameObject.SetActive(false);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            Select(Record.Party.Count > 0 ? Record.Party[0] : 0);
        }

        private void Close()
        {
            _detail.Hide();
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        /// <summary>枠を選んでいる間は、その枠へ入れる。それ以外は、枠の子を選ぶ</summary>
        private void ClickSlot(PenguinCard slot)
        {
            if (!_picking)
            {
                Select(slot.PenguinId);
                return;
            }

            if (Record.SetPartySlot(Array.IndexOf(_partySlots, slot), _selectedId))
            {
                GameSave.Save();
            }

            _picking = false;
            Refresh();
        }

        private void SelectCard(PenguinCard card)
        {
            Select(card.PenguinId);
        }

        private void Select(int id)
        {
            _selectedId = id;
            _picking = false;
            _confirmSell = false;
            Refresh();
        }

        private void TogglePicking()
        {
            _picking = !_picking && Record.Find(_selectedId) != null;
            _confirmSell = false;
            Refresh();
        }

        private void Sell()
        {
            if (!_confirmSell)
            {
                _confirmSell = true;
                _picking = false;
                Refresh();
                return;
            }

            if (Record.Sell(_selectedId))
            {
                GameSave.Save();
            }

            Select(0);
        }

        private void ChangeSort()
        {
            _sort = PenguinSort.Next(_sort);
            Refresh();
        }

        private void Refresh()
        {
            _coinText.text = $"コイン {Record.Coins}";
            _countText.text = $"{Record.Penguins.Count} / {Balance.MaxPenguins} 体";
            _sortLabel.text = PenguinSort.Label(_sort);
            RefreshParty();
            RefreshCards();
            RefreshActions();
            _detail.Show(Record.Find(_selectedId));
        }

        private void RefreshParty()
        {
            for (var i = 0; i < _partySlots.Length; i++)
            {
                var penguin = i < Record.Party.Count ? Record.Find(Record.Party[i]) : null;
                _partySlots[i].Show(penguin, i, selected: !_picking && penguin != null && penguin.id == _selectedId);
                _partySlots[i].SetPickable(_picking);
            }
        }

        private void RefreshCards()
        {
            var sorted = PenguinSort.Sorted(Record.Penguins, _sort);
            while (_cards.Count < sorted.Count)
            {
                var card = Instantiate(_cardTemplate, _content);
                card.OnClick(SelectCard);
                _cards.Add(card);
            }

            for (var i = 0; i < _cards.Count; i++)
            {
                var visible = i < sorted.Count;
                _cards[i].gameObject.SetActive(visible);
                if (visible)
                {
                    var penguin = sorted[i];
                    _cards[i].Show(penguin, IndexOf(Record.Party, penguin.id), penguin.id == _selectedId);
                }
            }
        }

        private void RefreshActions()
        {
            var penguin = Record.Find(_selectedId);
            var species = penguin != null ? SpeciesTable.Get(penguin.speciesNo) : null;
            var name = species?.Name ?? string.Empty;
            var inParty = penguin != null && Record.IsInParty(penguin.id);
            _hintText.text = _picking ? $"{name}を入れる枠を、上から選んでください"
                : penguin != null ? "「編成に入れる」を押してから、入れる枠を選びます"
                : "一覧からペンモンを選ぶと、右にくわしく出ます";

            _setPartyButton.interactable = species != null;
            _setPartyLabel.text = _picking ? "やめる" : inParty ? "並びを変える" : "編成に入れる";

            var canSell = penguin != null && !inParty;
            _sellButton.interactable = canSell;
            _sellLabel.text = !canSell ? (inParty ? "編成中は売れない" : "売る")
                : _confirmSell ? $"本当に売る？ +{PlayerRecord.SellPrice(penguin)}"
                : $"売る +{PlayerRecord.SellPrice(penguin)}";
        }

        private static int IndexOf(IReadOnlyList<int> list, int value)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i] == value)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
