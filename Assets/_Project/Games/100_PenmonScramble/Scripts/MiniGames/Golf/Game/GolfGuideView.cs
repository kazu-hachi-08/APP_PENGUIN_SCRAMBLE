using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.Game;
using PenmonScramble.Golf.Core;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// ガイド線の点線。球から最初に当たる所まで白い点（特殊ショットを ON にしているときは水色。風は曲がった線）が流れ、壁で跳ね返った後の短い線は黄色。当たる所に小さな輪。
    /// 点は少しずつ前へ流して、向きが一目で分かるようにする（線そのものの計算は Core の GolfGuide）
    /// </summary>
    public sealed class GolfGuideView
    {
        private const float DotSpacing = 0.32f;
        private const int DotCount = 48;
        private const float DotSize = 0.11f;
        private const float DotFlatten = 0.5f;
        private const float DotHeight = 0.05f;
        private const float FlowSpeed = 0.8f;
        // 球のすぐ前は打つ子のクラブと重なるので、少し空けてから点を置く
        private const float StartGap = GolfBalance.BallRadius * 1.6f;
        private const float MarkerSize = 0.42f;

        private static readonly Color GuideColor = Color.white;
        private static readonly Color BounceColor = new Color(1f, 0.88f, 0.35f);
        private static readonly Color SpecialColor = new Color(0.45f, 0.95f, 1f);

        private readonly List<Transform> _dots = new List<Transform>();
        private readonly List<MeshRenderer> _renderers = new List<MeshRenderer>();
        private readonly Material _guide;
        private readonly Material _bounce;
        private readonly Material _special;
        private readonly Transform _marker;
        private float _flow;

        public GolfGuideView(ToonMaterials materials, Transform parent)
        {
            var root = new GameObject("Guide").transform;
            root.SetParent(parent, false);
            _guide = materials.Get(GuideColor, 0f);
            _bounce = materials.Get(BounceColor, 0f);
            _special = materials.Get(SpecialColor, 0f);
            for (var i = 0; i < DotCount; i++)
            {
                var dot = Create("Dot", ToyMeshes.Sphere, root, new Vector3(DotSize, DotSize * DotFlatten, DotSize), _guide);
                _dots.Add(dot.transform);
                _renderers.Add(dot);
            }

            _marker = Create("Hit", ToyMeshes.Torus, root, Vector3.one * MarkerSize, _guide).transform;
            Hide();
        }

        /// <param name="special">特殊ショットを ON にしている（点を水色にして、ふつうの線と見分ける）</param>
        public void Show(GolfGuideLine? line, GolfSurface surface, float deltaTime, bool special = false)
        {
            if (!line.HasValue)
            {
                Hide();
                return;
            }

            _flow = Mathf.Repeat(_flow + deltaTime * FlowSpeed, DotSpacing);
            var guide = line.Value;
            var used = 0;
            var offset = StartGap + _flow;
            var path = guide.Path;
            var material = special ? _special : _guide;
            // 曲がった線は点を等間隔に保つよう、前の区間の余りを次の区間へ持ち越す
            for (var i = 0; i + 1 < path.Count; i++)
            {
                used = Place(path[i], path[i + 1], ref offset, used, material, surface);
            }

            if (guide.Stop == GolfGuideStop.Wall)
            {
                var bounceOffset = _flow;
                used = Place(guide.End, guide.BounceEnd, ref bounceOffset, used, _bounce, surface);
            }

            for (var i = used; i < _dots.Count; i++)
            {
                _dots[i].gameObject.SetActive(false);
            }

            _marker.gameObject.SetActive(guide.Stop != GolfGuideStop.None);
            _marker.localPosition = GolfSpace.ToWorld(guide.End, surface.HeightAt(guide.End) + DotHeight);
        }

        private void Hide()
        {
            foreach (var dot in _dots)
            {
                dot.gameObject.SetActive(false);
            }

            _marker.gameObject.SetActive(false);
        }

        /// <summary>from から to へ、start の距離から点を並べ、使い終わった点の番号を返す。start は次の区間で始める距離にする</summary>
        private int Place(Vec2 from, Vec2 to, ref float start, int first, Material material, GolfSurface surface)
        {
            var length = Vec2.Distance(from, to);
            var index = first;
            if (length <= 0f)
            {
                return index;
            }

            var s = start;
            for (; s <= length && index < _dots.Count; s += DotSpacing)
            {
                var point = from + (to - from) * (s / length);
                var dot = _dots[index];
                dot.gameObject.SetActive(true);
                dot.localPosition = GolfSpace.ToWorld(point, surface.HeightAt(point) + DotHeight);
                _renderers[index].sharedMaterial = material;
                index++;
            }

            start = s - length;
            return index;
        }

        private static MeshRenderer Create(string name, Mesh mesh, Transform parent, Vector3 scale, Material material)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localScale = scale;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return renderer;
        }
    }
}
