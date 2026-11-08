using System;
using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;
using UnityEngine;

namespace PenmonScramble.MiniGames.Game
{
    /// <summary>
    /// ミニゲームの地面の形のメッシュ（ゴルフのコース・サッカーのピッチ）: 多角形の上面（高さは位置ごとに決められる）と、
    /// 上面の縁から下へ下ろした側面。上面と側面は別の素材で塗れるよう、サブメッシュを分ける（0 が上面、1 が側面）。
    /// Core の地面の座標（X が右・Y が奥）を Unity の3D（Y が上）へ置く
    /// </summary>
    public static class MiniGameMeshes
    {
        public const int TopSubmesh = 0;
        public const int SideSubmesh = 1;

        /// <param name="top">上面の高さ（位置ごと）</param>
        /// <param name="bottom">側面を下ろす高さ</param>
        /// <param name="uv">上面の絵の位置（側面は絵を使わない）</param>
        public static Mesh Prism(string meshName, IReadOnlyList<Vec2> shape, Func<Vec2, float> top, float bottom, Func<Vec2, Vector2> uv)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var topTriangles = BuildTop(shape, top, uv, vertices, uvs);
            var sideTriangles = BuildSides(shape, top, bottom, vertices, uvs);

            var mesh = new Mesh { name = meshName, subMeshCount = 2 };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(topTriangles, TopSubmesh);
            mesh.SetTriangles(sideTriangles, SideSubmesh);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>平らな多角形の板（上向き）。池・砂地などの地面の種類の板</summary>
        public static Mesh Flat(string meshName, IReadOnlyList<Vec2> shape, float height, Func<Vec2, Vector2> uv)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = BuildTop(shape, _ => height, uv, vertices, uvs);
            var mesh = new Mesh { name = meshName };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// ToyMeshes の形を子の部品として置く。Unity の基本の形は使わない（当たりは Core が持つので Collider は付けない）。
        /// 親に付けてからローカルの値を入れる順番は、ビューごとの見た目を変えないためそのまま守る
        /// </summary>
        public static Transform AddPart(string objectName, Mesh mesh, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 localScale, Material material)
        {
            var obj = new GameObject(objectName);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPosition;
            obj.transform.localRotation = localRotation;
            obj.transform.localScale = localScale;
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = material;
            return obj.transform;
        }

        /// <summary>ビューが作ったメッシュを owned に登録して置く（ビューを消すときに一緒に消すため、登録は置く前に行う）</summary>
        public static MeshRenderer AddOwnedMesh(List<Mesh> owned, string objectName, Mesh mesh, Transform parent, params Material[] materials)
        {
            owned.Add(mesh);
            var obj = new GameObject(objectName);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            return renderer;
        }

        private static Vector3 ToWorld(Vec2 point, float height) => new Vector3(point.X, height, point.Y);

        private static List<int> BuildTop(IReadOnlyList<Vec2> shape, Func<Vec2, float> top, Func<Vec2, Vector2> uv, List<Vector3> vertices, List<Vector2> uvs)
        {
            var first = vertices.Count;
            foreach (var point in shape)
            {
                vertices.Add(ToWorld(point, top(point)));
                uvs.Add(uv(point));
            }

            // Core の三角形は上から見て左回り。Unity は表から見て右回りが表なので、並びを逆にする
            var indices = MiniGameGeometry.Triangulate(shape);
            indices.Reverse();
            for (var i = 0; i < indices.Count; i++)
            {
                indices[i] += first;
            }

            return indices;
        }

        /// <summary>縁の1辺ごとに、上面の高さから bottom まで下ろした四角を外向きに張る</summary>
        private static List<int> BuildSides(IReadOnlyList<Vec2> shape, Func<Vec2, float> top, float bottom, List<Vector3> vertices, List<Vector2> uvs)
        {
            var triangles = new List<int>();
            var counterClockwise = MiniGameGeometry.SignedArea(shape) > 0f;
            for (var i = 0; i < shape.Count; i++)
            {
                var a = shape[i];
                var b = shape[(i + 1) % shape.Count];
                var edge = b - a;
                // 左回りの多角形なら、辺の右側が外
                var outward = counterClockwise ? new Vector3(edge.Y, 0f, -edge.X) : new Vector3(-edge.Y, 0f, edge.X);
                var first = vertices.Count;
                vertices.Add(ToWorld(a, top(a)));
                vertices.Add(ToWorld(b, top(b)));
                vertices.Add(ToWorld(b, bottom));
                vertices.Add(ToWorld(a, bottom));
                for (var k = 0; k < 4; k++)
                {
                    uvs.Add(Vector2.zero);
                }

                AddQuad(triangles, vertices, first, outward);
            }

            return triangles;
        }

        /// <summary>四角を2枚の三角形で張る。表（外積の向き）が outward を向くよう、並びを決める</summary>
        private static void AddQuad(List<int> triangles, List<Vector3> vertices, int first, Vector3 outward)
        {
            var v0 = vertices[first];
            var v1 = vertices[first + 1];
            var v2 = vertices[first + 2];
            var facesOut = Vector3.Dot(Vector3.Cross(v1 - v0, v2 - v0), outward) >= 0f;
            if (facesOut)
            {
                triangles.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
            }
            else
            {
                triangles.AddRange(new[] { first, first + 2, first + 1, first, first + 3, first + 2 });
            }
        }
    }
}
