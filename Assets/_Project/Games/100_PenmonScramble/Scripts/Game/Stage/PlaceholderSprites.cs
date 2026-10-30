using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 絵ができるまでの仮の形（丸・輪・三角・四角）を実行時に作る。どれも1マスの大きさ。
    /// アセットにしないのは、絵に差し替えたら丸ごと消すため
    /// </summary>
    public static class PlaceholderSprites
    {
        private const int Size = 64;
        // 縁を1ピクセルぼかして、拡大してもギザギザに見えないようにする
        private const float EdgeSoftness = 1.5f;

        // 輪の太さ（半径に対する割合）
        private const float RingThickness = 0.12f;

        private static Sprite _circle;
        private static Sprite _ring;
        private static Sprite _triangle;
        private static Sprite _square;
        private static Sprite _glow;

        public static Sprite Circle => _circle != null ? _circle : _circle = Create(CircleAlpha);
        public static Sprite Ring => _ring != null ? _ring : _ring = Create(RingAlpha);
        public static Sprite Triangle => _triangle != null ? _triangle : _triangle = Create(TriangleAlpha);
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

        /// <summary>右向きの三角（向きの矢印）。左端の辺から右の頂点へ細くなる</summary>
        private static float TriangleAlpha(float x, float y)
        {
            var halfHeight = (1f - x / Size) * Size * 0.5f;
            var distanceFromEdge = halfHeight - Mathf.Abs(y - Size * 0.5f);
            return Mathf.Clamp01(distanceFromEdge / EdgeSoftness);
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
