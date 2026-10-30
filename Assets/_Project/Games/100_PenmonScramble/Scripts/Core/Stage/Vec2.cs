using System;

namespace PenmonScramble.Core
{
    /// <summary>
    /// Core は UnityEngine を参照しないので、Vector2 の代わりに使う2次元ベクトル
    /// </summary>
    public readonly struct Vec2 : IEquatable<Vec2>
    {
        // 長さ0のベクトルを正規化しないための下限
        private const float Epsilon = 1e-5f;

        public readonly float X;
        public readonly float Y;

        public Vec2(float x, float y)
        {
            X = x;
            Y = y;
        }

        public static Vec2 Zero => new Vec2(0f, 0f);
        public static Vec2 Right => new Vec2(1f, 0f);

        public float LengthSquared => X * X + Y * Y;
        public float Length => MathF.Sqrt(LengthSquared);
        public bool IsNearlyZero => LengthSquared < Epsilon * Epsilon;

        /// <summary>長さ0なら Zero を返す（割り算で NaN にしないため）</summary>
        public Vec2 Normalized
        {
            get
            {
                var length = Length;
                return length < Epsilon ? Zero : new Vec2(X / length, Y / length);
            }
        }

        public Vec2 ClampLength(float max)
        {
            var length = Length;
            return length > max ? this * (max / length) : this;
        }

        /// <summary>左回りに radians だけ回す</summary>
        public Vec2 Rotated(float radians)
        {
            var cos = MathF.Cos(radians);
            var sin = MathF.Sin(radians);
            return new Vec2(X * cos - Y * sin, X * sin + Y * cos);
        }

        public static float Dot(Vec2 a, Vec2 b) => a.X * b.X + a.Y * b.Y;
        public static float Distance(Vec2 a, Vec2 b) => (a - b).Length;

        public static Vec2 operator +(Vec2 a, Vec2 b) => new Vec2(a.X + b.X, a.Y + b.Y);
        public static Vec2 operator -(Vec2 a, Vec2 b) => new Vec2(a.X - b.X, a.Y - b.Y);
        public static Vec2 operator -(Vec2 a) => new Vec2(-a.X, -a.Y);
        public static Vec2 operator *(Vec2 a, float s) => new Vec2(a.X * s, a.Y * s);
        public static Vec2 operator /(Vec2 a, float s) => new Vec2(a.X / s, a.Y / s);

        public bool Equals(Vec2 other) => X.Equals(other.X) && Y.Equals(other.Y);
        public override bool Equals(object obj) => obj is Vec2 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y);
        public override string ToString() => $"({X:0.###}, {Y:0.###})";
    }
}
