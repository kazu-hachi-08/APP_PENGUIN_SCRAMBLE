using PenmonScramble.Game;
using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// おもちゃのサッカーボール: 白い球に、正二十面体の頂点の向きの12枚の黒い五角形（本物のボールの五角形と同じ並び）。
    /// 進んだ分だけ転がって回り、足元に丸い影（浮くと小さく薄く）
    /// </summary>
    public sealed class SoccerBallView
    {
        private const float BallGloss = 0.9f;
        private const float PatchGloss = 0.5f;
        // 五角形の大きさ・厚み（直径に対して）と、中心を置く深さ（少しだけ表に出す）
        private const float PatchSize = 0.36f;
        private const float PatchThickness = 0.12f;
        private const float PatchDepth = 0.46f;
        private const float ShadowSize = 2.2f;
        private const float ShadowLift = 0.02f;
        private const float ShadowFadeHeight = 1.5f;
        private const float ShadowMinScale = 0.45f;
        // これより大きく動いたら置き直し（キックオフ）とみなして回さない
        private const float TeleportDistance = 1.5f;

        private static readonly Color PatchColor = new Color(0.14f, 0.15f, 0.2f);
        private static Mesh _pentagon;

        private readonly Transform _root;
        private readonly Transform _spin;
        private readonly Transform _shadow;
        private Vector3 _last;
        private bool _placed;

        public SoccerBallView(ToonMaterials materials, Transform parent)
        {
            _root = new GameObject("Ball").transform;
            _root.SetParent(parent, false);
            _spin = new GameObject("Spin").transform;
            _spin.SetParent(_root, false);
            var diameter = SoccerBalance.BallRadius * 2f;
            Add("Shell", ToyMeshes.Sphere, _spin, Vector3.zero, Quaternion.identity, Vector3.one * diameter, materials.Get(Color.white, BallGloss));
            var patch = materials.Get(PatchColor, PatchGloss);
            foreach (var direction in IcosahedronVertices())
            {
                Add("Patch", Pentagon, _spin, direction * (diameter * PatchDepth), Quaternion.LookRotation(direction),
                    new Vector3(diameter * PatchSize, diameter * PatchSize, diameter * PatchThickness), patch);
            }

            _shadow = new GameObject("Shadow").transform;
            _shadow.SetParent(parent, false);
            var shadow = Add("Blob", ToyMeshes.FloorQuad, _shadow, Vector3.zero, Quaternion.identity, new Vector3(diameter * ShadowSize, 1f, diameter * ShadowSize), materials.BlobShadow);
            shadow.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shadow.receiveShadows = false;
        }

        /// <summary>center は補間したボールの中心（高さを含む）</summary>
        public void Sync(Vector3 center)
        {
            Roll(center);
            _root.localPosition = center;
            var height = center.y - SoccerBalance.BallRadius;
            var scale = Mathf.Lerp(1f, ShadowMinScale, Mathf.Clamp01(height / ShadowFadeHeight));
            _shadow.localPosition = new Vector3(center.x, ShadowLift, center.z);
            _shadow.localScale = Vector3.one * scale;
        }

        /// <summary>地面を進んだ分だけ、進む向きと直角の軸で回す（滑らず転がって見えるように）</summary>
        private void Roll(Vector3 center)
        {
            var moved = center - _last;
            moved.y = 0f;
            _last = center;
            if (!_placed)
            {
                _placed = true;
                return;
            }

            var distance = moved.magnitude;
            if (distance <= 0f || distance > TeleportDistance)
            {
                return;
            }

            var axis = Vector3.Cross(Vector3.up, moved / distance);
            var degrees = distance / SoccerBalance.BallRadius * Mathf.Rad2Deg;
            _spin.localRotation = Quaternion.AngleAxis(degrees, axis) * _spin.localRotation;
        }

        /// <summary>正二十面体の12の頂点の向き（長さ 1）</summary>
        private static Vector3[] IcosahedronVertices()
        {
            var phi = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var points = new Vector3[12];
            var index = 0;
            foreach (var a in new[] { -1f, 1f })
            {
                foreach (var b in new[] { -phi, phi })
                {
                    points[index++] = new Vector3(0f, a, b).normalized;
                    points[index++] = new Vector3(a, b, 0f).normalized;
                    points[index++] = new Vector3(b, 0f, a).normalized;
                }
            }

            return points;
        }

        /// <summary>五角形の板（直径 1・厚み 1。表は +Z）。一度作ったら使い回す</summary>
        private static Mesh Pentagon => _pentagon != null ? _pentagon : _pentagon = CreatePentagon();

        private static Mesh CreatePentagon()
        {
            const int sides = 5;
            var vertices = new Vector3[sides * 2 + 2];
            vertices[0] = new Vector3(0f, 0f, 0.5f);
            vertices[1] = new Vector3(0f, 0f, -0.5f);
            for (var i = 0; i < sides; i++)
            {
                var angle = Mathf.PI * 0.5f + i * Mathf.PI * 2f / sides;
                var p = new Vector3(Mathf.Cos(angle) * 0.5f, Mathf.Sin(angle) * 0.5f, 0f);
                vertices[2 + i] = p + Vector3.forward * 0.5f;
                vertices[2 + sides + i] = p - Vector3.forward * 0.5f;
            }

            var triangles = new System.Collections.Generic.List<int>();
            for (var i = 0; i < sides; i++)
            {
                var next = (i + 1) % sides;
                // 表（+Z）・裏（−Z）・側面（外向き）。どれも外から見て右回り
                triangles.AddRange(new[] { 0, 2 + i, 2 + next });
                triangles.AddRange(new[] { 1, 2 + sides + next, 2 + sides + i });
                triangles.AddRange(new[] { 2 + i, 2 + sides + next, 2 + next, 2 + i, 2 + sides + i, 2 + sides + next });
            }

            var mesh = new Mesh { name = "SoccerPentagon", vertices = vertices, triangles = triangles.ToArray(), hideFlags = HideFlags.DontSave };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static MeshRenderer Add(string name, Mesh mesh, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localRotation = rotation;
            obj.transform.localScale = scale;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }
    }
}
