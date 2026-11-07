using System;
using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Game;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 打順のカード（左が自分のチーム・右が CPU のチーム。属性の色・今の打者 / 投げる子・魔球と必殺打を使ったか）と、
    /// 魔球・必殺打の札（投げる前に ON にしておく。押すと切り替わる。PC ではキーの印つき）。
    /// 自分が守る打席の始めは、自分のカードを押すと投げる子が替わる（見出しも「投げる子を選ぶ」に替える）
    /// </summary>
    public partial class BaseballHud
    {
        private static readonly Color ToggleOffColor = new Color(0.30f, 0.33f, 0.42f);

        [Header("打順のカード")]
        [Tooltip("両チームのカードと見出しをまとめた物（結果を出している間は隠す）")]
        [SerializeField] private GameObject _lineups;
        [Tooltip("自分のチーム（左）の編成の 1・2・3 番目")]
        [SerializeField] private BaseballTeamCard[] _ownCards;
        [Tooltip("CPU のチーム（右）")]
        [SerializeField] private BaseballTeamCard[] _cpuCards;
        [SerializeField] private Text _ownHeading;
        [SerializeField] private Text _cpuHeading;

        [Header("魔球・必殺打の札")]
        [SerializeField] private Button _specialButton;
        [SerializeField] private Image _specialImage;
        [SerializeField] private Text _specialLabel;
        [Tooltip("札の下のキーの印（スマホでは隠す）")]
        [SerializeField] private GameObject _specialKeyHint;

        private IReadOnlyList<int>[] _teams;
        private int _humanTeam;
        private bool _choosingPitcher;
        private bool _lineupsShown = true;
        private BaseballGame _lastGame;

        /// <summary>自分のカードを押した（編成の何番目か）。投げる子を選べる間だけ効く</summary>
        public event Action<int> PitcherChosen;

        /// <summary>魔球・必殺打の札を押した</summary>
        public event Action SpecialToggled;

        /// <summary>カードと札の押したときを結ぶ（BaseballHud の Awake から。Start だと、出した札を後から隠してしまうため）</summary>
        private void AwakeLineup()
        {
            for (var i = 0; i < _ownCards.Length; i++)
            {
                var slot = i;
                _ownCards[i].Clicked += () => PitcherChosen?.Invoke(slot);
            }

            _specialButton.onClick.AddListener(() => SpecialToggled?.Invoke());
            _specialKeyHint.SetActive(!_isTouch);
            HideSpecialToggle();
        }

        /// <summary>両チームの子とチームの呼び方（自分のチームは明るい色で出す）</summary>
        public void SetTeams(IReadOnlyList<int>[] teams, int humanTeam, string away, string home)
        {
            _teams = teams;
            _humanTeam = humanTeam;
            _teamNames[BaseballRules.Away] = away;
            _teamNames[BaseballRules.Home] = home;
            _choosingPitcher = false;
        }

        /// <summary>両チームのカード（打つ側は今の打者、守る側は今の投げる子に印）</summary>
        public void ShowLineups(BaseballGame game)
        {
            _lastGame = game;
            ShowCards(_ownCards, game, _humanTeam);
            ShowCards(_cpuCards, game, 1 - _humanTeam);
            var batting = game.BattingTeam == _humanTeam;
            _ownHeading.text = batting ? "あなた（打つ）" : _choosingPitcher ? "投げる子を選ぶ（↑↓）" : "あなた（守る）";
            _cpuHeading.text = batting ? "CPU（守る）" : "CPU（打つ）";
        }

        private void ShowCards(BaseballTeamCard[] cards, BaseballGame game, int team)
        {
            var species = _teams[team];
            var batting = game.BattingTeam == team;
            var current = batting ? game.BatterSlot(team) : game.PitcherSlot(team);
            for (var i = 0; i < cards.Length; i++)
            {
                if (i >= species.Count)
                {
                    cards[i].Hide();
                    continue;
                }

                cards[i].Show(species[i], game.IsMagicUsed(team, i), game.IsSpecialHitUsed(team, i), i == current);
                cards[i].SetSelectable(_choosingPitcher && team == _humanTeam);
            }
        }

        /// <summary>自分が守る打席の始めだけ、自分のカードを押して投げる子を選べる</summary>
        public void SetPitcherChoice(bool choosing)
        {
            _choosingPitcher = choosing;
            if (_lastGame != null)
            {
                ShowLineups(_lastGame);
            }
        }

        private void SetLineupsShown(bool shown)
        {
            _lineupsShown = shown;
            _lineups.SetActive(shown);
        }

        /// <summary>
        /// 魔球（投げる番）・必殺打（打つ番）の札。ON なら属性の色で「ON！」と技の名前、OFF なら灰色。
        /// 残っていない・切り替えられない間は隠す（押しても何も起きない札は出さない）
        /// </summary>
        public void ShowSpecialToggle(bool shown, bool batting, bool on, BaseballSpecial special)
        {
            var visible = shown && special.IsActive && _lineupsShown;
            _specialButton.gameObject.SetActive(visible);
            _specialKeyHint.SetActive(visible && !_isTouch);
            if (!visible)
            {
                return;
            }

            var kind = batting ? "必殺打" : "魔球";
            var move = batting ? BaseballSpecialNames.Hit(special.Element) : BaseballSpecialNames.Magic(special.Element);
            _specialLabel.text = $"{kind} {(on ? "ON！" : "OFF")}\n<size=24>{move}</size>";
            _specialImage.color = on ? PenguinPalette.ElementColor(special.Element) : ToggleOffColor;
            _specialLabel.color = on ? Color.black : Color.white;
        }

        private void HideSpecialToggle()
        {
            _specialButton.gameObject.SetActive(false);
            _specialKeyHint.SetActive(false);
        }
    }
}
