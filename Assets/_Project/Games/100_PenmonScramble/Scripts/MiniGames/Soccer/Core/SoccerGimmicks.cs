using System;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Soccer.Core
{
    public enum SoccerFlowKind
    {
        /// <summary>水流（深海）。地面を転がるボールだけを押す</summary>
        Current,
        /// <summary>風（嵐の崖）。浮いた球ほど強く押す</summary>
        Wind,
    }

    /// <summary>
    /// ボールを押し流す帯（長方形）。押す向きは時刻だけで決まる（ホストとゲストで同じ時刻なら同じ向き）:
    /// Cycle の「動いている」間だけ押し、Reverses なら1回りごとに向きを反対にする（水流は止まらずに向きだけ替わり、風は吹く・止むをくり返す）。
    /// ボールの速さが押す向きに MaxSpeed より遅い間だけ押す（帯の中でいつまでも速くならないように）
    /// </summary>
    public sealed class SoccerFlowZone
    {
        public SoccerFlowZone(SoccerFlowKind kind, Vec2 min, Vec2 max, Vec2 direction, float accel, float maxSpeed, MiniGameCycle cycle, bool reverses)
        {
            Kind = kind;
            Min = min;
            Max = max;
            Direction = direction.Normalized;
            Accel = accel;
            MaxSpeed = maxSpeed;
            Cycle = cycle;
            Reverses = reverses;
        }

        public SoccerFlowKind Kind { get; }

        /// <summary>帯の左下と右上</summary>
        public Vec2 Min { get; }

        public Vec2 Max { get; }

        /// <summary>はじめの1回りに押す向き（長さ 1）</summary>
        public Vec2 Direction { get; }

        /// <summary>押す強さ（毎秒これだけ速くする）。転がりの摩擦より大きくして、止まった球も動かす</summary>
        public float Accel { get; }

        public float MaxSpeed { get; }

        public MiniGameCycle Cycle { get; }

        public bool Reverses { get; }

        public Vec2 Center => (Min + Max) * 0.5f;

        public bool Contains(Vec2 p) => p.X >= Min.X && p.X <= Max.X && p.Y >= Min.Y && p.Y <= Max.Y;

        /// <summary>この時刻に押す向き（長さ 1）。押していなければ 0</summary>
        public Vec2 DirectionAt(float time)
        {
            if (!Cycle.IsActive(time))
            {
                return Vec2.Zero;
            }

            return Reverses && Round(time) % 2 != 0 ? Direction * -1f : Direction;
        }

        /// <summary>何回り目か（向きを替えるのに使う）</summary>
        private long Round(float time)
        {
            return Cycle.Period > 0f ? (long)Math.Floor((time + Cycle.Offset) / Cycle.Period) : 0L;
        }
    }

    /// <summary>
    /// 間欠泉（丸い噴き出し口）。Cycle の「動いている」間に噴き、口の上の地面のボールを上へ跳ね上げる（持っている子の足元からも離す）。
    /// 1回噴く間に跳ね上げるのは1度だけ（落ちてきた球をまた跳ね上げ続けないように）
    /// </summary>
    public sealed class SoccerGeyser
    {
        public SoccerGeyser(Vec2 center, float radius, MiniGameCycle cycle)
        {
            Center = center;
            Radius = radius;
            Cycle = cycle;
        }

        public Vec2 Center { get; }

        public float Radius { get; }

        public MiniGameCycle Cycle { get; }

        public bool IsErupting(float time) => Cycle.IsActive(time);

        /// <summary>何回目に噴いているか（1回の噴き出しに1度だけ跳ね上げるのに使う）</summary>
        public long Eruption(float time)
        {
            return Cycle.Period > 0f ? (long)Math.Floor((time + Cycle.Offset) / Cycle.Period) : 0L;
        }

        public bool Contains(Vec2 p) => Vec2.Distance(p, Center) <= Radius;
    }

    /// <summary>ピッチの中に立つバンパーの柱（丸い。当たりは SoccerPitchDef が多角形の壁にする）</summary>
    public readonly struct SoccerPost
    {
        /// <summary>当たりの多角形の辺の数</summary>
        public const int Sides = 12;

        public readonly Vec2 Center;
        public readonly float Radius;

        public SoccerPost(Vec2 center, float radius)
        {
            Center = center;
            Radius = radius;
        }
    }

    /// <summary>壁のバンパー（外周の上にある線分）。当たったボールを強く跳ね返す。SoccerPitchDef が外周の板をここで分ける</summary>
    public readonly struct SoccerBumper
    {
        public readonly Vec2 A;
        public readonly Vec2 B;

        public SoccerBumper(Vec2 a, Vec2 b)
        {
            A = a;
            B = b;
        }
    }
}
