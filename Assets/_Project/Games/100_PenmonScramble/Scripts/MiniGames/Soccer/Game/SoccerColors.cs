using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>チームの色（赤・青）。HUD の点・キーパーのユニフォーム・AI の子の足元の輪で同じ色を使う</summary>
    public static class SoccerColors
    {
        private static readonly Color[] Teams = { new Color(0.95f, 0.33f, 0.30f), new Color(0.27f, 0.52f, 0.97f) };

        public static Color Team(int team) => Teams[team == SoccerRules.Blue ? 1 : 0];

        /// <summary>チームの色を暗くした色（帽子・輪など、同じ色の物を重ねたときに見分ける）</summary>
        public static Color TeamDark(int team) => Color.Lerp(Team(team), Color.black, 0.35f);

        public static string TeamName(int team) => team == SoccerRules.Blue ? "青" : "赤";
    }
}
