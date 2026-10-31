using PenmonScramble.Common.Editor;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// タイトルの見た目: 後ろで動く乱戦のジオラマ（TitleDiorama）とロゴ（§9.6）。
    /// ロゴの絵は Art/Title に置く。無ければ縁取りした文字で作る（絵を待たずに Rebuild できるように）
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string TitleArtFolder = "Assets/_Project/Games/100_PenmonScramble/Art/Title/";
        private const string TitleLogoPath = TitleArtFolder + "TitleLogo.png";
        // 1920x1080 の画面に足りる大きさ。これ以上はブラウザ版のダウンロードが重くなるだけ
        private const int TitleArtMaxSize = 2048;
        // ジオラマより遠くは描かない
        private const float TitleCameraFarClip = 60f;

        private const string LogoSmallLine = "乱戦！";
        private const string LogoMainLine = "ペンモンスクランブル";
        // スタートの画面では大きくまん中より上に、ロビーでは小さく上の札の間に置く（TitleLogoMotion.SetPose）
        private static readonly Vector2 LogoPosition = new Vector2(0f, 190f);
        private static readonly Vector2 LogoLobbyPosition = new Vector2(0f, 410f);
        private const float LogoLobbyScale = 0.48f;
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

        // 画面の上下のぼかし: 上の札・ロゴの下と、下の「タップしてスタート」・ボタンの下を暗くして読めるようにする
        private const float TitleTopFadeHeight = 300f;
        private const float TitleBottomFadeHeight = 420f;
        private static readonly Color TitleFadeColor = new Color(0.03f, 0.06f, 0.18f, 0.6f);

        private static readonly Color LabelOutlineColor = new Color(0.05f, 0.08f, 0.18f, 0.9f);
        private static readonly Vector2 LabelOutlineDistance = new Vector2(3f, -3f);
        private static readonly Color ButtonShadowColor = new Color(0f, 0f, 0f, 0.45f);

        /// <summary>
        /// ジオラマの役者と床は実行時に TitleDiorama が組む（ステージと同じく、シーンには保存しない）。
        /// ここではそれを映すカメラと、使うシェーダーだけを用意する
        /// </summary>
        private static void CreateTitleDiorama(GameObject root, Camera stageCamera)
        {
            var cameraObj = new GameObject("TitleCamera");
            var camera = cameraObj.AddComponent<Camera>();
            camera.farClipPlane = TitleCameraFarClip;
            // タイトルが開いたときに TitleDiorama が点ける
            camera.enabled = false;

            var diorama = root.AddComponent<TitleDiorama>();
            var so = new SerializedObject(diorama);
            so.FindProperty("_camera").objectReferenceValue = camera;
            so.FindProperty("_stageCamera").objectReferenceValue = stageCamera;
            so.FindProperty("_toonShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "Toon.shader");
            so.FindProperty("_blobShadowShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "BlobShadow.shader");
            so.FindProperty("_fxShader").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "Fx.shader");
            so.ApplyModifiedProperties();
        }

        private static void CreateTitleFades(Transform root)
        {
            CreateEdgeFade(root, "TopFade", top: true, TitleTopFadeHeight, TitleFadeColor);
            CreateEdgeFade(root, "BottomFade", top: false, TitleBottomFadeHeight, TitleFadeColor);
        }

        /// <summary>ロゴの絵があればそれを、無ければ「乱戦！」を斜めに小さく・「ペンモンスクランブル」を大きく文字で組む</summary>
        private static void CreateTitleLogo(Transform root)
        {
            var logo = UIDialogBuilder.CreateUIObject("Logo", root);
            PlaceCenter(logo.GetComponent<RectTransform>(), LogoPosition, LogoSize);
            logo.AddComponent<TitleLogoMotion>();

            var sprite = LoadArtSprite(TitleLogoPath, TitleArtMaxSize);
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
            // 枠より少し長くても折り返さない（「ル」だけが次の行に落ちないように）
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            // CreateLabel の細い縁取りを、ロゴ用の太い縁取りに替える
            var outline = text.GetComponent<Outline>();
            outline.effectColor = LogoOutlineColor;
            outline.effectDistance = LogoOutlineDistance;
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = LogoOutlineColor;
            shadow.effectDistance = LogoShadowDistance;
            return text;
        }

        /// <summary>Art/ の PNG をスプライトとして読む。無ければ null。取り込み設定が違えば直してから読む（透過はそのまま使う）</summary>
        private static Sprite LoadArtSprite(string path, int maxSize)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                return null;
            }

            if (importer.textureType != TextureImporterType.Sprite || importer.maxTextureSize != maxSize || !importer.alphaIsTransparency)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.maxTextureSize = maxSize;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
