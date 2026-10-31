using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// スマホの仮想スティック・ボタンの絵（§3・§9.6）。メニューのボタン（UiArt）と同じく、Rebuild のたびにコードで描いて Art/UI/ に置く。
    /// 丸・面・つや・光・リングは白で描いて Image の色で塗る（属性の色でも同じ絵を使えるように）。スティックの台だけは色付きで描く
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string TouchCirclePath = UiArtFolder + "TouchCircle.png";
        private const string TouchFacePath = UiArtFolder + "TouchFace.png";
        private const string TouchGlossPath = UiArtFolder + "TouchGloss.png";
        private const string TouchGlowPath = UiArtFolder + "TouchGlow.png";
        private const string TouchRingPath = UiArtFolder + "TouchRing.png";
        private const string TouchSparkRingPath = UiArtFolder + "TouchSparkRing.png";
        private const string TouchStickBasePath = UiArtFolder + "TouchStickBase.png";

        // 丸の半径（絵の半径を1とする）。少し内側にして、縁のぼかしが切れないようにする
        private const float TouchRadius = 0.96f;
        // 面: 上が明るく下へ暗く、縁の近くも少し暗くして丸くふくらんで見せる
        private const float TouchFaceTopShade = 1f;
        private const float TouchFaceBottomShade = 0.74f;
        private const float TouchFaceEdgeShade = 0.82f;
        // リングは、白い縁（面の外）のところに重なる太さ。ボタンの面は直径の 88%（CreateTouchButton）
        private const float TouchRingCenter = 0.905f;
        private const float TouchRingHalfWidth = 0.055f;
        private const int SparkCount = 12;

        private static readonly Color StickBaseFillColor = new Color(0.05f, 0.08f, 0.20f, 0.42f);
        private static readonly Color StickRingColor = new Color(1f, 1f, 1f, 0.85f);
        private static readonly Color StickInnerRingColor = new Color(1f, 1f, 1f, 0.25f);
        private static readonly Color StickArrowColor = new Color(1f, 1f, 1f, 0.75f);

        private static void CreateTouchArt()
        {
            WriteUiSprite(TouchCirclePath, DrawIcon((p, px) => White(IconCoverage(p.magnitude - TouchRadius, px))), 0);
            WriteUiSprite(TouchFacePath, DrawIcon(TouchFacePixel), 0);
            WriteUiSprite(TouchGlossPath, DrawIcon(TouchGlossPixel), 0);
            WriteUiSprite(TouchGlowPath, DrawIcon((p, _) => White(Mathf.Pow(Mathf.Clamp01(1f - p.magnitude), 1.6f))), 0);
            WriteUiSprite(TouchRingPath, DrawIcon((p, px) => White(IconCoverage(Mathf.Abs(p.magnitude - TouchRingCenter) - TouchRingHalfWidth, px))), 0);
            WriteUiSprite(TouchSparkRingPath, DrawIcon(SparkRingPixel), 0);
            WriteUiSprite(TouchStickBasePath, DrawIcon(StickBasePixel), 0);
        }

        private static Color White(float alpha) => new Color(1f, 1f, 1f, alpha);

        private static Color TouchFacePixel(Vector2 p, float pixel)
        {
            var r = p.magnitude;
            var shade = Mathf.Lerp(TouchFaceBottomShade, TouchFaceTopShade, (p.y + 1f) * 0.5f);
            shade *= Mathf.Lerp(1f, TouchFaceEdgeShade, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.7f, TouchRadius, r)));
            return new Color(shade, shade, shade, IconCoverage(r - TouchRadius, pixel));
        }

        /// <summary>上の横長のつや。上ほど濃く、下へ消える</summary>
        private static Color TouchGlossPixel(Vector2 p, float pixel)
        {
            var center = new Vector2(0f, 0.4f);
            var radii = new Vector2(0.62f, 0.38f);
            var q = new Vector2((p.x - center.x) / radii.x, (p.y - center.y) / radii.y);
            // だ円の外までの長さを、短い方の半径で画素の長さに直す（縁のぼかしの近似）
            var distance = (q.magnitude - 1f) * radii.y;
            var fade = Mathf.Lerp(0.1f, 0.55f, Mathf.InverseLerp(0.05f, 0.78f, p.y));
            return White(IconCoverage(distance, pixel) * fade);
        }

        /// <summary>満タンの必殺技のまわりを回る、短い線の輪</summary>
        private static Color SparkRingPixel(Vector2 p, float pixel)
        {
            var r = p.magnitude;
            var turns = (Mathf.Atan2(p.y, p.x) / (Mathf.PI * 2f) + 1f) * SparkCount;
            var frac = Mathf.Abs(turns - Mathf.Round(turns));
            // 線は輪に沿った長さで太さを決める（外ほど長く見えないように）
            var along = frac / SparkCount * Mathf.PI * 2f * r;
            var spark = IconCoverage(Mathf.Abs(r - 0.88f) - 0.05f, pixel) * IconCoverage(along - 0.09f, pixel);
            return White(spark);
        }

        /// <summary>スティックの台: 暗い半透明の丸に、白い外の輪・うすい内の輪・上下左右の矢印</summary>
        private static Color StickBasePixel(Vector2 p, float pixel)
        {
            var r = p.magnitude;
            var color = StickBaseFillColor;
            color.a *= IconCoverage(r - TouchRadius, pixel);
            color = Over(color, StickInnerRingColor, IconCoverage(Mathf.Abs(r - 0.5f) - 0.012f, pixel));
            color = Over(color, StickRingColor, IconCoverage(Mathf.Abs(r - 0.92f) - 0.03f, pixel));
            for (var i = 0; i < 4; i++)
            {
                var angle = i * Mathf.PI * 0.5f;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                color = Over(color, StickArrowColor, IconCoverage(ChevronDistance(p, direction), pixel));
            }

            return color;
        }

        /// <summary>外向きの「＞」の形（縁からの距離）</summary>
        private static float ChevronDistance(Vector2 p, Vector2 direction)
        {
            var side = new Vector2(-direction.y, direction.x);
            var local = new Vector2(Vector2.Dot(p, direction), Vector2.Dot(p, side));
            var tip = new Vector2(0.8f, 0f);
            const float halfWidth = 0.028f;
            var upper = SegmentDistance(local, new Vector2(0.71f, 0.1f), tip);
            var lower = SegmentDistance(local, new Vector2(0.71f, -0.1f), tip);
            return Mathf.Min(upper, lower) - halfWidth;
        }

        private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + ab * t)).magnitude;
        }

        /// <summary>下の色に、上の色を amount の濃さで重ねる（ふつうのアルファの重ね方）</summary>
        private static Color Over(Color below, Color above, float amount)
        {
            var a = above.a * amount;
            var outAlpha = a + below.a * (1f - a);
            if (outAlpha <= 0f)
            {
                return Color.clear;
            }

            var rgb = ((Vector4)above * a + (Vector4)below * below.a * (1f - a)) / outAlpha;
            return new Color(rgb.x, rgb.y, rgb.z, outAlpha);
        }
    }
}
