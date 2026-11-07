using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Core;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 外野のフェンス: 円弧に沿った板（高さは当たりの高さと同じ。浮いた球がこれより高く越えたらホームラン）と、
    /// 一定の間隔の柱と柱の上の玉（色はワールドの柵の色。本編の闘技場・サッカーの柵と同じ）、両端の黄色いポール。
    /// 板の内側の面が当たりの円に来るよう、厚みの分だけ外へ置く
    /// </summary>
    public partial class BaseballView
    {
        private const float FenceThickness = 0.18f;
        private const float FenceCapHeight = 0.06f;
        private const float FenceGloss = 0.3f;
        private const int FencePosts = 12;
        private const float PostDiameter = 0.22f;
        private const float PostRaise = 0.12f;
        private const float CapDiameter = 0.3f;
        private const float PoleDiameter = 0.12f;
        private const float PoleHeight = 2.4f;
        private static readonly Color PoleColor = new Color(1f, 0.85f, 0.2f);

        private void BuildFence(BaseballFieldDef field, StageTheme theme)
        {
            var inner = field.FenceRadius;
            var outer = inner + FenceThickness;
            var band = Band(field, inner, outer);
            AddOwnedMesh("Fence", MiniGameMeshes.Prism("BaseballFence", band, _ => field.FenceHeight, 0f, Uv),
                Materials.Get(theme.FenceRail, FenceGloss), Materials.Get(theme.FenceRail, FenceGloss));
            // 上の縁は柱の色の帯（板の高さが一目で分かるように）
            AddOwnedMesh("FenceCap", MiniGameMeshes.Prism("BaseballFenceCap", band, _ => field.FenceHeight + FenceCapHeight, field.FenceHeight, Uv),
                Materials.Get(theme.FencePost, FenceGloss), Materials.Get(theme.FencePost, FenceGloss));
            BuildPosts(field, theme, inner + FenceThickness * 0.5f);
            BuildPoles(field, inner + FenceThickness * 0.5f);
        }

        private void BuildPosts(BaseballFieldDef field, StageTheme theme, float radius)
        {
            var post = Materials.Get(theme.FencePost, FenceGloss);
            var height = field.FenceHeight + PostRaise;
            for (var i = 1; i < FencePosts; i++)
            {
                var degrees = Mathf.Lerp(-field.FoulDegrees, field.FoulDegrees, i / (float)FencePosts);
                var at = BaseballSpace.ToWorld(BaseballFieldDef.At(degrees, radius));
                AddPart("Post", ToyMeshes.Puck, _root, at + Vector3.up * (height * 0.5f), Quaternion.identity, new Vector3(PostDiameter, height, PostDiameter), post);
                var cap = Materials.Get(i % 2 == 0 ? theme.FenceCap : theme.FenceAltCap, FenceGloss);
                AddPart("PostCap", ToyMeshes.Sphere, _root, at + Vector3.up * height, Quaternion.identity, Vector3.one * CapDiameter, cap);
            }
        }

        /// <summary>ファウルラインの先の黄色いポール（フェアとファウルの境目）</summary>
        private void BuildPoles(BaseballFieldDef field, float radius)
        {
            var material = Materials.Get(PoleColor, FenceGloss);
            foreach (var side in new[] { -1f, 1f })
            {
                var at = BaseballSpace.ToWorld(BaseballFieldDef.At(side * field.FoulDegrees, radius));
                AddPart("FoulPole", ToyMeshes.Puck, _root, at + Vector3.up * (PoleHeight * 0.5f), Quaternion.identity, new Vector3(PoleDiameter, PoleHeight, PoleDiameter), material);
                AddPart("FoulPoleCap", ToyMeshes.Sphere, _root, at + Vector3.up * PoleHeight, Quaternion.identity, Vector3.one * (PoleDiameter * 1.6f), material);
            }
        }

        /// <summary>ファウルラインの間の、半径 inner から outer までの円弧の帯（左回り）</summary>
        private static List<Vec2> Band(BaseballFieldDef field, float inner, float outer)
        {
            var points = new List<Vec2>();
            for (var i = 0; i <= ArcSegments; i++)
            {
                points.Add(BaseballFieldDef.At(Mathf.Lerp(field.FoulDegrees, -field.FoulDegrees, i / (float)ArcSegments), outer));
            }

            for (var i = 0; i <= ArcSegments; i++)
            {
                points.Add(BaseballFieldDef.At(Mathf.Lerp(-field.FoulDegrees, field.FoulDegrees, i / (float)ArcSegments), inner));
            }

            return points;
        }
    }
}
