using PenmonScramble.Common.Editor;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// タイトルの絵: 背景のキービジュアルとロゴ（§9）。
    /// 絵は Art/Title に置く。まだ無ければ、背景は単色・ロゴは縁取りした文字で作る（絵を待たずに Rebuild できるように）
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string TitleArtFolder = "Assets/_Project/Games/100_PenmonScramble/Art/Title/";
        private const string TitleBackgroundPath = TitleArtFolder + "TitleBackground.png";
        private const string TitleLogoPath = TitleArtFolder + "TitleLogo.png";
        // 1920x1080 の画面に足りる大きさ。これ以上はブラウザ版のダウンロードが重くなるだけ
        private const int TitleArtMaxSize = 2048;

        private const string LogoSmallLine = "乱戦！";
        private const string LogoMainLine = "ペンモンスクランブル";
        private static readonly Vector2 LogoPosition = new Vector2(0f, 270f);
        private static readonly Vector2 LogoSize = new Vector2(1300f, 420f);
        private static readonly Vector2 LogoSmallPosition = new Vector2(-380f, 110f);
        private static readonly Vector2 LogoSmallSize = new Vector2(500f, 140f);
        private const float LogoSmallTilt = 8f;
        private static readonly Vector2 LogoMainPosition = new Vector2(0f, -40f);
        private static readonly Vector2 LogoMainSize = new Vector2(1300f, 200f);
        private const int LogoSmallFontSize = 96;
        private const int LogoMainFontSize = 140;
        private static readonly Color LogoSmallColor = new Color(1f, 0.35f, 0.25f);
        private static readonly Color LogoMainColor = new Color(1f, 0.85f, 0.25f);
        private static readonly Color LogoOutlineColor = new Color(0.10f, 0.12f, 0.30f);
        private static readonly Vector2 LogoOutlineDistance = new Vector2(6f, -6f);
        private static readonly Vector2 LogoShadowDistance = new Vector2(10f, -14f);

        // 下の帯: ボタンと文字を絵の上でも読めるようにする
        private const float TitleBandHeight = 430f;
        private static readonly Color TitleBandColor = new Color(0.05f, 0.08f, 0.18f, 0.55f);

        private static readonly Color LabelOutlineColor = new Color(0.05f, 0.08f, 0.18f, 0.9f);
        private static readonly Vector2 LabelOutlineDistance = new Vector2(3f, -3f);
        private static readonly Color ButtonShadowColor = new Color(0f, 0f, 0f, 0.45f);
        private static readonly Vector2 ButtonShadowDistance = new Vector2(0f, -10f);

        private static void CreateTitleBackground(Transform root)
        {
            var sprite = LoadTitleArt(TitleBackgroundPath);
            if (sprite == null)
            {
                return;
            }

            var obj = UIDialogBuilder.CreateUIObject("Background", root);
            UIDialogBuilder.SetStretchAll(obj.GetComponent<RectTransform>());
            var image = obj.AddComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = false;
            // 画面の比率が違っても、すき間を作らず画面いっぱいに広げる（はみ出た端は切れる）
            var fitter = obj.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = sprite.rect.width / sprite.rect.height;
        }

        private static void CreateTitleBand(Transform root)
        {
            var obj = UIDialogBuilder.CreateUIObject("Band", root);
            var rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(0f, TitleBandHeight);
            var image = obj.AddComponent<Image>();
            image.color = TitleBandColor;
            image.raycastTarget = false;
        }

        /// <summary>ロゴの絵があればそれを、無ければ「乱戦！」を斜めに小さく・「ペンモンスクランブル」を大きく文字で組む</summary>
        private static void CreateTitleLogo(Transform root)
        {
            var logo = UIDialogBuilder.CreateUIObject("Logo", root);
            PlaceCenter(logo.GetComponent<RectTransform>(), LogoPosition, LogoSize);
            logo.AddComponent<TitleLogoMotion>();

            var sprite = LoadTitleArt(TitleLogoPath);
            if (sprite != null)
            {
                var image = logo.AddComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
                return;
            }

            var small = CreateLogoLine(logo.transform, "Small", LogoSmallLine, LogoSmallFontSize, LogoSmallColor, LogoSmallPosition, LogoSmallSize);
            small.rectTransform.localRotation = Quaternion.Euler(0f, 0f, LogoSmallTilt);
            CreateLogoLine(logo.transform, "Main", LogoMainLine, LogoMainFontSize, LogoMainColor, LogoMainPosition, LogoMainSize);
        }

        private static Text CreateLogoLine(Transform parent, string name, string value, int fontSize, Color color, Vector2 position, Vector2 size)
        {
            var text = CreateLabel(parent, name, value, fontSize, position, size);
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            // CreateLabel の細い縁取りを、ロゴ用の太い縁取りに替える
            var outline = text.GetComponent<Outline>();
            outline.effectColor = LogoOutlineColor;
            outline.effectDistance = LogoOutlineDistance;
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = LogoOutlineColor;
            shadow.effectDistance = LogoShadowDistance;
            return text;
        }

        /// <summary>Art/Title の PNG を UI 用のスプライトとして読む。取り込み設定が違えば直してから読む</summary>
        private static Sprite LoadTitleArt(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                return null;
            }

            if (importer.textureType != TextureImporterType.Sprite || importer.maxTextureSize != TitleArtMaxSize)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = TitleArtMaxSize;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
