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
            while (job.IsAlive)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Balance Lab", $"ボットで回しています… {progress * 100f:0}%", progress))
                {
                    cancel.Cancel();
                }

                Thread.Sleep(100);
            }

            EditorUtility.ClearProgressBar();
            if (cancel.IsCancellationRequested || report == null)
            {
                Debug.Log("[BalanceLab] やめました（表は書き出していません）");
                return;
            }

            File.WriteAllText(path, report);
            Debug.Log($"[BalanceLab] {ReportPath} に書き出しました");
        }
    }
}
