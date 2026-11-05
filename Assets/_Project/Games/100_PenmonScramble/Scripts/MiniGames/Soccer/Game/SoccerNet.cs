using System.Collections.Generic;
using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// ゴールの網: 奥・天井・左右の4面に、細い糸（四角い管）を格子に張った1つのメッシュ。
    /// ボールが当たると、当たった所を中心に外へ膨らんでから揺れが収まる（枠に留まった縁は動かない）。
    /// 揺れている間だけ頂点を動かす（止まっている網は描き直さない）
    /// </summary>
    public sealed class SoccerNet
    {
        public static readonly Color Color = new Color(0.94f, 0.96f, 1f);

        private const float MeshSpacing = 0.32f;
        private const float StringHalfWidth = 0.014f;
        // 膨らみ: いちばん強い当たりの大きさ・広がり（当たった所からの距離）・揺れの速さ・収まる速さ・膨らみ始めの速さ
        private const float BulgeMax = 0.4f;
        private const float BulgeMin = 0.08f;
        private const float BulgeFullSpeed = 16f;
        private const float BulgeRadius = 0.9f;
        private const float WobbleRate = 9f;
        private const float WobbleDamping = 3.2f;
        private const float BulgeRise = 25f;
        private const float WobbleSeconds = 1.6f;

        private readonly List<Vector3> _base = new List<Vector3>();
        private readonly List<Vector3> _push = new List<Vector3>();
        private readonly List<float> _pin = new List<float>();
        private readonly List<Vector3> _anchor = new List<Vector3>();
        private readonly List<int> _triangles = new List<int>();
        private Vector3[] _moved;
        private Vector3 _hit;
        private float _amplitude;
        private float _time = WobbleSeconds;

        public Mesh Mesh { get; }

        /// <param name="backHeight">奥の枠の高さ（天井は口のバーから奥へ下がる）</param>
        public SoccerNet(SoccerGoal goal, float backHeight)
        {
            var front = goal.LineX;
            var back = goal.LineX + goal.Outward * goal.Depth;
            var half = goal.HalfWidth;
            var bar = goal.BarHeight;
            var outward = new Vector3(goal.Outward, 0f, 0f);

            // 各面の4つの角（u は横、v は縦の向き）と、膨らむ向き
            AddPanel(new Vector3(back, 0f, -half), new Vector3(back, 0f, half), new Vector3(back, backHeight, half), new Vector3(back, backHeight, -half), outward);
            AddPanel(new Vector3(front, bar, -half), new Vector3(front, bar, half), new Vector3(back, backHeight, half), new Vector3(back, backHeight, -half), (Vector3.up + outward).normalized);
            foreach (var side in new[] { -1f, 1f })
            {
                var z = side * half;
                AddPanel(new Vector3(front, 0f, z), new Vector3(back, 0f, z), new Vector3(back, backHeight, z), new Vector3(front, bar, z), new Vector3(0f, 0f, side));
            }

            _moved = _base.ToArray();
            Mesh = new Mesh { name = "SoccerNet" };
            Mesh.MarkDynamic();
            Mesh.SetVertices(_base);
            Mesh.SetTriangles(_triangles, 0);
            Mesh.RecalculateNormals();
            Mesh.RecalculateBounds();
            // 膨らんだ網が見切れて消えないよう、境界を少し広げておく
            var bounds = Mesh.bounds;
            bounds.Expand(BulgeMax * 2f);
            Mesh.bounds = bounds;
        }

        /// <summary>at に当たった。speed が速いほど大きく膨らむ</summary>
        public void Hit(Vector3 at, float speed)
        {
            var amplitude = Mathf.Lerp(BulgeMin, BulgeMax, Mathf.Clamp01(speed / BulgeFullSpeed));
            // 揺れている途中の弱い当たりで、大きな揺れを小さく上書きしない
            if (_time < WobbleSeconds && amplitude < CurrentAmplitude())
            {
                return;
            }

            _hit = at;
            _amplitude = amplitude;
            _time = 0f;
        }

        public void Tick(float dt)
        {
            if (_time >= WobbleSeconds)
            {
                return;
            }

            _time = Mathf.Min(WobbleSeconds, _time + dt);
            var wave = _time >= WobbleSeconds ? 0f : Wave(_time) * _amplitude;
            var radius2 = BulgeRadius * BulgeRadius;
            for (var i = 0; i < _base.Count; i++)
            {
                var near = Mathf.Exp(-(_anchor[i] - _hit).sqrMagnitude / radius2);
                _moved[i] = _base[i] + _push[i] * (wave * near * _pin[i]);
            }

            Mesh.vertices = _moved;
        }

        private float CurrentAmplitude() => Mathf.Abs(Wave(_time)) * _amplitude;

        /// <summary>すぐ膨らみ（1 へ）、揺れながら収まる</summary>
        private static float Wave(float t)
        {
            return (1f - Mathf.Exp(-t * BulgeRise)) * Mathf.Cos(t * WobbleRate) * Mathf.Exp(-t * WobbleDamping);
        }

        /// <summary>
        /// 4つの角で決まる面に、縦横の糸を張る。糸は網目ごとに区切って、頂点を面の中の位置（留まり具合 pin）付きで持つ
        /// </summary>
        private void AddPanel(Vector3 c00, Vector3 c10, Vector3 c11, Vector3 c01, Vector3 push)
        {
            var columns = Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(c00, c10) / MeshSpacing));
            var rows = Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(c00, c01) / MeshSpacing));
            for (var i = 0; i <= columns; i++)
            {
                for (var j = 0; j < rows; j++)
                {
                    AddString(c00, c10, c11, c01, (float)i / columns, (float)j / rows, (float)i / columns, (float)(j + 1) / rows, push);
                }
            }

            for (var j = 0; j <= rows; j++)
            {
                for (var i = 0; i < columns; i++)
                {
                    AddString(c00, c10, c11, c01, (float)i / columns, (float)j / rows, (float)(i + 1) / columns, (float)j / rows, push);
                }
            }
        }

        /// <summary>面の中の (u0, v0) から (u1, v1) までの1区切りの糸（四角い管。4面）</summary>
        private void AddString(Vector3 c00, Vector3 c10, Vector3 c11, Vector3 c01, float u0, float v0, float u1, float v1, Vector3 push)
        {
            var a = Bilinear(c00, c10, c11, c01, u0, v0);
            var b = Bilinear(c00, c10, c11, c01, u1, v1);
            var along = (b - a).normalized;
            var side = Vector3.Cross(along, push).normalized * StringHalfWidth;
            var up = push.normalized * StringHalfWidth;
            var first = _base.Count;
            AddCorners(a, side, up, push, Pin(u0, v0));
            AddCorners(b, side, up, push, Pin(u1, v1));
            for (var k = 0; k < 4; k++)
            {
                var next = (k + 1) % 4;
                _triangles.AddRange(new[] { first + k, first + 4 + k, first + 4 + next, first + k, first + 4 + next, first + next });
            }
        }

        private void AddCorners(Vector3 center, Vector3 side, Vector3 up, Vector3 push, float pin)
        {
            Vector3[] offsets = { side + up, -side + up, -side - up, side - up };
            foreach (var offset in offsets)
            {
                _base.Add(center + offset);
                _anchor.Add(center);
                _push.Add(push);
                _pin.Add(pin);
            }
        }

        /// <summary>枠に留まった縁は動かず、面の真ん中ほどよく動く</summary>
        private static float Pin(float u, float v) => Mathf.Sin(u * Mathf.PI) * Mathf.Sin(v * Mathf.PI);

        private static Vector3 Bilinear(Vector3 c00, Vector3 c10, Vector3 c11, Vector3 c01, float u, float v)
        {
            return Vector3.Lerp(Vector3.Lerp(c00, c10, u), Vector3.Lerp(c01, c11, u), v);
        }
    }
}
