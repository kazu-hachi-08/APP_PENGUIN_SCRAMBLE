using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Golf.Core
{
    /// <summary>ガイド線の当たり先</summary>
    public enum GolfGuideStop
    {
        /// <summary>何にも当たらず、線の長さの上限まで</summary>
        None,
        /// <summary>壁で跳ね返る（跳ね返った後の向きを短く見せる）</summary>
        Wall,
        /// <summary>ほかの人の球に当たる</summary>
        Ball,
    }

    /// <summary>ガイド線: 球 → 最初に当たる所（壁なら跳ね返った後の短い線つき）</summary>
    public readonly struct GolfGuideLine
    {
        public readonly Vec2 Start;
        public readonly Vec2 End;
        public readonly GolfGuideStop Stop;
        public readonly Vec2 BounceEnd;

        public GolfGuideLine(Vec2 start, Vec2 end, GolfGuideStop stop, Vec2 bounceEnd)
        {
            Start = start;
            End = end;
            Stop = stop;
            BounceEnd = bounceEnd;
        }
    }

    /// <summary>
    /// ガイド線の計算。最初の跳ね返りまでを見せる（バンクショットは狙えるが、強さと坂は自分で読む）。
    /// 表示とオンラインのゲストで同じ線になるよう Core に置く。坂・摩擦は見ない（まっすぐの線）
    /// </summary>
    public static class GolfGuide
    {
        public static GolfGuideLine Compute(GolfHoleDef hole, IReadOnlyList<GolfBall> balls, int player, float radians)
        {
            var start = balls[player].Position;
            var direction = GolfHoleWorld.Direction(radians);
            var previous = 0f;
            for (var s = GolfBalance.GuideMarchStep; s <= GolfBalance.GuideMaxLength; s += GolfBalance.GuideMarchStep)
            {
                if (Probe(hole, balls, player, start + direction * s, direction, out _) == GolfGuideStop.None)
                {
                    previous = s;
                    continue;
                }

                return Refine(hole, balls, player, start, direction, previous, s);
            }

            var end = start + direction * GolfBalance.GuideMaxLength;
            return new GolfGuideLine(start, end, GolfGuideStop.None, end);
        }

        /// <summary>当たらない距離と当たる距離の間を挟み込んで、当たり始める所を詰める</summary>
        private static GolfGuideLine Refine(GolfHoleDef hole, IReadOnlyList<GolfBall> balls, int player, Vec2 start, Vec2 direction, float clear, float blocked)
        {
            for (var i = 0; i < GolfBalance.GuideRefineIterations; i++)
            {
                var mid = (clear + blocked) * 0.5f;
                if (Probe(hole, balls, player, start + direction * mid, direction, out _) == GolfGuideStop.None)
                {
                    clear = mid;
                }
                else
                {
                    blocked = mid;
                }
            }

            var end = start + direction * blocked;
            var stop = Probe(hole, balls, player, end, direction, out var normal);
            if (stop != GolfGuideStop.Wall)
            {
                return new GolfGuideLine(start, end, stop, end);
            }

            var reflected = direction - normal * (2f * Vec2.Dot(direction, normal));
            return new GolfGuideLine(start, end, stop, end + reflected * GolfBalance.GuideBounceLength);
        }

        /// <summary>
        /// この位置に球を置いたら、進む向きの壁・球に当たるか。進む向きと反対側の物（いま触れている後ろの壁など）は数えない
        /// </summary>
        private static GolfGuideStop Probe(GolfHoleDef hole, IReadOnlyList<GolfBall> balls, int player, Vec2 point, Vec2 direction, out Vec2 normal)
        {
            foreach (var wall in hole.Walls)
            {
                var offset = point - GolfGeometry.ClosestOnSegment(wall.A, wall.B, point);
                if (offset.Length < GolfBalance.BallRadius && Vec2.Dot(direction, offset) < 0f)
                {
                    normal = offset.Normalized;
                    return GolfGuideStop.Wall;
                }
            }

            foreach (var ball in balls)
            {
                if (ball.Player == player || !ball.IsInPlay)
                {
                    continue;
                }

                var toBall = ball.Position - point;
                if (toBall.Length < GolfBalance.BallRadius * 2f && Vec2.Dot(direction, toBall) > 0f)
                {
                    normal = -toBall.Normalized;
                    return GolfGuideStop.Ball;
                }
            }

            normal = Vec2.Zero;
            return GolfGuideStop.None;
        }
    }
}
