using System;
using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.MiniGames.Core
{
    /// <summary>
    /// ミニゲームの地面の形の計算（多角形の中か・線分の一番近い点・多角形の三角形への分け方）。
    /// どのミニゲームでも使う汎用の物だけを置く
    /// </summary>
    public static class MiniGameGeometry
    {
        public static float Cross(Vec2 a, Vec2 b) => a.X * b.Y - a.Y * b.X;

        /// <summary>多角形の中にあるか（偶奇の判定。へこんだ形でもよい）</summary>
        public static bool Contains(IReadOnlyList<Vec2> polygon, Vec2 point)
        {
            var inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                if ((a.Y > point.Y) != (b.Y > point.Y)
                    && point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        public static Vec2 ClosestOnSegment(Vec2 a, Vec2 b, Vec2 point)
        {
            var ab = b - a;
            var lengthSquared = ab.LengthSquared;
            if (lengthSquared <= 0f)
            {
                return a;
            }

            var t = Math.Clamp(Vec2.Dot(point - a, ab) / lengthSquared, 0f, 1f);
            return a + ab * t;
        }

        /// <summary>左下 (x0, y0)・右上 (x1, y1) の長方形（左回り）</summary>
        public static Vec2[] Rect(float x0, float y0, float x1, float y1)
        {
            return new[] { new Vec2(x0, y0), new Vec2(x1, y0), new Vec2(x1, y1), new Vec2(x0, y1) };
        }

        /// <summary>点の集まりを囲む凸な多角形（左回り。並んだ点は省く）</summary>
        public static Vec2[] ConvexHull(IReadOnlyList<Vec2> points)
        {
            var sorted = new List<Vec2>(points);
            sorted.Sort((a, b) => a.X != b.X ? a.X.CompareTo(b.X) : a.Y.CompareTo(b.Y));
            if (sorted.Count < 3)
            {
                return sorted.ToArray();
            }

            var hull = new List<Vec2>();
            // 下側を左から右へ、上側を右から左へ（右へ曲がる点を取り除く）
            for (var pass = 0; pass < 2; pass++)
            {
                var start = hull.Count;
                for (var k = 0; k < sorted.Count; k++)
                {
                    var p = pass == 0 ? sorted[k] : sorted[sorted.Count - 1 - k];
                    while (hull.Count >= start + 2 && Cross(hull[hull.Count - 1] - hull[hull.Count - 2], p - hull[hull.Count - 2]) <= 0f)
                    {
                        hull.RemoveAt(hull.Count - 1);
                    }

                    hull.Add(p);
                }

                hull.RemoveAt(hull.Count - 1);
            }

            return hull.ToArray();
        }

        /// <summary>符号つきの面積。左回りなら正</summary>
        public static float SignedArea(IReadOnlyList<Vec2> polygon)
        {
            var sum = 0f;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                sum += Cross(polygon[j], polygon[i]);
            }

            return sum * 0.5f;
        }

        /// <summary>
        /// 多角形を三角形に分ける（耳を切り取るやり方）。へこんだ形も分けられる（穴は無し）。
        /// 返す番号は3つずつ1枚で、どれも左回り（上から見て表）
        /// </summary>
        public static List<int> Triangulate(IReadOnlyList<Vec2> polygon)
        {
            var result = new List<int>();
            var remaining = new List<int>();
            var counterClockwise = SignedArea(polygon) > 0f;
            for (var i = 0; i < polygon.Count; i++)
            {
                remaining.Add(counterClockwise ? i : polygon.Count - 1 - i);
            }

            // 耳が見つからない（形が壊れている）ときに止まらなくならないよう、回す回数に上限を置く
            var guard = polygon.Count * polygon.Count;
            while (remaining.Count > 3 && guard-- > 0)
            {
                for (var i = 0; i < remaining.Count; i++)
                {
                    var prev = remaining[(i + remaining.Count - 1) % remaining.Count];
                    var cur = remaining[i];
                    var next = remaining[(i + 1) % remaining.Count];
                    if (!IsEar(polygon, remaining, prev, cur, next))
                    {
                        continue;
                    }

                    result.Add(prev);
                    result.Add(cur);
                    result.Add(next);
                    remaining.RemoveAt(i);
                    break;
                }
            }

            if (remaining.Count == 3)
            {
                result.AddRange(remaining);
            }

            return result;
        }

        private static bool IsEar(IReadOnlyList<Vec2> polygon, List<int> remaining, int prev, int cur, int next)
        {
            var a = polygon[prev];
            var b = polygon[cur];
            var c = polygon[next];
            if (Cross(b - a, c - b) <= 0f)
            {
                return false;
            }

            foreach (var index in remaining)
            {
                if (index != prev && index != cur && index != next && InTriangle(polygon[index], a, b, c))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool InTriangle(Vec2 p, Vec2 a, Vec2 b, Vec2 c)
        {
            return Cross(b - a, p - a) >= 0f && Cross(c - b, p - b) >= 0f && Cross(a - c, p - c) >= 0f;
        }
    }
}
