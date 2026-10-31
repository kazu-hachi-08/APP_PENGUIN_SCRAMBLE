using System.IO;
using UnityEditor;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// ステージの3Dの見た目のスクショ（見た目を人が確かめる）。
    /// ゲームのシーンを作り直して Play し、StageCaptureDriver が台本どおりに戦わせて、決めた時刻にカメラを撮る。
    /// 実際の Update で動くので、ゲーム中と同じ見た目になる（HUD などの UI は写らない）。
    /// バッチモードからは -executeMethod PenmonScramble.Editor.StageCapture.RunBatch で撮って終わる
    /// </summary>
    [InitializeOnLoad]
    public static class StageCapture
    {
        public const string CaptureFolder = "Screenshots/Stage3D";
        private const string PendingKey = "PenmonScramble.StageCapture.Pending";
        private const string QuitKey = "PenmonScramble.StageCapture.Quit";

        private static StageCaptureDriver _driver;

        static StageCapture()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        [MenuItem("Tools/PenmonScramble/3D Stage Capture")]
        public static void Run() => Start(quit: false);

        public static void RunBatch() => Start(quit: true);

        private static void Start(bool quit)
        {
            if (!quit && !UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            PenmonScrambleSceneBuilder.BuildScene();
            Directory.CreateDirectory(CaptureFolder);
            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(QuitKey, quit);
            EditorApplication.EnterPlaymode();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(PendingKey, false))
            {
                SessionState.SetBool(PendingKey, false);
                _driver = new StageCaptureDriver();
                _driver.Finished += OnFinished;
                _driver.Begin();
                EditorApplication.update += StepDriver;
            }
            else if (change == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(QuitKey, false))
            {
                SessionState.SetBool(QuitKey, false);
                EditorApplication.Exit(0);
            }
        }

        private static void StepDriver() => _driver?.Step();

        private static void OnFinished()
        {
            EditorApplication.update -= StepDriver;
            _driver = null;
            Debug.Log($"[StageCapture] {CaptureFolder} に撮りました");
            EditorApplication.ExitPlaymode();
        }

        /// <summary>
        /// カメラの絵を1枚の Texture2D に撮る（試作の場とステージの撮影で共通）。
        /// 撮る間だけ一時の RenderTexture に描き、カメラの targetTexture と RenderTexture.active は撮る前の値に戻す。
        /// 色は sRGB で受ける（リニアの色空間でも PNG の色が画面と同じになるように）。Apply は呼び出し側が必要なら行う
        /// </summary>
        internal static Texture2D RenderToTexture(Camera camera, int width, int height, int msaa, TextureFormat format)
        {
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, msaa);
            var previous = camera.targetTexture;
            camera.targetTexture = target;
            camera.Render();
            camera.targetTexture = previous;

            RenderTexture.active = target;
            var texture = new Texture2D(width, height, format, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(target);
            return texture;
        }
    }
}
