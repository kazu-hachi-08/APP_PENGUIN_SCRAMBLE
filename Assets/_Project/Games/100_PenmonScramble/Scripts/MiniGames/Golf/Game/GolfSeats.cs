using PenmonScramble.Common.Profile;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>席の色と名前。球の帯・HUD の打順で同じ色を使う（本編のステージの席の色とそろえる）</summary>
    public static class GolfSeats
    {
        private static readonly Color[] Colors =
        {
            new Color(0.35f, 0.65f, 1f),
            new Color(1f, 0.45f, 0.45f),
            new Color(0.40f, 0.85f, 0.45f),
            new Color(1f, 0.80f, 0.25f),
        };

        public static Color ColorOf(int seat) => Colors[Mathf.Abs(seat) % Colors.Length];

        /// <summary>名前の無い席は「P1」など（SeatNames）</summary>
        public static string Label(int seat) => SeatNames.Get(seat);
    }
}
