using System.Collections.Generic;

namespace PenmonScramble.MiniGames.Core
{
    /// <summary>みんなでの MVP: 席ごとの数（サッカーは入れた点・野球は打点）が一番多い人</summary>
    public static class MiniGameMvp
    {
        /// <summary>一番多い席（並んだら全員。席の若い順）。誰も 1 以上でなければ空</summary>
        public static List<int> Find(IReadOnlyList<int> seatCounts)
        {
            var seats = new List<int>();
            var most = 0;
            for (var seat = 0; seat < (seatCounts?.Count ?? 0); seat++)
            {
                var count = seatCounts[seat];
                if (count <= 0 || count < most)
                {
                    continue;
                }

                if (count > most)
                {
                    most = count;
                    seats.Clear();
                }

                seats.Add(seat);
            }

            return seats;
        }
    }
}
