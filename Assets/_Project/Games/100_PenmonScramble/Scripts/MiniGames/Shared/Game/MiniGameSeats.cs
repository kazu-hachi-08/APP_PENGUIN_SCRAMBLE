using System.Collections.Generic;
using PenmonScramble.Common.Profile;
using UnityEngine;

namespace PenmonScramble.MiniGames.Game
{
    /// <summary>
    /// 席（P1〜P4）の色。どのミニゲームでも同じ色で、誰の物かを見分ける（ゴルフの球の帯・サッカーの足元の輪など。本編のステージの席の色とそろえる）
    /// </summary>
    public static class MiniGameSeats
    {
        private static readonly Color[] Colors =
        {
            new Color(0.35f, 0.65f, 1f),
            new Color(1f, 0.45f, 0.45f),
            new Color(0.40f, 0.85f, 0.45f),
            new Color(1f, 0.80f, 0.25f),
        };

        public static Color ColorOf(int seat) => Colors[Mathf.Abs(seat) % Colors.Length];

        /// <summary>席の表示名を「・」でつなぐ（MVP が並んだとき用。名前の並びは席の順）</summary>
        public static string NamesOf(IReadOnlyList<int> seats)
        {
            var names = new List<string>();
            foreach (var seat in seats)
            {
                names.Add(SeatNames.Get(seat));
            }

            return string.Join("・", names);
        }
    }
}
