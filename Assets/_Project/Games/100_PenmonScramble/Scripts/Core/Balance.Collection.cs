using System;

namespace PenmonScramble.Core
{
    /// <summary>ガチャ・コイン・持てる数と、ステージの報酬の調整値</summary>
    public static partial class Balance
    {
        public const int GachaCost = 100;

        /// <summary>初回だけ無料で引ける回数。3体そろえてから始めるため、編成の数と同じにする</summary>
        public const int FreePulls = PartySize;

        public const int MaxPenguins = 100;

        // ★1 / ★2 / ★3 の出る確率（%）。添字は★-1
        private static readonly int[] StarRates = { 70, 25, 5 };

        // 売るともらえるコイン。添字は★-1
        private static readonly int[] SellPrices = { 10, 30, 100 };

        // 解放したワールドの数ごとの、引いたときのパワーの範囲。添字はワールドの数-1
        private static readonly int[] PowerMins = { 80, 160, 280 };
        private static readonly int[] PowerMaxes = { 120, 240, 400 };

        // ★が高いほど強い（パワーに掛ける）。添字は★-1
        private static readonly float[] StarPowerScales = { 1f, 1.2f, 1.5f };

        public const int ClearCoins = 100;
        public const int FirstClearBonusCoins = 100;

        /// <summary>失敗したときは、倒した数 × これだけもらえる（遊んだ分が無駄にならないように少しだけ）</summary>
        public const int FailCoinsPerEnemy = 1;

        public const int MaxStars = 3;

        /// <summary>0〜99 の値から★を決める（確率の表どおりに分ける）</summary>
        public static int StarsFromRoll(int roll)
        {
            var total = 0;
            for (var i = 0; i < StarRates.Length; i++)
            {
                total += StarRates[i];
                if (roll < total)
                {
                    return i + 1;
                }
            }

            return MaxStars;
        }

        public static int StarRatePercent(int stars) => StarRates[StarIndex(stars)];

        public static int SellPrice(int stars) => SellPrices[StarIndex(stars)];

        public static int PowerMin(int worldsUnlocked) => PowerMins[WorldIndex(worldsUnlocked)];

        public static int PowerMax(int worldsUnlocked) => PowerMaxes[WorldIndex(worldsUnlocked)];

        public static float StarPowerScale(int stars) => StarPowerScales[StarIndex(stars)];

        private static int StarIndex(int stars) => Math.Clamp(stars, 1, MaxStars) - 1;

        private static int WorldIndex(int worlds) => Math.Clamp(worlds, 1, PowerMins.Length) - 1;
    }
}
