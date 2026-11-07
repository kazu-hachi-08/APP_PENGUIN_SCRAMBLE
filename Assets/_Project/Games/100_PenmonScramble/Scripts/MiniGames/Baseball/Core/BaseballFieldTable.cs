using PenmonScramble.Core;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// 盤の定義表。いまは試しの盤だけ（球場ごとの盤はフェーズ 4・6 で足す）。
    /// 穴の並びと大きさ・止まった場所の札はフェーズ 3 の試合の確認で決め直す（案）
    /// </summary>
    public static class BaseballFieldTable
    {
        private const float FenceRadius = 14f;
        private const float FenceHeight = 1f;
        private const float FoulDegrees = 45f;
        private const float BaseDistance = 4.6f;
        private const float ZoneHalfWidth = 0.55f;
        private const float CatcherY = -0.8f;
        private static readonly Vec2 Mound = new Vec2(0f, 5.5f);
        // 右打ち: ホームの左に立ち、芯がホームの真ん中の上を通るように置く（軸から芯まで BaseballBalance.SweetSpot）
        private static readonly Vec2 BatPivot = new Vec2(-1.15f, 0f);
        private static readonly Vec2 BowlCenter = new Vec2(0f, 7f);

        private const float InfieldRadius = 7.5f;
        private const float OutfieldRadius = 12.6f;

        private const float OutHoleRadius = 0.5f;
        private const float HitHoleRadius = 0.42f;

        /// <summary>
        /// 試しの盤: 内野にゴロアウト4つ・二塁の前にゲッツー・内野の間に単打2つ、外野の手前に単打2つ・フライアウト3つ・
        /// 外野の間に二塁打2つ・フェンス際の隅に三塁打2つ。止まったら内野の土＝ゴロアウト・外野の芝＝単打・フェンス際の帯＝二塁打
        /// </summary>
        public static BaseballFieldDef Test()
        {
            var holes = new[]
            {
                Hole(BaseballHitResult.GroundOut, -32f, 4.6f, OutHoleRadius),
                Hole(BaseballHitResult.GroundOut, -13f, 6.0f, OutHoleRadius),
                Hole(BaseballHitResult.GroundOut, 13f, 6.0f, OutHoleRadius),
                Hole(BaseballHitResult.GroundOut, 32f, 4.6f, OutHoleRadius),
                Hole(BaseballHitResult.DoublePlay, 0f, 6.7f, OutHoleRadius),
                Hole(BaseballHitResult.Single, -23f, 5.4f, HitHoleRadius),
                Hole(BaseballHitResult.Single, 23f, 5.4f, HitHoleRadius),

                Hole(BaseballHitResult.Single, -14f, 9f, HitHoleRadius),
                Hole(BaseballHitResult.Single, 14f, 9f, HitHoleRadius),
                Hole(BaseballHitResult.FlyOut, -29f, 10.5f, OutHoleRadius),
                Hole(BaseballHitResult.FlyOut, 0f, 11f, OutHoleRadius),
                Hole(BaseballHitResult.FlyOut, 29f, 10.5f, OutHoleRadius),
                Hole(BaseballHitResult.Double, -14f, 12f, HitHoleRadius),
                Hole(BaseballHitResult.Double, 14f, 12f, HitHoleRadius),
                Hole(BaseballHitResult.Triple, -39f, 12.9f, HitHoleRadius),
                Hole(BaseballHitResult.Triple, 39f, 12.9f, HitHoleRadius),
            };

            var stopZones = new[]
            {
                new BaseballStopZone(InfieldRadius, BaseballHitResult.GroundOut),
                new BaseballStopZone(OutfieldRadius, BaseballHitResult.Single),
                new BaseballStopZone(FenceRadius, BaseballHitResult.Double),
            };

            return new BaseballFieldDef(
                "試しの盤", FenceRadius, FenceHeight, FoulDegrees, holes, stopZones,
                ZoneHalfWidth, CatcherY, Mound, BatPivot, BaseDistance, BowlCenter);
        }

        private static BaseballHole Hole(BaseballHitResult result, float degrees, float distance, float radius)
        {
            return new BaseballHole(result, BaseballFieldDef.At(degrees, distance), radius);
        }
    }
}
