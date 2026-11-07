using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// ホストが待ちの窓で選んだサッカーの遊び方（スタジアム・対戦 / 協力・席ごとの赤・青）。
    /// シーンをまたいで残す（待ちの窓の板が書き、サッカーのシーンのホストがチームを組むときに読む。試合から待ちの窓へ戻ったときも同じ選びで開く）
    /// </summary>
    public static class SoccerRoomSetup
    {
        private static readonly int[] TeamsBySeat = new int[SoccerRoomChoice.MaxSeats];

        static SoccerRoomSetup()
        {
            ResetStatics();
        }

        /// <summary>選んだスタジアム（まだ選んでいなければ 0）</summary>
        public static int Stadium { get; set; }

        public static SoccerOnlineMode Mode { get; set; }

        /// <summary>席ごとのチーム（対戦で使う）</summary>
        public static int[] Teams => TeamsBySeat;

        public static void SetTeam(int seat, int team)
        {
            if (seat >= 0 && seat < TeamsBySeat.Length)
            {
                TeamsBySeat[seat] = team;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Stadium = 0;
            // はじめての部屋は、友達どうしで戦う対戦から（ひとりのときは開始できないので、協力へ替える一言を出す）
            Mode = SoccerOnlineMode.Versus;
            for (var seat = 0; seat < TeamsBySeat.Length; seat++)
            {
                TeamsBySeat[seat] = SoccerTeamPlan.DefaultTeam(seat);
            }
        }
    }
}
