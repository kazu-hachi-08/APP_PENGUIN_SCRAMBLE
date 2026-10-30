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

        public static Sprite Circle => _circle != null ? _circle : _circle = Create(CircleAlpha);
        public static Sprite Ring => _ring != null ? _ring : _ring = Create(RingAlpha);
        public static Sprite Square => _square != null ? _square : _square = Create((x, y) => 1f);

        /// <summary>真ん中ほど濃く、縁へなめらかに消える丸（光）</summary>
        public static Sprite Glow => _glow != null ? _glow : _glow = Create(GlowAlpha);

        private static float CircleAlpha(float x, float y)
        {
            var radius = Size * 0.5f;
            var distance = new Vector2(x - radius, y - radius).magnitude;
            return Mathf.Clamp01((radius - distance) / EdgeSoftness);
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

        private static Sprite Create(System.Func<float, float, float> alpha)
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            var pixels = new Color32[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    // ピクセルの中心で測る
                    var a = alpha(x + 0.5f, y + 0.5f);
                    pixels[y * Size + x] = new Color(1f, 1f, 1f, a);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, Size, Size), new Vector2(0.5f, 0.5f), Size);
        }
    }
}
