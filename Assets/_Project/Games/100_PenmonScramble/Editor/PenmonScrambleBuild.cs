using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 3つの環境へのお試しビルド。出力先は Builds/（git に入れない）。
    /// 配る版は Tools/Release/ の手順でビルドする（出力先がリリース用のフォルダになる）
    /// </summary>
    public static class PenmonScrambleBuild
    {
        private const string OutputRoot = "Builds/";

        [MenuItem("Tools/PenmonScramble/Build/Windows")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Windows/PenmonScramble.exe");

        [MenuItem("Tools/PenmonScramble/Build/Android")]
        public static void BuildAndroid() => Build(BuildTarget.Android, "Android/PenmonScramble.apk");

        [MenuItem("Tools/PenmonScramble/Build/Web")]
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
            Debug.Log($"[PenmonScrambleBuild] {target}: {report.summary.result} ({report.summary.totalTime})");

            // バッチモードで失敗を終了コードに出し、スクリプトから気づけるようにする
            if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }
    }
}
