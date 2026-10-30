using System;
using System.Collections.Generic;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ペンギンの部品に使う形（球・卵形・しずく・丸い円柱・輪）。縦の輪郭を回して作る。形ごとに1つだけ作って全部の部品で使い回す。
    /// Unity の Primitive を使わないのは、Collider が付くのと、面の数を自分で決めたいため（スマホで重いときに減らせる）
    /// </summary>
    public static class ToyMeshes
    {
        private const int Segments = 24;
        private const int Rings = 16;
        private const int TubeSegments = 10;

        // 卵形の下側の膨らみ。おもちゃのペンギンは下が太い
        private const float EggBulge = 0.18f;
        // しずくの先の細さ。大きいほど先がとがる
        private const float DropTaper = 1.5f;
        // しずくのいちばん太い所が直径1になるように割る値（sinθ・cos(θ/2)^1.5 の最大）
        private const float DropMaxWidth = 0.70f;
        // 丸い円柱の角の丸さ。0 に近いほど角ばる（1 で球）
        private const float PuckRoundness = 0.25f;
        // 輪の管の太さ（外側の直径1に対する半径）。細くして、胴に巻く帯が浮き輪に見えないようにする
        private const float TorusTube = 0.05f;

        private static Mesh _sphere;
        private static Mesh _egg;
        private static Mesh _drop;
        private static Mesh _puck;
        private static Mesh _torus;
        private static Mesh _quad;
        private static Mesh _star;

        /// <summary>直径1の球</summary>
        public static Mesh Sphere => _sphere != null ? _sphere : _sphere = Lathe("ToySphere", SphereProfile(_ => 1f));

        /// <summary>高さ1・いちばん太い所の直径が約1の卵形（下が太い）</summary>
        public static Mesh Egg => _egg != null ? _egg : _egg = Lathe("ToyEgg", SphereProfile(y => 1f - EggBulge * y));

        /// <summary>高さ1・下の太い所の直径が1のしずく形。先は +Y。とげ・羽・炎・水滴に使う</summary>
        public static Mesh Drop => _drop != null ? _drop : _drop = Lathe("ToyDrop", DropProfile);

        /// <summary>直径1・高さ1の、角の丸い円柱。軸は Y。帽子・太鼓・レンズに使う</summary>
        public static Mesh Puck => _puck != null ? _puck : _puck = Lathe("ToyPuck", PuckProfile);

        /// <summary>外側の直径1の輪（ドーナツ）。軸は Y。帯・マフラー・メガネに使う</summary>
        public static Mesh Torus => _torus != null ? _torus : _torus = CreateTorus();

        /// <summary>上を向いた1x1の板（床・丸い影）</summary>
        public static Mesh FloorQuad => _quad != null ? _quad : _quad = CreateFloorQuad();

        /// <summary>外側の直径1の、ふくらんだ五角の星。厚みは Z。撃破のときに飛び散る星に使う</summary>
        public static Mesh Star => _star != null ? _star : _star = CreateStar();

        /// <summary>球の輪郭（y = -0.5〜0.5）の半径に widthScale(y) を掛ける。y は -1（下）〜1（上）</summary>
        private static Func<float, Vector2> SphereProfile(Func<float, float> widthScale)
        {
            return theta =>
            {
                var y = -Mathf.Cos(theta);
                return new Vector2(0.5f * Mathf.Sin(theta) * widthScale(y), 0.5f * y);
            };
        }

        private static Vector2 DropProfile(float theta)
        {
            var radius = 0.5f * Mathf.Sin(theta) * Mathf.Pow(Mathf.Cos(theta * 0.5f), DropTaper) / DropMaxWidth;
            return new Vector2(radius, -0.5f * Mathf.Cos(theta));
        }

        /// <summary>超楕円の輪郭。sin・cos を小さい指数で持ち上げると、平らな面と丸い角になる</summary>
        private static Vector2 PuckProfile(float theta)
        {
            var sin = Mathf.Sin(theta);
            var cos = Mathf.Cos(theta);
            var radius = 0.5f * Mathf.Pow(Mathf.Abs(sin), PuckRoundness);
            var height = -0.5f * Mathf.Sign(cos) * Mathf.Pow(Mathf.Abs(cos), PuckRoundness);
            return new Vector2(radius, height);
        }

        /// <summary>
        /// 輪郭 profile(θ)（θ = 0 が下の端、π が上の端。戻り値は (半径, 高さ)）を Y 軸のまわりに回す。法線は輪郭の傾きから求める
        /// </summary>
        private static Mesh Lathe(string meshName, Func<float, Vector2> profile)
        {
            var vertices = new Vector3[(Rings + 1) * (Segments + 1)];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            for (var ring = 0; ring <= Rings; ring++)
            {
                var (point, radial, up) = ProfilePoint(ring, profile);
                for (var seg = 0; seg <= Segments; seg++)
                {
                    var angle = seg / (float)Segments * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                    var i = ring * (Segments + 1) + seg;
                    vertices[i] = dir * point.x + Vector3.up * point.y;
                    normals[i] = (dir * radial + Vector3.up * up).normalized;
                    uvs[i] = new Vector2(seg / (float)Segments, ring / (float)Rings);
                }
            }

            var mesh = new Mesh { name = meshName, vertices = vertices, normals = normals, uv = uvs, triangles = GridTriangles(Rings, Segments) };
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>ring 段目の点と、その点での法線の (横, 縦) 成分</summary>
        private static (Vector2 point, float radial, float up) ProfilePoint(int ring, Func<float, Vector2> profile)
        {
            var theta = ring / (float)Rings * Mathf.PI;
            var point = profile(theta);
            if (ring == 0) return (new Vector2(0f, point.y), 0f, -1f);
            if (ring == Rings) return (new Vector2(0f, point.y), 0f, 1f);

            // 輪郭の接線 (dr, dy) を直角に回すと法線になる。角が鋭い形でも向きが崩れないよう前後の差で求める
            const float delta = 0.001f;
            var tangent = profile(theta + delta) - profile(theta - delta);
            return (point, tangent.y, -tangent.x);
        }

        private static Mesh CreateTorus()
        {
            const float major = 0.5f - TorusTube;
            var vertices = new Vector3[(TubeSegments + 1) * (Segments + 1)];
            var normals = new Vector3[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            for (var tube = 0; tube <= TubeSegments; tube++)
            {
                // 管の断面を内側の下から回す（Lathe と同じ向きに並べて、三角形の作り方を共通にする）
                var phi = tube / (float)TubeSegments * Mathf.PI * 2f;
                var radial = -Mathf.Cos(phi);
                var up = -Mathf.Sin(phi);
                for (var seg = 0; seg <= Segments; seg++)
                {
                    var angle = seg / (float)Segments * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle));
                    var i = tube * (Segments + 1) + seg;
                    vertices[i] = dir * (major + TorusTube * radial) + Vector3.up * (TorusTube * up);
                    normals[i] = (dir * radial + Vector3.up * up).normalized;
                    uvs[i] = new Vector2(seg / (float)Segments, tube / (float)TubeSegments);
                }
            }

            var mesh = new Mesh { name = "ToyTorus", vertices = vertices, normals = normals, uv = uvs, triangles = GridTriangles(TubeSegments, Segments) };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static int[] GridTriangles(int rows, int columns)
        {
            var triangles = new int[rows * columns * 6];
            var t = 0;
            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < columns; col++)
                {
                    var a = row * (columns + 1) + col;
                    var b = a + columns + 1;
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

        // 星のとがり（内側の角の半径 / 外側の角の半径）と、真ん中のふくらみ（外側の直径1に対する厚み）
        private const int StarPoints = 5;
        private const float StarInnerRatio = 0.45f;
        private const float StarPuff = 0.18f;

        /// <summary>
        /// 表と裏の真ん中を盛り上げた星。面ごとに頂点を分けて、トゥーンの陰影で面の切り替わりが見えるようにする（おもちゃの星らしく）
        /// </summary>
        private static Mesh CreateStar()
        {
            var vertices = new List<Vector3>();
            var corners = StarPoints * 2;
            for (var side = -1f; side <= 1f; side += 2f)
            {
                var tip = new Vector3(0f, 0f, side * StarPuff * 0.5f);
                for (var i = 0; i < corners; i++)
                {
                    var a = StarCorner(i);
                    var b = StarCorner((i + 1) % corners);
                    // 表と裏で回り方を逆にして、どちらも外を向かせる
                    vertices.Add(tip);
                    vertices.Add(side > 0f ? a : b);
                    vertices.Add(side > 0f ? b : a);
                }
            }

            var triangles = new int[vertices.Count];
            for (var i = 0; i < triangles.Length; i++)
            {
                triangles[i] = i;
            }

            var mesh = new Mesh { name = "ToyStar" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>星の i 番目の角。偶数が外側（とがった先）、奇数が内側。0 番は真上</summary>
        private static Vector3 StarCorner(int i)
        {
            var angle = Mathf.PI * 0.5f + i * Mathf.PI / StarPoints;
            var radius = i % 2 == 0 ? 0.5f : 0.5f * StarInnerRatio;
            return new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }
    }
}
