using System;
using System.IO;
using System.Threading;
using PenmonScramble.Core;
using UnityEditor;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// バランスの試験場（BalanceLab）を回して、結果の表を Docs/BALANCE_REPORT.md に書き出す。
    /// ボットで全ステージ・全条件を回すので1分ほどかかる（裏の糸で回し、進み具合を出す）。Play しなくてよい
    /// </summary>
    public static class BalanceLabMenu
    {
        // リポジトリの直下の Docs（Assets の1つ上）
        private const string ReportPath = "Docs/BALANCE_REPORT.md";

        // 1つの条件を回す回数。40 回で割合のぶれが ±8% ほどになる
        private const int Seeds = 40;

        [MenuItem(EditorMenu.BalanceRoot + "Run", priority = EditorMenu.BalancePriority)]
        public static void Run()
        {
            var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ReportPath);
            using var cancel = new CancellationTokenSource();
            var options = new BalanceLabOptions { Seeds = Seeds, Cancel = cancel.Token };
            var progress = 0f;
            string report = null;
            var job = new Thread(() => report = BalanceLab.Report(BalanceLab.Run(options, p => progress = p), options.Seeds)) { IsBackground = true };
            job.Start();
            WaitWithProgress(job, cancel, "Balance Lab", () => $"ボットで回しています… {progress * 100f:0}%", () => progress);
            if (cancel.IsCancellationRequested || report == null)
            {
                Debug.Log("[BalanceLab] やめました（表は書き出していません）");
                return;
            }

            File.WriteAllText(path, report);
            Debug.Log($"[BalanceLab] {ReportPath} に書き出しました");
        }

        /// <summary>
        /// 裏の糸（job）が終わるまで進み具合の表示を出し、キャンセルが押されたら cancel を立てる。
        /// 表示は100msごとに更新する（裏の糸を待つだけの空回りで、メインのスレッドを使い切らないため）
        /// </summary>
        internal static void WaitWithProgress(Thread job, CancellationTokenSource cancel, string title, Func<string> message, Func<float> progress)
        {
            while (job.IsAlive)
            {
                if (EditorUtility.DisplayCancelableProgressBar(title, message(), progress()))
                {
                    cancel.Cancel();
                }

                Thread.Sleep(100);
            }

            EditorUtility.ClearProgressBar();
        }

        /// <summary>集計の見出し。途中でやめた集計には、その旨を題名の後ろに付ける（改行は呼び出し側で付ける）</summary>
        internal static string ReportHeader(string title, bool cancelled) => cancelled ? title + "（途中でやめました）" : title;
    }
}
