using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 床に寝かせる範囲・輪（FloorDecal）と、はじける光（BurstFx）の形を実行時に作る。どれも1マスの大きさ。
    /// 単純な形なのでアセットにせず、ブラウザ版のダウンロードを増やさない
    /// </summary>
    public static class DecalSprites
    {
        private const int Size = 64;
        // 縁を1ピクセルぼかして、拡大してもギザギザに見えないようにする
        private const float EdgeSoftness = 1.5f;

        // 輪の太さ（半径に対する割合）
        private const float RingThickness = 0.12f;

        private static Sprite _circle;
        private static Sprite _ring;
        private static Sprite _square;
        private static Sprite _glow;
        private static readonly System.Collections.Generic.Dictionary<int, Sprite> Sectors = new System.Collections.Generic.Dictionary<int, Sprite>();

        // 扇は大きく引き伸ばして使うので、ほかより細かく作る
        private const int SectorSize = 128;

        public static Sprite Circle => _circle != null ? _circle : _circle = Create(CircleAlphaOf(Size));
        public static Sprite Ring => _ring != null ? _ring : _ring = Create(RingAlpha);
        public static Sprite Square => _square != null ? _square : _square = Create((x, y) => 1f);

        /// <summary>真ん中ほど濃く、縁へなめらかに消える丸（光）</summary>
        public static Sprite Glow => _glow != null ? _glow : _glow = Create(GlowAlpha);

        /// <summary>
        /// 真ん中を要にして +X へ開く扇（半分の角度は度）。絵の大きさは半径の2倍なので、置くときは半径×2 の大きさにする。
        /// 角度ごとに1枚作って使い回す（技の角度の種類は少ない）
        /// </summary>
        public static Sprite Sector(float halfAngleDegrees)
        {
            var key = Mathf.Clamp(Mathf.RoundToInt(halfAngleDegrees), 1, 180);
            if (!Sectors.TryGetValue(key, out var sprite) || sprite == null)
            {
                var cos = Mathf.Cos(key * Mathf.Deg2Rad);
                sprite = key >= 180 ? Create(CircleAlphaOf(SectorSize), SectorSize) : Create((x, y) => SectorAlpha(x, y, cos), SectorSize);
                Sectors[key] = sprite;
            }

            return sprite;
        }

        private static float SectorAlpha(float x, float y, float minCos)
        {
            var radius = SectorSize * 0.5f;
            var offset = new Vector2(x - radius, y - radius);
            var distance = offset.magnitude;
            var inside = Mathf.Clamp01((radius - distance) / EdgeSoftness);
            if (distance < EdgeSoftness)
            {
                return inside;
            }

            // 縁の線を角度でぼかすと要の近くで細くなりすぎるので、扇の辺までの距離（ピクセル）でぼかす
            var cos = offset.x / distance;
            var sin = Mathf.Sqrt(Mathf.Max(0f, 1f - cos * cos));
            var minSin = Mathf.Sqrt(Mathf.Max(0f, 1f - minCos * minCos));
            var edgeDistance = (cos * minSin - sin * minCos) * distance;
            return inside * Mathf.Clamp01(edgeDistance / EdgeSoftness + 0.5f);
        }

        private static System.Func<float, float, float> CircleAlphaOf(int size)
        {
            return (x, y) =>
            {
                var radius = size * 0.5f;
                var distance = new Vector2(x - radius, y - radius).magnitude;
                return Mathf.Clamp01((radius - distance) / EdgeSoftness);
            };
        }

        private static float GlowAlpha(float x, float y)
        {
            var radius = Size * 0.5f;
            var t = Mathf.Clamp01(new Vector2(x - radius, y - radius).magnitude / radius);
            return (1f - t) * (1f - t);
        }

        private static float RingAlpha(float x, float y)
        {
            var radius = Size * 0.5f;
            var distance = new Vector2(x - radius, y - radius).magnitude;
            var halfWidth = radius * RingThickness * 0.5f;
            var center = radius - halfWidth - EdgeSoftness;
            return Mathf.Clamp01((halfWidth - Mathf.Abs(distance - center)) / EdgeSoftness);
        }

        private static Sprite Create(System.Func<float, float, float> alpha, int size = Size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    // ピクセルの中心で測る
                    var a = alpha(x + 0.5f, y + 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
