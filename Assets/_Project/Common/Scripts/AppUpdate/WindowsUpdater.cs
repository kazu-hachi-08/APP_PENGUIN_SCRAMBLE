using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace PenmonScramble.Common.AppUpdate
{
    /// <summary>
    /// Windows 版の自動更新。exe の横の version.txt（公開した版の名前）と GitHub Releases の最新のタグを比べる。
    /// 入れ替えは exe の横の update.ps1 に任せる。動いている exe は自分自身を上書きできないので、ゲームを閉じてからスクリプトが入れ替える
    /// </summary>
    public static class WindowsUpdater
    {
        private const string VersionFileName = "version.txt";
        private const string ScriptFileName = "update.ps1";
        private const int TimeoutSeconds = 10;
        // ヘッダーには ASCII しか入らないので、日本語の productNameは使えない
        private const string UserAgent = "PenmonScramble";

        [Serializable]
        private class Release
        {
            public string tag_name;
        }

        /// <summary>exe の横のフォルダ（dataPath は「名前_Data」を指すので1つ上）</summary>
        private static string AppFolder => Path.GetDirectoryName(Application.dataPath);

        /// <summary>
        /// 配った zip から起動したときだけ更新できる。エディタや手元のビルドには version.txt と update.ps1 が無いので確かめない
        /// </summary>
        public static bool CanUpdate
        {
            get
            {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
                return File.Exists(Path.Combine(AppFolder, VersionFileName)) && File.Exists(Path.Combine(AppFolder, ScriptFileName));
#else
                return false;
#endif
            }
        }

        /// <summary>最新のタグが手元と違えばそのタグを、同じか確かめられなければ null を返す</summary>
        public static IEnumerator FetchNewVersion(string latestReleaseApi, Action<string> onFetched)
        {
            using var request = UnityWebRequest.Get(latestReleaseApi);
            request.timeout = TimeoutSeconds;
            // GitHub の API は User-Agent が無いと断る
            request.SetRequestHeader("User-Agent", UserAgent);
            yield return request.SendWebRequest();

            string latest = request.result == UnityWebRequest.Result.Success
                ? JsonUtility.FromJson<Release>(request.downloadHandler.text)?.tag_name
                : null;
            string local = File.ReadAllText(Path.Combine(AppFolder, VersionFileName)).Trim();
            onFetched(string.IsNullOrEmpty(latest) || latest == local ? null : latest);
        }

        public static void InstallAndQuit()
        {
            string script = Path.Combine(AppFolder, ScriptFileName);
            string args = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -WaitPid {Process.GetCurrentProcess().Id}";
            Process.Start(new ProcessStartInfo("powershell.exe", args) { UseShellExecute = true, WorkingDirectory = AppFolder });
            Application.Quit();
        }
    }
}
