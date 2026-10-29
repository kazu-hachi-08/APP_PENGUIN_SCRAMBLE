using System;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ペンギンの部品に使う形（球・卵形）。縦の輪郭を回して作る。形ごとに1つだけ作って全部の部品で使い回す。
    /// Unity の Primitive を使わないのは、Collider が付くのと、面の数を自分で決めたいため（スマホで重いときに減らせる）
    /// </summary>
    public static class ToyMeshes
    {
        private const int Segments = 24;
        private const int Rings = 16;

        // 卵形の下側の膨らみ。おもちゃのペンギンは下が太い
        private const float EggBulge = 0.18f;

        private static Mesh _sphere;
        private static Mesh _egg;
        private static Mesh _quad;

        /// <summary>直径1の球</summary>
        public static Mesh Sphere => _sphere != null ? _sphere : _sphere = Lathe("ToySphere", y => 1f);

        /// <summary>高さ1・いちばん太い所の直径が約1の卵形（下が太い）</summary>
        public static Mesh Egg => _egg != null ? _egg : _egg = Lathe("ToyEgg", y => 1f - EggBulge * y);

        /// <summary>上を向いた1x1の板（床・丸い影）</summary>
        public static Mesh FloorQuad => _quad != null ? _quad : _quad = CreateFloorQuad();

        /// <summary>
        /// 球の輪郭（y = -0.5〜0.5）の半径に widthScale(y) を掛けて回す。法線は輪郭の傾きから求める
        /// </summary>
        private static Mesh Lathe(string meshName, Func<float, float> widthScale)
        {
            var vertices = new Vector3[(Rings + 1) * (Segments + 1)];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            for (var ring = 0; ring <= Rings; ring++)
            {
                var (radius, height, radial, up) = Profile(ring, widthScale);
                for (var seg = 0; seg <= Segments; seg++)
                {
                    var angle = seg / (float)Segments * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                    var i = ring * (Segments + 1) + seg;
                    vertices[i] = dir * radius + Vector3.up * height;
                    normals[i] = (dir * radial + Vector3.up * up).normalized;
                    uvs[i] = new Vector2(seg / (float)Segments, ring / (float)Rings);
                }
            }

            var mesh = new Mesh { name = meshName, vertices = vertices, normals = normals, uv = uvs, triangles = LatheTriangles() };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>ring 段目の半径・高さと、その点での法線の (横, 縦) 成分</summary>
        private static (float radius, float height, float radial, float up) Profile(int ring, Func<float, float> widthScale)
        {
            var theta = ring / (float)Rings * Mathf.PI;
            var y = -0.5f * Mathf.Cos(theta);
            var radius = 0.5f * Mathf.Sin(theta) * widthScale(y * 2f);

            // 輪郭の接線 (dr, dy) を直角に回すと法線になる。両端は真上・真下
            const float delta = 0.001f;
            var y2 = -0.5f * Mathf.Cos(theta + delta);
            var radius2 = 0.5f * Mathf.Sin(theta + delta) * widthScale(y2 * 2f);
            var dr = radius2 - radius;
            var dy = y2 - y;
            if (ring == 0) return (0f, y, 0f, -1f);
            if (ring == Rings) return (0f, y, 0f, 1f);
            return (radius, y, dy, -dr);
        }

        private static int[] LatheTriangles()
        {
            var triangles = new int[Rings * Segments * 6];
            var t = 0;
            for (var ring = 0; ring < Rings; ring++)
            {
                for (var seg = 0; seg < Segments; seg++)
                {
                    var a = ring * (Segments + 1) + seg;
                    var b = a + Segments + 1;
                    // Unity は外から見て時計回りが表
                    triangles[t++] = a;
                    triangles[t++] = a + 1;
                    triangles[t++] = b;
                    triangles[t++] = a + 1;
                    triangles[t++] = b + 1;
                    triangles[t++] = b;
                }
            }

            return triangles;
        }

        private static Mesh CreateFloorQuad()
        {
            var mesh = new Mesh
            {
                name = "ToyFloorQuad",
                vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f) },
                normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up },
                uv = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
            };
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
