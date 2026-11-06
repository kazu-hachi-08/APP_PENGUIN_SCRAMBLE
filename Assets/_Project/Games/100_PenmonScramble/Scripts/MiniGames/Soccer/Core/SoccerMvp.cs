using System.Collections.Generic;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>みんなでの MVP: 3分の間に点を一番入れた人（PK戦の点は数えない）</summary>
    public static class SoccerMvp
    {
        /// <summary>点を一番入れた席（並んだら全員。席の若い順）。誰も入れていなければ空</summary>
        public static List<int> Find(IReadOnlyList<int> seatGoals)
        {
            var seats = new List<int>();
            var most = 0;
            for (var seat = 0; seat < (seatGoals?.Count ?? 0); seat++)
            {
                var goals = seatGoals[seat];
                if (goals <= 0 || goals < most)
                {
                    continue;
                }

                if (goals > most)
                {
                    most = goals;
                    seats.Clear();
                }

                seats.Add(seat);
            }

            return seats;
        }
    }
}
