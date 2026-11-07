using PenmonScramble.MiniGames.Core;
using UnityEngine;

namespace PenmonScramble.MiniGames.Game
{
    /// <summary>チームの色（赤・青）。待ちの窓の席の札・HUD の点・ユニフォームなど、チームで戦うミニゲームで同じ色を使う</summary>
    public static class MiniGameTeamColors
    {
        private static readonly Color[] Teams = { new Color(0.95f, 0.33f, 0.30f), new Color(0.27f, 0.52f, 0.97f) };

        public static Color Team(int team) => Teams[team == MiniGameTeams.Blue ? 1 : 0];

        /// <summary>チームの色を暗くした色（帽子・輪など、同じ色の物を重ねたときに見分ける）</summary>
        public static Color TeamDark(int team) => Color.Lerp(Team(team), Color.black, 0.35f);

        public static string TeamName(int team) => team == MiniGameTeams.Blue ? "青" : "赤";
    }
}
