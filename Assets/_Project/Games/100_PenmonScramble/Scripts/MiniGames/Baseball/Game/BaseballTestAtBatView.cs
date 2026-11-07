using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Core;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.MiniGames.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 試しの打席の仮の見た目（板と球と棒のバット）。盤の定義から、木の台・扇形の芝と土・ファウルライン・フェンス・穴・塁を組み、
    /// 毎フレーム球とバットを写す。本番の見た目（ブリキと木のおもちゃの盤・Penguin3D・穴の札）はフェーズ 2 で作り直す
    /// </summary>
    public class BaseballTestAtBatView : MonoBehaviour
    {
        private const int ArcSegments = 48;
        private const float BoardMargin = 1.6f;
        private const float BoardBack = 2.6f;
        private const float BoardThickness = 0.4f;
        // 重なる面がちらつかないよう、上に重ねる面ほど少しずつ持ち上げる
        private const float LayerLift = 0.004f;
        private const float LineWidth = 0.08f;
        private const float FenceThickness = 0.2f;
        private const float HoleDepthLook = 0.01f;
        private const float BaseSize = 0.45f;
        private const float MoundRadius = 0.9f;
        private const float MoundHeight = 0.08f;
        private const float BatThickness = 0.12f;
        private const float FigureHeight = 1f;
        private const float FigureRadius = 0.32f;
        private const float BatterBehind = 0.45f;
        private const float ZonePostSize = 0.08f;
        private const float ZonePostHeight = 0.5f;
        private const float ShadowLift = 0.01f;
        private const float ShadowShrinkPerHeight = 0.12f;

        private static readonly Color BoardColor = new Color(0.72f, 0.52f, 0.32f);
        private static readonly Color FenceBandColor = new Color(0.30f, 0.58f, 0.30f);
        private static readonly Color GrassColor = new Color(0.42f, 0.74f, 0.36f);
        private static readonly Color DirtColor = new Color(0.82f, 0.62f, 0.40f);
        private static readonly Color LineColor = new Color(0.97f, 0.97f, 0.94f);
        private static readonly Color FenceColor = new Color(0.20f, 0.36f, 0.62f);
        private static readonly Color MoundColor = new Color(0.76f, 0.55f, 0.34f);
        private static readonly Color BallColor = Color.white;
        private static readonly Color BatColor = new Color(0.86f, 0.66f, 0.38f);
        private static readonly Color BuntBatColor = new Color(1f, 0.85f, 0.35f);
        private static readonly Color ShadowColor = new Color(0.1f, 0.12f, 0.1f);
        private static readonly Color BatterColor = new Color(0.92f, 0.3f, 0.3f);
        private static readonly Color PitcherColor = new Color(0.3f, 0.5f, 0.95f);
        private static readonly Color ZoneColor = new Color(1f, 0.9f, 0.3f);

        [SerializeField] private Shader _toonShader;
        [SerializeField] private Shader _blobShadowShader;

        private readonly List<Mesh> _meshes = new List<Mesh>();
        private ToonMaterials _materials;
        private Transform _ball;
        private Transform _shadow;
        private Transform _bat;
        private Renderer _batRenderer;

        private ToonMaterials Materials => _materials ??= new ToonMaterials(_toonShader, _blobShadowShader);

        public void Build(BaseballFieldDef field)
        {
            BuildBoard(field);
            BuildLines(field);
            BuildFence(field);
            for (var i = 0; i < field.Holes.Count; i++)
            {
                BuildHole(field.Holes[i], i);
            }

            BuildBases(field);
            BuildZone(field);
            BuildFigures(field);
            BuildBat(field);
            _ball = Primitive(PrimitiveType.Sphere, "Ball", BallColor);
            _ball.localScale = Vector3.one * (BaseballBalance.BallRadius * 2f);
            _shadow = Primitive(PrimitiveType.Cylinder, "BallShadow", ShadowColor);
        }

        public void Draw(BaseballBoard board, BaseballFrame frame)
        {
            var sunk = board.Phase == BaseballBoardPhase.Done && board.Ball.HoleIndex >= 0;
            // 穴に入った球は穴の中に沈めて見せる
            _ball.localPosition = sunk ? frame.Ball + Vector3.down * (BaseballBalance.BallRadius * 2f) : frame.Ball;
            var height = frame.Ball.y - BaseballBalance.BallRadius;
            var size = BaseballBalance.BallRadius * 2f * Mathf.Max(0.4f, 1f - height * ShadowShrinkPerHeight);
            _shadow.gameObject.SetActive(!sunk);
            _shadow.localPosition = new Vector3(frame.Ball.x, ShadowLift, frame.Ball.z);
            _shadow.localScale = new Vector3(size, ShadowLift, size);
            _bat.localRotation = Quaternion.Euler(0f, -frame.BatDegrees, 0f);
            _batRenderer.sharedMaterial = Materials.Get(board.Bat.State == BaseballBatState.Bunting ? BuntBatColor : BatColor, 0f);
        }

        /// <summary>木の台の上に、フェンス際の帯 → 外野の芝 → 内野の土の扇形を重ねる（止まった場所の札と同じ区切り）</summary>
        private void BuildBoard(BaseballFieldDef field)
        {
            var reach = field.FenceRadius + BoardMargin;
            var board = MiniGameGeometry.Rect(-reach, -BoardBack, reach, reach);
            AddMesh("Board", MiniGameMeshes.Prism("BaseballBoard", board, _ => 0f, -BoardThickness, UvOf), BoardColor, BoardColor);

            var zones = field.StopZones;
            var colors = new[] { DirtColor, GrassColor, FenceBandColor };
            for (var i = zones.Count - 1; i >= 0; i--)
            {
                var radius = i == zones.Count - 1 ? field.FenceRadius : zones[i].OuterRadius;
                var color = colors[Mathf.Min(i, colors.Length - 1)];
                AddMesh($"Zone{i}", MiniGameMeshes.Flat($"BaseballZone{i}", Fan(field, radius), LayerLift * (zones.Count - i), UvOf), color);
            }
        }

        /// <summary>ホームから左右のファウルラインまでの扇形（左回り）</summary>
        private static List<Vec2> Fan(BaseballFieldDef field, float radius)
        {
            var points = new List<Vec2> { Vec2.Zero };
            for (var i = 0; i <= ArcSegments; i++)
            {
                var degrees = Mathf.Lerp(field.FoulDegrees, -field.FoulDegrees, i / (float)ArcSegments);
                points.Add(BaseballFieldDef.At(degrees, radius));
            }

            return points;
        }

        private void BuildLines(BaseballFieldDef field)
        {
            foreach (var side in new[] { -1f, 1f })
            {
                var end = BaseballFieldDef.At(side * field.FoulDegrees, field.FenceRadius);
                Strip("FoulLine", Vec2.Zero, end, LineWidth, LayerLift * 5f, LineColor);
            }
        }

        /// <summary>フェンスは円弧に沿って短い板を並べる（高さは当たりの高さと同じ）</summary>
        private void BuildFence(BaseballFieldDef field)
        {
            var radius = field.FenceRadius + FenceThickness * 0.5f;
            for (var i = 0; i < ArcSegments; i++)
            {
                var a = BaseballFieldDef.At(Mathf.Lerp(-field.FoulDegrees, field.FoulDegrees, i / (float)ArcSegments), radius);
                var b = BaseballFieldDef.At(Mathf.Lerp(-field.FoulDegrees, field.FoulDegrees, (i + 1) / (float)ArcSegments), radius);
                var along = ToUnity(b) - ToUnity(a);
                var panel = Primitive(PrimitiveType.Cube, "Fence", FenceColor);
                panel.localScale = new Vector3(FenceThickness, field.FenceHeight, along.magnitude + FenceThickness * 0.5f);
                panel.localPosition = (ToUnity(a) + ToUnity(b)) * 0.5f + Vector3.up * (field.FenceHeight * 0.5f);
                panel.localRotation = Quaternion.LookRotation(along);
            }
        }

        /// <summary>穴は色の付いた円盤（アウトは暗い色・ヒットは明るい色。本番の穴と札はフェーズ 2）</summary>
        private void BuildHole(BaseballHole hole, int index)
        {
            var disc = Primitive(PrimitiveType.Cylinder, $"Hole{index}_{hole.Result}", HoleColor(hole.Result));
            disc.localScale = new Vector3(hole.Radius * 2f, HoleDepthLook, hole.Radius * 2f);
            disc.localPosition = new Vector3(hole.Center.X, LayerLift * 6f, hole.Center.Y);
        }

        private static Color HoleColor(BaseballHitResult result)
        {
            return result switch
            {
                BaseballHitResult.GroundOut => new Color(0.22f, 0.22f, 0.28f),
                BaseballHitResult.DoublePlay => new Color(0.42f, 0.12f, 0.16f),
                BaseballHitResult.FlyOut => new Color(0.16f, 0.22f, 0.40f),
                BaseballHitResult.Single => new Color(1f, 0.92f, 0.3f),
                BaseballHitResult.Double => new Color(1f, 0.6f, 0.2f),
                BaseballHitResult.Triple => new Color(1f, 0.4f, 0.75f),
                _ => Color.gray,
            };
        }

        private void BuildBases(BaseballFieldDef field)
        {
            for (var number = 0; number <= 3; number++)
            {
                var p = field.Base(number);
                var bag = Primitive(PrimitiveType.Cube, number == 0 ? "Home" : $"Base{number}", LineColor);
                bag.localScale = new Vector3(BaseSize, LayerLift * 4f, BaseSize);
                bag.localPosition = new Vector3(p.X, LayerLift * 6f, p.Y);
                bag.localRotation = Quaternion.Euler(0f, 45f, 0f);
            }

            var mound = Primitive(PrimitiveType.Cylinder, "Mound", MoundColor);
            mound.localScale = new Vector3(MoundRadius * 2f, MoundHeight * 0.5f, MoundRadius * 2f);
            mound.localPosition = new Vector3(field.Mound.X, MoundHeight * 0.5f, field.Mound.Y);
        }

        /// <summary>ゾーンの幅をホームの線の左右の柱で見せる（本番の光る枠はフェーズ 2）</summary>
        private void BuildZone(BaseballFieldDef field)
        {
            foreach (var side in new[] { -1f, 1f })
            {
                var post = Primitive(PrimitiveType.Cube, "ZonePost", ZoneColor);
                post.localScale = new Vector3(ZonePostSize, ZonePostHeight, ZonePostSize);
                post.localPosition = new Vector3(side * field.ZoneHalfWidth, ZonePostHeight * 0.5f, BaseballFieldDef.PlateY);
            }
        }

        /// <summary>打つ子（バットの軸の後ろ）と投げる子（マウンド）の円柱</summary>
        private void BuildFigures(BaseballFieldDef field)
        {
            var batter = Primitive(PrimitiveType.Cylinder, "Batter", BatterColor);
            batter.localScale = new Vector3(FigureRadius * 2f, FigureHeight * 0.5f, FigureRadius * 2f);
            batter.localPosition = new Vector3(field.BatPivot.X - BatterBehind, FigureHeight * 0.5f, field.BatPivot.Y);
            var pitcher = Primitive(PrimitiveType.Cylinder, "Pitcher", PitcherColor);
            pitcher.localScale = new Vector3(FigureRadius * 2f, FigureHeight * 0.5f, FigureRadius * 2f);
            pitcher.localPosition = new Vector3(field.Mound.X, FigureHeight * 0.5f + MoundHeight, field.Mound.Y + FigureRadius * 2f);
        }

        /// <summary>バットは軸を中心に Y 軸で回る棒（当たり始める所から先まで）</summary>
        private void BuildBat(BaseballFieldDef field)
        {
            _bat = new GameObject("Bat").transform;
            _bat.SetParent(transform, false);
            _bat.localPosition = new Vector3(field.BatPivot.X, BaseballBalance.BallRadius, field.BatPivot.Y);
            var length = BaseballBalance.BatLength - BaseballBalance.BatInner;
            var stick = Primitive(PrimitiveType.Cube, "Stick", BatColor, _bat);
            stick.localScale = new Vector3(length, BatThickness, BatThickness);
            stick.localPosition = new Vector3(BaseballBalance.BatInner + length * 0.5f, 0f, 0f);
            _batRenderer = stick.GetComponent<Renderer>();
        }

        private void Strip(string name, Vec2 from, Vec2 to, float width, float lift, Color color)
        {
            var a = ToUnity(from);
            var b = ToUnity(to);
            var strip = Primitive(PrimitiveType.Cube, name, color);
            strip.localScale = new Vector3(width, lift, (b - a).magnitude);
            strip.localPosition = (a + b) * 0.5f + Vector3.up * lift;
            strip.localRotation = Quaternion.LookRotation(b - a);
        }

        private void AddMesh(string name, Mesh mesh, Color top, Color? side = null)
        {
            _meshes.Add(mesh);
            var obj = new GameObject(name);
            obj.transform.SetParent(transform, false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = side.HasValue
                ? new[] { Materials.Get(top, 0f), Materials.Get(side.Value, 0f) }
                : new[] { Materials.Get(top, 0f) };
        }

        /// <summary>形だけ使う（当たりは Core で計算するので Collider は外す）</summary>
        private Transform Primitive(PrimitiveType type, string name, Color color, Transform parent = null)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            Destroy(obj.GetComponent<Collider>());
            obj.GetComponent<Renderer>().sharedMaterial = Materials.Get(color, 0f);
            obj.transform.SetParent(parent != null ? parent : transform, false);
            return obj.transform;
        }

        private static Vector2 UvOf(Vec2 p) => new Vector2(p.X, p.Y);

        private static Vector3 ToUnity(Vec2 p) => new Vector3(p.X, 0f, p.Y);

        private void OnDestroy()
        {
            foreach (var mesh in _meshes)
            {
                Destroy(mesh);
            }

            _materials?.DestroyAll();
        }
    }
}
