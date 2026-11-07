using System;
using System.Collections.Generic;

namespace PenmonScramble.MiniGames.Core
{
    /// <summary>みんなでの遊び方（チームで戦うミニゲーム。サッカー・野球）。値は通信で送るので、一度配ったら番号を変えない</summary>
    public enum MiniGameOnlineMode
    {
        /// <summary>人を赤・青に分けて戦う</summary>
        Versus = 0,
        /// <summary>人は全員赤で、CPU（青）と戦う</summary>
        Coop = 1,
    }

    /// <summary>
    /// 赤・青のチーム分け（待ちの窓でホストが席ごとに選ぶ。サッカー・野球で同じ決まり）。
    /// チームの中の組み方（子の並び・打順）は各ミニゲームの Core が持つ
    /// </summary>
    public static class MiniGameTeams
    {
        /// <summary>部屋の席の数（ホストを含む）</summary>
        public const int MaxSeats = 4;

        public const int Count = 2;

        /// <summary>赤（サッカーは左のゴールを守る・野球は先攻）</summary>
        public const int Red = 0;

        /// <summary>青（サッカーは右のゴールを守る・野球は後攻）</summary>
        public const int Blue = 1;

        public static int Opponent(int team) => 1 - team;

        public static bool IsTeam(int team) => team == Red || team == Blue;

        /// <summary>席の初めのチーム（待ちの窓で、ホストが替えるまで）: P1・P3 が赤、P2・P4 が青</summary>
        public static int DefaultTeam(int seat) => seat % Count == 0 ? Red : Blue;

        /// <summary>協力は全員赤。対戦は渡されたチーム（赤・青のどちらでもない値は初めのチーム）</summary>
        public static int[] Resolve(MiniGameOnlineMode mode, IReadOnlyList<int> seatTeams, int seatCount)
        {
            var teams = new int[seatCount];
            for (var seat = 0; seat < seatCount; seat++)
            {
                var team = seatTeams != null && seat < seatTeams.Count ? seatTeams[seat] : DefaultTeam(seat);
                teams[seat] = mode == MiniGameOnlineMode.Coop ? Red : IsTeam(team) ? team : DefaultTeam(seat);
            }

            return teams;
        }

        /// <summary>開始できるか: 協力は1人から。対戦は赤・青の両方に人がいること</summary>
        public static bool CanStart(MiniGameOnlineMode mode, IReadOnlyList<int> seatTeams, int seatCount)
        {
            if (seatCount < 1)
            {
                return false;
            }

            if (mode == MiniGameOnlineMode.Coop)
            {
                return true;
            }

            var teams = Resolve(mode, seatTeams, seatCount);
            return Array.IndexOf(teams, Red) >= 0 && Array.IndexOf(teams, Blue) >= 0;
        }

        /// <summary>人がいちばん多いチームの人数</summary>
        public static int LargestTeam(MiniGameOnlineMode mode, IReadOnlyList<int> seatTeams, int seatCount)
        {
            var teams = Resolve(mode, seatTeams, seatCount);
            var most = 0;
            for (var team = 0; team < Count; team++)
            {
                most = Math.Max(most, Array.FindAll(teams, t => t == team).Length);
            }

            return most;
        }

        /// <summary>そのチームの人の席（若い順）</summary>
        public static List<int> SeatsOf(IReadOnlyList<int> seatTeams, int team)
        {
            var seats = new List<int>();
            for (var seat = 0; seat < (seatTeams?.Count ?? 0); seat++)
            {
                if (seatTeams[seat] == team)
                {
                    seats.Add(seat);
                }
            }

            return seats;
        }
    }
}
