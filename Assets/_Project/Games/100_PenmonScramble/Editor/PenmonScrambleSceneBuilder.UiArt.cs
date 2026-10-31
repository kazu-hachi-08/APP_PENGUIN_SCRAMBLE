using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// メニューの部品の絵（§9.6）: ボタンの色の面・つや・白い縁、角の丸い板、上下を暗くするぼかし、アイコン（歯車・コイン・ガチャのカプセル）。
    /// Rebuild のたびにコードで描いて Art/UI/ に PNG で置く（ボタンの形を変えるときはここを直す。生成物なので手で直さない）。
    /// ボタンと板は白で描いて Image の色で塗る（1枚の絵でどの色のボタンも作れるように）。9スライスなので大きさは自由
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string UiArtFolder = ArtFolder + "UI/";
        private const string ButtonBodyPath = UiArtFolder + "ButtonBody.png";
        private const string ButtonGlossPath = UiArtFolder + "ButtonGloss.png";
        private const string ButtonFramePath = UiArtFolder + "ButtonFrame.png";
        private const string RoundRectPath = UiArtFolder + "RoundRect.png";
        private const string FadePath = UiArtFolder + "Fade.png";
        private const string GearIconPath = UiArtFolder + "IconGear.png";
        private const string CoinIconPath = UiArtFolder + "IconCoin.png";
        private const string CapsuleIconPath = UiArtFolder + "IconCapsule.png";

        // 角の丸い絵: 128 四方・角の半径 40。9スライスの境目は半径より少し内側まで取る（つやのぼかしが伸びないように）
        private const int RoundSpriteSize = 128;
        private const float RoundSpriteRadius = 40f;
        private const int RoundSpriteBorder = 44;
        private const float FrameThickness = 5f;
        private const float GlossInset = 7f;
        // 色の面: 上が明るく下へ少し暗く、いちばん下は厚みに見える濃い帯
        private const float BodyTopShade = 1f;
        private const float BodyBottomShade = 0.8f;
        private const float BodyLipShade = 0.6f;
        private const float BodyLipHeight = 12f;
        private const float GlossTopAlpha = 0.45f;
        private const float GlossBottomAlpha = 0.08f;
        private const int FadeHeight = 128;
        private const int UiIconSize = 256;

        private static readonly Color CoinRimColor = new Color(0.80f, 0.52f, 0.08f);
        private static readonly Color CoinFaceColor = new Color(1f, 0.78f, 0.20f);
        private static readonly Color CoinStarColor = new Color(1f, 0.93f, 0.55f);
        // カプセルは水色のふたと、だいだいの受け皿。どこかの玉に見えないよう、合わせ目を傾けて黒い線は使わない
        private static readonly Color CapsuleTopColor = new Color(0.62f, 0.88f, 1f);
        private static readonly Color CapsuleBottomColor = new Color(1f, 0.62f, 0.22f);
        private const float CapsuleTilt = 20f;
        private const float CapsuleRimDarken = 0.7f;

        /// <summary>部品の絵を描いて取り込む。絵が変わらなければ書き直さない（取り込み直しと差分を出さないため）</summary>
        private static void CreateUiArt()
        {
            Directory.CreateDirectory(UiArtFolder);
            WriteUiSprite(ButtonBodyPath, DrawRound(RoundSpriteSize, RoundSpriteSize, ButtonBodyPixel), RoundSpriteBorder);
            WriteUiSprite(ButtonGlossPath, DrawRound(RoundSpriteSize, RoundSpriteSize, ButtonGlossPixel), RoundSpriteBorder);
            WriteUiSprite(ButtonFramePath, DrawRound(RoundSpriteSize, RoundSpriteSize, ButtonFramePixel), RoundSpriteBorder);
            WriteUiSprite(RoundRectPath, DrawRound(RoundSpriteSize, RoundSpriteSize, (d, _) => Color.white * Coverage(d)), RoundSpriteBorder);
            WriteUiSprite(FadePath, DrawFade(), 0);
            WriteUiSprite(GearIconPath, DrawIcon(GearPixel), 0);
            WriteUiSprite(CoinIconPath, DrawIcon(CoinPixel), 0);
            WriteUiSprite(CapsuleIconPath, DrawIcon(CapsulePixel), 0);
            CreateTouchArt();
        }

        private static Sprite UiSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static void WriteUiSprite(string path, Texture2D texture, int border)
        {
            var png = texture.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(texture);
            if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(png))
            {
                File.WriteAllBytes(path, png);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var spriteBorder = Vector4.one * border;
            if (importer.textureType == TextureImporterType.Sprite && importer.spriteBorder == spriteBorder && !importer.mipmapEnabled)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = spriteBorder;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        // ---- 角の丸い板 ----

        /// <summary>角の丸い四角の形を、縁からの距離（内側が負）と縦の位置（0=下・1=上）から1画素ずつ塗る</summary>
        private static Texture2D DrawRound(int width, int height, Func<float, float, Color> pixel)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var half = new Vector2(width, height) * 0.5f;
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f) - half;
                    texture.SetPixel(x, y, pixel(RoundRectDistance(p, half, RoundSpriteRadius), (y + 0.5f) / height));
                }
            }

            texture.Apply();
            return texture;
        }

        private static float RoundRectDistance(Vector2 p, Vector2 half, float radius)
        {
            var q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - half + Vector2.one * radius;
            return Vector2.Max(q, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        /// <summary>縁をなめらかにする（1画素ぶんだけぼかす）</summary>
        private static float Coverage(float distance) => Mathf.Clamp01(0.5f - distance);

        private static Color ButtonBodyPixel(float distance, float v)
        {
            var heightPx = v * RoundSpriteSize;
            var shade = heightPx < BodyLipHeight ? BodyLipShade : Mathf.Lerp(BodyBottomShade, BodyTopShade, v);
            return new Color(shade, shade, shade, Coverage(distance));
        }

        /// <summary>上半分に白いつや。下へ向かって消える</summary>
        private static Color ButtonGlossPixel(float distance, float v)
        {
            var alpha = v < 0.5f ? 0f : Mathf.Lerp(GlossBottomAlpha, GlossTopAlpha, (v - 0.5f) * 2f);
            return new Color(1f, 1f, 1f, alpha * Coverage(distance + GlossInset));
        }

        private static Color ButtonFramePixel(float distance, float v)
        {
            var ring = Coverage(distance) * Coverage(-distance - FrameThickness);
            return new Color(1f, 1f, 1f, ring);
        }

        /// <summary>下が濃く上へ消える帯。画面の上下を暗くして、ジオラマの上でも文字やボタンを読めるようにする</summary>
        private static Texture2D DrawFade()
        {
            var texture = new Texture2D(1, FadeHeight, TextureFormat.RGBA32, false);
            for (var y = 0; y < FadeHeight; y++)
            {
                var t = 1f - (y + 0.5f) / FadeHeight;
                texture.SetPixel(0, y, new Color(1f, 1f, 1f, t * t * (3f - 2f * t)));
            }

            texture.Apply();
            return texture;
        }

        // ---- アイコン ----

        /// <summary>まん中が原点・半径1の座標で1画素ずつ塗る。scale は1画素の大きさ（縁のぼかしに使う）</summary>
        private static Texture2D DrawIcon(Func<Vector2, float, Color> pixel)
        {
            var texture = new Texture2D(UiIconSize, UiIconSize, TextureFormat.RGBA32, false);
            var scale = 2f / UiIconSize;
            for (var y = 0; y < UiIconSize; y++)
            {
                for (var x = 0; x < UiIconSize; x++)
                {
                    var p = new Vector2((x + 0.5f) * scale - 1f, (y + 0.5f) * scale - 1f);
                    texture.SetPixel(x, y, pixel(p, scale));
                }
            }

            texture.Apply();
            return texture;
        }

        private static float IconCoverage(float distance, float pixel) => Mathf.Clamp01(0.5f - distance / pixel);

        /// <summary>白い歯車（Image の色で塗る）。8枚の歯と、まん中の穴</summary>
        private static Color GearPixel(Vector2 p, float pixel)
        {
            const int teeth = 8;
            const float body = 0.66f;
            const float tooth = 0.9f;
            const float hole = 0.28f;
            var r = p.magnitude;
            var angle = Mathf.Atan2(p.y, p.x) / (Mathf.PI * 2f) * teeth;
            var frac = Mathf.Abs(angle - Mathf.Round(angle));
            // 歯は角を少し落とした台形にする（frac が 0 のところが歯のまん中）
            var outer = Mathf.Lerp(tooth, body, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.18f, 0.3f, frac)));
            var coverage = IconCoverage(r - outer, pixel) * IconCoverage(hole - r, pixel);
            return new Color(1f, 1f, 1f, coverage);
        }

        /// <summary>金色のコイン: 濃い縁・明るい面・まん中に星</summary>
        private static Color CoinPixel(Vector2 p, float pixel)
        {
            const float outer = 0.94f;
            const float face = 0.78f;
            var r = p.magnitude;
            var color = Color.Lerp(CoinRimColor, CoinFaceColor, IconCoverage(r - face, pixel));
            // 左上だけ少し明るくして、丸く光って見せる
            color = Color.Lerp(color, Color.white, Mathf.Clamp01(Vector2.Dot(p, new Vector2(-0.6f, 0.8f)) - 0.35f) * 0.6f);
            color = Color.Lerp(color, CoinStarColor, IconCoverage(StarDistance(p, 0.46f, 0.45f), pixel));
            color.a = IconCoverage(r - outer, pixel);
            return color;
        }

        /// <summary>ガチャのカプセル: 丸い玉の上は水色・下はだいだい、傾いた白い合わせ目。縁は同じ色を濃くした線</summary>
        private static Color CapsulePixel(Vector2 p, float pixel)
        {
            const float radius = 0.86f;
            const float rim = 0.07f;
            const float seam = 0.035f;
            var q = (Vector2)(Quaternion.Euler(0f, 0f, -CapsuleTilt) * p);
            var distance = q.magnitude - radius;
            var half = q.y > 0f ? CapsuleTopColor : CapsuleBottomColor;
            var color = Color.Lerp(half, half * CapsuleRimDarken, 1f - IconCoverage(distance + rim, pixel));
            color = Color.Lerp(color, Color.white, IconCoverage(Mathf.Abs(q.y) - seam, pixel));
            // ふたの左上に小さなつや
            color = Color.Lerp(color, Color.white, IconCoverage((q - new Vector2(-0.32f, 0.48f)).magnitude - 0.13f, pixel) * 0.85f);
            color.a = IconCoverage(distance, pixel);
            return color;
        }

        /// <summary>5つの角の星の、縁からの距離（内側が負）。r は大きさ、rf はへこみの深さ（iq さんの sdStar5）</summary>
        private static float StarDistance(Vector2 p, float r, float rf)
        {
            var k1 = new Vector2(0.809016994375f, -0.587785252292f);
            var k2 = new Vector2(-k1.x, k1.y);
            p.x = Mathf.Abs(p.x);
            p -= 2f * Mathf.Max(Vector2.Dot(k1, p), 0f) * k1;
            p -= 2f * Mathf.Max(Vector2.Dot(k2, p), 0f) * k2;
            p.x = Mathf.Abs(p.x);
            p.y -= r;
            var ba = rf * new Vector2(-k1.y, k1.x) - new Vector2(0f, 1f);
            var h = Mathf.Clamp(Vector2.Dot(p, ba) / Vector2.Dot(ba, ba), 0f, r);
            return (p - ba * h).magnitude * Mathf.Sign(p.y * ba.x - p.x * ba.y);
        }
    }
}
