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
    /// CPU の試合の確認。CPU 同士の 3対3 を描かずに計算だけで何試合も回し、
    /// 1試合の点の数・誰も触らない時間・壁ぎわの詰まり・団子・動かす子のガタつき・強さの差での勝ち負けを Console に出す
    /// （ゴルフのコースの確認と同じ考え方）。Play しなくてよい。裏の糸で回し、組み合わせごとに進み具合を出す。
    /// 全スタジアムの確認は、スタジアムごとの仕掛けのピッチで、仕掛けがどれだけ試合に絡んだかも出す
    /// </summary>
    public static class SoccerMatchCheckMenu
    {
        private const int MatchesPerPairing = 20;
        private const int FirstSeed = 1000;

        // スタジアムごとの確認で、人のチームの代わりに使う強さ（味方の AI と同じ「ふつう」）
        private const int StandInStars = SoccerBalance.TeammateAiStars;

        // 強さの組み合わせ（赤 vs 青）。同じ強さは左右の差が無いか、違う強さは強い方が勝つかを見る
        private static readonly (int red, int blue)[] Pairings =
        {
            (1, 1), (3, 3), (5, 5), (2, 3), (3, 4), (1, 5), (5, 1),
        };

        [MenuItem(EditorMenu.Soccer + "CPUの試合の確認", priority = EditorMenu.SoccerPriority + 20)]
        public static void Run()
        {
            var jobs = Pairings.Select(p => new Job($"★{p.red} 対 ★{p.blue}", p.red, p.blue, null)).ToList();
            var (results, cancelled) = Play(jobs);
            Debug.Log(Report("[Soccer] CPUの試合の確認", results, cancelled));
        }

        /// <summary>
        /// スタジアムごとの確認: そのスタジアムの仕掛けのピッチで、★3（人のチームの代わり）対 そのスタジアムの CPU。
        /// 仕掛けで詰まる所が無いか・奥のスタジアムほど勝ちにくくなるか・仕掛けがどれだけ試合に絡んだかを見る
        /// </summary>
        [MenuItem(EditorMenu.Soccer + "全スタジアムの確認", priority = EditorMenu.SoccerPriority + 21)]
        public static void RunStadiums()
        {
            var jobs = SoccerStadiumTable.Stadiums
                .Select(s => new Job($"{s.No} {s.Name} ★{StandInStars} 対 CPU★{s.CpuStars}", StandInStars, s.CpuStars, s.CreatePitch))
                .ToList();
            var (results, cancelled) = Play(jobs);
            Debug.Log(Report("[Soccer] 全スタジアムの確認（赤は人のチームの代わりの ★3）", results, cancelled));
        }

        /// <summary>裏の糸で組み合わせを順に回し、組み合わせごとに進み具合を出す</summary>
        private static (List<(Job job, List<SoccerMatchStats> stats)> results, bool cancelled) Play(List<Job> jobs)
        {
            using var cancel = new CancellationTokenSource();
            var results = new List<(Job job, List<SoccerMatchStats> stats)>();
            var done = 0;
            var thread = new Thread(() =>
            {
                foreach (var job in jobs)
                {
                    var stats = new List<SoccerMatchStats>();
                    for (var i = 0; i < MatchesPerPairing && !cancel.IsCancellationRequested; i++)
                    {
                        var pitch = job.Pitch?.Invoke();
                        stats.Add(SoccerMatchCheck.Play(SoccerBalance.AiLevel(job.Red), SoccerBalance.AiLevel(job.Blue), FirstSeed + i, SoccerRules.MatchSeconds, pitch));
                        Interlocked.Increment(ref done);
                    }

                    lock (results)
                    {
                        results.Add((job, stats));
                    }
                }
            }) { IsBackground = true };
            thread.Start();

            var total = jobs.Count * MatchesPerPairing;
            BalanceLabMenu.WaitWithProgress(thread, cancel, "サッカー: CPUの試合の確認", () => $"CPU 同士で試合をしています… {done}/{total} 試合", () => (float)done / total);
            return (results, cancel.IsCancellationRequested);
        }

        private static string Report(string title, List<(Job job, List<SoccerMatchStats> stats)> results, bool cancelled)
        {
            var text = new StringBuilder(BalanceLabMenu.ReportHeader(title, cancelled) + "\n");
            text.AppendLine($"1組 {MatchesPerPairing} 試合・1試合 {SoccerRules.MatchSeconds:0} 秒。点・シュート・パス・奪った・外した は1試合の平均");
            foreach (var (job, stats) in results)
            {
                if (stats.Count == 0)
                {
                    continue;
                }

                var redWins = stats.Count(s => s.Winner == SoccerRules.Red);
                var blueWins = stats.Count(s => s.Winner == SoccerRules.Blue);
                text.AppendLine(
                    $"{job.Label}: 点 {stats.Average(s => s.Goals[0]):0.0} - {stats.Average(s => s.Goals[1]):0.0}" +
                    $"  勝ち 赤{redWins} 青{blueWins} 引き分け{stats.Count - redWins - blueWins}" +
                    $"  シュート {stats.Average(s => s.Shots):0}（必殺 {stats.Average(s => s.Specials):0.0}）  パス {stats.Average(s => s.Passes):0}" +
                    $"  奪った {stats.Average(s => s.Tackles):0}  外した {stats.Average(s => s.SlideMisses):0}" +
                    $"  誰も触らない最長 {stats.Max(s => s.LongestIdleSeconds):0.0}秒  壁ぎわの詰まり {stats.Average(s => s.WallStuckSeconds):0.0}秒" +
                    $"  ボールのまわり {stats.Average(s => s.CrowdAverage):0.00}体  団子 {stats.Average(s => s.CrowdRate) * 100f:0.0}%" +
                    $"  切り替え {stats.Average(s => s.ControlSwitches):0}回（すぐ戻った {stats.Average(s => s.FlipSwitches):0.0}）" +
                    $"  板 {stats.Average(s => s.WallHits):0.0}  バンパー {stats.Average(s => s.Bumpers):0.0}  間欠泉 {stats.Average(s => s.Geysers):0.0}  帯の中の離れた球 {stats.Average(s => s.FlowSeconds):0}秒");
            }

            return text.ToString();
        }

        private sealed class Job
        {
            public readonly string Label;
            public readonly int Red;
            public readonly int Blue;

            /// <summary>試合ごとにピッチを作る（null なら試しのピッチ）</summary>
            public readonly System.Func<SoccerPitchDef> Pitch;

            public Job(string label, int red, int blue, System.Func<SoccerPitchDef> pitch)
            {
                Label = label;
                Red = red;
                Blue = blue;
                Pitch = pitch;
            }
        }
    }
}
