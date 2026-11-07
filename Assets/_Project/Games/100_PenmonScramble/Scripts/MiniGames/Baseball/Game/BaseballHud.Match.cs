using System;
using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 試合の HUD: 上のまん中の点の板（先攻 点 − 点 後攻・何回の表 / 裏・塁の走者の3つのひし形）、
    /// 人が守る打席の始めに出す投げる子の札（左。押すとその子に替わる）、ゲームセットの欄（勝ち負けと点・もう一度・もどる）
    /// </summary>
    public partial class BaseballHud
    {
        private static readonly Color BaseOff = new Color(0.22f, 0.24f, 0.30f);
        private static readonly Color BaseOn = new Color(1f, 0.85f, 0.3f);
        private static readonly Color PitcherOff = new Color(0.35f, 0.42f, 0.55f);
        private static readonly Color PitcherOn = new Color(1f, 0.62f, 0.25f);
        private static readonly Color HumanTeamColor = new Color(1f, 0.92f, 0.5f);

        [Header("点の板")]
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _inningText;
        [Tooltip("一塁・二塁・三塁の順")]
        [SerializeField] private Image[] _baseLights;

        [Header("投げる子の札（人が守る打席の始めだけ）")]
        [SerializeField] private GameObject _pitcherCards;
        [Tooltip("編成の 1・2・3 番目の順")]
        [SerializeField] private Button[] _pitcherButtons;
        [SerializeField] private Text[] _pitcherLabels;

        [Header("ゲームセット")]
        [SerializeField] private GameObject _endPanel;
        [SerializeField] private Text _endText;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _leaveButton;

        private readonly string[] _teamNames = new string[BaseballRules.TeamCount];
        private int _humanTeam;

        /// <summary>投げる子の札を押した（編成の何番目か）</summary>
        public event Action<int> PitcherChosen;

        /// <summary>ゲームセットの「もう一度」「もどる」を押した</summary>
        public event Action RetryChosen;
        public event Action LeaveChosen;

        /// <summary>札とボタンの押したときを結ぶ（BaseballHud の Awake から。Start だと、出した札を後から隠してしまうため）</summary>
        private void AwakeMatch()
        {
            for (var i = 0; i < _pitcherButtons.Length; i++)
            {
                var slot = i;
                _pitcherButtons[i].onClick.AddListener(() => PitcherChosen?.Invoke(slot));
            }

            _retryButton.onClick.AddListener(() => RetryChosen?.Invoke());
            _leaveButton.onClick.AddListener(() => LeaveChosen?.Invoke());
            HidePitchers();
        }

        /// <summary>チームの呼び方（自分のチームは明るい色で出す）</summary>
        public void SetTeams(string away, string home, int humanTeam)
        {
            _teamNames[BaseballRules.Away] = away;
            _teamNames[BaseballRules.Home] = home;
            _humanTeam = humanTeam;
        }

        /// <summary>点・イニング（表 ▲ / 裏 ▼）・塁の走者</summary>
        public void ShowScore(BaseballGame game)
        {
            var away = TeamLabel(BaseballRules.Away);
            var home = TeamLabel(BaseballRules.Home);
            _scoreText.text = $"{away}  {game.Score(BaseballRules.Away)} − {game.Score(BaseballRules.Home)}  {home}";
            _inningText.text = game.IsPractice ? "れんしゅう" : $"{game.Inning}回{(game.IsTop ? "表 ▲" : "裏 ▼")}";
            for (var b = 0; b < _baseLights.Length; b++)
            {
                _baseLights[b].color = game.Bases.IsOccupied(b + 1) ? BaseOn : BaseOff;
            }
        }

        private string TeamLabel(int team)
        {
            var name = _teamNames[team] ?? string.Empty;
            return team == _humanTeam ? $"<color=#{ColorUtility.ToHtmlStringRGB(HumanTeamColor)}>{name}</color>" : name;
        }

        /// <summary>投げる子の札を出す（編成の子の名前。今の子を光らせる。編成が3体より少なければ余った札は隠す）</summary>
        public void ShowPitchers(IReadOnlyList<int> species, int selected)
        {
            _pitcherCards.SetActive(true);
            for (var i = 0; i < _pitcherButtons.Length; i++)
            {
                var shown = i < species.Count;
                _pitcherButtons[i].gameObject.SetActive(shown);
                if (!shown)
                {
                    continue;
                }

                var name = SpeciesTable.Get(species[i])?.Name ?? string.Empty;
                _pitcherLabels[i].text = i == selected ? $"▶ {name}" : name;
                _pitcherButtons[i].targetGraphic.color = i == selected ? PitcherOn : PitcherOff;
            }
        }

        public void HidePitchers() => _pitcherCards.SetActive(false);

        /// <summary>ゲームセットの欄を出す（ほかの札とボタンは隠す）</summary>
        public void ShowEnd(string text, Color color)
        {
            SetPitchCards(false);
            HidePitchers();
            foreach (var button in _batButtons)
            {
                button.SetActive(false);
            }

            _bendStick.SetActive(false);
            _banner.gameObject.SetActive(false);
            _endText.text = text;
            _endText.color = color;
            _endPanel.SetActive(true);
        }

        public void HideEnd() => _endPanel.SetActive(false);
    }
}
