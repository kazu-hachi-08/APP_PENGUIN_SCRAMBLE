using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 3つの環境へのお試しビルド。出力先は Builds/（git に入れない）。
    /// 配る版は Release メニューでビルドし、そのあと Tools/Release/ の bat で公開する
    /// </summary>
    public static class PenmonScrambleBuild
    {
        private const string OutputRoot = "Builds/";

        [MenuItem("Tools/PenmonScramble/Build/Windows")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, OutputRoot + "Windows/PenmonScramble.exe");

        [MenuItem("Tools/PenmonScramble/Build/Android")]
        public static void BuildAndroid() => Build(BuildTarget.Android, OutputRoot + "Android/PenmonScramble.apk");

        [MenuItem("Tools/PenmonScramble/Build/Web")]
        public static void BuildWeb() => Build(BuildTarget.WebGL, OutputRoot + "Web");

        // 重さの計測用（StageBenchmark）。起動するとすぐ4人のボットで戦って測る。配る版と同じ最適化で、計測の処理だけ足す
        private const string BenchmarkDefine = "PENMON_BENCHMARK";
        private const string BenchmarkRoot = OutputRoot + "Benchmark/";

        [MenuItem("Tools/PenmonScramble/Build/Benchmark/Windows")]
        public static void BuildBenchmarkWindows() => Build(BuildTarget.StandaloneWindows64, BenchmarkRoot + "Windows/PenmonScramble.exe", BenchmarkDefine);

        [MenuItem("Tools/PenmonScramble/Build/Benchmark/Android")]
        public static void BuildBenchmarkAndroid() => Build(BuildTarget.Android, BenchmarkRoot + "Android/PenmonScramble_Benchmark.apk", BenchmarkDefine);

        [MenuItem("Tools/PenmonScramble/Build/Benchmark/Web")]
        public static void BuildBenchmarkWeb() => Build(BuildTarget.WebGL, BenchmarkRoot + "Web", BenchmarkDefine);

        // 配る版の出力先。リポジトリの隣のフォルダで、Tools/Release/ の bat が読む場所と同じにする
        // （release-windows-test.bat の -BuildDir と、release-web.ps1 の -Repo の名前）
        private const string WindowsReleaseFolder = "APP_PENGUIN_SCRAMBLE_EXE";
        private const string WebReleaseFolder = "penmon-scramble-web";

        [MenuItem("Tools/PenmonScramble/Release/Windows")]
        public static void ReleaseWindows()
        {
            string output = Path.Combine(ReleasePath(WindowsReleaseFolder), "PenmonScramble.exe");
            if (Build(BuildTarget.StandaloneWindows64, output))
            {
                Debug.Log("[PenmonScrambleBuild] 次は Tools/Release/release-windows-test.bat（正式版は release-windows.ps1 -Version vX.Y）で公開する");
            }
        }

        [MenuItem("Tools/PenmonScramble/Release/Web")]
        public static void ReleaseWeb()
        {
            string output = ReleasePath(WebReleaseFolder);
            // ビルド先は公開用リポジトリの clone そのもの。clone より先にビルドすると中身のあるフォルダになり clone できなくなる
            if (!Directory.Exists(Path.Combine(output, ".git")))
            {
                Debug.LogError($"[PenmonScrambleBuild] {output} が公開用リポジトリの clone ではありません。先に Tools/Release/release-web.bat を1回実行して準備してください");
                return;
            }
            if (Build(BuildTarget.WebGL, output))
            {
                Debug.Log("[PenmonScrambleBuild] 次は Tools/Release/release-web.bat で公開する");
            }
        }

        private static string ReleasePath(string folder)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.GetFullPath(Path.Combine(projectRoot, "..", folder));
        }

        private static bool Build(BuildTarget target, string location, params string[] defines)
        {
            // 画質ごとの描画の設定（縁の線・影など）を、ビルドの前にそろえておく
            RenderPipelineSetup.Ensure();
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                locationPathName = location,
                target = target,
                options = BuildOptions.None,
                extraScriptingDefines = defines,
            };
            var report = BuildPipeline.BuildPlayer(options);
            bool succeeded = report.summary.result == BuildResult.Succeeded;
            Debug.Log($"[PenmonScrambleBuild] {target}: {report.summary.result} ({report.summary.totalTime}) → {location}");

            // バッチモードで失敗を終了コードに出し、スクリプトから気づけるようにする
            if (Application.isBatchMode && !succeeded)
            {
                EditorApplication.Exit(1);
            }
            return succeeded;
        }
    }
}
