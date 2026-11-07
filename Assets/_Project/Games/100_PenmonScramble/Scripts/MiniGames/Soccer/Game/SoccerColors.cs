using PenmonScramble.MiniGames.Game;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>チームの色（赤・青。ミニゲームで共通の MiniGameTeamColors）。HUD の点・キーパーのユニフォーム・AI の子の足元の輪で同じ色を使う</summary>
    public static class SoccerColors
    {
        public static Color Team(int team) => MiniGameTeamColors.Team(team);

        public static Color TeamDark(int team) => MiniGameTeamColors.TeamDark(team);

        public static string TeamName(int team) => MiniGameTeamColors.TeamName(team);
    }
}
