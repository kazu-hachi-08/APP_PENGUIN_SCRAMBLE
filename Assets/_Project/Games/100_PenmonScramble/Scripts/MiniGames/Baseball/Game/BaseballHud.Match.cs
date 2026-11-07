using PenmonScramble.Baseball.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// スコアボード（上のまん中）: 先攻・後攻の2段に、イニングごとの点と合計。右に何回の表 / 裏と、塁の走者の3つのひし形。
    /// 打っている回の枠を光らせる。後攻が3回裏を打たずに勝ったら、その枠は「X」。試しの打席は合計だけ
    /// </summary>
    public partial class BaseballHud
    {
        private static readonly Color BaseOff = new Color(0.22f, 0.24f, 0.30f);
        private static readonly Color BaseOn = new Color(1f, 0.85f, 0.3f);
        private static readonly Color HumanTeamColor = new Color(1f, 0.92f, 0.5f);
        private static readonly Color CellColor = Color.white;
        private static readonly Color CurrentCellColor = new Color(1f, 0.85f, 0.3f);
        private const string SkippedInning = "X";

        [Header("スコアボード")]
        [SerializeField] private RectTransform _scoreboard;
        [Tooltip("結果を出している間に、スコアボードを置く所（上のまん中から。右の結果の欄と重ならない左）")]
        [SerializeField] private Vector2 _resultScoreboardPosition = new Vector2(-430f, -480f);
        [SerializeField] private Text _awayName;
        [SerializeField] private Text _homeName;
        [Tooltip("1回・2回・3回・合計の順")]
        [SerializeField] private Text[] _awayCells;
        [SerializeField] private Text[] _homeCells;
        [SerializeField] private Text _inningText;
        [Tooltip("一塁・二塁・三塁の順")]
        [SerializeField] private Image[] _baseLights;

        private readonly string[] _teamNames = new string[BaseballRules.TeamCount];
        private Vector2 _scoreboardHome;
        private bool _scoreboardHomeKnown;

        /// <summary>イニングごとの点・合計・イニング（表 ▲ / 裏 ▼）・塁の走者</summary>
        public void ShowScore(BaseballGame game)
        {
            ShowRow(game, BaseballRules.Away, _awayName, _awayCells);
            ShowRow(game, BaseballRules.Home, _homeName, _homeCells);
            _inningText.text = game.IsPractice ? "れんしゅう" : game.IsOver ? "試合終了" : $"{game.Inning}回{(game.IsTop ? "表 ▲" : "裏 ▼")}";
            for (var b = 0; b < _baseLights.Length; b++)
            {
                _baseLights[b].color = game.Bases.IsOccupied(b + 1) ? BaseOn : BaseOff;
            }
        }

        private void ShowRow(BaseballGame game, int team, Text name, Text[] cells)
        {
            name.text = TeamLabel(team);
            var innings = cells.Length - 1;
            for (var i = 0; i < innings; i++)
            {
                var inning = i + 1;
                cells[i].text = game.IsPractice ? string.Empty : InningCell(game, team, inning);
                var batting = !game.IsOver && game.Inning == inning && game.BattingTeam == team;
                cells[i].color = batting ? CurrentCellColor : CellColor;
            }

            cells[innings].text = game.Score(team).ToString();
        }

        /// <summary>その回の点（まだ打っていない回は空。後攻が3回裏を打たずに勝ったら X）</summary>
        private static string InningCell(BaseballGame game, int team, int inning)
        {
            var reached = inning < game.Inning || (inning == game.Inning && (team == BaseballRules.Away || !game.IsTop));
            if (reached)
            {
                return game.InningRuns(team, inning).ToString();
            }

            var skipped = game.IsOver && team == BaseballRules.Home && inning == game.Inning && game.IsTop;
            return skipped ? SkippedInning : string.Empty;
        }

        private string TeamLabel(int team)
        {
            var name = _teamNames[team] ?? string.Empty;
            return team == _humanTeam ? $"<color=#{ColorUtility.ToHtmlStringRGB(HumanTeamColor)}>{name}</color>" : name;
        }

        /// <summary>結果を出している間は、右の結果の欄と重ならない左へ寄せる</summary>
        private void PlaceScoreboard(bool result)
        {
            if (!_scoreboardHomeKnown)
            {
                _scoreboardHome = _scoreboard.anchoredPosition;
                _scoreboardHomeKnown = true;
            }

            _scoreboard.anchoredPosition = result ? _resultScoreboardPosition : _scoreboardHome;
        }
    }
}
