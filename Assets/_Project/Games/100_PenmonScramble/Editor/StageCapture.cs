using System.IO;
using PenmonScramble.Core;
using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// ステージの3Dの見た目のスクショ（PLAN_3D フェーズ3。見た目を人が確かめる）。
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
    }
}
