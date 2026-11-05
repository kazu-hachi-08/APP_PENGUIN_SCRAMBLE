using System;
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

    /// <summary>
    /// ガイド線: 球 → 最初に当たる所（壁なら跳ね返った後の短い線つき）。
    /// Path は Start から End までの道筋の点（まっすぐなら2点。風の特殊ショットでは曲がった道筋）
    /// </summary>
    public readonly struct GolfGuideLine
    {
        public readonly Vec2 Start;
        public readonly Vec2 End;
        public readonly GolfGuideStop Stop;
        public readonly Vec2 BounceEnd;
        public readonly IReadOnlyList<Vec2> Path;

        public GolfGuideLine(Vec2 start, Vec2 end, GolfGuideStop stop, Vec2 bounceEnd, IReadOnlyList<Vec2> path = null)
        {
            Start = start;
            End = end;
            Stop = stop;
            BounceEnd = bounceEnd;
            Path = path ?? new[] { start, end };
        }
    }

    /// <summary>
    /// ガイド線の計算。最初の跳ね返りまでを見せる（バンクショットは狙えるが、強さと坂は自分で読む）。
    /// 表示とオンラインのゲストで同じ線になるよう Core に置く。坂・摩擦は見ない。
    /// 風の特殊ショットの曲がり方は「進んだ距離あたりの角度」なので、打つ強さによらず同じ道筋になり、ガイド線で見せられる
    /// </summary>
    public static class GolfGuide
    {
        // 曲がった道筋の点を置く間（m）
        private const float PathPointSpacing = 0.25f;

        public static GolfGuideLine Compute(GolfHoleDef hole, IReadOnlyList<GolfBall> balls, int player, float radians)
        {
            return Compute(hole, balls, player, radians, GolfSpecial.None);
        }

        /// <param name="special">打つ予定の特殊ショット。風なら曲がった線になる（ほかの種類はまっすぐ）</param>
        public static GolfGuideLine Compute(GolfHoleDef hole, IReadOnlyList<GolfBall> balls, int player, float radians, GolfSpecial special)
        {
            var curvature = special.Kind == GolfSpecialKind.WindCurve ? -special.Curve * GolfSpecialBalance.Curvature(special.Stars) : 0f;
            var state = new March(balls[player].Position, GolfHoleWorld.Direction(radians), 0f);
            var path = new List<Vec2> { state.Position };
            var lastPoint = 0f;
            // 刻みの数で回す（距離を足し続けると、丸めの誤差で上限を1刻み越えることがあるため）
            var steps = (int)MathF.Round(GolfBalance.GuideMaxLength / GolfBalance.GuideMarchStep);
            for (var i = 0; i < steps; i++)
            {
                var next = state.Advance(GolfBalance.GuideMarchStep, curvature);
                if (Probe(hole, balls, player, next.Position, next.Direction, out _) != GolfGuideStop.None)
                {
                    return Refine(hole, balls, player, state, curvature, path);
                }

                state = next;
                if (curvature != 0f && state.Distance - lastPoint >= PathPointSpacing)
                {
                    path.Add(state.Position);
                    lastPoint = state.Distance;
                }
            }

            path.Add(state.Position);
            return new GolfGuideLine(path[0], state.Position, GolfGuideStop.None, state.Position, path);
        }

        /// <summary>当たらない所から1刻みの間を挟み込んで、当たり始める所を詰める</summary>
        private static GolfGuideLine Refine(GolfHoleDef hole, IReadOnlyList<GolfBall> balls, int player, March clear, float curvature, List<Vec2> path)
        {
            float low = 0f, high = GolfBalance.GuideMarchStep;
            for (var i = 0; i < GolfBalance.GuideRefineIterations; i++)
            {
                var mid = (low + high) * 0.5f;
                var probe = clear.Advance(mid, curvature);
                if (Probe(hole, balls, player, probe.Position, probe.Direction, out _) == GolfGuideStop.None)
                {
                    low = mid;
                }
                else
                {
                    high = mid;
                }
            }

            var hit = clear.Advance(high, curvature);
            path.Add(hit.Position);
            var stop = Probe(hole, balls, player, hit.Position, hit.Direction, out var normal);
            if (stop != GolfGuideStop.Wall)
            {
                return new GolfGuideLine(path[0], hit.Position, stop, hit.Position, path);
            }

            var reflected = hit.Direction - normal * (2f * Vec2.Dot(hit.Direction, normal));
            return new GolfGuideLine(path[0], hit.Position, stop, hit.Position + reflected * GolfBalance.GuideBounceLength, path);
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

        /// <summary>線をたどる途中の位置・向き・たどった距離。曲がるのは風の曲がる距離の間だけ（球の計算と同じ）</summary>
        private readonly struct March
        {
            public readonly Vec2 Position;
            public readonly Vec2 Direction;
            public readonly float Distance;

            public March(Vec2 position, Vec2 direction, float distance)
            {
                Position = position;
                Direction = direction;
                Distance = distance;
            }

            public March Advance(float step, float curvature)
            {
                var curved = Math.Clamp(GolfSpecialBalance.CurveLength - Distance, 0f, step);
                var direction = curvature != 0f ? Direction.Rotated(curvature * curved) : Direction;
                return new March(Position + direction * step, direction, Distance + step);
            }
        }
    }
}
