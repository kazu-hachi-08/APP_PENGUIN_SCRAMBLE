using System;

namespace PenmonScramble.Core
{
    /// <summary>育成（強化・上限解放）の調整値。数値はフェーズ 7 のバランス調整で決め直す（案）</summary>
    public static partial class Balance
    {
        /// <summary>1回の強化で選べる素材の数</summary>
        public const int MaxEnhanceMaterials = 10;

        /// <summary>上限解放の最大回数（上限は 引いたときのパワー × 2.5 まで）</summary>
        public const int MaxLimitBreak = 4;

        // 素材1体で上がるパワー: ★の基本値 ＋ 素材のパワー × この割合。添字は★-1
        private static readonly int[] EnhanceStarGains = { 8, 20, 50 };
        private const float EnhanceMaterialPowerRate = 0.10f;

        // 上限 ＝ 引いたときのパワー × (Base ＋ Step × 上限解放の回数)
        private const float PowerCapBaseScale = 1.5f;
        private const float PowerCapStepScale = 0.25f;

        // 素材1体ごとのコイン ＝ 強化する子の今のパワー × この割合（強い子ほど育てるのに要る）
        private const float EnhanceCoinRate = 0.5f;

        public static int EnhanceGain(int materialStars, int materialPower)
        {
            return EnhanceStarGains[StarIndex(materialStars)] + (int)MathF.Round(materialPower * EnhanceMaterialPowerRate);
        }

        public static int PowerCap(int basePower, int limitBreak)
        {
            var scale = PowerCapBaseScale + PowerCapStepScale * Math.Clamp(limitBreak, 0, MaxLimitBreak);
            return (int)MathF.Round(basePower * scale);
        }

        public static int EnhanceCoinsPerMaterial(int targetPower)
        {
            return Math.Max(1, (int)MathF.Round(targetPower * EnhanceCoinRate));
        }
    }
}
