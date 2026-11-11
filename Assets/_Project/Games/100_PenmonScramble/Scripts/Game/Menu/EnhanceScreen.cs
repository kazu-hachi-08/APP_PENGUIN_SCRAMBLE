using System;
using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 強化の窓（ペンモン画面の上に重ねる）。左に強化する子と強化後の見込み、右に素材を選ぶ一覧、下に必要なコインと「強化する」。
    /// 計算は Core の PlayerRecord.Enhance。強化した瞬間の演出は EnhanceScreen.Effects.cs
    /// </summary>
    public partial class EnhanceScreen : MonoBehaviour
    {
        private const string AfterColor = "#7CFF8A";
        private const string OverflowColor = "#FF6B6B";
        private const string LimitUpColor = "#FFD24D";

        [Header("強化する子")]
        [SerializeField] private Image _face;
        [SerializeField] private Text _titleText;
        [SerializeField] private Text[] _pips;
        [SerializeField] private Text _powerText;
        [SerializeField] private Image _gaugeNow;
        [SerializeField] private Image _gaugeAfter;
        [SerializeField] private Text _capText;
        [SerializeField] private Text _roomText;

        [Header("素材")]
        [SerializeField] private PenguinCard _cardTemplate;
        [SerializeField] private RectTransform _content;
        [SerializeField] private Text _materialCountText;
        [SerializeField] private Text _emptyText;
        [SerializeField] private Button _clearButton;

        [Header("下の段")]
        [SerializeField] private Text _coinText;
        [SerializeField] private Text _costText;
        [SerializeField] private Text _hintText;
        [SerializeField] private Button _enhanceButton;
        [SerializeField] private Text _enhanceLabel;
        [SerializeField] private Button _backButton;

        [Header("◆の色")]
        [SerializeField] private Color _pipOnColor = new Color(1f, 0.82f, 0.30f);
        [SerializeField] private Color _pipOffColor = new Color(1f, 1f, 1f, 0.35f);

        private readonly List<PenguinCard> _cards = new List<PenguinCard>();
        private readonly List<int> _selected = new List<int>();
        private int _targetId;
        // ★3 や強い子を素材に入れたときの2回押し（売るのと同じ）
        private bool _confirm;

        public event Action Closed;

        private static PlayerRecord Record => GameSave.Record;

        private void Awake()
        {
            _cardTemplate.gameObject.SetActive(false);
            _clearButton.onClick.AddListener(ClearSelection);
            _enhanceButton.onClick.AddListener(PressEnhance);
            _backButton.onClick.AddListener(Close);
            AwakeEffects();
        }

        public void Open(int targetId)
        {
            _targetId = targetId;
            _selected.Clear();
            _confirm = false;
            gameObject.SetActive(true);
            Refresh();
        }

        private void Close()
        {
            if (IsPlaying)
            {
                return;
            }

            gameObject.SetActive(false);
            Closed?.Invoke();
        }

        private void ToggleMaterial(PenguinCard card)
        {
            if (IsPlaying)
            {
                return;
            }

            if (!_selected.Remove(card.PenguinId) && _selected.Count < Balance.MaxEnhanceMaterials)
            {
                _selected.Add(card.PenguinId);
            }

            _confirm = false;
            GameSounds.Play(GameSound.Click);
            Refresh();
        }

        private void ClearSelection()
        {
            _selected.Clear();
            _confirm = false;
            Refresh();
        }

        private void PressEnhance()
        {
            var plan = Record.PlanEnhance(_targetId, _selected);
            if (!plan.CanEnhance || IsPlaying)
            {
                return;
            }

            if (plan.NeedsConfirm && !_confirm)
            {
                _confirm = true;
                Refresh();
                return;
            }

            // 吸い込まれる顔は、消える前のカードの位置から飛ばす
            var flights = CollectFlights();
            var result = Record.Enhance(_targetId, _selected);
            if (!result.CanEnhance)
            {
                return;
            }

            GameSave.Save();
            _selected.Clear();
            _confirm = false;
            RefreshMaterials();
            PlayEnhance(result, flights);
        }

        private void Refresh()
        {
            var plan = Record.PlanEnhance(_targetId, _selected);
            _coinText.text = $"コイン {Record.Coins}";
            RefreshTarget(plan);
            RefreshMaterials();
            RefreshActions(plan);
        }

        private void RefreshTarget(EnhancePlan plan)
        {
            var target = Record.Find(_targetId);
            var species = target != null ? SpeciesTable.Get(target.speciesNo) : null;
            if (species == null)
            {
                return;
            }

            CreatureArt.ShowFace(_face, species.No);
            _titleText.text = PenmonDetailPanel.TitleOf(species);
            var hasPlan = _selected.Count > 0;
            ShowNumbers(plan, hasPlan ? plan.PowerAfter : target.Power, hasPlan);
            ShowPips(hasPlan ? plan.LimitBreakAfter : target.limitBreak, target.limitBreak);
        }

        /// <summary>パワーの見込み（150 → 182）・ゲージ・上限・あと何か。演出の数え上げからも呼ぶ</summary>
        private void ShowNumbers(EnhancePlan plan, int shownAfter, bool hasPlan)
        {
            var cap = hasPlan ? plan.CapAfter : plan.CapBefore;
            _powerText.text = hasPlan && shownAfter != plan.PowerBefore
                ? $"パワー {plan.PowerBefore} → <color={AfterColor}>{shownAfter}</color>"
                : $"パワー {shownAfter}";
            _gaugeNow.fillAmount = Ratio(plan.PowerBefore, cap);
            _gaugeAfter.fillAmount = Ratio(shownAfter, cap);
            _capText.text = plan.CapAfter != plan.CapBefore && hasPlan
                ? $"上限 {plan.CapBefore} → <color={LimitUpColor}>{plan.CapAfter}</color>　上限UP！"
                : $"上限 {cap}";
            _roomText.text = plan.Overflow > 0 && hasPlan
                ? $"<color={OverflowColor}>上限をこえる {plan.Overflow} はむだになります</color>"
                : $"上限まで あと {Mathf.Max(0, cap - shownAfter)}";
        }

        /// <summary>◆: 解放済みは金、これから解放する分は金で明滅（演出で灯す前の見込み）</summary>
        private void ShowPips(int shown, int owned)
        {
            for (var i = 0; i < _pips.Length; i++)
            {
                var on = i < shown;
                _pips[i].text = on ? "◆" : "◇";
                _pips[i].color = on ? _pipOnColor : _pipOffColor;
                _pips[i].transform.localScale = Vector3.one;
                SetPipPreview(i, on && i >= owned);
            }
        }

        private void RefreshMaterials()
        {
            var target = Record.Find(_targetId);
            var candidates = Record.MaterialCandidates(_targetId);
            while (_cards.Count < candidates.Count)
            {
                var card = Instantiate(_cardTemplate, _content);
                card.OnClick(ToggleMaterial);
                _cards.Add(card);
            }

            for (var i = 0; i < _cards.Count; i++)
            {
                var visible = i < candidates.Count;
                _cards[i].gameObject.SetActive(visible);
                if (visible)
                {
                    ShowCandidate(_cards[i], target, candidates[i]);
                }
            }

            _emptyText.gameObject.SetActive(candidates.Count == 0);
            _materialCountText.text = $"素材を選ぶ　{_selected.Count} / {Balance.MaxEnhanceMaterials}";
        }

        private void ShowCandidate(PenguinCard card, OwnedPenguin target, OwnedPenguin penguin)
        {
            var partyIndex = PenguinLabels.IndexOf(Record.Party, penguin.id);
            card.Show(penguin, partyIndex, _selected.Contains(penguin.id));
            var usable = Record.CanBeMaterial(_targetId, penguin);
            card.SetMaterialState(!usable, PlayerRecord.IsSameSpecies(target, penguin));
        }

        private void RefreshActions(EnhancePlan plan)
        {
            _costText.text = _selected.Count > 0 ? $"必要なコイン {plan.Coins}" : string.Empty;
            _costText.color = plan.Result == EnhanceResult.NotEnoughCoins ? Color.red : Color.white;
            _clearButton.interactable = _selected.Count > 0;
            _enhanceButton.interactable = plan.CanEnhance;
            _enhanceLabel.text = ButtonLabel(plan);
            _hintText.text = HintText(plan);
        }

        private string ButtonLabel(EnhancePlan plan)
        {
            switch (plan.Result)
            {
                case EnhanceResult.Ok: return _confirm ? "本当に強化する？" : "強化する";
                case EnhanceResult.NotEnoughCoins: return "コインが足りない";
                case EnhanceResult.NoEffect: return "上限に達しています";
                default: return "素材を選んでください";
            }
        }

        private string HintText(EnhancePlan plan)
        {
            if (_confirm)
            {
                return $"<color={OverflowColor}>★3 や、この子より強い子が素材に入っています</color>";
            }

            if (plan.Result == EnhanceResult.NoEffect)
            {
                return "同じ種類の子を素材にすると、上限が上がります";
            }

            return _selected.Count > 0 ? "選んだ素材は、強化すると居なくなります"
                : "素材にした子は居なくなります。同じ種類の子は「上限UP」";
        }

        private static float Ratio(int value, int cap) => cap > 0 ? Mathf.Clamp01((float)value / cap) : 0f;
    }
}
