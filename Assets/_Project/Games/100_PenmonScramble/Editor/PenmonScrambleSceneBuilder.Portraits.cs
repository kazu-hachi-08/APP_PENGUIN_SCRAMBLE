using System.Collections.Generic;
using System.IO;
using System.Linq;
using PenmonScramble.Core;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// UI の顔（一覧・ガチャ・カットイン）の絵を、ゲーム中と同じ3Dのペンギン・大ボスを撮って作る。
    /// Rebuild のたびに Art/Portraits/ へ撮り直すので、定義表の見た目を変えれば UI の顔も一緒に変わる。
    /// 少し斜め前から・背景は透過・右向き
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        private const string PortraitFolder = ArtFolder + "Portraits/";
        private const int PortraitSize = 512;
        private const int PortraitMsaa = 8;

        // 顔が画面の右を向くよう体を回し、少し上から見下ろす。
        // 大ボスは横に長く、カモメの翼は低い所から見ると細い線になるので、もっと上から見下ろす
        private const float PortraitYaw = -28f;
        private const float PortraitPitch = 12f;
        private const float BossPortraitYaw = -35f;
        private const float BossPortraitPitch = 32f;
        private const float PortraitCameraDistance = 20f;
        // 縁の線は形の外へはみ出すので、その分と余白を足す
        private const float PortraitMargin = 0.08f;
        // ペンギンはどの種類も同じ大きさで撮る（体型の違いが一覧で見えるように）。足元をそろえ、背の高い飾りがはみ出すときだけ引く。
        // 標準の体型（背の高さ約1.2）が絵の高さの 8 割ほどになる値
        private const float PenguinPortraitHalfSize = 0.78f;

        private static string PenguinPortraitPath(int speciesNo) => $"{PortraitFolder}Penguin{speciesNo:00}.png";

        private static string BossPortraitPath(int bossNo) => $"{PortraitFolder}Boss{bossNo}.png";

        /// <summary>
        /// 20種類と大ボス3体を撮って PNG にする。絵が変わらなければ書き直さない（取り込み直しと差分を出さないため）。
        /// 描画できない環境（-nographics）では撮らずに、前に撮った絵をそのまま使う
        /// </summary>
        private static void CapturePortraits()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                Debug.LogWarning("[PenmonScrambleSceneBuilder] 描画できないので、UI の顔は撮り直しませんでした");
                return;
            }

            Directory.CreateDirectory(PortraitFolder);
            var materials = new ToonMaterials(AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "Toon.shader"), AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "BlobShadow.shader"));
            var studio = new GameObject("PortraitStudio");
            try
            {
                var camera = CreatePortraitCamera(studio.transform);
                CreatePortraitLight(studio.transform);
                var changed = 0;
                for (var no = 1; no <= SpeciesTable.Count; no++)
                {
                    var model = PenguinModelBuilder.Build(PenguinLooks.Get(no), materials, studio.transform);
                    model.gameObject.AddComponent<PenguinMotion>().Apply(PenguinPose.Idle);
                    changed += ShootPortrait(camera, model, PortraitYaw, PortraitPitch, PenguinPortraitHalfSize, PenguinPortraitPath(no)) ? 1 : 0;
                }

                for (var i = 0; i < Balance.WorldCount; i++)
                {
                    var no = BigBossTable.SkuaNo + i;
                    var model = BigBossModelBuilder.Build(no, materials, studio.transform);
                    model.gameObject.AddComponent<BigBossMotion>().Apply(BigBossPose.Idle);
                    changed += ShootPortrait(camera, model, BossPortraitYaw, BossPortraitPitch, 0f, BossPortraitPath(no)) ? 1 : 0;
                }

                Debug.Log($"[PenmonScrambleSceneBuilder] UI の顔を撮りました（変わった絵 {changed} 枚）");
            }
            finally
            {
                Object.DestroyImmediate(studio);
                materials.DestroyAll();
            }

            AssetDatabase.Refresh();
        }

        private static Camera CreatePortraitCamera(Transform parent)
        {
            var obj = new GameObject("PortraitCamera");
            obj.transform.SetParent(parent, false);
            var camera = obj.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = PortraitCameraDistance * 2f;
            camera.aspect = 1f;
            return camera;
        }

        /// <summary>ステージと同じ太陽。カメラが前から見るので、左上の奥からの光が顔の側を照らすように合わせて回す</summary>
        private static void CreatePortraitLight(Transform parent)
        {
            var obj = new GameObject("PortraitSun");
            obj.transform.SetParent(parent, false);
            obj.transform.rotation = Quaternion.Euler(0f, 180f, 0f) * Quaternion.Euler(SunAngles);
            var light = obj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = SunColor;
            light.intensity = SunIntensity;
            light.shadows = LightShadows.None;
        }

        /// <summary>1体を撮って保存し、撮り終えたら消す。halfSize が 0 なら体に合わせて目いっぱいに撮る。書き直したら true</summary>
        private static bool ShootPortrait(Camera camera, ToyModel model, float yaw, float pitch, float halfSize, string path)
        {
            // 床に影を落とす場所が無いので、足元の丸い影は消す
            model.BlobShadow.enabled = false;
            model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            camera.transform.rotation = Quaternion.Euler(pitch, 180f, 0f);
            FramePortrait(camera, model, halfSize);
            var texture = RenderPortrait(camera, PortraitSize);
            var png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(model.gameObject);
            if (File.Exists(path) && File.ReadAllBytes(path).SequenceEqual(png))
            {
                return false;
            }

            File.WriteAllBytes(path, png);
            return true;
        }

        /// <summary>
        /// 部品の頂点をカメラから見た平面に写し、その広がりに合わせて構える。
        /// halfSize を決めたときは足元を絵の下の余白にそろえ、収まらないときだけ広げる
        /// </summary>
        private static void FramePortrait(Camera camera, ToyModel model, float halfSize)
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

            var size = max - min;
            var fit = Mathf.Max(size.x, size.y) * 0.5f * (1f + PortraitMargin * 2f);
            var half = Mathf.Max(fit, halfSize);
            var center = (min + max) * 0.5f;
            if (half > fit)
            {
                center.y = min.y - fit * PortraitMargin * 2f + half;
            }

            camera.orthographicSize = half;
            camera.transform.position = rotation * new Vector3(center.x, center.y, -PortraitCameraDistance);
        }

        private static IEnumerable<Vector3> ModelVertices(ToyModel model)
        {
            foreach (var filter in model.GetComponentsInChildren<MeshFilter>())
            {
                var renderer = filter.GetComponent<Renderer>();
                if (renderer == null || !renderer.enabled || filter.sharedMesh == null)
                {
                    continue;
                }

                var matrix = filter.transform.localToWorldMatrix;
                foreach (var vertex in filter.sharedMesh.vertices)
                {
                    yield return matrix.MultiplyPoint3x4(vertex);
                }
            }
        }

        /// <summary>
        /// 透過で撮る。縁は背景の透明な黒と混ざって暗くなるので、色を透明度で割り戻す
        /// （UI は混ざる前の色として重ねるため、割り戻さないと縁に黒いにじみが出る）
        /// </summary>
        private static Texture2D RenderPortrait(Camera camera, int size)
        {
            var target = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, PortraitMsaa);
            camera.targetTexture = target;
            camera.Render();
            camera.targetTexture = null;

            RenderTexture.active = target;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(target);

            var pixels = texture.GetPixels32();
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Unpremultiply(pixels[i]);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static Color32 Unpremultiply(Color32 c)
        {
            if (c.a == 0)
            {
                return new Color32(0, 0, 0, 0);
            }

            if (c.a == 255)
            {
                return c;
            }

            return new Color32(Divide(c.r, c.a), Divide(c.g, c.a), Divide(c.b, c.a), c.a);
        }

        private static byte Divide(byte value, byte alpha) => (byte)Mathf.Min(255, value * 255 / alpha);
    }
}
