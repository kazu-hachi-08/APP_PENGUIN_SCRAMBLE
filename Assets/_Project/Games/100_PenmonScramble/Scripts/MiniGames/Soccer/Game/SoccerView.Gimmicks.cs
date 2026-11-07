using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// スタジアムの仕掛けの見た目: 氷の床（白い縁の水色の板）・押し流す帯（水流は青い帯と流れる「く」の字、風は流れる白い筋）・
    /// バンパーの柱（弾むと膨らむ）・間欠泉（暗い口と岩の縁、噴いている間だけ伸びる白い柱、噴く前に縁が揺れる）。
    /// 柵のバンパーは .Boards.cs。様子はピッチの時刻（SoccerWorld.Time）だけで決まる。
    /// ゲストの時刻は 1/15 秒ごとにしか届かないので、手元の時計を進めて、届いた時刻から少しだけずれるのを許す（帯の印がカクつかないように）
    /// </summary>
    public partial class SoccerView
    {
        // 手元の時計が、届いた時刻から離れてよい秒（ゲストの写しの間隔より少し長く）
        private const float ClockSlack = 0.1f;

        // 仕掛けの板の浮かせ方（マットとちらつかず、白い線より下に）
        private const float GimmickLift = 0.003f;
        private const float GimmickMarkLift = 0.008f;

        private static readonly Color IceFloor = new Color(0.80f, 0.94f, 1f);
        private static readonly Color IceRim = new Color(0.95f, 0.99f, 1f);
        private const float IceRimWidth = 0.12f;

        // 押し流す帯: 水流の帯の色と印、風の筋。印の並べ方と流れる速さ（押す強さに掛ける）
        private static readonly Color CurrentFloor = new Color(0.30f, 0.58f, 0.92f);
        private static readonly Color CurrentMark = new Color(0.82f, 0.94f, 1f);
        private static readonly Color WindMark = new Color(1f, 1f, 1f);
        private const float FlowMarkSpacing = 1.5f;
        private const float FlowMarkAcross = 1.4f;
        private const float FlowScrollScale = 0.35f;
        private const float ChevronArm = 0.42f;
        private const float ChevronWidth = 0.09f;
        private const float ChevronSpread = 40f;
        private const float WindStreakLength = 1f;
        private const float WindStreakWidth = 0.05f;
        // 風が止んでいる間は筋を短く縮める（吹いていないのが分かるように）
        private const float WindCalmScale = 0.15f;
        private const float WindScaleRate = 6f;

        // バンパーの柱: 本体（黄）と上の帯（赤）、弾むときの膨らみ
        private static readonly Color BumperBody = new Color(1f, 0.82f, 0.22f);
        private static readonly Color BumperCap = new Color(0.93f, 0.26f, 0.24f);
        private const float BumperGloss = 0.6f;
        private const float PostHeight = 0.8f;
        private const float PostCapHeight = 0.12f;
        private const float PostPopSeconds = 0.25f;
        private const float PostPopScale = 0.3f;
        private const float PostPopReach = 0.4f;

        // 間欠泉: 口・縁・噴く柱
        private static readonly Color VentColor = new Color(0.20f, 0.16f, 0.16f);
        private static readonly Color VentRock = new Color(0.30f, 0.25f, 0.24f);
        private static readonly Color VentGlow = new Color(1f, 0.48f, 0.16f);
        private static readonly Color SteamColor = new Color(0.96f, 0.94f, 0.90f);
        private const float VentRingHeight = 0.6f;
        private const float GeyserColumnHeight = 2.6f;
        private const float GeyserColumnRatio = 0.7f;
        private const float GeyserWarnSeconds = 0.9f;
        private const float GeyserWobble = 0.08f;
        private const float GeyserWobbleRate = 40f;
        private const float GeyserRiseShare = 0.15f;
        private const float GeyserFallShare = 0.2f;

        private readonly List<FlowMark> _flowMarks = new List<FlowMark>();
        private readonly List<PostPart> _posts = new List<PostPart>();
        private readonly List<GeyserPart> _geysers = new List<GeyserPart>();
        private float _gimmickClock;

        /// <summary>
        /// 仕掛けを静かにする（間欠泉は噴かず、帯の印は止まる）。PK戦と試合の後はピッチの時刻が止まるので、
        /// 噴いている途中の柱が立ったまま残らないように
        /// </summary>
        public bool QuietGimmicks { get; set; }

        private void BuildGimmicks(SoccerWorld world)
        {
            var pitch = world.Pitch;
            foreach (var zone in pitch.Grounds)
            {
                if (zone.Ground == SoccerGround.Ice)
                {
                    BuildIce(zone);
                }
            }

            foreach (var flow in pitch.Flows)
            {
                BuildFlow(flow);
            }

            foreach (var post in pitch.Posts)
            {
                BuildPost(post);
            }

            foreach (var geyser in pitch.Geysers)
            {
                BuildGeyser(geyser);
            }

            _gimmickClock = world.Time;
            SyncGimmicks(world.Time, 0f);
        }

        /// <summary>仕掛けの様子をピッチの時刻に合わせる（毎フレーム）</summary>
        private void SyncGimmicks(float worldTime, float deltaTime)
        {
            _gimmickClock = Mathf.Clamp(_gimmickClock + deltaTime, worldTime - ClockSlack, worldTime + ClockSlack);
            foreach (var mark in _flowMarks)
            {
                TickFlowMark(mark, _gimmickClock, deltaTime, QuietGimmicks);
            }

            foreach (var post in _posts)
            {
                TickPost(post, deltaTime);
            }

            foreach (var geyser in _geysers)
            {
                TickGeyser(geyser, _gimmickClock, QuietGimmicks);
            }
        }

        private void ClearGimmicks()
        {
            _flowMarks.Clear();
            _posts.Clear();
            _geysers.Clear();
        }

        private static Vec2[] RectOf(Vec2 min, Vec2 max) => MiniGameGeometry.Rect(min.X, min.Y, max.X, max.Y);

        // ---- 氷の床 ----

        private void BuildIce(SoccerGroundZone zone)
        {
            AddOwnedMesh("Ice", MiniGameMeshes.Flat("SoccerIce", RectOf(zone.Min, zone.Max), GimmickLift, _ => Vector2.zero),
                Materials.Floor(Texture2D.whiteTexture, IceFloor));
            var rim = Materials.Floor(Texture2D.whiteTexture, IceRim);
            var corners = RectOf(zone.Min, zone.Max);
            for (var i = 0; i < corners.Length; i++)
            {
                FlatStrip(corners[i], corners[(i + 1) % corners.Length], IceRimWidth, GimmickLift * 2f, rim);
            }
        }

        /// <summary>a から b への平たい帯（線より太さを選べる）</summary>
        private void FlatStrip(Vec2 a, Vec2 b, float width, float lift, Material material)
        {
            var along = SoccerSpace.Direction(b - a);
            AddPart("Strip", ToyMeshes.FloorQuad, _pitch, SoccerSpace.ToWorld((a + b) * 0.5f, lift), Quaternion.LookRotation(along),
                new Vector3(width, 1f, along.magnitude + width), material);
        }

        // ---- 押し流す帯 ----

        /// <summary>水流は青い帯を敷いて「く」の字の印、風は帯を敷かずに白い筋を並べる（芝が見えたまま風だと分かるように）</summary>
        private void BuildFlow(SoccerFlowZone flow)
        {
            var isWind = flow.Kind == SoccerFlowKind.Wind;
            if (!isWind)
            {
                AddOwnedMesh("Current", MiniGameMeshes.Flat("SoccerCurrent", RectOf(flow.Min, flow.Max), GimmickLift, _ => Vector2.zero),
                    Materials.Floor(Texture2D.whiteTexture, CurrentFloor));
            }

            var along = flow.Direction;
            var across = new Vec2(-along.Y, along.X);
            var reach = (flow.Max - flow.Min).Length * 0.5f + FlowMarkSpacing;
            var material = Materials.Floor(Texture2D.whiteTexture, isWind ? WindMark : CurrentMark);
            var row = 0;
            for (var a = -reach; a <= reach; a += FlowMarkAcross, row++)
            {
                // 列ごとに半分ずらして、印が格子に見えないように
                var stagger = row % 2 == 0 ? 0f : FlowMarkSpacing * 0.5f;
                for (var b = -reach; b <= reach; b += FlowMarkSpacing)
                {
                    var origin = flow.Center + across * a + along * (b + stagger);
                    _flowMarks.Add(new FlowMark(flow, BuildFlowMark(isWind, material), origin));
                }
            }
        }

        private Transform BuildFlowMark(bool isWind, Material material)
        {
            var root = new GameObject("FlowMark").transform;
            root.SetParent(_pitch, false);
            if (isWind)
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

        /// <summary>印を押す向きへ流す。向きが替わったら印も向きを替え、風が止んでいる間は筋を縮める</summary>
        private static void TickFlowMark(FlowMark mark, float time, float deltaTime, bool quiet)
        {
            var flow = mark.Flow;
            var direction = quiet ? Vec2.Zero : flow.DirectionAt(time);
            var blowing = !direction.IsNearlyZero;
            if (blowing)
            {
                var sign = Vec2.Dot(direction, flow.Direction) >= 0f ? 1f : -1f;
                mark.Travel += sign * flow.Accel * FlowScrollScale * deltaTime;
                mark.Facing = sign;
            }

            var target = blowing ? 1f : WindCalmScale;
            mark.Scale = Mathf.MoveTowards(mark.Scale, target, WindScaleRate * deltaTime);
            var at = mark.Origin + flow.Direction * Mathf.Repeat(mark.Travel, FlowMarkSpacing);
            var inside = flow.Contains(at);
            mark.Root.gameObject.SetActive(inside);
            if (!inside)
            {
                return;
            }

            mark.Root.localPosition = SoccerSpace.ToWorld(at, GimmickMarkLift);
            mark.Root.localRotation = Quaternion.LookRotation(SoccerSpace.Direction(flow.Direction * mark.Facing));
            mark.Root.localScale = new Vector3(1f, 1f, mark.Scale);
        }

        // ---- バンパーの柱 ----

        private void BuildPost(SoccerPost post)
        {
            var root = new GameObject("BumperPost").transform;
            root.SetParent(_pitch, false);
            root.localPosition = SoccerSpace.ToWorld(post.Center);
            var diameter = post.Radius * 2f;
            AddPart("Body", ToyMeshes.Puck, root, Vector3.up * (PostHeight * 0.5f), Quaternion.identity,
                new Vector3(diameter, PostHeight, diameter), Materials.Get(BumperBody, BumperGloss));
            AddPart("Cap", ToyMeshes.Puck, root, Vector3.up * PostHeight, Quaternion.identity,
                new Vector3(diameter * 1.1f, PostCapHeight, diameter * 1.1f), Materials.Get(BumperCap, BumperGloss));
            AddPart("Ring", ToyMeshes.Torus, root, Vector3.up * (PostHeight * 0.5f), Quaternion.identity,
                Vector3.one * (diameter * 1.05f), Materials.Get(BumperCap, BumperGloss));
            _posts.Add(new PostPart(post, root));
        }

        /// <summary>弾いた所の近くの柱を膨らませる</summary>
        private void PopPostNear(Vec2 point)
        {
            foreach (var part in _posts)
            {
                if (Vec2.Distance(part.Post.Center, point) <= part.Post.Radius + SoccerBalance.BallRadius + PostPopReach)
                {
                    part.PopLeft = PostPopSeconds;
                }
            }
        }

        private static void TickPost(PostPart part, float deltaTime)
        {
            if (part.PopLeft <= 0f)
            {
                return;
            }

            part.PopLeft = Mathf.Max(0f, part.PopLeft - deltaTime);
            var swell = Mathf.Sin((1f - part.PopLeft / PostPopSeconds) * Mathf.PI) * PostPopScale;
            part.Root.localScale = new Vector3(1f + swell, 1f - swell * 0.3f, 1f + swell);
        }

        // ---- 間欠泉 ----

        /// <summary>暗い口（溶岩の光る縁）と岩の縁、噴いている間だけ伸びる白い柱</summary>
        private void BuildGeyser(SoccerGeyser geyser)
        {
            var center = SoccerSpace.ToWorld(geyser.Center, GimmickLift);
            var diameter = geyser.Radius * 2f;
            AddPart("VentGlow", ToyMeshes.FloorDisc, _pitch, center, Quaternion.identity, new Vector3(diameter, 1f, diameter),
                Materials.Floor(Texture2D.whiteTexture, VentGlow));
            AddPart("Vent", ToyMeshes.FloorDisc, _pitch, center + Vector3.up * GimmickLift, Quaternion.identity,
                new Vector3(diameter * 0.8f, 1f, diameter * 0.8f), Materials.Floor(Texture2D.whiteTexture, VentColor));
            var ring = AddPart("VentRing", ToyMeshes.Torus, _pitch, center, Quaternion.identity,
                new Vector3(diameter, VentRingHeight, diameter), Materials.Get(VentRock, 0.25f));
            var column = AddPart("Steam", ToyMeshes.Puck, _pitch, center, Quaternion.identity, Vector3.one, Materials.Get(SteamColor, 0.6f));
            _geysers.Add(new GeyserPart(geyser, ring, column, center, diameter));
        }

        private static void TickGeyser(GeyserPart part, float time, bool quiet)
        {
            var cycle = part.Geyser.Cycle;
            var active = !quiet && cycle.IsActive(time);
            part.Column.gameObject.SetActive(active);
            var until = quiet ? float.MaxValue : cycle.UntilActive(time);
            // 噴く前は縁が小刻みに揺れる（いつ噴くか読めるように）
            var warn = !active && until < GeyserWarnSeconds
                ? Mathf.Sin(time * GeyserWobbleRate) * GeyserWobble * (1f - until / GeyserWarnSeconds)
                : 0f;
            part.Ring.localScale = new Vector3(part.Size * (1f + warn), VentRingHeight, part.Size * (1f + warn));
            if (!active)
            {
                return;
            }

            // 噴き始めに素早く伸び、終わりに縮む
            var t = cycle.Local(time) / Mathf.Max(0.01f, cycle.Active);
            var rise = Mathf.Clamp01(Mathf.Min(t / GeyserRiseShare, (1f - t) / GeyserFallShare));
            var height = Mathf.Max(0.01f, GeyserColumnHeight * rise);
            var width = part.Size * GeyserColumnRatio * (1f + Mathf.Sin(time * GeyserWobbleRate * 0.6f) * GeyserWobble);
            part.Column.localScale = new Vector3(width, height, width);
            part.Column.localPosition = part.Center + Vector3.up * (height * 0.5f);
        }

        private sealed class FlowMark
        {
            public readonly SoccerFlowZone Flow;
            public readonly Transform Root;
            public readonly Vec2 Origin;
            public float Travel;
            public float Facing = 1f;
            public float Scale = 1f;

            public FlowMark(SoccerFlowZone flow, Transform root, Vec2 origin)
            {
                Flow = flow;
                Root = root;
                Origin = origin;
            }
        }

        private sealed class PostPart
        {
            public readonly SoccerPost Post;
            public readonly Transform Root;
            public float PopLeft;

            public PostPart(SoccerPost post, Transform root)
            {
                Post = post;
                Root = root;
            }
        }

        private sealed class GeyserPart
        {
            public readonly SoccerGeyser Geyser;
            public readonly Transform Ring;
            public readonly Transform Column;
            public readonly Vector3 Center;
            public readonly float Size;

            public GeyserPart(SoccerGeyser geyser, Transform ring, Transform column, Vector3 center, float size)
            {
                Geyser = geyser;
                Ring = ring;
                Column = column;
                Center = center;
                Size = size;
            }
        }
    }
}
