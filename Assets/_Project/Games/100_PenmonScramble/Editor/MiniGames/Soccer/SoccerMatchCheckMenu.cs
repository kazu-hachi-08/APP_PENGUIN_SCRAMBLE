using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using PenmonScramble.Soccer.Core;
using UnityEditor;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// CPU の試合の確認（Docs/MINIGAME_SOCCER_PLAN.md フェーズ 3）。CPU 同士の 3対3 を描かずに計算だけで何試合も回し、
    /// 1試合の点の数・誰も触らない時間・壁ぎわの詰まり・団子・動かす子のガタつき・強さの差での勝ち負けを Console に出す
    /// （ゴルフのコースの確認と同じ考え方）。Play しなくてよい。裏の糸で回し、組み合わせごとに進み具合を出す
    /// </summary>
    public static class SoccerMatchCheckMenu
    {
        private const int MatchesPerPairing = 20;
        private const int FirstSeed = 1000;

        // 強さの組み合わせ（赤 vs 青）。同じ強さは左右の差が無いか、違う強さは強い方が勝つかを見る
        private static readonly (int red, int blue)[] Pairings =
        {
            (1, 1), (3, 3), (5, 5), (2, 3), (3, 4), (1, 5), (5, 1),
        };

        [MenuItem(EditorMenu.Soccer + "CPUの試合の確認", priority = EditorMenu.SoccerPriority + 20)]
        public static void Run()
        {
            using var cancel = new CancellationTokenSource();
            var results = new List<(int red, int blue, List<SoccerMatchStats> stats)>();
            var done = 0;
            var job = new Thread(() =>
            {
                foreach (var (red, blue) in Pairings)
                {
                    var stats = new List<SoccerMatchStats>();
                    for (var i = 0; i < MatchesPerPairing && !cancel.IsCancellationRequested; i++)
                    {
                        stats.Add(SoccerMatchCheck.Play(SoccerBalance.AiLevel(red), SoccerBalance.AiLevel(blue), FirstSeed + i));
                        Interlocked.Increment(ref done);
                    }

                    lock (results)
                    {
                        results.Add((red, blue, stats));
                    }
                }
            }) { IsBackground = true };
            job.Start();

            var total = Pairings.Length * MatchesPerPairing;
            while (job.IsAlive)
            {
                if (EditorUtility.DisplayCancelableProgressBar("サッカー: CPUの試合の確認", $"CPU 同士で試合をしています… {done}/{total} 試合", (float)done / total))
                {
                    cancel.Cancel();
                }

                Thread.Sleep(100);
            }

            EditorUtility.ClearProgressBar();
            Debug.Log(Report(results, cancel.IsCancellationRequested));
        }

        private static string Report(List<(int red, int blue, List<SoccerMatchStats> stats)> results, bool cancelled)
        {
            var text = new StringBuilder(cancelled ? "[Soccer] CPUの試合の確認（途中でやめました）\n" : "[Soccer] CPUの試合の確認\n");
            text.AppendLine($"1組 {MatchesPerPairing} 試合・1試合 {SoccerRules.MatchSeconds:0} 秒。点・シュート・パス・奪った・外した は1試合の平均");
            foreach (var (red, blue, stats) in results)
            {
                if (stats.Count == 0)
                {
                    continue;
                }

                var redWins = stats.Count(s => s.Winner == SoccerRules.Red);
                var blueWins = stats.Count(s => s.Winner == SoccerRules.Blue);
                text.AppendLine(
                    $"★{red} 対 ★{blue}: 点 {stats.Average(s => s.Goals[0]):0.0} - {stats.Average(s => s.Goals[1]):0.0}" +
                    $"  勝ち 赤{redWins} 青{blueWins} 引き分け{stats.Count - redWins - blueWins}" +
                    $"  シュート {stats.Average(s => s.Shots):0}  パス {stats.Average(s => s.Passes):0}" +
                    $"  奪った {stats.Average(s => s.Tackles):0}  外した {stats.Average(s => s.SlideMisses):0}" +
                    $"  誰も触らない最長 {stats.Max(s => s.LongestIdleSeconds):0.0}秒  壁ぎわの詰まり {stats.Average(s => s.WallStuckSeconds):0.0}秒" +
                    $"  ボールのまわり {stats.Average(s => s.CrowdAverage):0.00}体  団子 {stats.Average(s => s.CrowdRate) * 100f:0.0}%" +
                    $"  切り替え {stats.Average(s => s.ControlSwitches):0}回（すぐ戻った {stats.Average(s => s.FlipSwitches):0.0}）");
            }

            return text.ToString();
        }
    }
}
