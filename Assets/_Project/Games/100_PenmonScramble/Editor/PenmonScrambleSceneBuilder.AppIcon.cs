using System.IO;
using System.Linq;
using PenmonScramble.Core;
using PenmonScramble.Game;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// アプリのアイコン。Rebuild のたびに、ゲーム中と同じ3Dのアデリンを寄りで撮り、コードで描いた背景に重ねて作る。
    /// Windows / Linux（デスクトップ共通）・Android（前と背景を分けたアダプティブ。Unity 6.6 からは Android はこれだけ）・ブラウザ（favicon）に入れる。
    /// 生成物なので手で直さない（見た目は PenguinLooks とここの定数を直す）
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string IconFolder = ArtFolder + "Icon/";
        private const string IconPath = IconFolder + "AppIcon.png";
        private const string IconForegroundPath = IconFolder + "AppIconForeground.png";
        private const string IconBackgroundPath = IconFolder + "AppIconBackground.png";
        private const string FaviconPath = "Assets/WebGLTemplates/PenmonScramble/favicon.png";
        private const int IconSize = 1024;
        private const int FaviconSize = 256;

        // 主人公格のアデリン（No.1・負けずぎらいのツッパリ）が、こちらへヒレを振りかぶる
        private const int IconSpeciesNo = 1;
        private const float IconAttackProgress = 0.3f;
        private const float IconYaw = -22f;
        private const float IconPitch = 10f;
        // 頭のてっぺんから下へ、背の高さのこの割合までを写す（胸から上の寄り）
        private const float IconVisibleHeight = 0.8f;
        // 頭のてっぺんの上の余白（絵の高さに対する割合）
        private const float IconTopMargin = 0.06f;
        // Android のアダプティブアイコンは、端末の切り抜きで外側が欠ける。真ん中の 66% に収めれば必ず見える
        private const float AdaptiveSafeZone = 0.66f;

        // 背景: まん中が明るい氷の水色、外へ行くほど深い青。放射の筋で「乱戦」の勢いを出す
        private static readonly Color IconCenterColor = new Color(0.62f, 0.90f, 1f);
        private static readonly Color IconEdgeColor = new Color(0.08f, 0.30f, 0.70f);
        private static readonly Vector2 IconBurstCenter = new Vector2(0.5f, 0.62f);
        private const int IconBurstRays = 14;
        private const float IconBurstStrength = 0.08f;
        // 角丸の半径（絵の幅に対する割合）
        private const float IconCornerRadius = 0.2f;

        private static void CaptureAppIcons()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Debug.LogWarning("[PenmonScrambleSceneBuilder] 描画できないので、アイコンは作り直しませんでした");
                return;
            }

            Directory.CreateDirectory(IconFolder);
            var (figure, adaptiveFigure) = ShootIconFigures();
            var background = DrawIconBackground(IconSize);
            var full = Composite(background, figure);
            var changed = 0;
            changed += WriteIfChanged(IconPath, Mask(full, IconCornerRadius)) ? 1 : 0;
            changed += WriteIfChanged(IconForegroundPath, adaptiveFigure) ? 1 : 0;
            changed += WriteIfChanged(IconBackgroundPath, background) ? 1 : 0;
            changed += WriteIfChanged(FaviconPath, Resize(Mask(full, IconCornerRadius), FaviconSize)) ? 1 : 0;
            AssetDatabase.Refresh();
            ApplyIcons();
            Debug.Log($"[PenmonScrambleSceneBuilder] アイコンを作りました（変わった絵 {changed} 枚）");
        }

        /// <summary>アデリンを、アイコン全体用と、アダプティブの前の層用（安全な範囲に収める）の2枚撮る</summary>
        private static (Color32[] full, Color32[] adaptive) ShootIconFigures()
        {
            var materials = new ToonMaterials(AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "Toon.shader"), AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "BlobShadow.shader"));
            var studio = new GameObject("IconStudio");
            try
            {
                var camera = CreatePortraitCamera(studio.transform);
                CreatePortraitLight(studio.transform);
                var model = PenguinModelBuilder.Build(PenguinLooks.Get(IconSpeciesNo), materials, studio.transform);
                model.BlobShadow.enabled = false;
                var pose = PenguinPose.Idle;
                pose.Attack = IconAttackProgress;
                pose.AttackShape = SkillShape.Melee;
                model.gameObject.AddComponent<PenguinMotion>().Apply(pose);
                model.transform.localRotation = Quaternion.Euler(0f, IconYaw, 0f);
                camera.transform.rotation = Quaternion.Euler(IconPitch, 180f, 0f);

                FrameIcon(camera, model, 1f);
                var full = RenderIconLayer(camera);
                FrameIcon(camera, model, 1f / AdaptiveSafeZone);
                var adaptive = RenderIconLayer(camera);
                return (full, adaptive);
            }
            finally
            {
                Object.DestroyImmediate(studio);
                materials.DestroyAll();
            }
        }

        /// <summary>
        /// 頭のてっぺんを上の余白にそろえ、胸から上を写す。zoomOut を大きくすると、同じ構図のまま周りを広く写す
        /// （アダプティブの前の層は、端末が外側を切り抜くので、その分だけ引いて撮る）
        /// </summary>
        private static void FrameIcon(Camera camera, ToyModel model, float zoomOut)
        {
            var rotation = camera.transform.rotation;
            var inverse = Quaternion.Inverse(rotation);
            var min = new Vector2(float.MaxValue, float.MaxValue);
            var max = new Vector2(float.MinValue, float.MinValue);
            foreach (var point in ModelVertices(model))
            {
                var p = inverse * point;
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }

            var half = (max.y - min.y) * IconVisibleHeight / (2f * (1f - IconTopMargin * 2f));
            var top = max.y + half * IconTopMargin * 2f;
            var center = new Vector2((min.x + max.x) * 0.5f, top - half);
            camera.orthographicSize = half * zoomOut;
            camera.transform.position = rotation * new Vector3(center.x, center.y, -PortraitCameraDistance);
        }

        private static Color32[] RenderIconLayer(Camera camera)
        {
            var texture = RenderPortrait(camera, IconSize);
            var pixels = texture.GetPixels32();
            Object.DestroyImmediate(texture);
            return pixels;
        }

        /// <summary>放射状のグラデーションと、交互に少し明るい放射の筋</summary>
        private static Color32[] DrawIconBackground(int size)
        {
            var pixels = new Color32[size * size];
            var center = IconBurstCenter * size;
            var reach = size * 0.75f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var offset = new Vector2(x + 0.5f, y + 0.5f) - center;
                    var t = Mathf.Clamp01(offset.magnitude / reach);
                    var color = Color.Lerp(IconCenterColor, IconEdgeColor, t * t * (3f - 2f * t));
                    var angle = Mathf.Atan2(offset.y, offset.x) / (Mathf.PI * 2f) * IconBurstRays;
                    // 筋の境目はなめらかにぼかす（小さいアイコンでちらつかないように）
                    var ray = Mathf.SmoothStep(0f, 1f, Mathf.Abs(Mathf.Repeat(angle, 1f) - 0.5f) * 4f - 0.5f);
                    color = Color.Lerp(color, Color.white, ray * IconBurstStrength * (1f - t * 0.5f));
                    pixels[y * size + x] = color;
                }
            }

            return pixels;
        }

        /// <summary>背景の上に、透過で撮ったペンギンを重ねる</summary>
        private static Color32[] Composite(Color32[] background, Color32[] figure)
        {
            var pixels = new Color32[background.Length];
            for (var i = 0; i < pixels.Length; i++)
            {
                var front = (Color)figure[i];
                var color = Color.Lerp(background[i], front, front.a);
                color.a = 1f;
                pixels[i] = color;
            }

            return pixels;
        }

        /// <summary>角を丸く切り抜く。縁は1ピクセルぶんぼかす</summary>
        private static Color32[] Mask(Color32[] source, float radius)
        {
            var size = (int)Mathf.Sqrt(source.Length);
            var r = radius * size;
            var inner = size * 0.5f - r;
            var pixels = new Color32[source.Length];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = Mathf.Max(0f, Mathf.Abs(x + 0.5f - size * 0.5f) - inner);
                    var dy = Mathf.Max(0f, Mathf.Abs(y + 0.5f - size * 0.5f) - inner);
                    var coverage = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    var c = source[y * size + x];
                    c.a = (byte)(c.a * coverage);
                    pixels[y * size + x] = c;
                }
            }

            return pixels;
        }

        /// <summary>小さいアイコン用に縮める（GPU で縮めて読み戻す）</summary>
        private static Color32[] Resize(Color32[] source, int size)
        {
            var from = (int)Mathf.Sqrt(source.Length);
            var src = new Texture2D(from, from, TextureFormat.RGBA32, true);
            src.SetPixels32(source);
            src.Apply(true);
            var target = RenderTexture.GetTemporary(size, size, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Graphics.Blit(src, target);
            RenderTexture.active = target;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(target);
            var pixels = texture.GetPixels32();
            Object.DestroyImmediate(src);
            Object.DestroyImmediate(texture);
            return pixels;
        }

        /// <summary>PNG にして書く。中身が同じなら書き直さない（取り込み直しと差分を出さないため）</summary>
        private static bool WriteIfChanged(string path, Color32[] pixels)
        {
            var size = (int)Mathf.Sqrt(pixels.Length);
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.SetPixels32(pixels);
            texture.Apply();
            var png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            if (File.Exists(path) && File.ReadAllBytes(path).SequenceEqual(png))
            {
                return false;
            }

            File.WriteAllBytes(path, png);
            return true;
        }

        /// <summary>Player Settings のアイコンへ入れる。デスクトップ（Windows / Linux 共通）と Android</summary>
        private static void ApplyIcons()
        {
            var icon = LoadIconTexture(IconPath);
            var foreground = LoadIconTexture(IconForegroundPath);
            var background = LoadIconTexture(IconBackgroundPath);
            if (icon == null || foreground == null || background == null)
            {
                return;
            }

            // 環境ごとの指定が無いときに使われる既定のアイコンと、デスクトップの全サイズ
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            var standaloneSizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Standalone, IconKind.Application);
            PlayerSettings.SetIcons(NamedBuildTarget.Standalone, Enumerable.Repeat(icon, standaloneSizes.Length).ToArray(), IconKind.Application);

            // Android はアダプティブだけ。層 0 が背景、層 1 が前
            var android = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive);
            foreach (var platformIcon in android)
            {
                platformIcon.SetTextures(background, foreground);
            }

            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, AndroidPlatformIconKind.Adaptive, android);
        }

        /// <summary>アイコン用の取り込み設定（圧縮しない・縮めない）にそろえてから読む</summary>
        private static Texture2D LoadIconTexture(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                return null;
            }

            if (importer.textureType != TextureImporterType.Default || importer.maxTextureSize != IconSize || importer.textureCompression != TextureImporterCompression.Uncompressed
                || importer.npotScale != TextureImporterNPOTScale.None || !importer.alphaIsTransparency || importer.mipmapEnabled)
            {
                importer.textureType = TextureImporterType.Default;
                importer.maxTextureSize = IconSize;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
