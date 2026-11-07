using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Game;
using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// 地面: 芝のおもちゃのマット（刈り目の縞・厚みのある台）と、マットのまわりに敷いたワールドの地面の絵（スタジアムの景色）。
    /// マットの上に白い線（外の線・真ん中の線・センターサークル・センターマーク）。ゴールの中の床もマットでつなぐ
    /// </summary>
    public partial class SoccerView
    {
        private const float TurfDepth = 0.14f;
        private const float StripeWidth = 2f;
        private const float OutsideMargin = 30f;
        private const float OutsideDrop = 0.01f;
        // 外の地面の絵を少しだけ暗くして、明るい芝のマットを浮かせる
        private const float OutsideShade = 0.35f;

        // 白い線: 太さ・浮かせる高さ（マットとちらつかないように）・柵から内側へ離す距離
        private const float LineWidth = 0.1f;
        private const float LineLift = 0.006f;
        private const float LineInset = 0.35f;
        private const float CenterCircleRadius = 2.4f;
        private const float CenterSpotSize = 0.26f;
        private const int CircleSegments = 48;
        // 直線とみなす角の内積（外周の、ゴールの口の両端のような一直線の点は飛ばす）
        private const float StraightDot = 0.9999f;

        private static readonly Color StripeLight = new Color(0.50f, 0.80f, 0.36f);
        private static readonly Color StripeDark = new Color(0.42f, 0.72f, 0.30f);
        private static readonly Color TurfSide = new Color(0.27f, 0.50f, 0.22f);
        private static readonly Color LineColor = new Color(0.97f, 0.98f, 0.95f);
        private static readonly Color GroundFallback = new Color(0.86f, 0.80f, 0.62f);

        private void BuildGround(SoccerPitchDef pitch, int stadiumWorld, StageTheme theme)
        {
            BuildOutside(pitch, stadiumWorld, theme);
            var top = Materials.Floor(StripeTexture(), Color.white);
            var side = Materials.Floor(Texture2D.whiteTexture, TurfSide);
            Vector2 Uv(Vec2 p) => new Vector2((p.X + pitch.HalfLength) / (StripeWidth * 2f), 0.5f);
            AddOwnedMesh("Turf", MiniGameMeshes.Prism("SoccerTurf", pitch.Outline, _ => 0f, -TurfDepth, Uv), top, side);
            foreach (var goal in pitch.Goals)
            {
                AddOwnedMesh("GoalTurf", MiniGameMeshes.Prism("SoccerGoalTurf", GoalFloor(goal), _ => 0f, -TurfDepth, Uv), top, side);
            }

            BuildLines(pitch);
        }

        /// <summary>
        /// ワールドの地面の絵を、マットより少し低く広く敷く。絵がまだ無いワールドは本編と同じく前のワールドの絵を借りて色を替える
        /// </summary>
        private void BuildOutside(SoccerPitchDef pitch, int stadiumWorld, StageTheme theme)
        {
            var art = CreatureArt.Ground(stadiumWorld);
            var borrowed = art == null && theme.FallbackArtWorld > 0 ? CreatureArt.Ground(theme.FallbackArtWorld) : null;
            Texture texture = art != null ? art.texture : borrowed != null ? borrowed.texture : Texture2D.whiteTexture;
            var tint = art != null ? Color.white : borrowed != null ? theme.FallbackArtTint : GroundFallback;
            var shade = Color.Lerp(Color.white, StageTheme.OutsideTint, OutsideShade);
            var size = new Vector3((pitch.HalfLength + OutsideMargin) * 2f, 1f, (pitch.HalfWidth + OutsideMargin) * 2f);
            AddPart("Outside", ToyMeshes.FloorQuad, _pitch, Vector3.down * (TurfDepth + OutsideDrop), Quaternion.identity, size,
                Materials.Floor(texture, tint * shade));
        }

        /// <summary>刈り目の縞（明るい・暗いの2画素を、縞の幅ごとにくり返して貼る）</summary>
        private Texture StripeTexture()
        {
            var texture = new Texture2D(2, 1, TextureFormat.RGBA32, false)
            {
                name = "SoccerStripes",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat,
                hideFlags = HideFlags.DontSave,
            };
            texture.SetPixels(new[] { StripeLight, StripeDark });
            texture.Apply();
            _textures.Add(texture);
            return texture;
        }

        /// <summary>ゴールの口から網の奥までの床（左回り）</summary>
        private static Vec2[] GoalFloor(SoccerGoal goal)
        {
            var back = goal.LineX + goal.Outward * goal.Depth;
            var minX = Mathf.Min(goal.LineX, back);
            var maxX = Mathf.Max(goal.LineX, back);
            return new[] { new Vec2(minX, -goal.HalfWidth), new Vec2(maxX, -goal.HalfWidth), new Vec2(maxX, goal.HalfWidth), new Vec2(minX, goal.HalfWidth) };
        }

        private void BuildLines(SoccerPitchDef pitch)
        {
            var material = Materials.Floor(Texture2D.whiteTexture, LineColor);
            var border = Inset(pitch.Outline, LineInset);
            for (var i = 0; i < border.Count; i++)
            {
                Line(border[i], border[(i + 1) % border.Count], material);
            }

            var inner = pitch.HalfWidth - LineInset;
            Line(new Vec2(0f, -inner), new Vec2(0f, inner), material);
            AddOwnedMesh("CenterCircle", Ring(CenterCircleRadius, LineWidth), material);
            AddPart("CenterSpot", ToyMeshes.FloorDisc, _pitch, Vector3.up * LineLift, Quaternion.identity,
                new Vector3(CenterSpotSize, 1f, CenterSpotSize), material);
        }

        /// <summary>a から b への白い線（つなぎ目が欠けないよう、線の太さだけ長くする）</summary>
        private void Line(Vec2 a, Vec2 b, Material material)
        {
            var along = SoccerSpace.Direction(b - a);
            AddPart("Line", ToyMeshes.FloorQuad, _pitch, SoccerSpace.ToWorld((a + b) * 0.5f, LineLift), Quaternion.LookRotation(along),
                new Vector3(LineWidth, 1f, along.magnitude + LineWidth), material);
        }

        /// <summary>
        /// 凸の多角形（左回り）を distance だけ内側へ縮めた角の点。一直線に並ぶ点は飛ばす（ゴールの口の両端など）
        /// </summary>
        private static List<Vec2> Inset(IReadOnlyList<Vec2> outline, float distance)
        {
            var corners = new List<Vec2>();
            for (var i = 0; i < outline.Count; i++)
            {
                var prev = outline[(i + outline.Count - 1) % outline.Count];
                var next = outline[(i + 1) % outline.Count];
                if (Vec2.Dot((outline[i] - prev).Normalized, (next - outline[i]).Normalized) < StraightDot)
                {
                    corners.Add(outline[i]);
                }
            }

            var result = new List<Vec2>();
            for (var i = 0; i < corners.Count; i++)
            {
                var prev = corners[(i + corners.Count - 1) % corners.Count];
                var current = corners[i];
                var next = corners[(i + 1) % corners.Count];
                result.Add(InsetCorner(prev, current, next, distance));
            }

            return result;
        }

        /// <summary>前後の辺をそれぞれ内側（左回りの左手）へ distance ずらした2本の線の交わる点</summary>
        private static Vec2 InsetCorner(Vec2 prev, Vec2 current, Vec2 next, float distance)
        {
            var d1 = (current - prev).Normalized;
            var d2 = (next - current).Normalized;
            var n1 = new Vec2(-d1.Y, d1.X);
            var n2 = new Vec2(-d2.Y, d2.X);
            var p1 = current + n1 * distance;
            var p2 = current + n2 * distance;
            var cross = d1.X * d2.Y - d1.Y * d2.X;
            var t = ((p2.X - p1.X) * d2.Y - (p2.Y - p1.Y) * d2.X) / cross;
            return p1 + d1 * t;
        }

        /// <summary>地面に寝かせた輪（センターサークル）</summary>
        private static Mesh Ring(float radius, float width)
        {
            var vertices = new Vector3[CircleSegments * 2];
            var triangles = new int[CircleSegments * 6];
            for (var i = 0; i < CircleSegments; i++)
            {
                var angle = i * Mathf.PI * 2f / CircleSegments;
                var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i * 2] = direction * (radius - width * 0.5f) + Vector3.up * LineLift;
                vertices[i * 2 + 1] = direction * (radius + width * 0.5f) + Vector3.up * LineLift;
                var next = (i + 1) % CircleSegments;
                // 上から見て右回りが表（Unity）
                var t = i * 6;
                triangles[t] = i * 2;
                triangles[t + 1] = next * 2;
                triangles[t + 2] = i * 2 + 1;
                triangles[t + 3] = i * 2 + 1;
                triangles[t + 4] = next * 2;
                triangles[t + 5] = next * 2 + 1;
            }

            var mesh = new Mesh { name = "SoccerRing", vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
