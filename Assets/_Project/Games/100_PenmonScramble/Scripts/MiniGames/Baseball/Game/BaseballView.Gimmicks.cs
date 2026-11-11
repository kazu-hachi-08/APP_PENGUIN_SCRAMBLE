using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Core;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 球場の仕掛けの見た目（様子は盤の時計だけで決まる。サッカーの仕掛けの見た目と同じ考え方）:
    /// 氷の床は外野の芝と帯を氷の色にする（.Board.cs の BuildTurf）。水流は青い弧の帯と流れる「く」の字の印、
    /// 風はセンターのフェンスの上の旗（吹く向きへなびき、止んでいる間は次に吹く向きへ垂れる）と、空を流れる白い筋。
    /// 間欠泉は暗い口と溶岩の光る縁・岩の縁、噴いている間だけ伸びる白い柱、噴く前に縁が揺れる。動く穴は .Holes.cs
    /// </summary>
    public partial class BaseballView
    {
        private const float GimmickLift = 0.0335f;
        private const float GimmickMarkLift = 0.036f;

        // 氷の床（外野の芝・フェンス際の帯を氷の色に）
        private static readonly Color IceStripeLight = new Color(0.84f, 0.95f, 1f);
        private static readonly Color IceStripeDark = new Color(0.74f, 0.89f, 0.98f);
        private static readonly Color IceBandColor = new Color(0.60f, 0.76f, 0.90f);

        // 水流の帯と印
        private static readonly Color CurrentFloor = new Color(0.30f, 0.58f, 0.92f);
        private static readonly Color CurrentMark = new Color(0.82f, 0.94f, 1f);
        private const float CurrentMarkDegrees = 9f;
        private const float CurrentScrollScale = 1f;
        private const float ChevronArm = 0.3f;
        private const float ChevronWidth = 0.07f;
        private const float ChevronSpread = 40f;

        // 風: 旗（センターのフェンスの後ろの柱の上）と空を流れる筋
        private static readonly Color FlagPoleColor = new Color(0.85f, 0.85f, 0.88f);
        private static readonly Color FlagColor = new Color(0.95f, 0.30f, 0.26f);
        private const float FlagPoleHeight = 2.6f;
        private const float FlagPoleDiameter = 0.1f;
        private const float FlagLength = 0.9f;
        private const float FlagWidth = 0.45f;
        private const float FlagTurnRate = 6f;
        private const float FlagFlutter = 8f;
        private const float FlagFlutterRate = 14f;
        private const float FlagDroop = 70f;
        private const int WindStreaks = 14;
        private const float WindStreakLength = 1.2f;
        private const float WindStreakWidth = 0.05f;
        private const float WindStreakHeight = 1.3f;
        private const float WindStreakSpeed = 6f;
        private const float WindStreakSpan = 22f;
        private const float WindScaleRate = 5f;

        // 間欠泉: 口・縁・噴く柱
        private static readonly Color VentColor = new Color(0.20f, 0.16f, 0.16f);
        private static readonly Color VentRock = new Color(0.30f, 0.25f, 0.24f);
        private static readonly Color VentGlow = new Color(1f, 0.48f, 0.16f);
        private static readonly Color SteamColor = new Color(0.96f, 0.94f, 0.90f);
        private const float VentRingHeight = 0.6f;
        private const float GeyserColumnHeight = 1.6f;
        private const float GeyserColumnRatio = 0.7f;
        private const float GeyserWarnSeconds = 0.8f;
        private const float GeyserWobble = 0.08f;
        private const float GeyserWobbleRate = 40f;
        private const float GeyserRiseShare = 0.15f;
        private const float GeyserFallShare = 0.2f;

        private readonly List<CurrentMarkPart> _currentMarks = new List<CurrentMarkPart>();
        private readonly List<GeyserPart> _geysers = new List<GeyserPart>();
        private readonly List<Transform> _windStreaks = new List<Transform>();
        private Transform _flag;
        private float _flagYaw;
        private float _windScale;
        private float _windTravel;

        private void BuildGimmicks(BaseballFieldDef field)
        {
            var gimmicks = field.Gimmicks;
            foreach (var current in gimmicks.Currents)
            {
                BuildCurrent(current);
            }

            if (gimmicks.Wind != null)
            {
                BuildWind(field);
            }

            foreach (var geyser in gimmicks.Geysers)
            {
                BuildGeyser(geyser);
            }
        }

        /// <summary>仕掛けの様子を盤の時計に合わせる（毎フレーム）</summary>
        private void TickGimmicks(float clock, float deltaTime)
        {
            foreach (var mark in _currentMarks)
            {
                TickCurrentMark(mark, clock, deltaTime);
            }

            TickWind(clock, deltaTime);
            foreach (var geyser in _geysers)
            {
                TickGeyser(geyser, clock);
            }
        }

        private void ClearGimmicks()
        {
            _currentMarks.Clear();
            _geysers.Clear();
            _windStreaks.Clear();
            _flag = null;
        }

        /// <summary>止まった場所の札の地面が氷の床か（帯の真ん中の距離で見る）</summary>
        private static bool IsIcyRing(BaseballFieldDef field, float inner, float outer)
        {
            var middle = (inner + outer) * 0.5f;
            return field.Gimmicks.IsOnIce(BaseballFieldDef.At(0f, middle));
        }

        // ---- 水流 ----

        private void BuildCurrent(BaseballCurrent current)
        {
            var band = current.Band;
            AddOwnedMesh("Current", MiniGameMeshes.Flat("BaseballCurrent", ArcBand(band.InnerRadius, band.OuterRadius, band.HalfDegrees), GimmickLift, Uv),
                Materials.Floor(Texture2D.whiteTexture, CurrentFloor));
            var material = Materials.Floor(Texture2D.whiteTexture, CurrentMark);
            var radius = (band.InnerRadius + band.OuterRadius) * 0.5f;
            for (var degrees = -band.HalfDegrees; degrees <= band.HalfDegrees; degrees += CurrentMarkDegrees)
            {
                _currentMarks.Add(new CurrentMarkPart(current, BuildChevron(material), radius, degrees));
            }
        }

        private Transform BuildChevron(Material material)
        {
            var root = new GameObject("CurrentMark").transform;
            root.SetParent(_root, false);
            foreach (var side in new[] { -1f, 1f })
            {
                var rotation = Quaternion.Euler(0f, 180f + side * ChevronSpread, 0f);
                AddPart("Chevron", ToyMeshes.FloorQuad, root, rotation * Vector3.forward * (ChevronArm * 0.5f), rotation,
                    new Vector3(ChevronWidth, 1f, ChevronArm), material);
            }

            return root;
        }

        /// <summary>印を流れる向きへ弧に沿って動かす（向きが替わったら印も向きを替える）。帯の端を越えた印は反対の端から出す</summary>
        private static void TickCurrentMark(CurrentMarkPart mark, float clock, float deltaTime)
        {
            var current = mark.Current;
            var side = current.SideAt(clock);
            var degreesPerSecond = current.MaxSpeed * CurrentScrollScale / mark.Radius * Mathf.Rad2Deg;
            mark.Travel += side * degreesPerSecond * deltaTime;
            var span = current.Band.HalfDegrees * 2f;
            var degrees = Mathf.Repeat(mark.StartDegrees + current.Band.HalfDegrees + mark.Travel, span) - current.Band.HalfDegrees;
            var at = BaseballFieldDef.At(degrees, mark.Radius);
            mark.Root.localPosition = BaseballSpace.ToWorld(at, GimmickMarkLift);
            var direction = current.DirectionAt(at, clock);
            mark.Root.localRotation = Quaternion.LookRotation(BaseballSpace.Direction(direction));
        }

        // ---- 風 ----

        /// <summary>センターのフェンスの後ろの旗と、空を流れる白い筋</summary>
        private void BuildWind(BaseballFieldDef field)
        {
            var at = BaseballSpace.ToWorld(BaseballFieldDef.At(0f, field.FenceRadius + FenceThickness + FlagPoleDiameter));
            AddPole("FlagPole", at, FlagPoleDiameter, FlagPoleHeight, Materials.Get(FlagPoleColor, 0.5f));
            _flag = new GameObject("Flag").transform;
            _flag.SetParent(_root, false);
            _flag.localPosition = at + Vector3.up * (FlagPoleHeight - FlagWidth * 0.5f);
            // 旗の布は縦に立てた板（根元が柱、先が風下）
            // 板は片面なので、裏返した板を重ねてどちらから見えても見えるようにする
            foreach (var roll in new[] { 90f, -90f })
            {
                AddPart("Cloth", ToyMeshes.FloorQuad, _flag, new Vector3(0f, 0f, FlagLength * 0.5f), Quaternion.Euler(0f, 0f, roll),
                    new Vector3(FlagWidth, 1f, FlagLength), Materials.Get(FlagColor, 0.2f));
            }

            var streak = Materials.Floor(Texture2D.whiteTexture, Color.white);
            var random = new System.Random(field.Name.Length);
            for (var i = 0; i < WindStreaks; i++)
            {
                var depth = 3f + (float)random.NextDouble() * (field.FenceRadius - 2f);
                var x = ((float)random.NextDouble() - 0.5f) * WindStreakSpan;
                var height = WindStreakHeight + (float)random.NextDouble() * 0.8f;
                var part = AddPart("WindStreak", ToyMeshes.FloorQuad, _root, new Vector3(x, height, depth), Quaternion.Euler(0f, 90f, 0f),
                    new Vector3(WindStreakWidth, 1f, WindStreakLength), streak);
                _windStreaks.Add(part);
            }
        }

        /// <summary>
        /// 旗は吹いている向きへなびいて小刻みに揺れ、止んでいる間は次に吹く向きへ垂れる（どちらから吹くか、投げる前に読めるように）。
        /// 筋は吹いている間だけ伸びて流れる
        /// </summary>
        private void TickWind(float clock, float deltaTime)
        {
            if (_flag == null)
            {
                return;
            }

            var wind = _field.Gimmicks.Wind;
            var side = wind.SideAt(clock);
            var blowing = side != 0f;
            var facing = blowing ? side : wind.NextSideAt(clock);
            var yaw = facing > 0f ? 90f : -90f;
            _flagYaw = Mathf.LerpAngle(_flagYaw, yaw, 1f - Mathf.Exp(-FlagTurnRate * deltaTime));
            var flutter = blowing ? Mathf.Sin(Time.time * FlagFlutterRate) * FlagFlutter : 0f;
            var droop = blowing ? 0f : FlagDroop;
            _flag.localRotation = Quaternion.Euler(droop, _flagYaw + flutter, 0f);

            _windScale = Mathf.MoveTowards(_windScale, blowing ? 1f : 0f, WindScaleRate * deltaTime);
            _windTravel += side * WindStreakSpeed * deltaTime;
            for (var i = 0; i < _windStreaks.Count; i++)
            {
                var streak = _windStreaks[i];
                var visible = _windScale > 0.01f;
                streak.gameObject.SetActive(visible);
                if (!visible)
                {
                    continue;
                }

                var start = (i * 7.3f) % WindStreakSpan;
                var position = streak.localPosition;
                position.x = Mathf.Repeat(start + _windTravel, WindStreakSpan) - WindStreakSpan * 0.5f;
                streak.localPosition = position;
                streak.localScale = new Vector3(WindStreakWidth, 1f, WindStreakLength * _windScale);
            }
        }

        // ---- 間欠泉 ----

        private void BuildGeyser(BaseballGeyser geyser)
        {
            var center = BaseballSpace.ToWorld(geyser.Center, GimmickLift);
            var diameter = geyser.Radius * 2f;
            AddPart("VentGlow", ToyMeshes.FloorDisc, _root, center, Quaternion.identity, new Vector3(diameter, 1f, diameter),
                Materials.Floor(Texture2D.whiteTexture, VentGlow));
            AddPart("Vent", ToyMeshes.FloorDisc, _root, center + Vector3.up * LayerLift, Quaternion.identity,
                new Vector3(diameter * 0.8f, 1f, diameter * 0.8f), Materials.Floor(Texture2D.whiteTexture, VentColor));
            var ring = AddPart("VentRing", ToyMeshes.Torus, _root, center, Quaternion.identity,
                new Vector3(diameter, VentRingHeight, diameter), Materials.Get(VentRock, 0.25f));
            var column = AddPart("Steam", ToyMeshes.Puck, _root, center, Quaternion.identity, Vector3.one, Materials.Get(SteamColor, 0.6f));
            _geysers.Add(new GeyserPart(geyser, ring, column, center, diameter));
        }

        private static void TickGeyser(GeyserPart part, float clock)
        {
            var cycle = part.Geyser.Cycle;
            var active = cycle.IsActive(clock);
            part.Column.gameObject.SetActive(active);
            var until = cycle.UntilActive(clock);
            // 噴く前は縁が小刻みに揺れる（いつ噴くか読めるように）
            var warn = !active && until < GeyserWarnSeconds
                ? Mathf.Sin(clock * GeyserWobbleRate) * GeyserWobble * (1f - until / GeyserWarnSeconds)
                : 0f;
            part.Ring.localScale = new Vector3(part.Size * (1f + warn), VentRingHeight, part.Size * (1f + warn));
            if (!active)
            {
                return;
            }

            // 噴き始めに素早く伸び、終わりに縮む
            var t = cycle.Local(clock) / Mathf.Max(0.01f, cycle.Active);
            var rise = Mathf.Clamp01(Mathf.Min(t / GeyserRiseShare, (1f - t) / GeyserFallShare));
            var height = Mathf.Max(0.01f, GeyserColumnHeight * rise);
            var width = part.Size * GeyserColumnRatio * (1f + Mathf.Sin(clock * GeyserWobbleRate * 0.6f) * GeyserWobble);
            part.Column.localScale = new Vector3(width, height, width);
            part.Column.localPosition = part.Center + Vector3.up * (height * 0.5f);
        }

        /// <summary>半径 inner から outer まで、センターから左右 halfDegrees までの弧の帯（左回り）</summary>
        private static List<Vec2> ArcBand(float inner, float outer, float halfDegrees)
        {
            var points = new List<Vec2>();
            AddArc(points, halfDegrees, -halfDegrees, outer);
            AddArc(points, -halfDegrees, halfDegrees, inner);
            return points;
        }

        private sealed class CurrentMarkPart
        {
            public CurrentMarkPart(BaseballCurrent current, Transform root, float radius, float startDegrees)
            {
                Current = current;
                Root = root;
                Radius = radius;
                StartDegrees = startDegrees;
            }

            public BaseballCurrent Current { get; }
            public Transform Root { get; }
            public float Radius { get; }
            public float StartDegrees { get; }
            public float Travel { get; set; }
        }

        private sealed class GeyserPart
        {
            public GeyserPart(BaseballGeyser geyser, Transform ring, Transform column, Vector3 center, float size)
            {
                Geyser = geyser;
                Ring = ring;
                Column = column;
                Center = center;
                Size = size;
            }

            public BaseballGeyser Geyser { get; }
            public Transform Ring { get; }
            public Transform Column { get; }
            public Vector3 Center { get; }
            public float Size { get; }
        }
    }
}
