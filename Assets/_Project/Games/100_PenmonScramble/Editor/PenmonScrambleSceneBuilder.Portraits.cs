using System.Collections.Generic;
using System.IO;
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

        // 撮った絵の写っている所のうち、いちばん多い色がこの割合を超えたら「シェーダーが間に合わず仮の色で描かれた」とみなす。
        // まともな絵は陰の段・白黒の模様・縁の線で色が分かれて 0.13〜0.33 ほど、仮の色で撮れた絵は 0.6 ほどになる
        private const float BlankShotDominantShare = 0.5f;
        // 色を数えるときに下の桁を捨てる量（隣り合う陰のわずかな違いを同じ色として数える）
        private const int BlankShotColorShift = 3;
        // 半分より透けた所は縁のにじみなので数えない
        private const byte BlankShotMinAlpha = 128;
        // 準備の空撮りは小さくてよい（シェーダーを読み込ませるだけ）
        private const int WarmUpShotSize = 64;

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
            WithPortraitStudio("PortraitStudio", (camera, materials, parent) =>
            {
                var changed = 0;
                var failed = 0;
                for (var no = 1; no <= SpeciesTable.Count; no++)
                {
                    var model = PenguinModelBuilder.Build(PenguinLooks.Get(no), materials, parent);
                    model.gameObject.AddComponent<PenguinMotion>().Apply(PenguinPose.Idle);
                    Tally(ShootPortrait(camera, model, PortraitYaw, PortraitPitch, PenguinPortraitHalfSize, PenguinPortraitPath(no)), ref changed, ref failed);
                }

                for (var i = 0; i < BigBossTable.Count; i++)
                {
                    var no = BigBossTable.SkuaNo + i;
                    var model = BigBossModelBuilder.Build(no, materials, parent);
                    model.gameObject.AddComponent<BigBossMotion>().Apply(BigBossPose.Idle);
                    Tally(ShootPortrait(camera, model, BossPortraitYaw, BossPortraitPitch, 0f, BossPortraitPath(no)), ref changed, ref failed);
                }

                Debug.Log($"[PenmonScrambleSceneBuilder] UI の顔を撮りました（変わった絵 {changed} 枚・撮り損ねて前の絵のままにした絵 {failed} 枚）");
                return changed;
            });

            AssetDatabase.Refresh();
        }

        private enum ShotResult
        {
            Unchanged,
            Written,
            Failed,
        }

        private static void Tally(ShotResult result, ref int changed, ref int failed)
        {
            changed += result == ShotResult.Written ? 1 : 0;
            failed += result == ShotResult.Failed ? 1 : 0;
        }

        /// <summary>
        /// 撮影用の箱（ToonMaterials と studio の GameObject）を作り、中にカメラと太陽を置いて shoot で撮る。
        /// 撮り終えたら（例外で止まっても）箱とマテリアルを消す。
        /// エディタはシェーダーを裏で読み込む（非同期コンパイル）ので、準備が間に合わない1枚目が仮の水色一色で撮れることがあった。
        /// 撮る間だけ裏読み込みを止め、念のため空撮りで準備させてから撮る
        /// </summary>
        private static T WithPortraitStudio<T>(string name, System.Func<Camera, ToonMaterials, Transform, T> shoot)
        {
            var asyncCompilation = ShaderUtil.allowAsyncCompilation;
            ShaderUtil.allowAsyncCompilation = false;
            var materials = new ToonMaterials(AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "Toon.shader"), AssetDatabase.LoadAssetAtPath<Shader>(ShaderFolder + "BlobShadow.shader"));
            var studio = new GameObject(name);
            try
            {
                var camera = CreatePortraitCamera(studio.transform);
                // ステージと同じ太陽。カメラが前から見るので、左上の奥からの光が顔の側を照らすように合わせて回す
                CreateSunLight("PortraitSun", studio.transform, Quaternion.Euler(0f, 180f, 0f) * Quaternion.Euler(SunAngles), LightShadows.None);
                WarmUpStudio(camera, materials, studio.transform);
                return shoot(camera, materials, studio.transform);
            }
            finally
            {
                Object.DestroyImmediate(studio);
                materials.DestroyAll();
                ShaderUtil.allowAsyncCompilation = asyncCompilation;
            }
        }

        /// <summary>ペンギンと大ボスを1体ずつ小さく撮って捨てる。本番の1枚目より先に、同じシェーダーで描かせておく</summary>
        private static void WarmUpStudio(Camera camera, ToonMaterials materials, Transform parent)
        {
            WarmUpShot(camera, PenguinModelBuilder.Build(PenguinLooks.Get(1), materials, parent), PortraitYaw, PortraitPitch);
            WarmUpShot(camera, BigBossModelBuilder.Build(BigBossTable.SkuaNo, materials, parent), BossPortraitYaw, BossPortraitPitch);
        }

        private static void WarmUpShot(Camera camera, ToyModel model, float yaw, float pitch)
        {
            PoseForPortrait(camera, model, yaw, pitch);
            FramePortrait(camera, model, 0f);
            Object.DestroyImmediate(StageCapture.RenderToTexture(camera, WarmUpShotSize, WarmUpShotSize, 1, TextureFormat.RGBA32));
            Object.DestroyImmediate(model.gameObject);
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

        /// <summary>撮る向き。体を yaw、カメラを pitch で構える（大ボスとペンギンで角度を変える）</summary>
        private static void PoseForPortrait(Camera camera, ToyModel model, float yaw, float pitch)
        {
            // 床に影を落とす場所が無いので、足元の丸い影は消す
            model.BlobShadow.enabled = false;
            model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            camera.transform.rotation = Quaternion.Euler(pitch, 180f, 0f);
        }

        /// <summary>
        /// 1体を撮って保存し、撮り終えたら消す。halfSize が 0 なら体に合わせて目いっぱいに撮る。
        /// 撮り損ねた絵（ほとんど1色）は書かずに前の絵を残す（正しい絵を壊さないため）
        /// </summary>
        private static ShotResult ShootPortrait(Camera camera, ToyModel model, float yaw, float pitch, float halfSize, string path)
        {
            PoseForPortrait(camera, model, yaw, pitch);
            FramePortrait(camera, model, halfSize);
            var texture = RenderPortrait(camera, PortraitSize);
            var blank = IsBlankShot(texture.GetPixels32());
            var png = blank ? null : texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(model.gameObject);
            if (blank)
            {
                Debug.LogWarning($"[PenmonScrambleSceneBuilder] {path} がほとんど1色で撮れたので、書き込まずに前の絵を残しました。もう一度 Rebuild してください");
                return ShotResult.Failed;
            }

            return WriteBytesIfChanged(path, png) ? ShotResult.Written : ShotResult.Unchanged;
        }

        /// <summary>
        /// 何も写っていない、または写っている所のほとんどが同じ色なら、撮り損ねとみなす。
        /// シェーダーの準備が間に合わないと、体・目・くちばしまで仮の色一色になり、縁の線だけが残る
        /// </summary>
        private static bool IsBlankShot(Color32[] pixels)
        {
            var counts = new Dictionary<int, int>();
            var opaque = 0;
            var top = 0;
            foreach (var c in pixels)
            {
                if (c.a < BlankShotMinAlpha)
                {
                    continue;
                }

                opaque++;
                var key = (c.r >> BlankShotColorShift) << 16 | (c.g >> BlankShotColorShift) << 8 | (c.b >> BlankShotColorShift);
                counts.TryGetValue(key, out var count);
                counts[key] = ++count;
                top = Mathf.Max(top, count);
            }

            return opaque == 0 || top > opaque * BlankShotDominantShare;
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
            var texture = StageCapture.RenderToTexture(camera, size, size, PortraitMsaa, TextureFormat.RGBA32);
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
