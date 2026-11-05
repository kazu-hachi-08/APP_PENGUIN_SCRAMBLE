using PenmonScramble.Common.Profile;
using PenmonScramble.MiniGames.Game;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>席の色と名前。球の帯・HUD の打順で同じ色を使う（本編のステージの席の色とそろえる）</summary>
    public static class GolfSeats
    {
        /// <summary>色はミニゲームで共通（MiniGameSeats）</summary>
        public static Color ColorOf(int seat) => MiniGameSeats.ColorOf(seat);

        /// <summary>名前の無い席は「P1」など（SeatNames）</summary>
        public static string Label(int seat) => SeatNames.Get(seat);
    }
}
