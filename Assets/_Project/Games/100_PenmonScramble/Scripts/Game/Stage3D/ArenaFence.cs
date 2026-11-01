using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 大ボスの闘技場の楕円に沿って並べる、おもちゃの丸い柵（丸い柱・柱の上の玉・柱をつなぐ太い横木）。
    /// 柱は百本近くになるので、部品の種類ごとに1つのメッシュへまとめて描く回数を減らす。
    /// 入口（前のエリアからの通り道）と出口（次の闘技場への通り道）にかかる柱だけは別にまとめ、開いている間は隠す。
    /// 闘技場どうしの通り道の両側にも、同じ柵を立てる（BuildCorridor）
    /// </summary>
    public static class ArenaFence
    {
        /// <summary>柱の間（マス）。ペンギン（直径1くらい）が通れそうに見えないよう、柱と横木で詰まって見える間にする</summary>
        public const float PostSpacing = 1.1f;

        // 柵の大きさ（マス）。奥の柵が手前のペンギンを隠さない、手前の柵がペンギンの足元を隠しすぎない高さ
        private const float PostDiameter = 0.32f;
        private const float PostHeight = 0.5f;
        private const float CapDiameter = 0.34f;
        private const float RailThickness = 0.16f;
        private const float RailHeight = 0.3f;
        private const float Gloss = 0.35f;
        // 楕円の長さを測る細かさ。柱の間が見た目でそろう程度
        private const int ArcSamples = 720;

        /// <summary>楕円の縁に、間をそろえて並べた柱の位置。左端（入口の真ん中）から始めて、入口が上下で対称になるようにする</summary>
        public static List<Vector2> PostPoints(AreaShape arena, float spacing)
        {
            var center = arena.Center.ToVector2();
            var radii = new Vector2(arena.Width, arena.Height) * 0.5f;
            var lengths = new float[ArcSamples + 1];
            var previous = PointAt(center, radii, 0);
            for (var i = 1; i <= ArcSamples; i++)
            {
                var point = PointAt(center, radii, i);
                lengths[i] = lengths[i - 1] + Vector2.Distance(previous, point);
                previous = point;
            }

            var count = Mathf.Max(3, Mathf.RoundToInt(lengths[ArcSamples] / spacing));
            var step = lengths[ArcSamples] / count;
            var posts = new List<Vector2>(count);
            var sample = 0;
            for (var i = 0; i < count; i++)
            {
                var target = i * step;
                while (lengths[sample + 1] < target)
                {
                    sample++;
                }

                var t = Mathf.InverseLerp(lengths[sample], lengths[sample + 1], target);
                posts.Add(Vector2.Lerp(PointAt(center, radii, sample), PointAt(center, radii, sample + 1), t));
            }

            return posts;
        }

        /// <summary>入口の通り道にかかる柱（入口が開いている間は隠す）。柱の太さぶん広く見る</summary>
        public static bool IsInDoorway(Vector2 post, AreaShape doorway) => doorway.Contains(post.ToVec2(), -PostDiameter * 0.5f);

        /// <summary>
        /// 柵を組んで parent の下に置く。entrance / exit は通り道（無ければ null）。
        /// 戻り値は入口・出口にかかる柱（開いている間は隠す）。通り道が無ければ null
        /// </summary>
        public static (GameObject entrance, GameObject exit) Build(Transform parent, AreaShape arena, AreaShape? entrance, AreaShape? exit, ToonMaterials materials, StageTheme theme)
        {
            var posts = PostPoints(arena, PostSpacing);
            var fence = new Parts(materials, theme);
            var entranceDoor = new Parts(materials, theme);
            var exitDoor = new Parts(materials, theme);
            for (var i = 0; i < posts.Count; i++)
            {
                var next = posts[(i + 1) % posts.Count];
                var parts = PartsFor(posts[i], entrance, exit, fence, entranceDoor, exitDoor);
                parts.AddPost(posts[i], i);
                // 横木は、どちらかの柱が入口・出口にかかっていればそちらと一緒に隠す
                (parts == fence ? PartsFor(next, entrance, exit, fence, entranceDoor, exitDoor) : parts).AddRail(posts[i], next);
            }

            fence.Combine("ArenaFence", parent);
            var entranceObj = entrance.HasValue ? entranceDoor.Combine("ArenaEntrance", parent) : null;
            var exitObj = exit.HasValue ? exitDoor.Combine("ArenaExit", parent) : null;
            return (entranceObj, exitObj);
        }

        /// <summary>
        /// 闘技場どうしの通り道の、楕円の外に出ている所の両側に柵を立てる。
        /// 前の楕円の柵が通り道の縁の高さを横切る所から、次の楕円の同じ所まで
        /// </summary>
        public static void BuildCorridor(Transform parent, AreaShape previous, AreaShape next, ToonMaterials materials, StageTheme theme)
        {
            var parts = new Parts(materials, theme);
            var half = Balance.ArenaDoorHalfWidth;
            var fromX = previous.Max.X - TipInset(previous, half);
            var toX = next.Min.X + TipInset(next, half);
            var count = Mathf.Max(1, Mathf.RoundToInt((toX - fromX) / PostSpacing));
            foreach (var side in new[] { -1f, 1f })
            {
                var y = next.Center.Y + half * side;
                for (var i = 0; i <= count; i++)
                {
                    var post = new Vector2(Mathf.Lerp(fromX, toX, (float)i / count), y);
                    parts.AddPost(post, i);
                    if (i < count)
                    {
                        parts.AddRail(post, new Vector2(Mathf.Lerp(fromX, toX, (float)(i + 1) / count), y));
                    }
                }
            }

            parts.Combine("CorridorFence", parent);
        }

        /// <summary>楕円の左右の端から、高さ halfWidth の所で縁が横切るまでの横の距離</summary>
        public static float TipInset(AreaShape arena, float halfWidth)
        {
            var a = arena.Width * 0.5f;
            var b = arena.Height * 0.5f;
            var ratio = Mathf.Clamp01(halfWidth / b);
            return a * (1f - Mathf.Sqrt(1f - ratio * ratio));
        }

        private static Parts PartsFor(Vector2 post, AreaShape? entrance, AreaShape? exit, Parts fence, Parts entranceDoor, Parts exitDoor)
        {
            if (entrance.HasValue && IsInDoorway(post, entrance.Value))
            {
                return entranceDoor;
            }

            return exit.HasValue && IsInDoorway(post, exit.Value) ? exitDoor : fence;
        }

        /// <summary>楕円を ArcSamples に分けた i 番目の点。0 が左端で、上（奥）へ回る</summary>
        private static Vector2 PointAt(Vector2 center, Vector2 radii, int i)
        {
            var angle = Mathf.PI - i * Mathf.PI * 2f / ArcSamples;
            return center + new Vector2(Mathf.Cos(angle) * radii.x, Mathf.Sin(angle) * radii.y);
        }

        /// <summary>柱・玉・横木を、種類ごとに1つのメッシュへまとめる組み立て場</summary>
        private sealed class Parts
        {
            private readonly ToonMaterials _materials;
            private readonly StageTheme _theme;
            private readonly List<CombineInstance> _posts = new List<CombineInstance>();
            private readonly List<CombineInstance> _caps = new List<CombineInstance>();
            private readonly List<CombineInstance> _altCaps = new List<CombineInstance>();
            private readonly List<CombineInstance> _rails = new List<CombineInstance>();

            public Parts(ToonMaterials materials, StageTheme theme)
            {
                _materials = materials;
                _theme = theme;
            }

            /// <param name="index">玉の色を1本おきに変える（おもちゃらしく、柵の続きが目で追えるように）</param>
            public void AddPost(Vector2 point, int index)
            {
                var floor = point.ToFloor();
                Add(_posts, ToyMeshes.Puck, floor + Vector3.up * (PostHeight * 0.5f), Quaternion.identity, new Vector3(PostDiameter, PostHeight, PostDiameter));
                Add(index % 2 == 0 ? _caps : _altCaps, ToyMeshes.Sphere, floor + Vector3.up * PostHeight, Quaternion.identity, Vector3.one * CapDiameter);
            }

            /// <summary>柱から次の柱へ、横に寝かせた丸い棒を渡す</summary>
            public void AddRail(Vector2 from, Vector2 to)
            {
                var direction = (to - from).ToFloor();
                var middle = ((from + to) * 0.5f).ToFloor(RailHeight);
                var rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
                Add(_rails, ToyMeshes.Puck, middle, rotation, new Vector3(RailThickness, direction.magnitude, RailThickness));
            }

            public GameObject Combine(string objName, Transform parent)
            {
                var root = new GameObject(objName);
                root.transform.SetParent(parent, false);
                CombineInto(root.transform, "Rails", _rails, _theme.FenceRail);
                CombineInto(root.transform, "Posts", _posts, _theme.FencePost);
                CombineInto(root.transform, "Caps", _caps, _theme.FenceCap);
                CombineInto(root.transform, "AltCaps", _altCaps, _theme.FenceAltCap);
                return root;
            }

            private static void Add(List<CombineInstance> list, Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale)
            {
                list.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(position, rotation, scale) });
            }

            private void CombineInto(Transform parent, string objName, List<CombineInstance> parts, Color color)
            {
                if (parts.Count == 0)
                {
                    return;
                }

                var mesh = new Mesh { name = objName };
                // 柱の数によっては頂点が 65535 を超えるので、32bit の番号にしておく
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.CombineMeshes(parts.ToArray(), true, true);
                var obj = new GameObject(objName);
                obj.transform.SetParent(parent, false);
                obj.AddComponent<OwnedMesh>().Mesh = mesh;
                var renderer = ToyParts.AddMesh(obj, mesh, _materials.Get(color, Gloss));
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
}
