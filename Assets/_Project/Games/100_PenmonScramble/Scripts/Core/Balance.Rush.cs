using System;

namespace PenmonScramble.Core
{
    /// <summary>
    /// ボスラッシュの報酬（コインだけ）。難易度とステージで量が変わる。
    /// 先のステージほど多くする: 難易度だけで決めると、育ちきった編成で 1 つ目のステージの「げきむず」を一瞬で回すのが一番もうかってしまうため
    /// </summary>
    public static partial class Balance
    {
        // 添字は Difficulty。3 つ目のステージ（深海の闘技場）のクリアのコイン。各難易度の初クリアは同じだけ上乗せする
        private static readonly int[] RushClearCoinsByDifficulty = { 350, 600, 900 };

        // ステージごとの重み。添字はステージの番号 - RushTable.FirstNo。強化に要るコインは先のワールドほど増えるので、それに合わせて伸ばす
        private static readonly float[] RushStageCoinScales = { 0.6f, 0.8f, 1f, 1.2f, 1.4f };

        /// <summary>失敗したとき、勝った戦い1つにつき、クリアのコインのこの割合をもらう（あと少しの挑戦が無駄にならないように）</summary>
        public const float RushFailCoinsPerRoundRatio = 0.15f;

        /// <summary>クリアのコイン ＝ 難易度の基本の量 × ステージの重み（10 枚単位）</summary>
        public static int RushClearCoins(int stageNo, Difficulty difficulty)
        {
            var index = Math.Clamp(stageNo - RushTable.FirstNo, 0, RushStageCoinScales.Length - 1);
            var coins = RushClearCoinsByDifficulty[IndexOf(difficulty)] * RushStageCoinScales[index];
            return (int)MathF.Round(coins / 10f) * 10;
        }

        /// <summary>初クリアの上乗せ。クリアのコインと同じだけ</summary>
        public static int RushFirstClearBonus(int stageNo, Difficulty difficulty) => RushClearCoins(stageNo, difficulty);

        /// <summary>失敗: 勝った戦いの数 × クリアのコインの 15% ＋ 倒した数 × 1</summary>
        public static int RushFailCoins(int stageNo, Difficulty difficulty, int roundsWon, int enemiesDefeated)
        {
            var perRound = (int)Math.Round(RushClearCoins(stageNo, difficulty) * RushFailCoinsPerRoundRatio);
            return Math.Max(0, roundsWon) * perRound + Math.Max(0, enemiesDefeated) * FailCoinsPerEnemy;
        }
    }
}
