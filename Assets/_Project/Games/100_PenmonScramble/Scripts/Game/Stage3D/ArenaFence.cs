using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 大ボスの闘技場の楕円に沿って並べる柵。ストーリーはおもちゃの丸い柵（丸い柱・柱の上の玉・柱をつなぐ丸い横木）、
    /// ボスラッシュはコロシアムらしい太い石の柱と低い石の壁と旗（ArenaFenceStyle）。
    /// 柱は百本近くになるので、部品の種類ごとに1つのメッシュへまとめて描く回数を減らす。
    /// 入口（前のエリアからの通り道）と出口（次の闘技場への通り道）にかかる柱だけは別にまとめ、開いている間は隠す。
    /// 闘技場どうしの通り道の両側にも、同じ柵を立てる（BuildCorridor）
    /// </summary>
    public static class ArenaFence
    {
        /// <summary>おもちゃの柵の柱の間（マス）。ペンギン（直径1くらい）が通れそうに見えないよう、柱と横木で詰まって見える間にする</summary>
        public static float PostSpacing => ArenaFenceStyle.Toy.Spacing;

        private const float Gloss = 0.35f;
        // 石は光らせすぎない（つやつやだと石に見えないため）
        private const float StoneGloss = 0.12f;
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
        public static bool IsInDoorway(Vector2 post, AreaShape doorway) => IsInDoorway(post, doorway, ArenaFenceStyle.Toy);

        private static bool IsInDoorway(Vector2 post, AreaShape doorway, ArenaFenceStyle style) => doorway.Contains(post.ToVec2(), -style.PostDiameter * 0.5f);

        /// <summary>
        /// 柵を組んで parent の下に置く。entrance / exit は通り道（無ければ null）。style を省くと、おもちゃの柵。
        /// 戻り値は入口・出口にかかる柱（開いている間は隠す）。通り道が無ければ null
        /// </summary>
        public static (GameObject entrance, GameObject exit) Build(Transform parent, AreaShape arena, AreaShape? entrance, AreaShape? exit, ToonMaterials materials, StageTheme theme,
            ArenaFenceStyle style = null)
        {
            style ??= ArenaFenceStyle.Toy;
            var posts = PostPoints(arena, style.Spacing);
            var fence = new Parts(materials, theme, style);
            var entranceDoor = new Parts(materials, theme, style);
            var exitDoor = new Parts(materials, theme, style);
            var center = arena.Center.ToVector2();
            for (var i = 0; i < posts.Count; i++)
            {
                var next = posts[(i + 1) % posts.Count];
                var parts = PartsFor(posts[i], entrance, exit, fence, entranceDoor, exitDoor, style);
                var back = posts[i].y > center.y;
                parts.AddPost(posts[i], i, back);
                // 横木は、どちらかの柱が入口・出口にかかっていればそちらと一緒に隠す
                (parts == fence ? PartsFor(next, entrance, exit, fence, entranceDoor, exitDoor, style) : parts).AddRail(posts[i], next);
                // 旗は奥の柵にだけ立てる（手前に立てると、ペンギンや大ボスの予告を隠すため）
                if (back && parts == fence && style.BannerEvery > 0 && i % style.BannerEvery == 0)
                {
                    fence.AddBanner(posts[i], i / style.BannerEvery, center);
                }
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
        public static void BuildCorridor(Transform parent, AreaShape previous, AreaShape next, ToonMaterials materials, StageTheme theme, ArenaFenceStyle style = null)
        {
            style ??= ArenaFenceStyle.Toy;
            var parts = new Parts(materials, theme, style);
            var half = Balance.ArenaDoorHalfWidth;
            var fromX = previous.Max.X - TipInset(previous, half);
            var toX = next.Min.X + TipInset(next, half);
            var count = Mathf.Max(1, Mathf.RoundToInt((toX - fromX) / style.Spacing));
            foreach (var side in new[] { -1f, 1f })
            {
                var y = next.Center.Y + half * side;
                for (var i = 0; i <= count; i++)
                {
                    var post = new Vector2(Mathf.Lerp(fromX, toX, (float)i / count), y);
                    parts.AddPost(post, i, side > 0f);
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

        private static Parts PartsFor(Vector2 post, AreaShape? entrance, AreaShape? exit, Parts fence, Parts entranceDoor, Parts exitDoor, ArenaFenceStyle style)
        {
            if (entrance.HasValue && IsInDoorway(post, entrance.Value, style))
            {
                return entranceDoor;
            }

            return exit.HasValue && IsInDoorway(post, exit.Value, style) ? exitDoor : fence;
        }

        /// <summary>楕円を ArcSamples に分けた i 番目の点。0 が左端で、上（奥）へ回る</summary>
        private static Vector2 PointAt(Vector2 center, Vector2 radii, int i)
        {
            var angle = Mathf.PI - i * Mathf.PI * 2f / ArcSamples;
            return center + new Vector2(Mathf.Cos(angle) * radii.x, Mathf.Sin(angle) * radii.y);
        }

        /// <summary>柱・柱の上の飾り・横木・旗を、種類ごとに1つのメッシュへまとめる組み立て場</summary>
        private sealed class Parts
        {
            private readonly ToonMaterials _materials;
            private readonly StageTheme _theme;
            private readonly ArenaFenceStyle _style;
            private readonly List<CombineInstance> _posts = new List<CombineInstance>();
            private readonly List<CombineInstance> _caps = new List<CombineInstance>();
            private readonly List<CombineInstance> _altCaps = new List<CombineInstance>();
            private readonly List<CombineInstance> _rails = new List<CombineInstance>();
            private readonly List<CombineInstance> _poles = new List<CombineInstance>();
            private readonly List<CombineInstance> _banners = new List<CombineInstance>();
            private readonly List<CombineInstance> _altBanners = new List<CombineInstance>();

            public Parts(ToonMaterials materials, StageTheme theme, ArenaFenceStyle style)
            {
                _materials = materials;
                _theme = theme;
                _style = style;
            }

            /// <param name="index">飾りの色を1本おきに変える（おもちゃらしく、柵の続きが目で追えるように）</param>
            /// <param name="back">奥の柱か（コロシアムでは手前の柱を奥より低くする）</param>
            public void AddPost(Vector2 point, int index, bool back)
            {
                var floor = point.ToFloor();
                var height = back ? _style.PostHeight : _style.FrontPostHeight;
                var diameter = _style.PostDiameter;
                Add(_posts, ToyMeshes.Puck, floor + Vector3.up * (height * 0.5f), Quaternion.identity, new Vector3(diameter, height, diameter));
                var caps = index % 2 == 0 ? _caps : _altCaps;
                if (_style.CapHeight > 0f)
                {
                    // 平たい石の笠。柱より一回り大きく、柱の上に乗せる
                    var cap = new Vector3(_style.CapDiameter, _style.CapHeight, _style.CapDiameter);
                    Add(caps, ToyMeshes.Puck, floor + Vector3.up * (height + _style.CapHeight * 0.5f), Quaternion.identity, cap);
                    return;
                }

                Add(caps, ToyMeshes.Sphere, floor + Vector3.up * height, Quaternion.identity, Vector3.one * _style.CapDiameter);
            }

            /// <summary>柱から次の柱へ、横に寝かせた丸い棒（石の柵は太く低い壁）を渡す</summary>
            public void AddRail(Vector2 from, Vector2 to)
            {
                var direction = (to - from).ToFloor();
                var middle = ((from + to) * 0.5f).ToFloor(_style.RailHeight);
                var rotation = Quaternion.FromToRotation(Vector3.up, direction.normalized);
                Add(_rails, ToyMeshes.Puck, middle, rotation, new Vector3(_style.RailThickness, direction.magnitude, _style.RailThickness));
            }

            /// <summary>
            /// 柱の上に細い竿を立て、先に縦長の旗を下げる。旗は闘技場のまん中へ面を向け（どの旗も面が見えるように）、色は1本おきに2色
            /// </summary>
            public void AddBanner(Vector2 point, int index, Vector2 center)
            {
                var floor = point.ToFloor();
                var poleHeight = ArenaFenceStyle.BannerPoleHeight;
                var poleDiameter = ArenaFenceStyle.BannerPoleDiameter;
                Add(_poles, ToyMeshes.Puck, floor + Vector3.up * (poleHeight * 0.5f), Quaternion.identity, new Vector3(poleDiameter, poleHeight, poleDiameter));

                var size = ArenaFenceStyle.BannerSize;
                var facing = (center - point).ToFloor();
                facing.y = 0f;
                var rotation = Quaternion.LookRotation(facing.sqrMagnitude > 0f ? facing.normalized : Vector3.back);
                // 竿の横に、上の端をそろえて下げる。Puck（丸い柱）を前へ倒して薄くつぶし、角の丸い布に見せる
                var offset = rotation * new Vector3((size.x + poleDiameter) * 0.5f, 0f, 0f);
                var position = floor + Vector3.up * (poleHeight - size.y * 0.5f) + offset;
                var cloth = rotation * Quaternion.Euler(90f, 0f, 0f);
                Add(index % 2 == 0 ? _banners : _altBanners, ToyMeshes.Puck, position, cloth, new Vector3(size.x, size.z, size.y));
            }

            public GameObject Combine(string objName, Transform parent)
            {
                var root = new GameObject(objName);
                root.transform.SetParent(parent, false);
                var gloss = _style.IsStone ? StoneGloss : Gloss;
                CombineInto(root.transform, "Rails", _rails, _style.RailColor(_theme), gloss);
                CombineInto(root.transform, "Posts", _posts, _style.PostColor(_theme), gloss);
                CombineInto(root.transform, "Caps", _caps, _style.CapColor(_theme, false), gloss);
                CombineInto(root.transform, "AltCaps", _altCaps, _style.CapColor(_theme, true), gloss);
                CombineInto(root.transform, "Poles", _poles, _style.RailColor(_theme), gloss);
                CombineInto(root.transform, "Banners", _banners, ArenaFenceStyle.BannerColor(_theme, false), Gloss);
                CombineInto(root.transform, "AltBanners", _altBanners, ArenaFenceStyle.BannerColor(_theme, true), Gloss);
                return root;
            }

            private static void Add(List<CombineInstance> list, Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale)
            {
                list.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(position, rotation, scale) });
            }

            private void CombineInto(Transform parent, string objName, List<CombineInstance> parts, Color color, float gloss)
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
                var renderer = ToyParts.AddMesh(obj, mesh, _materials.Get(color, gloss));
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }
}
