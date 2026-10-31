using System;

namespace PenmonScramble.Core
{
    /// <summary>
    /// エリア1つの形。Min〜Max の四角か、その四角にぴったり収まる楕円（大ボスの闘技場）。
    /// margin はユニットの半径など「縁からこれだけ内側」の幅。楕円は軸の半分から margin を引いた楕円で近づける
    /// （正確な内側の曲線は楕円にならず計算が重いが、柵の手前で止まる見た目は変わらないため）
    /// </summary>
    public readonly struct AreaShape : IEquatable<AreaShape>
    {
        // 縁の近さを測る楕円の軸が 0 にならないための下限（大きなユニットが狭い場に置かれても割り算で壊れないように）
        private const float MinAxis = 0.01f;
        // 楕円の縁で一番近い点を探す繰り返しの回数。3〜4回で見た目に分からないほど近くなる
        private const int NearestIterations = 4;

        public AreaShape(Vec2 min, Vec2 max, bool isEllipse)
        {
            Min = min;
            Max = max;
            IsEllipse = isEllipse;
        }

        public Vec2 Min { get; }
        public Vec2 Max { get; }
        public bool IsEllipse { get; }

        public Vec2 Center => (Min + Max) * 0.5f;
        public float Width => Max.X - Min.X;
        public float Height => Max.Y - Min.Y;

        /// <summary>広さ（マス²）。広い場ほど大ボスの攻撃の数を増やすのに使う</summary>
        public float Size => IsEllipse ? MathF.PI * Width * Height * 0.25f : Width * Height;

        public static AreaShape Rect(Vec2 min, Vec2 max) => new AreaShape(min, max, false);
        public static AreaShape Ellipse(Vec2 min, Vec2 max) => new AreaShape(min, max, true);

        /// <summary>点が、縁から margin 以上内側にあるか</summary>
        public bool Contains(Vec2 point, float margin)
        {
            if (!IsEllipse)
            {
                return point.X >= Min.X + margin && point.X <= Max.X - margin && point.Y >= Min.Y + margin && point.Y <= Max.Y - margin;
            }

            var (a, b) = InnerAxes(margin);
            var d = point - Center;
            return (d.X / a) * (d.X / a) + (d.Y / b) * (d.Y / b) <= 1f;
        }

        /// <summary>縁から margin 内側の範囲で、point に一番近い点。中にあればそのまま</summary>
        public Vec2 ClosestInside(Vec2 point, float margin)
        {
            if (Contains(point, margin))
            {
                return point;
            }

            if (!IsEllipse)
            {
                return new Vec2(Math.Clamp(point.X, Min.X + margin, Max.X - margin), Math.Clamp(point.Y, Min.Y + margin, Max.Y - margin));
            }

            var (a, b) = InnerAxes(margin);
            return Center + NearestOnEllipse(point - Center, a, b);
        }

        /// <summary>
        /// 外へ出た点を内側へ戻し、外へ向かって飛んでいたら縁の面に対して跳ね返す（面に沿う速さはそのまま、面へ向かう速さは restitution 倍で逆向き）
        /// </summary>
        public (Vec2 position, Vec2 velocity) Confine(Vec2 position, Vec2 velocity, float margin, float restitution)
        {
            if (!IsEllipse)
            {
                var (x, vx) = BounceAxis(position.X, velocity.X, Min.X + margin, Max.X - margin, restitution);
                var (y, vy) = BounceAxis(position.Y, velocity.Y, Min.Y + margin, Max.Y - margin, restitution);
                return (new Vec2(x, y), new Vec2(vx, vy));
            }

            if (Contains(position, margin))
            {
                return (position, velocity);
            }

            var inside = ClosestInside(position, margin);
            var normal = OutwardNormal(inside, margin);
            var outward = Vec2.Dot(velocity, normal);
            return (inside, outward > 0f ? velocity - normal * (outward * (1f + restitution)) : velocity);
        }

        /// <summary>origin から direction（長さ1）へまっすぐ進んで、縁から margin 内側の線に当たるまでの距離。外にいれば 0</summary>
        public float DistanceToWall(Vec2 origin, Vec2 direction, float margin)
        {
            if (!IsEllipse)
            {
                return MathF.Min(SlabDistance(origin.X, direction.X, Min.X + margin, Max.X - margin), SlabDistance(origin.Y, direction.Y, Min.Y + margin, Max.Y - margin));
            }

            // (o + t·d) を楕円の式に入れた t の2次式の、正の解
            var (a, b) = InnerAxes(margin);
            var o = origin - Center;
            var qa = (direction.X / a) * (direction.X / a) + (direction.Y / b) * (direction.Y / b);
            var qb = 2f * (o.X * direction.X / (a * a) + o.Y * direction.Y / (b * b));
            var qc = (o.X / a) * (o.X / a) + (o.Y / b) * (o.Y / b) - 1f;
            var discriminant = qb * qb - 4f * qa * qc;
            if (qa <= 0f || qc > 0f || discriminant < 0f)
            {
                return 0f;
            }

            return MathF.Max(0f, (-qb + MathF.Sqrt(discriminant)) / (2f * qa));
        }

        /// <summary>高さ y の横の線が、縁から margin 内側の範囲にかかる左端と右端</summary>
        public (float min, float max) SpanX(float y, float margin)
        {
            if (!IsEllipse)
            {
                return (Min.X + margin, Max.X - margin);
            }

            var (a, b) = InnerAxes(margin);
            var dy = (y - Center.Y) / b;
            var half = a * MathF.Sqrt(MathF.Max(0f, 1f - dy * dy));
            return (Center.X - half, Center.X + half);
        }

        /// <summary>横の位置 x で、縁から margin 内側に入る高さの半分（中心の高さから上下へ）</summary>
        public float HalfHeightAt(float x, float margin)
        {
            if (!IsEllipse)
            {
                return MathF.Max(0f, Height * 0.5f - margin);
            }

            var (a, b) = InnerAxes(margin);
            var dx = (x - Center.X) / a;
            return b * MathF.Sqrt(MathF.Max(0f, 1f - dx * dx));
        }

        /// <summary>縁から margin 内側の、どこでも同じ確からしさの点</summary>
        public Vec2 RandomPoint(Random random, float margin)
        {
            if (!IsEllipse)
            {
                return RandomPointRightOf(random, Min.X + margin, margin);
            }

            // 半径に平方根をかけないと、真ん中に寄ってしまう
            var (a, b) = InnerAxes(margin);
            var angle = (float)random.NextDouble() * MathF.PI * 2f;
            var radius = MathF.Sqrt((float)random.NextDouble());
            return Center + new Vec2(MathF.Cos(angle) * a * radius, MathF.Sin(angle) * b * radius);
        }

        /// <summary>横の位置 minX から右の端までを選び、その位置で入る高さから縦を選ぶ（敵をプレイヤーの入口から離して出すため）</summary>
        public Vec2 RandomPointRightOf(Random random, float minX, float margin)
        {
            var maxX = IsEllipse ? SpanX(Center.Y, margin).max : Max.X - margin;
            var x = minX + (float)random.NextDouble() * (maxX - minX);
            var half = HalfHeightAt(x, margin);
            var y = Center.Y - half + (float)random.NextDouble() * half * 2f;
            return new Vec2(x, y);
        }

        public bool Equals(AreaShape other) => Min.Equals(other.Min) && Max.Equals(other.Max) && IsEllipse == other.IsEllipse;
        public override bool Equals(object obj) => obj is AreaShape other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Min, Max, IsEllipse);
        public override string ToString() => $"{(IsEllipse ? "Ellipse" : "Rect")} {Min}-{Max}";

