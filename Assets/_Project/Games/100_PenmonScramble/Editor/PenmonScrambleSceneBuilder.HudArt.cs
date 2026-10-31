using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// ステージの HUD の絵:斜めに切った板（面・縁）と、5つに区切った必殺技のゲージの輪、画面の外を指すとがった丸。
    /// メニューの部品（UiArt）と同じく、Rebuild のたびにコードで描いて Art/UI/ に置く。白で描いて Image の色で塗る
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string HudSlantPath = UiArtFolder + "HudSlant.png";
        private const string HudSlantFramePath = UiArtFolder + "HudSlantFrame.png";
        private const string HudSegmentRingPath = UiArtFolder + "HudSegmentRing.png";
        private const string HudMarkerPinPath = UiArtFolder + "HudMarkerPin.png";

        // 斜めの板: 高さ 64 で、上が右へ 0.4 ずれる平行四辺形。角は少しだけ丸める。
        // 9スライスは左右だけ（上下を切ると、伸ばしたときに斜めの辺が途中で折れるため）
        private const int SlantSpriteWidth = 160;
        private const int SlantSpriteHeight = 64;
        private const float SlantSlope = 0.4f;
        private const float SlantCorner = 6f;
        private const int SlantBorder = 24;
        private const float SlantFrameThickness = 3f;
        private const float SlantTopShade = 1f;
        private const float SlantBottomShade = 0.78f;

        // 区切りの輪: スマホのボタンのリングと同じ太さ。区切りのすき間は輪に沿った長さ
        private const int GaugeSegments = 5;
        private const float SegmentGap = 0.045f;

        // 画面の外を指す印: 真ん中の丸（回す中心）から、右へとがった先が出る。丸の半径とがった先の位置（絵の半分を1とする）
        private const float PinDiscRadius = 0.6f;
        private const float PinTip = 0.98f;

        private static void CreateHudArt()
        {
            WriteUiSprite(HudSlantPath, DrawSlant(SlantPixel), SlantBorder, horizontalOnly: true);
            WriteUiSprite(HudSlantFramePath, DrawSlant(SlantFramePixel), SlantBorder, horizontalOnly: true);
            WriteUiSprite(HudSegmentRingPath, DrawIcon(SegmentRingPixel), 0);
            WriteUiSprite(HudMarkerPinPath, DrawIcon((p, px) => White(IconCoverage(PinDistance(p), px))), 0);
        }

        /// <summary>丸と、先から丸へ引いた2本の接線で囲んだ三角を合わせた形（しずくを横に倒した形）の、縁からの距離</summary>
        private static float PinDistance(Vector2 p)
        {
            var q = new Vector2(p.x, Mathf.Abs(p.y));
            var disc = q.magnitude - PinDiscRadius;
            // 先から丸への接線: 先と中心を結ぶ線から sinα = 半径 / 先までの距離 だけ開く
            var sin = PinDiscRadius / PinTip;
            var cos = Mathf.Sqrt(1f - sin * sin);
            var tip = new Vector2(PinTip, 0f);
            var line = Vector2.Dot(q - tip, new Vector2(sin, cos));
            // 接点より左は丸が受け持つ
            var touchX = PinDiscRadius * sin;
            var cone = Mathf.Max(line, touchX - q.x);
            return Mathf.Min(disc, cone);
        }

        /// <summary>平行四辺形を、縁からの距離（内側が負）と縦の位置（0=下・1=上）から1画素ずつ塗る</summary>
        private static Texture2D DrawSlant(System.Func<float, float, Color> pixel)
        {
            var half = new Vector2(SlantSpriteWidth, SlantSpriteHeight) * 0.5f;
            // 傾けた座標では、横の半分の幅が傾いた分だけ狭くなる。1画素あけて縁のぼかしが切れないようにする
            var skewedHalf = new Vector2(half.x - SlantSlope * half.y - 1f, half.y - 1f);
            return PaintTexture(SlantSpriteWidth, SlantSpriteHeight, (x, y) =>
            {
                var p = new Vector2(x + 0.5f, y + 0.5f) - half;
                p.x -= SlantSlope * p.y;
                return pixel(RoundRectDistance(p, skewedHalf, SlantCorner), (y + 0.5f) / SlantSpriteHeight);
            });
        }

        /// <summary>上が明るく下へ少し暗い面（厚みのある札に見せる）</summary>
        private static Color SlantPixel(float distance, float v)
        {
            var shade = Mathf.Lerp(SlantBottomShade, SlantTopShade, v);
            return new Color(shade, shade, shade, Coverage(distance));
        }

        private static Color SlantFramePixel(float distance, float v)
        {
            return White(Coverage(distance) * Coverage(-distance - SlantFrameThickness));
        }

        /// <summary>上から時計回りに5つに区切った輪（Radial360 で1区切りずつたまって見える）</summary>
        private static Color SegmentRingPixel(Vector2 p, float pixel)
        {
            var r = p.magnitude;
            // 上（+y）を 0 に、時計回りに増える角度
            var turns = (Mathf.Atan2(p.x, p.y) / (Mathf.PI * 2f) + 1f) % 1f * GaugeSegments;
            var frac = Mathf.Abs(turns - Mathf.Round(turns));
            var along = frac / GaugeSegments * Mathf.PI * 2f * r;
            var ring = IconCoverage(Mathf.Abs(r - TouchRingCenter) - TouchRingHalfWidth, pixel);
            return White(ring * IconCoverage(SegmentGap - along, pixel));
        }
    }
}
