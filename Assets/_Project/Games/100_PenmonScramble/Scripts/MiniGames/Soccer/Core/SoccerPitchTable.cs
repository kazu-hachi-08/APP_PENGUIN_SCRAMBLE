using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// ピッチの定義表。今はどのスタジアムも同じ形（フットサルくらいの広さ）。スタジアムごとの仕掛けの地面はフェーズ 6 で足す
    /// </summary>
    public static class SoccerPitchTable
    {
        // 横 28 × 縦 16 マス・ゴールの口 幅 5 × 高さ 1.6 マス（案。計画書「ピッチ」）
        private const float HalfLength = 14f;
        private const float HalfWidth = 8f;
        private const float CornerCut = 1.6f;
        private const float GoalHalfWidth = 2.5f;
        private const float GoalBarHeight = 1.6f;
        private const float GoalDepth = 1.2f;

        // 赤（左の陣地）の並び。蹴る側の先頭はボール（真ん中）のすぐ後ろ、残りは少し後ろの上下。
        // 4体目（協力の4人のときの4対4）はゴールの前の真ん中
        private static readonly Vec2[] KickingSpots = { new Vec2(-0.9f, 0f), new Vec2(-5f, 3.5f), new Vec2(-5f, -3.5f), new Vec2(-9f, 0f) };
        private static readonly Vec2[] WaitingSpots = { new Vec2(-3.5f, 0f), new Vec2(-7f, 3.5f), new Vec2(-7f, -3.5f), new Vec2(-10f, 0f) };

        /// <summary>試しのピッチ（開発用メニュー）と、仕掛けの無いスタジアムの形</summary>
        public static SoccerPitchDef Standard() => new SoccerPitchDef(
            "試しのピッチ",
            HalfLength,
            HalfWidth,
            CornerCut,
            GoalHalfWidth,
            GoalBarHeight,
            GoalDepth,
            KickingSpots,
            WaitingSpots);
    }
}
