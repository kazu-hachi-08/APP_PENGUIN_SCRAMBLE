using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PenguinScramble.Editor
{
    /// <summary>
    /// 3つの環境へのお試しビルド。出力先は Builds/（git に入れない）。
    /// 配る版は Tools/Release/ の手順でビルドする（出力先がリリース用のフォルダになる）
    /// </summary>
    public static class PenguinScrambleBuild
    {
        private const string OutputRoot = "Builds/";

        [MenuItem("Tools/PenguinScramble/Build/Windows")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Windows/PenguinScramble.exe");

        [MenuItem("Tools/PenguinScramble/Build/Android")]
        public static void BuildAndroid() => Build(BuildTarget.Android, "Android/PenguinScramble.apk");

        [MenuItem("Tools/PenguinScramble/Build/Web")]
        public static void BuildWeb() => Build(BuildTarget.WebGL, "Web");

        private static void Build(BuildTarget target, string output)
        {
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = OutputRoot + output,
                target = target,
                options = BuildOptions.None,
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log($"[PenguinScrambleBuild] {target}: {report.summary.result} ({report.summary.totalTime})");

            // バッチモードで失敗を終了コードに出し、スクリプトから気づけるようにする
            if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
