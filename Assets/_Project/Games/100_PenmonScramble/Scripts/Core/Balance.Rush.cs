using System;

namespace PenmonScramble.Core
{
    /// <summary>ボスラッシュの報酬（コインだけ）。難易度で量が変わる。数値は置き値（フェーズ 7 のバランス調整で決め直す）</summary>
    public static partial class Balance
    {
        // 添字は Difficulty。クリアのコイン。各難易度の初クリアは同じだけ上乗せする
        private static readonly int[] RushClearCoinsByDifficulty = { 300, 800, 2000 };

        /// <summary>失敗したとき、勝った戦い1つにつき、クリアのコインのこの割合をもらう（あと少しの挑戦が無駄にならないように）</summary>
        public const float RushFailCoinsPerRoundRatio = 0.15f;

        public static int RushClearCoins(Difficulty difficulty) => RushClearCoinsByDifficulty[IndexOf(difficulty)];

        /// <summary>初クリアの上乗せ。クリアのコインと同じだけ</summary>
        public static int RushFirstClearBonus(Difficulty difficulty) => RushClearCoins(difficulty);

        /// <summary>失敗: 勝った戦いの数 × クリアのコインの 15% ＋ 倒した数 × 1</summary>
        public static int RushFailCoins(Difficulty difficulty, int roundsWon, int enemiesDefeated)
        {
            var perRound = (int)Math.Round(RushClearCoins(difficulty) * RushFailCoinsPerRoundRatio);
            return Math.Max(0, roundsWon) * perRound + Math.Max(0, enemiesDefeated) * FailCoinsPerEnemy;
        }
    }
}
