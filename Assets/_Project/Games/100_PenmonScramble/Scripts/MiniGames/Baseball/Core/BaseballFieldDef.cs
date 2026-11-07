using System;
using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>打球の結果（穴・止まった場所・フェンス・ファウルラインで決まる）</summary>
    public enum BaseballHitResult
    {
        Single,
        Double,
        Triple,
        HomeRun,
        GroundOut,
        /// <summary>ゲッツーの穴（走者の扱いはフェーズ 3 の BaseballBases。走者がいなければゴロアウトと同じ）</summary>
        DoublePlay,
        FlyOut,
        Foul,
    }

    /// <summary>盤の穴。入ったら Result の結果（アウトの穴の後ろには守備の子が立つ）</summary>
    public sealed class BaseballHole
    {
        public BaseballHole(BaseballHitResult result, Vec2 center, float radius)
        {
            Result = result;
            Center = center;
            Radius = radius;
        }

        public BaseballHitResult Result { get; }
        public Vec2 Center { get; }
        public float Radius { get; }

        public bool IsOut => BaseballResults.IsOut(Result);
    }

    /// <summary>穴に入らずに止まったときの札。ホームからの距離が OuterRadius までならこの結果（内野の土＝ゴロアウトなど）</summary>
    public readonly struct BaseballStopZone
    {
        public BaseballStopZone(float outerRadius, BaseballHitResult result)
        {
            OuterRadius = outerRadius;
            Result = result;
        }

        public float OuterRadius { get; }
        public BaseballHitResult Result { get; }
    }

    /// <summary>
    /// 盤の定義（コードの定義表。Docs/MINIGAME_BASEBALL_PLAN.md「盤」）。扇形の盤で、手前がホーム、奥が外野のフェンス。
    /// 座標はホームが原点、Y が外野の奥（センター）へ、X が右（一塁・ライトの側）。
    /// 方角は「センターから何度」で、負が左（三塁・レフト）・正が右（一塁・ライト）
    /// </summary>
    public sealed class BaseballFieldDef
    {
        public BaseballFieldDef(
            string name,
            float fenceRadius,
            float fenceHeight,
            float foulDegrees,
            IReadOnlyList<BaseballHole> holes,
            IReadOnlyList<BaseballStopZone> stopZones,
            float zoneHalfWidth,
            float catcherY,
            Vec2 mound,
            Vec2 batPivot,
            float baseDistance,
            Vec2 bowlCenter)
        {
            if (stopZones.Count == 0)
            {
                throw new ArgumentException("止まった場所の札が1つも無い", nameof(stopZones));
            }

            Name = name;
            FenceRadius = fenceRadius;
            FenceHeight = fenceHeight;
            FoulDegrees = foulDegrees;
            Holes = holes;
            StopZones = stopZones;
            ZoneHalfWidth = zoneHalfWidth;
            CatcherY = catcherY;
            Mound = mound;
            BatPivot = batPivot;
            BaseDistance = baseDistance;
            BowlCenter = bowlCenter;
        }

        public string Name { get; }

        /// <summary>ホームからフェンスまでの距離（フェンスは円弧）</summary>
        public float FenceRadius { get; }

        /// <summary>浮いた球がこれより高くフェンスを越えたらホームラン。低ければ当たって跳ね返る</summary>
        public float FenceHeight { get; }

        /// <summary>ファウルラインの方角（センターから左右に何度）。外はファウルの溝</summary>
        public float FoulDegrees { get; }

        public IReadOnlyList<BaseballHole> Holes { get; }

        /// <summary>近い順（OuterRadius の小さい順）。最後の札より遠く（フェンス際）で止まったら最後の札</summary>
        public IReadOnlyList<BaseballStopZone> StopZones { get; }

        /// <summary>ストライクのゾーンの幅（片側。ホームの上の、ホームの線を越える所で測る）</summary>
        public float ZoneHalfWidth { get; }

        /// <summary>ホームの線は Y = 0。投げた球はこの線（ホームの後ろ）まで来たらミットに収まる</summary>
        public const float PlateY = 0f;

        public float CatcherY { get; }

        /// <summary>投げる所（マウンド）。球はここからホームへ盤の上を転がってくる</summary>
        public Vec2 Mound { get; }

        /// <summary>バットの軸（打つ子が立つ所。右打ちで、三塁側＝左に立つ）</summary>
        public Vec2 BatPivot { get; }

        /// <summary>ホームから一塁・三塁までの距離（塁の置き場所。見た目と走者の演出に使う）</summary>
        public float BaseDistance { get; }

        /// <summary>すり鉢の底（動いている打球がゆるく引かれる先）</summary>
        public Vec2 BowlCenter { get; }

        /// <summary>方角（センターから何度。負が左）と距離の点</summary>
        public static Vec2 At(float degrees, float distance)
        {
            var radians = degrees * MathF.PI / 180f;
            return new Vec2(MathF.Sin(radians) * distance, MathF.Cos(radians) * distance);
        }

        /// <summary>点の方角（センターから何度。負が左）</summary>
        public static float DegreesOf(Vec2 point) => MathF.Atan2(point.X, point.Y) * 180f / MathF.PI;

        /// <summary>フェアの扇形の中か（ホームより前で、ファウルラインの内側）</summary>
        public bool IsFair(Vec2 point) => point.Y > 0f && MathF.Abs(DegreesOf(point)) <= FoulDegrees;

        /// <summary>塁の位置（1 = 一塁・2 = 二塁・3 = 三塁。ホームは 0）</summary>
        public Vec2 Base(int number)
        {
            return number switch
            {
                0 => Vec2.Zero,
                1 => At(FoulDegrees, BaseDistance),
                2 => new Vec2(0f, BaseDistance * MathF.Sqrt(2f)),
                3 => At(-FoulDegrees, BaseDistance),
                _ => throw new ArgumentOutOfRangeException(nameof(number), number, null),
            };
        }

        /// <summary>止まった所の結果（ホームからの距離で札を引く）</summary>
        public BaseballHitResult StopResult(Vec2 point)
        {
            var distance = point.Length;
            foreach (var zone in StopZones)
            {
                if (distance <= zone.OuterRadius)
                {
                    return zone.Result;
                }
            }

            return StopZones[StopZones.Count - 1].Result;
        }
    }
}
