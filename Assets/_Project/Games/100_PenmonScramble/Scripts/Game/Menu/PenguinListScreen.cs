using System;
using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ペンギン一覧と編成（§7）。上に編成の3枠、下に持っている子を並べる。
    /// 枠を選んでから一覧の子を選び「編成に入れる」で入れ替える。編成にいない子は「売る」（2回押しで確定）
    /// </summary>
    public class PenguinListScreen : MonoBehaviour
    {
        [SerializeField] private PenguinCard[] _partySlots;
        [SerializeField] private PenguinCard _cardTemplate;
        [SerializeField] private RectTransform _content;
        [SerializeField] private Text _coinText;
        [SerializeField] private Text _countText;
        [SerializeField] private Text _detailText;
        [SerializeField] private Button _setPartyButton;
        [SerializeField] private Text _setPartyLabel;
        [SerializeField] private Button _sellButton;
        [SerializeField] private Text _sellLabel;
        [SerializeField] private Button _sortButton;
        [SerializeField] private Text _sortLabel;
        [SerializeField] private Button _backButton;

        private readonly List<PenguinCard> _cards = new List<PenguinCard>();
        private PenguinSortMode _sort = PenguinSortMode.Power;
        private int _selectedSlot;
        private int _selectedId;
        private bool _confirmSell;

        public event Action Closed;

        private static PlayerRecord Record => GameSave.Record;

        private void Awake()
        {
            for (var i = 0; i < _partySlots.Length; i++)
            {
                _partySlots[i].OnClick(SelectSlot);
            }

            _setPartyButton.onClick.AddListener(SetParty);
            _sellButton.onClick.AddListener(Sell);
            _sortButton.onClick.AddListener(ChangeSort);
            _backButton.onClick.AddListener(Close);
            _cardTemplate.gameObject.SetActive(false);
        }

        public void Open()
        {
            gameObject.SetActive(true);
            _selectedSlot = 0;
            Select(Record.Party.Count > 0 ? Record.Party[0] : 0);
        }

        private void Close()
        {
            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        private void SelectSlot(PenguinCard slot)
        {
            _selectedSlot = Array.IndexOf(_partySlots, slot);
            Select(slot.PenguinId);
        }

        private void SelectCard(PenguinCard card)
        {
            Select(card.PenguinId);
        }

        private void Select(int id)
        {
            _selectedId = id;
            _confirmSell = false;
            Refresh();
        }

        private void SetParty()
        {
            if (Record.SetPartySlot(_selectedSlot, _selectedId))
            {
                GameSave.Save();
            }

            Refresh();
        }

        private void Sell()
        {
            if (!_confirmSell)
            {
                _confirmSell = true;
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
            RefreshDetail();
        }

        private void RefreshParty()
        {
            for (var i = 0; i < _partySlots.Length; i++)
            {
                var penguin = i < Record.Party.Count ? Record.Find(Record.Party[i]) : null;
                _partySlots[i].Show(penguin, inParty: false, selected: i == _selectedSlot);
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
                    _cards[i].Show(penguin, Record.IsInParty(penguin.id), penguin.id == _selectedId);
                }
            }
        }

        private void RefreshDetail()
        {
            var penguin = Record.Find(_selectedId);
            var species = penguin != null ? SpeciesTable.Get(penguin.speciesNo) : null;
            _detailText.text = species != null ? Detail(penguin, species) : "ペンギンを選んでください";

            var canSet = species != null && !(_selectedSlot < Record.Party.Count && Record.Party[_selectedSlot] == _selectedId);
            _setPartyButton.interactable = canSet;
            _setPartyLabel.text = $"編成の{_selectedSlot + 1}番目に入れる";

            var canSell = penguin != null && !Record.IsInParty(penguin.id);
            _sellButton.interactable = canSell;
            _sellLabel.text = !canSell ? "売る" : _confirmSell ? $"もう一度押すと売る（+{PlayerRecord.SellPrice(penguin)}）" : $"売る（+{PlayerRecord.SellPrice(penguin)}）";
        }

        private static string Detail(OwnedPenguin penguin, SpeciesDef species)
        {
            var stats = PlayerRecord.StatsOf(penguin);
            return $"{PenguinLabels.Title(species)}\n" +
                $"パワー {penguin.power}　HP {stats.MaxHp}\n" +
                $"技1 {species.Skill1.Name}\n技2 {species.Skill2.Name}";
        }
    }
}
