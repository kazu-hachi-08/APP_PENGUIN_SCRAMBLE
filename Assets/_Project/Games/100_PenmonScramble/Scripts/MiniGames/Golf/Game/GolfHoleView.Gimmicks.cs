using System;
using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.Game;
using PenmonScramble.Golf.Core;
using PenmonScramble.MiniGames.Game;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// 時間で動くギミックの見た目。どれもホールの時刻（GolfHoleWorld.Time）だけで決まるので、SyncGimmicks で毎フレーム写す
    /// （オンラインのゲストもホストの時刻を写すので、全員に同じ様子が見える）。
    /// 動く物（行ったり来たりする岩・回る風車）・押し流す帯（流れる「く」の字・風の筋）・間欠泉（噴く柱と、噴く前に揺れる縁）・ワープの泡
    /// </summary>
    public partial class GolfHoleView
    {
        private const float RockGloss = 0.25f;
        private const float RockCapHeight = 0.08f;

        // 風車: まん中の柱と、羽根（交互に赤と白）
        private const float HubWidth = 0.5f;
        private const float HubHeight = 1.1f;
        private const float BladeWidth = 0.16f;
        private static readonly Color BladeColor = new Color(0.93f, 0.32f, 0.28f);
        private static readonly Color BladeAltColor = new Color(0.97f, 0.97f, 0.97f);
        private static readonly Color HubColor = new Color(0.62f, 0.44f, 0.30f);

        // 押し流す帯の印: 並べる間（流れる向きと横）と、流れる速さ（押す力に掛ける）
        private const float FlowMarkSpacing = 1.3f;
        private const float FlowMarkAcross = 1.6f;
        private const float FlowScrollScale = 0.7f;
        private const float WindStreakLength = 0.9f;
        private const float WindStreakWidth = 0.05f;

        // 間欠泉: 噴く柱の高さ・太さ、噴く前に縁が揺れ始める秒
        private const float GeyserColumnHeight = 1.8f;
        private const float GeyserColumnRatio = 0.55f;
        private const float GeyserWarnSeconds = 0.8f;
        private const float GeyserWobble = 0.08f;

        // ワープの泡: 入口の泡のふわふわ揺れる幅と速さ、出口の輪
        private const float BubbleBob = 0.06f;
        private const float BubbleBobRate = 2.2f;
        private static readonly Color[] WarpColors = { new Color(0.45f, 0.95f, 1f), new Color(1f, 0.55f, 0.85f), new Color(0.75f, 1f, 0.45f) };

        private readonly List<MoverPart> _movers = new List<MoverPart>();
        private readonly List<FlowMark> _flowMarks = new List<FlowMark>();
        private readonly List<GeyserPart> _geysers = new List<GeyserPart>();
        private readonly List<Transform> _bubbles = new List<Transform>();

        /// <summary>ギミックの様子をホールの時刻に合わせる（毎フレーム）</summary>
        public void SyncGimmicks(float time)
        {
            foreach (var part in _movers)
            {
                part.Root.localPosition = GolfSpace.ToWorld(part.Mover.Center(time));
                part.Root.localRotation = YawOf(part.Mover.Angle(time));
            }

            foreach (var mark in _flowMarks)
            {
                TickFlowMark(mark, time);
            }

            foreach (var geyser in _geysers)
            {
                TickGeyser(geyser, time);
            }

            for (var i = 0; i < _bubbles.Count; i++)
            {
                var bubble = _bubbles[i];
                var p = bubble.localPosition;
                bubble.localPosition = new Vector3(p.x, GolfBalance.WarpRadius + Mathf.Sin(time * BubbleBobRate + i) * BubbleBob, p.z);
            }
        }

        /// <summary>ワープの泡に入った: 入口と出口に輪を広げる</summary>
        public void WarpAt(int warpIndex)
        {
            if (_hole == null || warpIndex < 0 || warpIndex >= _hole.Warps.Count)
            {
                return;
            }

            var warp = _hole.Warps[warpIndex];
            var color = WarpColors[warpIndex % WarpColors.Length];
            BurstAt(warp.Entry, color);
            BurstAt(warp.Exit, color);
        }

        /// <summary>間欠泉で跳んだ・止まった球を動く物の外へよけた所に、白い輪を広げる</summary>
        public void PuffAt(Vec2 point) => BurstAt(point, _scenery.Geyser);

        /// <summary>Core の向き（上から見て左回りが正）を Unity の Y 軸まわりの回転にする（Y 軸まわりは上から見て右回りが正）</summary>
        private static Quaternion YawOf(float radians) => Quaternion.Euler(0f, -radians * Mathf.Rad2Deg, 0f);

        private void BuildGimmicks(GolfHoleDef hole)
        {
            foreach (var mover in hole.Movers)
            {
                BuildMover(mover);
            }

            for (var i = 0; i < hole.Warps.Count; i++)
            {
                BuildWarp(hole.Warps[i], WarpColors[i % WarpColors.Length]);
            }

            SyncGimmicks(0f);
        }

        // ---- 動く物 ----

        private void BuildMover(GolfMover mover)
        {
            var root = new GameObject(mover.Look == GolfMoverLook.Windmill ? "Windmill" : "Rock").transform;
            root.SetParent(_course, false);
            if (mover.Look == GolfMoverLook.Windmill)
            {
                BuildWindmill(mover, root);
            }
            else
            {
                BuildRock(mover, root);
            }

            _movers.Add(new MoverPart(mover, root));
        }

        /// <summary>塊: 形の壁を囲む角の丸い箱と、上の色の帯</summary>
        private void BuildRock(GolfMover mover, Transform root)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            var height = 0f;
            foreach (var wall in mover.LocalWalls)
            {
                min = Vector2.Min(min, Vector2.Min(new Vector2(wall.A.X, wall.A.Y), new Vector2(wall.B.X, wall.B.Y)));
                max = Vector2.Max(max, Vector2.Max(new Vector2(wall.A.X, wall.A.Y), new Vector2(wall.B.X, wall.B.Y)));
                height = Mathf.Max(height, wall.Height);
            }

            var size = max - min;
            var center = (min + max) * 0.5f;
            var at = new Vector3(center.x, 0f, center.y);
            // 壁の線は球の中心が触れる所なので、見た目は少しだけ太らせる（すき間に見えないように）
            var scale = new Vector3(size.x + WallThickness, height, size.y + WallThickness);
            AddPart("Body", ToyMeshes.Puck, root, at + Vector3.up * (height * 0.5f), Quaternion.identity, scale, Materials.Get(_scenery.Rock, RockGloss));
            AddPart("Cap", ToyMeshes.Puck, root, at + Vector3.up * height, Quaternion.identity,
                new Vector3(scale.x * 0.92f, RockCapHeight, scale.z * 0.92f), Materials.Get(_scenery.RockCap, RockGloss));
        }

        /// <summary>風車: まん中の柱と、羽根（壁の線に沿った板。交互に赤と白）</summary>
        private void BuildWindmill(GolfMover mover, Transform root)
        {
            AddPart("Hub", ToyMeshes.Puck, root, Vector3.up * (HubHeight * 0.5f), Quaternion.identity,
                new Vector3(HubWidth, HubHeight, HubWidth), Materials.Get(HubColor, WallGloss));
            AddPart("HubTop", ToyMeshes.Sphere, root, Vector3.up * HubHeight, Quaternion.identity,
                Vector3.one * (HubWidth * 0.8f), Materials.Get(BladeColor, WallGloss));
            for (var i = 0; i < mover.LocalWalls.Count; i++)
            {
                var wall = mover.LocalWalls[i];
                var along = GolfSpace.ToWorld(wall.B - wall.A);
                var color = i % 2 == 0 ? BladeColor : BladeAltColor;
                AddPart("Blade", ToyMeshes.Puck, root, GolfSpace.ToWorld((wall.A + wall.B) * 0.5f, wall.Height * 0.5f), Quaternion.LookRotation(along),
                    new Vector3(BladeWidth, wall.Height, along.magnitude + BladeWidth), Materials.Get(color, WallGloss));
            }
        }

        // ---- 押し流す帯 ----

        /// <summary>平らな帯（景色の色）に、流れる向きへ動く印を並べる（海流は「く」の字、風は細い筋）</summary>
        private void BuildFlow(GolfArea area, float lift, Func<Vec2, Vector2> uv)
        {
            AddOwnedMesh("Flow", MiniGameMeshes.Flat("GolfFlow", area.Shape, lift, uv), Materials.Floor(Texture2D.whiteTexture, _scenery.Flow));
            var along = area.Push.Normalized;
            var across = new Vec2(-along.Y, along.X);
            var (min, max) = Bounds(area.Shape);
            var center = new Vec2((min.x + max.x) * 0.5f, (min.y + max.y) * 0.5f);
            var reach = (max - min).magnitude * 0.5f + FlowMarkSpacing;
            var material = Materials.Floor(Texture2D.whiteTexture, _scenery.FlowMark);
            var row = 0;
            for (var a = -reach; a <= reach; a += FlowMarkAcross, row++)
            {
                // 列ごとに半分ずらして、印が格子に見えないように
                var stagger = row % 2 == 0 ? 0f : FlowMarkSpacing * 0.5f;
                for (var b = -reach; b <= reach; b += FlowMarkSpacing)
                {
                    var origin = center + across * a + along * (b + stagger);
                    _flowMarks.Add(new FlowMark(area, BuildFlowMark(along, material), origin, along, lift + AreaLift));
                }
            }
        }

        private Transform BuildFlowMark(Vec2 along, Material material)
        {
            var root = new GameObject("FlowMark").transform;
            root.SetParent(_course, false);
            root.localRotation = Quaternion.LookRotation(GolfSpace.ToWorld(along));
            if (_scenery.WindFlow)
            {
                AddPart("Streak", ToyMeshes.FloorQuad, root, Vector3.zero, Quaternion.identity, new Vector3(WindStreakWidth, 1f, WindStreakLength), material);
                return root;
            }

            foreach (var side in new[] { -1f, 1f })
            {
                var rotation = Quaternion.Euler(0f, 180f + side * ChevronSpread, 0f);
                AddPart("Chevron", ToyMeshes.FloorQuad, root, rotation * Vector3.forward * (ChevronArm * 0.5f), rotation,
                    new Vector3(ChevronWidth, 1f, ChevronArm), material);
            }

            return root;
        }

        private static void TickFlowMark(FlowMark mark, float time)
        {
            var speed = mark.Area.Push.Length * FlowScrollScale;
            var offset = Mathf.Repeat(time * speed, FlowMarkSpacing);
            var at = mark.Origin + mark.Along * offset;
            var inside = mark.Area.Contains(at);
            mark.Root.gameObject.SetActive(inside);
            if (inside)
            {
                mark.Root.localPosition = GolfSpace.ToWorld(at, mark.Height);
            }
        }

        // ---- 間欠泉 ----

        /// <summary>暗い噴き出し口と縁の輪、噴いている間だけ伸びる白い柱</summary>
        private void BuildGeyser(GolfArea area, float lift, Func<Vec2, Vector2> uv)
        {
            AddOwnedMesh("Geyser", MiniGameMeshes.Flat("GolfGeyser", area.Shape, lift, uv), Materials.Floor(Texture2D.whiteTexture, _scenery.Vent));
            var (min, max) = Bounds(area.Shape);
            var size = Mathf.Min(max.x - min.x, max.y - min.y);
            var center = GolfSpace.ToWorld(Centroid(area.Shape), lift);
            var ring = AddPart("GeyserRing", ToyMeshes.Torus, _course, center, Quaternion.identity, Vector3.one * size, Materials.Get(_scenery.Rock, RockGloss));
            var column = AddPart("GeyserColumn", ToyMeshes.Puck, _course, center, Quaternion.identity, Vector3.one, Materials.Get(_scenery.Geyser, 0.6f));
            _geysers.Add(new GeyserPart(area, ring, column, center, size));
        }

        private static void TickGeyser(GeyserPart geyser, float time)
        {
            var cycle = geyser.Area.Cycle;
            var active = cycle.IsActive(time);
            geyser.Column.gameObject.SetActive(active);
            var until = cycle.UntilActive(time);
            // 噴く前は縁が小刻みに揺れる（いつ噴くか読めるように）
            var warn = !active && until < GeyserWarnSeconds ? Mathf.Sin(time * 40f) * GeyserWobble * (1f - until / GeyserWarnSeconds) : 0f;
            geyser.Ring.localScale = Vector3.one * (geyser.Size * (1f + warn));
            if (!active)
            {
                return;
            }

            // 噴き始めに素早く伸び、終わりに縮む
            var t = cycle.Local(time) / Mathf.Max(0.01f, cycle.Active);
            var rise = Mathf.Clamp01(Mathf.Min(t / 0.15f, (1f - t) / 0.2f));
            var height = GeyserColumnHeight * rise;
            var width = geyser.Size * GeyserColumnRatio * (1f + Mathf.Sin(time * 25f) * GeyserWobble);
            geyser.Column.localScale = new Vector3(width, Mathf.Max(0.01f, height), width);
            geyser.Column.localPosition = geyser.Center + Vector3.up * (height * 0.5f);
        }

        // ---- ワープの泡 ----

        /// <summary>入口は透きとおった泡（ふわふわ揺れる）と色の輪、出口は同じ色の輪</summary>
        private void BuildWarp(GolfWarp warp, Color color)
        {
            var diameter = GolfBalance.WarpRadius * 2f;
            var bubble = AddPart("WarpBubble", ToyMeshes.Sphere, _course, GolfSpace.ToWorld(warp.Entry, GolfBalance.WarpRadius), Quaternion.identity,
                Vector3.one * diameter, Materials.Ice);
            _bubbles.Add(bubble);
            var ringMaterial = Materials.Get(color, 0.6f);
            AddPart("WarpRing", ToyMeshes.Torus, _course, GroundPoint(warp.Entry) + Vector3.up * AreaLift * 3f, Quaternion.identity, Vector3.one * diameter, ringMaterial);
            AddPart("WarpExit", ToyMeshes.Torus, _course, GroundPoint(warp.Exit) + Vector3.up * AreaLift * 3f, Quaternion.identity, Vector3.one * (diameter * 0.8f), ringMaterial);
        }

        private void ClearGimmicks()
        {
            _movers.Clear();
            _flowMarks.Clear();
            _geysers.Clear();
            _bubbles.Clear();
        }

        private sealed class MoverPart
        {
            public readonly GolfMover Mover;
            public readonly Transform Root;

            public MoverPart(GolfMover mover, Transform root)
            {
                Mover = mover;
                Root = root;
            }
        }

        private sealed class FlowMark
        {
            public readonly GolfArea Area;
            public readonly Transform Root;
            public readonly Vec2 Origin;
            public readonly Vec2 Along;
            public readonly float Height;

            public FlowMark(GolfArea area, Transform root, Vec2 origin, Vec2 along, float height)
            {
                Area = area;
                Root = root;
                Origin = origin;
                Along = along;
                Height = height;
            }
        }

        private sealed class GeyserPart
        {
            public readonly GolfArea Area;
            public readonly Transform Ring;
            public readonly Transform Column;
            public readonly Vector3 Center;
            public readonly float Size;

            public GeyserPart(GolfArea area, Transform ring, Transform column, Vector3 center, float size)
            {
                Area = area;
                Ring = ring;
                Column = column;
                Center = center;
                Size = size;
            }
        }
    }
}
