using System;
using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.Game;
using PenmonScramble.Golf.Core;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// 地面: ワールドの地面の絵を貼った厚みのある台（おもちゃのミニゴルフの台）と、その下に暗く敷いた外の床。
    /// 地面の種類は台の上に重ねる: 池（水色に白い波の縁）・場外（暗い色。火山では光る縁の溶岩）・砂地（砂色。氷山では雪）・
    /// 坂（下る向きへ傾けた床に矢印）・ジャンプ台（赤い台に白い輪）・氷の床（水色に白い縁）。色は景色ごと（GolfScenery）。
    /// 押し流す帯と間欠泉は動くので GolfHoleView.Gimmicks.cs
    /// </summary>
    public partial class GolfHoleView
    {
        // 地面の絵は 16:9（ステージの1エリア分）。コースを覆うように、縦横比を保ったまま引き伸ばして貼る
        private const float ArtAspect = 16f / 9f;

        // 台の厚み。外の床はその下に敷く
        private const float SlabDepth = 0.5f;
        private const float OutsideMargin = 24f;
        private const float OutsideDrop = 0.02f;

        // 地面の種類の板は地面より少しずつ上に置く（同じ高さだとちらつく）
        private const float AreaLift = 0.006f;
        private const float RimWidth = 0.14f;

        // 坂の矢印（下る向きの「く」の字）: 腕の長さ・太さ・開き・間・数
        private const float ChevronArm = 0.55f;
        private const float ChevronWidth = 0.12f;
        private const float ChevronSpread = 38f;
        private const float ChevronSpacing = 0.9f;
        private const int ChevronCount = 3;

        // ジャンプ台の上の白い輪（ばねの台に見せる）。台の短い辺に対する大きさ
        private const float JumpRingRatio = 0.6f;

        private static readonly Color SlabSideColor = new Color(0.55f, 0.40f, 0.28f);
        private static readonly Color SlopeTint = new Color(1.08f, 1.08f, 1f);
        private static readonly Color ChevronColor = new Color(1f, 1f, 1f, 1f);
        private static readonly Color JumpColor = new Color(0.95f, 0.36f, 0.30f);
        private static readonly Color JumpSideColor = new Color(0.70f, 0.24f, 0.22f);

        private void BuildGround(GolfHoleDef hole, int world, StageTheme theme)
        {
            var (texture, tint) = GroundArt(world, theme);
            var uv = CoverUv(hole.Ground);
            var top = Materials.Floor(texture, tint);
            var side = Materials.Floor(Texture2D.whiteTexture, SlabSideColor);
            AddOwnedMesh("Ground", GolfMeshes.Prism("GolfGround", hole.Ground, _ => 0f, -SlabDepth, uv), top, side);
            BuildOutside(hole.Ground, texture, tint);
            for (var i = 0; i < hole.Areas.Count; i++)
            {
                BuildArea(hole.Areas[i], AreaLift * (i + 1), texture, tint, uv, side);
            }
        }

        /// <summary>
        /// ワールドの地面の絵と、それに掛ける色。絵がまだ無いワールドは、本編と同じく前のワールドの絵を借りて色を替える。
        /// どちらも無ければ白い板に芝の色
        /// </summary>
        private static (Texture texture, Color tint) GroundArt(int world, StageTheme theme)
        {
            var art = CreatureArt.Ground(world);
            if (art != null)
            {
                return (art.texture, Color.white);
            }

            var borrowed = theme.FallbackArtWorld > 0 ? CreatureArt.Ground(theme.FallbackArtWorld) : null;
            return borrowed != null ? (borrowed.texture, theme.FallbackArtTint) : (Texture2D.whiteTexture, new Color(0.55f, 0.80f, 0.45f));
        }

        /// <summary>絵をコースいっぱいに覆うように貼る位置（縦横比は保つ。はみ出した分は切れる）</summary>
        private static Func<Vec2, Vector2> CoverUv(IReadOnlyList<Vec2> ground)
        {
            var (min, max) = Bounds(ground);
            var size = max - min;
            var width = Mathf.Max(size.x, size.y * ArtAspect);
            var height = width / ArtAspect;
            var center = (min + max) * 0.5f;
            return point => new Vector2((point.X - center.x) / width + 0.5f, (point.Y - center.y) / height + 0.5f);
        }

        private static (Vector2 min, Vector2 max) Bounds(IReadOnlyList<Vec2> shape)
        {
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var point in shape)
            {
                min = Vector2.Min(min, new Vector2(point.X, point.Y));
                max = Vector2.Max(max, new Vector2(point.X, point.Y));
            }

            return (min, max);
        }

        /// <summary>台の下に、同じ絵を暗くした広い床を敷く（斜めに見下ろしたとき、台の外に背景の色だけが見えないように）</summary>
        private void BuildOutside(IReadOnlyList<Vec2> ground, Texture texture, Color tint)
        {
            var (min, max) = Bounds(ground);
            var center = (min + max) * 0.5f;
            var size = max - min + Vector2.one * (OutsideMargin * 2f);
            AddPart("Outside", ToyMeshes.FloorQuad, _course, new Vector3(center.x, -SlabDepth - OutsideDrop, center.y), Quaternion.identity,
                new Vector3(size.x, 1f, size.y), Materials.Floor(texture, StageTheme.OutsideTint * tint));
        }

        private void BuildArea(GolfArea area, float lift, Texture texture, Color tint, Func<Vec2, Vector2> uv, Material side)
        {
            var name = area.Kind.ToString();
            switch (area.Kind)
            {
                case GolfAreaKind.Water:
                    AddOwnedMesh(name, GolfMeshes.Flat(name, area.Shape, lift, uv), Materials.Floor(Texture2D.whiteTexture, _scenery.Water));
                    BuildRim(area.Shape, lift, _scenery.Foam);
                    break;
                case GolfAreaKind.OutOfBounds:
                    AddOwnedMesh(name, GolfMeshes.Flat(name, area.Shape, lift, uv), Materials.Floor(Texture2D.whiteTexture, _scenery.Out));
                    if (_scenery.OutRim.HasValue)
                    {
                        BuildRim(area.Shape, lift, _scenery.OutRim.Value);
                    }

                    break;
                case GolfAreaKind.Sand:
                    AddOwnedMesh(name, GolfMeshes.Flat(name, area.Shape, lift, uv), Materials.Floor(Texture2D.whiteTexture, _scenery.Sand));
                    BuildRim(area.Shape, lift, _scenery.SandRim);
                    break;
                case GolfAreaKind.Ice:
                    AddOwnedMesh(name, GolfMeshes.Flat(name, area.Shape, lift, uv), Materials.Floor(Texture2D.whiteTexture, _scenery.IceFloor));
                    BuildRim(area.Shape, lift, _scenery.IceRim);
                    break;
                case GolfAreaKind.Flow:
                    BuildFlow(area, lift, uv);
                    break;
                case GolfAreaKind.Geyser:
                    BuildGeyser(area, lift, uv);
                    break;
                case GolfAreaKind.Slope:
                    BuildSlope(area, lift, texture, tint, uv, side);
                    break;
                case GolfAreaKind.Jump:
                    BuildJump(area, lift, uv);
                    break;
            }
        }

        /// <summary>傾けた床（上面は地面の絵を少し明るく）と、下る向きの矢印</summary>
        private void BuildSlope(GolfArea area, float lift, Texture texture, Color tint, Func<Vec2, Vector2> uv, Material side)
        {
            var height = Surface.TopOf(area);
            AddOwnedMesh("Slope", GolfMeshes.Prism("GolfSlope", area.Shape, p => lift + height(p), 0f, uv), Materials.Floor(texture, SlopeTint * tint), side);

            var down = area.Push.Normalized;
            var center = Centroid(area.Shape);
            var material = Materials.Floor(Texture2D.whiteTexture, ChevronColor);
            for (var i = 0; i < ChevronCount; i++)
            {
                var at = center + down * ((i - (ChevronCount - 1) * 0.5f) * ChevronSpacing);
                if (area.Contains(at))
                {
                    BuildChevron(at, down, lift + height(at) + AreaLift, material);
                }
            }
        }

        /// <summary>先が down を向いた「く」の字（細い板2枚）</summary>
        private void BuildChevron(Vec2 tip, Vec2 down, float height, Material material)
        {
            var forward = GolfSpace.ToWorld(down);
            foreach (var side in new[] { -1f, 1f })
            {
                var rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, 180f + side * ChevronSpread, 0f);
                var offset = rotation * Vector3.forward * (ChevronArm * 0.5f);
                AddPart("Chevron", ToyMeshes.FloorQuad, _course, GolfSpace.ToWorld(tip, height) + offset, rotation,
                    new Vector3(ChevronWidth, 1f, ChevronArm), material);
            }
        }

        /// <summary>ジャンプ台: 赤い台（少し高い）と、上の白い輪</summary>
        private void BuildJump(GolfArea area, float lift, Func<Vec2, Vector2> uv)
        {
            var top = lift + GolfSurface.JumpPadHeight;
            AddOwnedMesh("Jump", GolfMeshes.Prism("GolfJump", area.Shape, _ => top, 0f, uv),
                Materials.Floor(Texture2D.whiteTexture, JumpColor), Materials.Floor(Texture2D.whiteTexture, JumpSideColor));
            var (min, max) = Bounds(area.Shape);
            var size = Mathf.Min(max.x - min.x, max.y - min.y) * JumpRingRatio;
            AddPart("JumpRing", ToyMeshes.Torus, _course, GolfSpace.ToWorld(Centroid(area.Shape), top), Quaternion.identity,
                Vector3.one * size, Materials.Get(Color.white, 0.3f));
        }

        /// <summary>多角形の縁に沿って細い板を並べる（池の波・砂地の縁）</summary>
        private void BuildRim(IReadOnlyList<Vec2> shape, float lift, Color color)
        {
            var material = Materials.Floor(Texture2D.whiteTexture, color);
            for (var i = 0; i < shape.Count; i++)
            {
                var a = shape[i];
                var b = shape[(i + 1) % shape.Count];
                var along = GolfSpace.ToWorld(b - a);
                // 角が欠けないよう、縁の幅だけ長くして重ねる
                AddPart("Rim", ToyMeshes.FloorQuad, _course, GolfSpace.ToWorld((a + b) * 0.5f, lift + AreaLift * 0.5f), Quaternion.LookRotation(along),
                    new Vector3(RimWidth, 1f, along.magnitude + RimWidth), material);
            }
        }

        private static Vec2 Centroid(IReadOnlyList<Vec2> shape)
        {
            var sum = Vec2.Zero;
            foreach (var point in shape)
            {
                sum += point;
            }

            return sum / shape.Count;
        }
    }
}
