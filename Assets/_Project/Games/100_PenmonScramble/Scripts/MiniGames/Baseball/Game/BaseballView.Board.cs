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
    /// 盤の台と地面: ブリキの受け皿の上に木の台、台の上に深緑のフェルト（ファウルの溝）。フェルトの上に、フェンス際の帯 → 外野の芝（同心の刈り目）
    /// → 内野の土の扇形を重ねる（止まった場所の札と同じ区切りなので、色で「止まったら何になるか」が分かる）。
    /// 白いファウルライン・塁の間の線・ホームベース・一塁・三塁・二塁（ゲッツーの穴を囲む白い台）・マウンドを置き、まわりにワールドの地面の絵を敷く
    /// </summary>
    public partial class BaseballView
    {
        private const int ArcSegments = 64;
        // 台の外形: フェンスの外へ広げる幅・ファウルラインの外へ回り込む角度・ホームの後ろの長さ
        private const float BoardMargin = 1.4f;
        private const float BoardExtraDegrees = 14f;
        private const float BoardBack = 3.2f;
        private const float BoardThickness = 0.45f;
        // 木の台の縁（フェルトの外に見える木の幅）とブリキの受け皿（台の外へはみ出す幅・台より下げる高さ）
        private const float WoodFrame = 0.45f;
        private const float TinWiden = 0.3f;
        private const float TinDrop = 0.06f;
        private const float TinDepth = 0.2f;
        private const float OutsideMargin = 40f;
        private const float OutsideShade = 0.35f;

        // 重なる面がちらつかないよう、上に重ねる面ほど少しずつ持ち上げる
        private const float LayerLift = 0.004f;
        private const float StripeWidth = 0.8f;
        private const float PlankWidth = 0.9f;
        private const float HomeDirtRadius = 2f;

        private const float LineWidth = 0.08f;
        internal const float BaseSize = 0.5f;
        private const float BaseHeight = 0.05f;
        // 二塁はゲッツーの穴を囲む大きな白い台（穴の縁が白の中に見える大きさ）
        internal const float SecondBaseSize = 1.55f;
        private const float MoundDiameter = 1.1f;
        private const float MoundHeight = 0.09f;

        private static readonly Color WoodLight = new Color(0.80f, 0.58f, 0.36f);
        private static readonly Color WoodDark = new Color(0.72f, 0.50f, 0.30f);
        private static readonly Color WoodSide = new Color(0.55f, 0.36f, 0.20f);
        private static readonly Color TinTop = new Color(0.70f, 0.74f, 0.80f);
        private static readonly Color TinSide = new Color(0.50f, 0.54f, 0.62f);
        private static readonly Color FeltColor = new Color(0.20f, 0.44f, 0.30f);
        private static readonly Color BandColor = new Color(0.66f, 0.46f, 0.30f);
        private static readonly Color StripeLight = new Color(0.47f, 0.78f, 0.36f);
        private static readonly Color StripeDark = new Color(0.40f, 0.70f, 0.31f);
        private static readonly Color DirtColor = new Color(0.86f, 0.64f, 0.42f);
        private static readonly Color LineColor = new Color(0.98f, 0.98f, 0.95f);
        private static readonly Color BaseSide = new Color(0.78f, 0.78f, 0.74f);
        private static readonly Color MoundColor = new Color(0.80f, 0.58f, 0.37f);
        private static readonly Color GroundFallback = new Color(0.86f, 0.80f, 0.62f);

        private void BuildBoard(BaseballFieldDef field, int world, StageTheme theme)
        {
            BuildOutside(world, theme);
            BuildTable(field);
            BuildTurf(field);
            BuildLines(field);
            BuildBases(field);
            BuildMound(field);
        }

        /// <summary>ワールドの地面の絵を、受け皿より低く広く敷く（サッカーと同じく少し暗くして盤を浮かせる）</summary>
        private void BuildOutside(int world, StageTheme theme)
        {
            var art = CreatureArt.Ground(world);
            var borrowed = art == null && theme.FallbackArtWorld > 0 ? CreatureArt.Ground(theme.FallbackArtWorld) : null;
            Texture texture = art != null ? art.texture : borrowed != null ? borrowed.texture : Texture2D.whiteTexture;
            var tint = art != null ? Color.white : borrowed != null ? theme.FallbackArtTint : GroundFallback;
            var shade = Color.Lerp(Color.white, StageTheme.OutsideTint, OutsideShade);
            var size = new Vector3(OutsideMargin * 2f, 1f, OutsideMargin * 2f);
            var height = -(BoardThickness + TinDrop + TinDepth);
            AddPart("Outside", ToyMeshes.FloorQuad, _root, new Vector3(0f, height, OutsideMargin * 0.25f), Quaternion.identity, size, Materials.Floor(texture, tint * shade));
        }

        /// <summary>ブリキの受け皿 → 木の台（板目の縞）→ フェルト（木の縁を残して内側）</summary>
        private void BuildTable(BaseballFieldDef field)
        {
            var tin = Outline(field, BoardMargin + TinWiden, BoardBack + TinWiden);
            AddOwnedMesh("Tin", MiniGameMeshes.Prism("BaseballTin", tin, _ => -TinDrop, -(BoardThickness + TinDrop + TinDepth), Uv),
                Materials.Get(TinTop, 0.6f), Materials.Get(TinSide, 0.6f));

            var wood = Materials.Floor(StripeTexture("BaseballPlanks", WoodLight, WoodDark), Color.white);
            Vector2 PlankUv(Vec2 p) => new Vector2(p.X / (PlankWidth * 2f), 0.5f);
            AddOwnedMesh("Wood", MiniGameMeshes.Prism("BaseballWood", Outline(field, BoardMargin, BoardBack), _ => 0f, -BoardThickness, PlankUv),
                wood, Materials.Get(WoodSide, 0f));

            var felt = Outline(field, BoardMargin - WoodFrame, BoardBack - WoodFrame);
            AddOwnedMesh("Felt", MiniGameMeshes.Flat("BaseballFelt", felt, LayerLift, Uv), Materials.Floor(Texture2D.whiteTexture, FeltColor));
        }

        /// <summary>フェンス際の帯 → 外野の芝 → 内野の土（止まった場所の札の区切り）と、ホームのまわりの土の丸</summary>
        private void BuildTurf(BaseballFieldDef field)
        {
            var zones = field.StopZones;
            var plain = Texture2D.whiteTexture;
            var stripes = StripeTexture("BaseballStripes", StripeLight, StripeDark);
            var iceStripes = field.Gimmicks.Ice.Count > 0 ? StripeTexture("BaseballIceStripes", IceStripeLight, IceStripeDark) : null;
            Vector2 RingUv(Vec2 p) => new Vector2(p.Length / (StripeWidth * 2f), 0.5f);
            for (var i = zones.Count - 1; i >= 0; i--)
            {
                var radius = i == zones.Count - 1 ? field.FenceRadius : zones[i].OuterRadius;
                var inner = i == 0 ? 0f : zones[i - 1].OuterRadius;
                // 氷の床（氷山）の所は、同じ区切りのまま氷の色にする（止まったら何になるかは色の濃さで分かる）
                var material = iceStripes != null && IsIcyRing(field, inner, radius)
                    ? IceZoneMaterial(zones[i].Result, iceStripes)
                    : ZoneMaterial(zones[i].Result, plain, stripes);
                AddOwnedMesh($"Zone{i}", MiniGameMeshes.Flat($"BaseballZone{i}", Fan(field, radius), LayerLift * (zones.Count - i + 1), RingUv), material);
            }

            AddPart("HomeDirt", ToyMeshes.FloorDisc, _root, Vector3.up * (LayerLift * (zones.Count + 2)), Quaternion.identity,
                new Vector3(HomeDirtRadius * 2f, 1f, HomeDirtRadius * 2f), Materials.Floor(plain, DirtColor));
        }

        /// <summary>止まった場所の札の色: ゴロアウトの所は土、単打の所は芝、長打の所はフェンス際の帯</summary>
        private Material ZoneMaterial(BaseballHitResult result, Texture plain, Texture stripes)
        {
            return result switch
            {
                BaseballHitResult.Single => Materials.Floor(stripes, Color.white),
                BaseballHitResult.Double or BaseballHitResult.Triple => Materials.Floor(plain, BandColor),
                _ => Materials.Floor(plain, DirtColor),
            };
        }

        /// <summary>氷の床の上の札の色: 単打の所は氷の縞、長打の所は濃い氷の色</summary>
        private Material IceZoneMaterial(BaseballHitResult result, Texture iceStripes)
        {
            return result == BaseballHitResult.Single
                ? Materials.Floor(iceStripes, Color.white)
                : Materials.Floor(Texture2D.whiteTexture, IceBandColor);
        }

        /// <summary>ファウルライン（ホームからフェンスまで）と、一塁・三塁から二塁への線</summary>
        private void BuildLines(BaseballFieldDef field)
        {
            var material = Materials.Floor(Texture2D.whiteTexture, LineColor);
            var lift = LayerLift * 8f;
            foreach (var side in new[] { -1f, 1f })
            {
                Line(Vec2.Zero, BaseballFieldDef.At(side * field.FoulDegrees, field.FenceRadius), lift, material);
                Line(field.Base(side > 0f ? 1 : 3), field.Base(2), lift, material);
            }
        }

        private void Line(Vec2 a, Vec2 b, float lift, Material material)
        {
            var along = BaseballSpace.Direction(b - a);
            AddPart("Line", ToyMeshes.FloorQuad, _root, BaseballSpace.ToWorld((a + b) * 0.5f, lift), Quaternion.LookRotation(along),
                new Vector3(LineWidth, 1f, along.magnitude + LineWidth), material);
        }

        /// <summary>ホームベース（とがった方が捕手）・一塁・三塁の白い台と、ゲッツーの穴を囲む二塁の大きな白い台</summary>
        private void BuildBases(BaseballFieldDef field)
        {
            var top = Materials.Get(LineColor, 0.2f);
            var side = Materials.Get(BaseSide, 0f);
            var home = new[] { new Vec2(0.26f, -0.04f), new Vec2(0.26f, 0.2f), new Vec2(-0.26f, 0.2f), new Vec2(-0.26f, -0.04f), new Vec2(0f, -0.3f) };
            AddOwnedMesh("Home", MiniGameMeshes.Prism("BaseballHome", home, _ => BaseHeight * 0.5f, 0f, Uv), top, side);
            for (var number = 1; number <= 3; number++)
            {
                var size = number == 2 ? SecondBaseSize : BaseSize;
                var bag = Diamond(field.Base(number), size * 0.5f);
                AddOwnedMesh($"Base{number}", MiniGameMeshes.Prism($"BaseballBase{number}", bag, _ => number == 2 ? LayerLift * 9f : BaseHeight, 0f, Uv), top, side);
            }
        }

        private void BuildMound(BaseballFieldDef field)
        {
            AddPart("Mound", ToyMeshes.Hemisphere, _root, BaseballSpace.ToWorld(field.Mound), Quaternion.identity,
                new Vector3(MoundDiameter, MoundHeight * 2f, MoundDiameter), Materials.Get(MoundColor, 0f));
        }

        /// <summary>台の外形: フェンスの外の円弧（ファウルラインの外へ少し回り込む）と、ホームの後ろの角を囲む凸な形</summary>
        private static Vec2[] Outline(BaseballFieldDef field, float margin, float back)
        {
            var points = new List<Vec2>();
            var spread = field.FoulDegrees + BoardExtraDegrees;
            for (var i = 0; i <= ArcSegments; i++)
            {
                points.Add(BaseballFieldDef.At(Mathf.Lerp(-spread, spread, i / (float)ArcSegments), field.FenceRadius + margin));
            }

            var width = (field.FenceRadius + margin) * Mathf.Sin(spread * Mathf.Deg2Rad) * 0.55f;
            points.Add(new Vec2(-width, -back));
            points.Add(new Vec2(width, -back));
            return MiniGameGeometry.ConvexHull(points);
        }

        /// <summary>ホームから左右のファウルラインまでの扇形（左回り）</summary>
        private static List<Vec2> Fan(BaseballFieldDef field, float radius)
        {
            var points = new List<Vec2> { Vec2.Zero };
            for (var i = 0; i <= ArcSegments; i++)
            {
                points.Add(BaseballFieldDef.At(Mathf.Lerp(field.FoulDegrees, -field.FoulDegrees, i / (float)ArcSegments), radius));
            }

            return points;
        }

        /// <summary>真ん中から角まで half の、45°傾けた正方形（左回り）</summary>
        private static Vec2[] Diamond(Vec2 center, float half)
        {
            return new[] { center + new Vec2(0f, -half), center + new Vec2(half, 0f), center + new Vec2(0f, half), center + new Vec2(-half, 0f) };
        }

        /// <summary>明るい・暗いの2画素の縞（くり返して貼る）。芝の刈り目・木の板目</summary>
        private Texture StripeTexture(string textureName, Color light, Color dark)
        {
            var texture = new Texture2D(2, 1, TextureFormat.RGBA32, false)
            {
                name = textureName,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.DontSave,
            };
            texture.SetPixels(new[] { light, dark });
            texture.Apply();
            _textures.Add(texture);
            return texture;
        }

        private static Vector2 Uv(Vec2 p) => new Vector2(p.X, p.Y);
    }
}
