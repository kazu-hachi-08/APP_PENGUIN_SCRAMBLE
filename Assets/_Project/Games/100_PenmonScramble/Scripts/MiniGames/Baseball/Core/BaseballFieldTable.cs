using System;
using PenmonScramble.Core;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// 盤の定義表。試しの盤と、球場1「海岸」のふつうの盤（同じ並び。球場2〜5の盤はフェーズ 6 で足す）。
    /// 穴の並びと大きさ・止まった場所の札は、CPU の試合の確認（BaseballMatchCheck）で 1試合 3〜6 点・ホームラン 0〜1 本くらいになるよう決めた
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
        private const float OutfieldRadius = 12.2f;

        private const float OutHoleRadius = 0.5f;
        private const float HitHoleRadius = 0.42f;
        private const float FlyHoleRadius = 0.7f;
        private const float FlyHoleDistance = 9.2f;
        private const float GapHoleDistance = 11.6f;
        private const float TripleHoleDegrees = 32f;
        private const float TripleHoleDistance = 11.2f;

        /// <summary>
        /// 試しの盤: 内野にゴロアウト4つ・二塁ベースの上にゲッツー・内野の間に単打2つ、外野の手前にフライアウト5つ（守備の子が横に並ぶ）・
        /// その奥の間に二塁打4つ・左右の隅に三塁打2つ。止まったら内野の土＝ゴロアウト・外野の芝＝単打・フェンス際の帯＝二塁打。
        /// 外野の芝で止まるのは強い当たりだけ（弱い当たりは内野で止まる）で、フライの穴がその手前で多くを捕る
        /// </summary>
        public static BaseballFieldDef Test() => Standard("試しの盤");

        /// <summary>球場1「海岸」: 仕掛けの無い、ふつうの盤（試しの盤と同じ並び）</summary>
        public static BaseballFieldDef Beach() => Standard("海岸");

        private static BaseballFieldDef Standard(string name)
        {
            var holes = new[]
            {
                Hole(BaseballHitResult.GroundOut, -32f, 4.6f, OutHoleRadius),
                Hole(BaseballHitResult.GroundOut, -13f, 6.0f, OutHoleRadius),
                Hole(BaseballHitResult.GroundOut, 13f, 6.0f, OutHoleRadius),
                Hole(BaseballHitResult.GroundOut, 32f, 4.6f, OutHoleRadius),
                // ゲッツーの穴は二塁ベースの上（二塁で封殺して一塁へ。見た目も白いベースの真ん中が穴）
                Hole(BaseballHitResult.DoublePlay, 0f, BaseDistance * MathF.Sqrt(2f), OutHoleRadius),
                Hole(BaseballHitResult.Single, -23f, 5.4f, HitHoleRadius),
                Hole(BaseballHitResult.Single, 23f, 5.4f, HitHoleRadius),

                Hole(BaseballHitResult.FlyOut, -30f, FlyHoleDistance, FlyHoleRadius),
                Hole(BaseballHitResult.FlyOut, -15f, FlyHoleDistance, FlyHoleRadius),
                Hole(BaseballHitResult.FlyOut, 0f, FlyHoleDistance, FlyHoleRadius),
                Hole(BaseballHitResult.FlyOut, 15f, FlyHoleDistance, FlyHoleRadius),
                Hole(BaseballHitResult.FlyOut, 30f, FlyHoleDistance, FlyHoleRadius),
                Hole(BaseballHitResult.Double, -22f, GapHoleDistance, HitHoleRadius),
                Hole(BaseballHitResult.Double, -7f, GapHoleDistance, HitHoleRadius),
                Hole(BaseballHitResult.Double, 7f, GapHoleDistance, HitHoleRadius),
                Hole(BaseballHitResult.Double, 22f, GapHoleDistance, HitHoleRadius),
                Hole(BaseballHitResult.Triple, -TripleHoleDegrees, TripleHoleDistance, HitHoleRadius),
                Hole(BaseballHitResult.Triple, TripleHoleDegrees, TripleHoleDistance, HitHoleRadius),
            };

            var stopZones = new[]
            {
                new BaseballStopZone(InfieldRadius, BaseballHitResult.GroundOut),
                new BaseballStopZone(OutfieldRadius, BaseballHitResult.Single),
                new BaseballStopZone(FenceRadius, BaseballHitResult.Double),
            };

            return new BaseballFieldDef(
                name, FenceRadius, FenceHeight, FoulDegrees, holes, stopZones,
                ZoneHalfWidth, CatcherY, Mound, BatPivot, BaseDistance, BowlCenter);
        }

        private static BaseballHole Hole(BaseballHitResult result, float degrees, float distance, float radius)
        {
            return new BaseballHole(result, BaseballFieldDef.At(degrees, distance), radius);
        }
    }
}