        private (float a, float b) InnerAxes(float margin)
        {
            return (MathF.Max(MinAxis, Width * 0.5f - margin), MathF.Max(MinAxis, Height * 0.5f - margin));
        }

        /// <summary>楕円の縁の点で、外向きの向き（長さ1）</summary>
        private Vec2 OutwardNormal(Vec2 onEdge, float margin)
        {
            var (a, b) = InnerAxes(margin);
            var d = onEdge - Center;
            return new Vec2(d.X / (a * a), d.Y / (b * b)).Normalized;
        }

        /// <summary>
        /// 中心が原点・軸の半分が a, b の楕円の縁で、p に一番近い点。
        /// 4分の1の弧の上で、曲がり具合の中心から見た角度を合わせ直す繰り返し（三角関数を使わず、どの点でも安定して近づく）
        /// </summary>
        private static Vec2 NearestOnEllipse(Vec2 p, float a, float b)
        {
            var px = MathF.Abs(p.X);
            var py = MathF.Abs(p.Y);
            var tx = 0.70710678f;
            var ty = 0.70710678f;
            for (var i = 0; i < NearestIterations; i++)
            {
                var x = a * tx;
                var y = b * ty;
                var ex = (a * a - b * b) * tx * tx * tx / a;
                var ey = (b * b - a * a) * ty * ty * ty / b;
                var rx = x - ex;
                var ry = y - ey;
                var qx = px - ex;
                var qy = py - ey;
                var r = MathF.Sqrt(rx * rx + ry * ry);
                var q = MathF.Max(MinAxis, MathF.Sqrt(qx * qx + qy * qy));
                tx = Math.Clamp((qx * r / q + ex) / a, 0f, 1f);
                ty = Math.Clamp((qy * r / q + ey) / b, 0f, 1f);
                var t = MathF.Sqrt(tx * tx + ty * ty);
                tx /= t;
                ty /= t;
            }

            return new Vec2(p.X < 0f ? -a * tx : a * tx, p.Y < 0f ? -b * ty : b * ty);
        }

        /// <summary>1軸ぶんの壁。外へ向かって飛んでいたら跳ね返す</summary>
        private static (float position, float velocity) BounceAxis(float position, float velocity, float min, float max, float restitution)
        {
            if (position < min)
            {
                return (min, velocity < 0f ? -velocity * restitution : velocity);
            }

            if (position > max)
            {
                return (max, velocity > 0f ? -velocity * restitution : velocity);
            }

            return (position, velocity);
        }

        /// <summary>1軸ぶんの、壁までの距離。その軸に動かなければ無限</summary>
        private static float SlabDistance(float origin, float direction, float min, float max)
        {
            if (direction > 0f)
            {
                return MathF.Max(0f, (max - origin) / direction);
            }

            if (direction < 0f)
            {
                return MathF.Max(0f, (min - origin) / direction);
            }

            return float.PositiveInfinity;
        }
    }
}
