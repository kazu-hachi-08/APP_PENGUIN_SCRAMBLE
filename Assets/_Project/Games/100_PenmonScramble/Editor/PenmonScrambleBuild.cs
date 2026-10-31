using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 配る版（Release）と重さの計測用（Benchmark）のビルド。
    /// 配る版は Release メニューでビルドし、そのあと Tools/Release/ の bat で公開する。
    /// お試しビルドは配る版と中身が同じなので置かず、Release の出力を起動して確かめる
    /// </summary>
    public static class PenmonScrambleBuild
    {
        // 計測用ビルドの出力先（git に入れない）
        private const string OutputRoot = "Builds/";

        // 重さの計測用（StageBenchmark）。起動するとすぐ4人のボットで戦って測る。配る版と同じ最適化で、計測の処理だけ足す
        private const string BenchmarkDefine = "PENMON_BENCHMARK";
        private const string BenchmarkRoot = OutputRoot + "Benchmark/";

        [MenuItem(EditorMenu.Benchmark + "Build Windows", priority = EditorMenu.BenchmarkPriority + 1)]
        public static void BuildBenchmarkWindows() => Build(BuildTarget.StandaloneWindows64, BenchmarkRoot + "Windows/PenmonScramble.exe", BenchmarkDefine);

        [MenuItem(EditorMenu.Benchmark + "Build Android", priority = EditorMenu.BenchmarkPriority + 2)]
        public static void BuildBenchmarkAndroid() => Build(BuildTarget.Android, BenchmarkRoot + "Android/PenmonScramble_Benchmark.apk", BenchmarkDefine);

        [MenuItem(EditorMenu.Benchmark + "Build Web", priority = EditorMenu.BenchmarkPriority + 3)]
        public static void BuildBenchmarkWeb() => Build(BuildTarget.WebGL, BenchmarkRoot + "Web", BenchmarkDefine);

        // 配る版の出力先。リポジトリの隣のフォルダで、Tools/Release/ の bat が読む場所と同じにする
        // （release-windows-test.bat の -BuildDir と、release-web.ps1 の -Repo の名前）
        private const string WindowsReleaseFolder = "APP_PENGUIN_SCRAMBLE_EXE";
        private const string WebReleaseFolder = "penmon-scramble-web";

        [MenuItem(EditorMenu.Release + "Windows", priority = EditorMenu.ReleasePriority)]
        public static void ReleaseWindows()
        {
            string output = Path.Combine(ReleasePath(WindowsReleaseFolder), "PenmonScramble.exe");
            if (Build(BuildTarget.StandaloneWindows64, output))
            {
                Debug.Log("[PenmonScrambleBuild] 次は Tools/Release/release-windows-test.bat（正式版は release-windows.ps1 -Version vX.Y）で公開する");
            }
        }

        [MenuItem(EditorMenu.Release + "Web", priority = EditorMenu.ReleasePriority + 1)]
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
