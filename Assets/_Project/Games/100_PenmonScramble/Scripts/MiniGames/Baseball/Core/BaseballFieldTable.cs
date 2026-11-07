using System;
using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// 盤の定義表。試しの盤と、5球場の盤（Docs/MINIGAME_BASEBALL_PLAN.md フェーズ 6 の表）。
    /// 穴の並びと大きさ・止まった場所の札は、CPU の試合の確認（BaseballMatchCheck）で 1試合 3〜6 点・ホームラン 0〜1 本くらいになるよう決めた。
    /// 球場2〜5は、ふつうの盤の並びをもとに仕掛け（BaseballGimmicks・動く穴）とフェンスを変える
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

        // 外野の並びは「フェンスからどれだけ手前か」で持つ（フェンスの近い球場でも同じ形に並ぶように）
        private const float OutfieldInset = 1.8f;
        private const float FlyHoleInset = 4.8f;
        private const float GapHoleInset = 2.4f;
        private const float TripleHoleInset = 2.8f;

        private const float OutHoleRadius = 0.5f;
        private const float HitHoleRadius = 0.42f;
        private const float FlyHoleRadius = 0.7f;
        private const float TripleHoleDegrees = 32f;

        // 火山の入り江: 近くて高いフェンス（浮いた球は届きやすいが、高く上がらないと越えない）
        private const float VolcanoFenceRadius = 12.6f;
        private const float VolcanoFenceHeight = 1.45f;

        // 火山の入り江: 間欠泉はフライの穴の列の後ろ（穴と重ならない所）
        private const float GeyserDistance = 9.3f;

        // 深海: 水流の帯（内野の土の外、フライの穴の手前を横切る弧）
        private const float CurrentInner = 7.7f;
        private const float CurrentOuter = 8.6f;
        private const float CurrentHalfDegrees = 38f;

        /// <summary>
        /// 試しの盤: 内野にゴロアウト4つ・二塁ベースの上にゲッツー・内野の間に単打2つ、外野の手前にフライアウト5つ（守備の子が横に並ぶ）・
        /// その奥の間に二塁打4つ・左右の隅に三塁打2つ。止まったら内野の土＝ゴロアウト・外野の芝＝単打・フェンス際の帯＝二塁打。
        /// 外野の芝で止まるのは強い当たりだけ（弱い当たりは内野で止まる）で、フライの穴がその手前で多くを捕る
        /// </summary>
        public static BaseballFieldDef Test() => Create("試しの盤", FenceRadius, FenceHeight, Standard(FenceRadius, null), BaseballGimmicks.None);

        /// <summary>球場1「海岸」: 仕掛けの無い、ふつうの盤（試しの盤と同じ並び）</summary>
        public static BaseballFieldDef Beach() => Create("海岸", FenceRadius, FenceHeight, Standard(FenceRadius, null), BaseballGimmicks.None);

        /// <summary>球場2「氷山」: 外野が氷の床（よく転がり、外野の奥・フェンス際まで届いて長打が出やすい）</summary>
        public static BaseballFieldDef Iceberg()
        {
            var ice = new[] { new BaseballArcBand(InfieldRadius, FenceRadius, FoulDegrees) };
            return Create("氷山", FenceRadius, FenceHeight, Standard(FenceRadius, null), new BaseballGimmicks(ice: ice));
        }

        /// <summary>球場3「深海」: 内野と外野の間を横切る水流の帯（転がる打球を左右へ流す。向きは時間で入れ替わる）</summary>
        public static BaseballFieldDef DeepSea()
        {
            var band = new BaseballArcBand(CurrentInner, CurrentOuter, CurrentHalfDegrees);
            var cycle = new MiniGameCycle(BaseballBalance.CurrentFlipSeconds, BaseballBalance.CurrentFlipSeconds);
            var currents = new[] { new BaseballCurrent(band, BaseballBalance.CurrentAccel, BaseballBalance.CurrentMaxSpeed, cycle) };
            return Create("深海", FenceRadius, FenceHeight, Standard(FenceRadius, null), new BaseballGimmicks(currents: currents));
        }

        /// <summary>
        /// 球場4「嵐の崖」: 外野のフライの穴の列が左右に動く。
        /// 風が浮いた打球を左右へ流す（吹く・止むをくり返し、吹くたびに向きが替わる）
        /// </summary>
        public static BaseballFieldDef StormCliff()
        {
            // フライの穴の列がそろって動く（隣どうしが逆に動くと穴が重なるため）。後ろの二塁打の穴は動かないので、列が動くと隠れたり開いたりする
            BaseballHoleSway Sway() => new BaseballHoleSway(BaseballBalance.SwayDegrees, BaseballBalance.SwaySeconds, 0f);

            var gust = new MiniGameCycle(BaseballBalance.WindBlowSeconds + BaseballBalance.WindCalmSeconds, BaseballBalance.WindBlowSeconds);
            var wind = new BaseballWind(BaseballBalance.WindAccel, gust);
            return Create("嵐の崖", FenceRadius, FenceHeight, Standard(FenceRadius, Sway), new BaseballGimmicks(wind: wind));
        }

        /// <summary>
        /// 球場5「火山の入り江」: 近くて高いフェンス。フライの穴の列の後ろに間欠泉が3つ（噴いている間に上を転がった打球を跳ね上げる。
        /// 跳ねている間は穴に落ちないので、二塁打の穴を越えてフェンスまで行くことも、跳ねた先でフェンスに当たって戻ることもある）
        /// </summary>
        public static BaseballFieldDef VolcanoCove()
        {
            BaseballGeyser Geyser(float degrees, float distance, float offset)
            {
                var cycle = new MiniGameCycle(BaseballBalance.GeyserPeriod, BaseballBalance.GeyserActive, offset);
                return new BaseballGeyser(BaseballFieldDef.At(degrees, distance), BaseballBalance.GeyserRadius, cycle);
            }

            var geysers = new[]
            {
                Geyser(-15f, GeyserDistance, 0f),
                Geyser(0f, GeyserDistance, BaseballBalance.GeyserPeriod / 3f),
                Geyser(15f, GeyserDistance, BaseballBalance.GeyserPeriod * 2f / 3f),
            };
            return Create("火山の入り江", VolcanoFenceRadius, VolcanoFenceHeight, Standard(VolcanoFenceRadius, null), new BaseballGimmicks(geysers: geysers));
        }

        /// <summary>ふつうの盤の穴の並び。sway を渡すと、外野のフライの穴を動く穴にする</summary>
        private static BaseballHole[] Standard(float fence, Func<BaseballHoleSway> sway)
        {
            var fly = fence - FlyHoleInset;
            var gap = fence - GapHoleInset;
            var triple = fence - TripleHoleInset;
            BaseballHoleSway Moving() => sway?.Invoke();
            return new[]
            {
                Hole(BaseballHitResult.GroundOut, -32f, 4.6f, OutHoleRadius),
                Hole(BaseballHitResult.GroundOut, -13f, 6.0f, OutHoleRadius),
                Hole(BaseballHitResult.GroundOut, 13f, 6.0f, OutHoleRadius),
                Hole(BaseballHitResult.GroundOut, 32f, 4.6f, OutHoleRadius),
                // ゲッツーの穴は二塁ベースの上（二塁で封殺して一塁へ。見た目も白いベースの真ん中が穴）
                Hole(BaseballHitResult.DoublePlay, 0f, BaseDistance * MathF.Sqrt(2f), OutHoleRadius),
                Hole(BaseballHitResult.Single, -23f, 5.4f, HitHoleRadius),
                Hole(BaseballHitResult.Single, 23f, 5.4f, HitHoleRadius),

                Hole(BaseballHitResult.FlyOut, -30f, fly, FlyHoleRadius, Moving()),
                Hole(BaseballHitResult.FlyOut, -15f, fly, FlyHoleRadius, Moving()),
                Hole(BaseballHitResult.FlyOut, 0f, fly, FlyHoleRadius, Moving()),
                Hole(BaseballHitResult.FlyOut, 15f, fly, FlyHoleRadius, Moving()),
                Hole(BaseballHitResult.FlyOut, 30f, fly, FlyHoleRadius, Moving()),
                Hole(BaseballHitResult.Double, -22f, gap, HitHoleRadius),
                Hole(BaseballHitResult.Double, -7f, gap, HitHoleRadius),
                Hole(BaseballHitResult.Double, 7f, gap, HitHoleRadius),
                Hole(BaseballHitResult.Double, 22f, gap, HitHoleRadius),
                Hole(BaseballHitResult.Triple, -TripleHoleDegrees, triple, HitHoleRadius),
                Hole(BaseballHitResult.Triple, TripleHoleDegrees, triple, HitHoleRadius),
            };
        }

        private static BaseballFieldDef Create(string name, float fence, float fenceHeight, IReadOnlyList<BaseballHole> holes, BaseballGimmicks gimmicks)
        {
            var stopZones = new[]
            {
                new BaseballStopZone(InfieldRadius, BaseballHitResult.GroundOut),
                new BaseballStopZone(fence - OutfieldInset, BaseballHitResult.Single),
                new BaseballStopZone(fence, BaseballHitResult.Double),
            };

            return new BaseballFieldDef(
                name, fence, fenceHeight, FoulDegrees, holes, stopZones,
                ZoneHalfWidth, CatcherY, Mound, BatPivot, BaseDistance, BowlCenter, gimmicks);
        }

        private static BaseballHole Hole(BaseballHitResult result, float degrees, float distance, float radius, BaseballHoleSway sway = null)
        {
            return new BaseballHole(result, BaseballFieldDef.At(degrees, distance), radius, sway);
        }
    }
}
