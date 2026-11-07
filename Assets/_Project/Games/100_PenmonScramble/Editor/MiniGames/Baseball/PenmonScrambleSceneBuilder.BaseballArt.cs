using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 野球の絵。コードで描いて取り込む（ゴルフの旗・サッカーのボールの絵と同じ描き方）:
    /// ミニゲーム画面のカード（土のダイヤモンドの上の白い球と、左右の赤い縫い目）
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string BaseballCardPath = UiArtFolder + "CardBaseball.png";

        private static readonly Color BaseballDirtColor = new Color(0.86f, 0.62f, 0.38f);
        private static readonly Color BaseballGrassColor = new Color(0.47f, 0.82f, 0.36f);
        private static readonly Color BaseballShadowColor = new Color(0.42f, 0.26f, 0.14f, 0.5f);
        private static readonly Color BaseballBallColor = Color.white;
        private static readonly Color BaseballRimColor = new Color(0.62f, 0.62f, 0.66f);
        private static readonly Color BaseballSeamColor = new Color(0.86f, 0.18f, 0.2f);

        private static readonly Vector2 BaseballBallCenter = new Vector2(0f, 0.06f);
        private const float BaseballBallRadius = 0.56f;
        private const float BaseballRimWidth = 0.04f;

        // 縫い目は、球の外の左右に中心を置いた大きな円の弧（球の中だけ見せる）。縫い目の小さな目は弧に沿った短い線
        private const float BaseballSeamCircleRadius = 0.5f;
        private const float BaseballSeamOffset = 0.78f;
        private const float BaseballSeamWidth = 0.035f;
        private const int BaseballStitchCount = 7;
        private const float BaseballStitchSpread = 1.1f;
        private const float BaseballStitchLength = 0.07f;
        private const float BaseballStitchWidth = 0.022f;

        // 下の土のダイヤモンド（ひし形）と、その下の芝の丸
        private static readonly Vector2 BaseballDiamondCenter = new Vector2(0f, -0.62f);
        private static readonly Vector2 BaseballDiamondHalf = new Vector2(0.8f, 0.28f);

        private static void CreateBaseballArt()
        {
            WriteUiSprite(BaseballCardPath, DrawIcon(BaseballCardPixel), 0);
        }

        /// <summary>奥から重ねる: 芝の丸 → 土のひし形 → 影 → 球の縁 → 白い地 → 赤い縫い目（球の中だけ）</summary>
        private static Color BaseballCardPixel(Vector2 p, float pixel)
        {
            var color = Color.clear;
            color = Over(color, BaseballGrassColor, IconCoverage(EllipseDistance(p - new Vector2(0f, -0.66f), new Vector2(0.94f, 0.26f)), pixel));
            color = Over(color, BaseballDirtColor, IconCoverage(DiamondDistance(p - BaseballDiamondCenter, BaseballDiamondHalf), pixel));
            color = Over(color, BaseballShadowColor, IconCoverage(EllipseDistance(p - new Vector2(0.06f, -0.6f), new Vector2(0.46f, 0.09f)), pixel));

            var local = p - BaseballBallCenter;
            var ball = local.magnitude - BaseballBallRadius;
            color = Over(color, BaseballRimColor, IconCoverage(ball, pixel));
            var inside = ball + BaseballRimWidth;
            color = Over(color, BaseballBallColor, IconCoverage(inside, pixel));
            return Over(color, BaseballSeamColor, IconCoverage(Mathf.Max(BaseballSeams(local), inside), pixel));
        }

        /// <summary>左右の縫い目の弧と、弧に沿って並ぶ小さな目（弧に直角な短い線）</summary>
        private static float BaseballSeams(Vector2 local)
        {
            var shape = float.MaxValue;
            foreach (var side in new[] { -1f, 1f })
            {
                var center = new Vector2(side * BaseballSeamOffset, 0f);
                var toPoint = local - center;
                shape = Mathf.Min(shape, Mathf.Abs(toPoint.magnitude - BaseballSeamCircleRadius) - BaseballSeamWidth);
                for (var i = 0; i < BaseballStitchCount; i++)
                {
                    // 弧のうち球の側（中心から球の真ん中へ向かう向き）のまわりに目を並べる
                    var t = i / (float)(BaseballStitchCount - 1) - 0.5f;
                    var angle = (side > 0f ? Mathf.PI : 0f) + t * BaseballStitchSpread;
                    var onArc = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    var a = center + onArc * (BaseballSeamCircleRadius - BaseballStitchLength);
                    var b = center + onArc * (BaseballSeamCircleRadius + BaseballStitchLength);
                    shape = Mathf.Min(shape, SegmentDistance(local, a, b) - BaseballStitchWidth);
                }
            }

            return shape;
        }

        /// <summary>ひし形の辺からのおおよその距離（内側が負）</summary>
        private static float DiamondDistance(Vector2 p, Vector2 half)
        {
            var q = new Vector2(Mathf.Abs(p.x) / half.x, Mathf.Abs(p.y) / half.y);
            return (q.x + q.y - 1f) * Mathf.Min(half.x, half.y) * 0.7f;
        }

        // 魔球と必殺打のカットインは上下に2本並ぶことがあるので、本編の帯より細くする
        private const float BaseballCutInHeight = 250f;
        private const float BaseballCutInFaceSize = 190f;
        private const float BaseballCutInFaceX = 300f;
        private const int BaseballCutInFontSize = 50;
        private const int BaseballCutInNameFontSize = 78;

        /// <summary>魔球・必殺打のカットイン（本編と同じ部品の細い帯）。帯を画面のまん中から offsetY だけずらして置く</summary>
        internal static SpecialCutIn CreateBaseballCutIn(Transform canvas, string name, float offsetY)
        {
            var layout = new CutInLayout(BaseballCutInHeight, BaseballCutInFaceSize, BaseballCutInFaceX, BaseballCutInFontSize, BaseballCutInNameFontSize);
            var cutIn = CreateCutIn(canvas, layout);
            cutIn.gameObject.name = name;
            var so = new SerializedObject(cutIn);
            var band = (RectTransform)so.FindProperty("_band").objectReferenceValue;
            band.anchoredPosition = new Vector2(0f, offsetY);
            return cutIn;
        }
    }
}
