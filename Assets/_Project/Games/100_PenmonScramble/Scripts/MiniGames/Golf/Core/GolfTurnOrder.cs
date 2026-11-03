using System.Collections.Generic;
using System.Linq;

namespace PenmonScramble.Golf.Core
{
    /// <summary>ホールごとの打つ順</summary>
    public static class GolfTurnOrder
    {
        /// <summary>1ホール目は席の順</summary>
        public static int[] SeatOrder(int playerCount)
        {
            return Enumerable.Range(0, playerCount).ToArray();
        }

        /// <summary>2ホール目からは、前のホールまでの打数が少ない順（同じなら席の順）</summary>
        public static int[] ByTotals(IReadOnlyList<int> totals)
        {
            // OrderBy は同じ値の並びを変えない（席の順が残る）
            return Enumerable.Range(0, totals.Count).OrderBy(seat => totals[seat]).ToArray();
        }
    }
}
