using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 技の形のアイコン。白で描くので、Image の色で属性の色に染めて使う。
    /// 7枚しかなく形も単純なので、絵を用意せずに実行時に描く（左の点が自分、右へ技が出る）
    /// </summary>
    public static class SkillShapeIcons
    {
        private const int Size = 128;
        // 縁をなめらかにする幅（画素）
        private const float Feather = 1.5f;
        private const float ZoneFillAlpha = 0.45f;

        private static readonly Vector2 User = new Vector2(0.2f, 0.5f);
        private const float UserRadius = 0.08f;

        private static readonly Dictionary<SkillShape, Sprite> Cache = new Dictionary<SkillShape, Sprite>();

        public static Sprite Get(SkillShape shape)
        {
            if (Cache.TryGetValue(shape, out var sprite) && sprite != null)
            {
                return sprite;
            }

            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { name = $"SkillShape_{shape}", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color32[Size * Size];
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var p = new Vector2((x + 0.5f) / Size, (y + 0.5f) / Size);
                    var alpha = Mathf.Clamp01(Coverage(shape, p));
                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            sprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f));
            Cache[shape] = sprite;
            return sprite;
        }

        /// <summary>p（0〜1）の濃さ。形ごとに、いくつかの図形の重ね合わせで描く</summary>
        private static float Coverage(SkillShape shape, Vector2 p)
        {
            var user = Fill(Circle(p, User, UserRadius));
            switch (shape)
            {
                case SkillShape.Melee:
                    return Mathf.Max(user, Fill(Sector(p, User, 0.62f, 50f)) * 0.85f);
                case SkillShape.Projectile:
                    return Max(user, Fill(Circle(p, new Vector2(0.78f, 0.5f), 0.11f)), Fill(Segment(p, new Vector2(0.38f, 0.5f), new Vector2(0.62f, 0.5f), 0.035f)) * 0.6f);
                case SkillShape.Spread:
                    return Max(user, Ball(p, -32f), Ball(p, 0f), Ball(p, 32f));
                case SkillShape.Dash:
                    return Max(Fill(Circle(p, new Vector2(0.78f, 0.5f), UserRadius)), Chevron(p, 0.36f), Chevron(p, 0.5f), Chevron(p, 0.64f));
                case SkillShape.Nova:
                    return Max(Fill(Circle(p, new Vector2(0.5f, 0.5f), UserRadius)), Fill(Ring(p, new Vector2(0.5f, 0.5f), 0.34f, 0.06f)));
                case SkillShape.Zone:
                    var zone = new Vector2(0.66f, 0.5f);
                    return Max(user, Fill(Circle(p, zone, 0.28f)) * ZoneFillAlpha, Fill(Ring(p, zone, 0.28f, 0.035f)));
                default:
                    var spot = new Vector2(0.66f, 0.28f);
                    return Max(user, Fill(Ring(p, spot, 0.16f, 0.03f)), Fill(Segment(p, new Vector2(0.66f, 0.86f), new Vector2(0.66f, 0.5f), 0.045f)),
                        Fill(Segment(p, new Vector2(0.66f, 0.46f), new Vector2(0.56f, 0.58f), 0.045f)), Fill(Segment(p, new Vector2(0.66f, 0.46f), new Vector2(0.76f, 0.58f), 0.045f)));
            }
        }

        private static float Ball(Vector2 p, float degrees)
        {
            var direction = Quaternion.Euler(0f, 0f, degrees) * Vector2.right;
            return Fill(Circle(p, User + (Vector2)direction * 0.55f, 0.09f));
        }

        private static float Chevron(Vector2 p, float x)
        {
            const float half = 0.14f;
            const float depth = 0.1f;
            const float width = 0.04f;
            var tip = new Vector2(x + depth, 0.5f);
            return Mathf.Max(Fill(Segment(p, new Vector2(x, 0.5f + half), tip, width)), Fill(Segment(p, new Vector2(x, 0.5f - half), tip, width)));
        }

        // 以下は符号付き距離（内側が負）。単位は画像の幅
        private static float Circle(Vector2 p, Vector2 center, float radius) => (p - center).magnitude - radius;

        private static float Ring(Vector2 p, Vector2 center, float radius, float halfWidth) => Mathf.Abs((p - center).magnitude - radius) - halfWidth;

        private static float Segment(Vector2 p, Vector2 a, Vector2 b, float halfWidth)
        {
            var ab = b - a;
            var t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - (a + ab * t)).magnitude - halfWidth;
        }

        /// <summary>右向きの扇形</summary>
        private static float Sector(Vector2 p, Vector2 center, float radius, float halfAngle)
        {
            var d = p - center;
            var outside = d.magnitude - radius;
            var angle = Mathf.Abs(Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            // 角度のはみ出しを、その場所の円周上の長さに直す
            var side = (angle - halfAngle) * Mathf.Deg2Rad * d.magnitude;
            return Mathf.Max(outside, side);
        }

        private static float Fill(float distance) => Mathf.Clamp01(0.5f - distance * Size / (Feather * 2f));

        private static float Max(params float[] values) => Mathf.Max(values);
    }
}
