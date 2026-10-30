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

        // 重さの計測用（StageBenchmark）。起動するとすぐ4人のボットで戦って測る。配る版と同じ最適化で、計測の処理だけ足す
        private const string BenchmarkDefine = "PENMON_BENCHMARK";
        private const string BenchmarkRoot = "Benchmark/";

        [MenuItem("Tools/PenmonScramble/Build/Benchmark/Windows")]
        public static void BuildBenchmarkWindows() => Build(BuildTarget.StandaloneWindows64, BenchmarkRoot + "Windows/PenmonScramble.exe", BenchmarkDefine);

        [MenuItem("Tools/PenmonScramble/Build/Benchmark/Android")]
        public static void BuildBenchmarkAndroid() => Build(BuildTarget.Android, BenchmarkRoot + "Android/PenmonScramble_Benchmark.apk", BenchmarkDefine);

        [MenuItem("Tools/PenmonScramble/Build/Benchmark/Web")]
        public static void BuildBenchmarkWeb() => Build(BuildTarget.WebGL, BenchmarkRoot + "Web", BenchmarkDefine);

        private static void Build(BuildTarget target, string output, params string[] defines)
        {
            // 画質ごとの描画の設定（縁の線・影など）を、ビルドの前にそろえておく
            RenderPipelineSetup.Ensure();
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = OutputRoot + output,
                target = target,
                options = BuildOptions.None,
                extraScriptingDefines = defines,
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
