using System;
using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.Golf.Core;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// 見た目の地面の高さ。坂は下る向きへ傾いた床、ジャンプ台は少し高い台にする。
    /// Core は平らな地面で計算するので高さは見た目だけ（球・打つ子・ガイド線をこの高さに乗せる）
    /// </summary>
    public sealed class GolfSurface
    {
        // 坂の傾きは、押す力から求めた本当の傾きを小さめにする（本当の傾きだと壁より高くなり、壁を埋めてしまうため）
        private const float SlopeRiseScale = 0.5f;
        private const float SlopeMaxRise = 0.35f;
        public const float JumpPadHeight = 0.08f;

        private readonly List<Raise> _raises = new List<Raise>();

        public GolfSurface(GolfHoleDef hole)
        {
            foreach (var area in hole.Areas)
            {
                if (area.Kind == GolfAreaKind.Slope)
                {
                    _raises.Add(Raise.Slope(area));
                }
                else if (area.Kind == GolfAreaKind.Jump)
                {
                    _raises.Add(Raise.Pad(area, JumpPadHeight));
                }
            }
        }

        /// <summary>この位置の地面の高さ（重なっていれば高い方）</summary>
        public float HeightAt(Vec2 point)
        {
            var height = 0f;
            foreach (var raise in _raises)
            {
                if (raise.Area.Contains(point))
                {
                    height = Math.Max(height, raise.HeightAt(point));
                }
            }

            return height;
        }

        /// <summary>坂・ジャンプ台の上面の高さの決め方（メッシュを組むとき、その範囲だけの高さを使う）</summary>
        public Func<Vec2, float> TopOf(GolfArea area)
        {
            foreach (var raise in _raises)
            {
                if (raise.Area == area)
                {
                    return raise.HeightAt;
                }
            }

            return _ => 0f;
        }

        /// <summary>高くなる範囲1つ。上る向きに沿って 0 から Rise まで上がる（向きが無ければ平らな台）</summary>
        private sealed class Raise
        {
            public GolfArea Area { get; }
            private readonly Vec2 _uphill;
            private readonly float _min;
            private readonly float _span;
            private readonly float _rise;

            private Raise(GolfArea area, Vec2 uphill, float rise)
            {
                Area = area;
                _uphill = uphill;
                _rise = rise;
                _min = float.MaxValue;
                var max = float.MinValue;
                foreach (var point in area.Shape)
                {
                    var along = Vec2.Dot(point, uphill);
                    _min = Math.Min(_min, along);
                    max = Math.Max(max, along);
                }

                _span = max - _min;
            }

            public static Raise Slope(GolfArea area)
            {
                var push = area.Push;
                var uphill = -push.Normalized;
                var raise = new Raise(area, uphill, 0f);
                var rise = raise._span * push.Length / GolfBalance.Gravity * SlopeRiseScale;
                return new Raise(area, uphill, Math.Min(SlopeMaxRise, rise));
            }

            public static Raise Pad(GolfArea area, float height) => new Raise(area, Vec2.Zero, height);

            public float HeightAt(Vec2 point)
            {
                if (_span <= 0f)
                {
                    return _rise;
                }

                var t = Math.Clamp((Vec2.Dot(point, _uphill) - _min) / _span, 0f, 1f);
                return _rise * t;
            }
        }
    }
}
