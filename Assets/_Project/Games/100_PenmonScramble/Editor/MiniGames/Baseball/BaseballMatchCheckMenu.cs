using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using PenmonScramble.Baseball.Core;
using UnityEditor;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// CPU の試合の確認（Docs/MINIGAME_BASEBALL_PLAN.md フェーズ 3）。CPU 同士の3イニングを描かずに計算だけで何試合も回し、
    /// 1試合の点・打率・ホームラン・三振・四球・打球の結果の割合・穴ごとの入った割合・1試合の長さ・強さの差での勝ち負けを Console に出す
    /// （サッカーの試合の確認と同じ考え方）。魔球は属性ごとに打たれた割合、必殺打は当てた後の結果の割合も出す（フェーズ 4）。
    /// Play しなくてよい。裏の糸で回し、進み具合を出す。
    /// 「全球場」は5球場の盤（仕掛けつき）で、球場の強さどうしと ★1 対 ★5 を回す（フェーズ 6。仕掛けで球が詰まらないか・点の入り方・強さの順）
    /// </summary>
    public static class BaseballMatchCheckMenu
    {
        private const int MatchesPerPairing = 200;
        private const int MatchesPerStadiumPairing = 100;
        private const int FirstSeed = 1000;

        // 強さの組み合わせ（先攻 vs 後攻）。同じ強さは先攻・後攻の差が無いか、違う強さは強い方が勝つかを見る
        private static readonly (int away, int home)[] Pairings =
        {
            (1, 1), (3, 3), (5, 5), (2, 3), (3, 4), (1, 5), (5, 1),
        };

        [MenuItem(EditorMenu.Baseball + "CPUの試合の確認", priority = EditorMenu.BaseballPriority + 20)]
        public static void Run()
        {
            var field = BaseballFieldTable.Test();
            var results = RunPairings("野球: CPUの試合の確認", field, Pairings, MatchesPerPairing, out var cancelled);
            Debug.Log(Report("[Baseball] CPUの試合の確認", field, results, MatchesPerPairing, cancelled));
        }

        [MenuItem(EditorMenu.Baseball + "CPUの試合の確認（全球場）", priority = EditorMenu.BaseballPriority + 21)]
        public static void RunStadiums()
        {
            foreach (var stadium in BaseballStadiumTable.Stadiums)
            {
                var field = stadium.CreateField();
                var stars = stadium.CpuStars;
                var pairings = new[] { (stars, stars), (1, 5) };
                var results = RunPairings($"野球: CPUの試合の確認（{stadium.Name}）", field, pairings, MatchesPerStadiumPairing, out var cancelled);
                Debug.Log(Report($"[Baseball] CPUの試合の確認 球場{stadium.No}「{stadium.Name}」（{stadium.Gimmick}）", field, results, MatchesPerStadiumPairing, cancelled));
                if (cancelled)
                {
                    return;
                }
            }
        }

        private static List<(int away, int home, List<BaseballMatchStats> stats)> RunPairings(
            string title, BaseballFieldDef field, (int away, int home)[] pairings, int matches, out bool cancelled)
        {
            using var cancel = new CancellationTokenSource();
            var results = new List<(int away, int home, List<BaseballMatchStats> stats)>();
            var done = 0;
            var thread = new Thread(() =>
            {
                foreach (var (away, home) in pairings)
                {
                    var stats = new List<BaseballMatchStats>();
                    for (var i = 0; i < matches && !cancel.IsCancellationRequested; i++)
                    {
                        stats.Add(BaseballMatchCheck.Play(BaseballBalance.AiSkill(away), BaseballBalance.AiSkill(home), FirstSeed + i, field));
                        Interlocked.Increment(ref done);
                    }

                    lock (results)
                    {
                        results.Add((away, home, stats));
                    }
                }
            }) { IsBackground = true };
            thread.Start();

            var total = pairings.Length * matches;
            while (thread.IsAlive)
            {
                if (EditorUtility.DisplayCancelableProgressBar(title, $"CPU 同士で試合をしています… {done}/{total} 試合", (float)done / total))
                {
                    cancel.Cancel();
                }

                Thread.Sleep(100);
            }

            EditorUtility.ClearProgressBar();
            cancelled = cancel.IsCancellationRequested;
            return results;
        }

        private static string Report(string title, BaseballFieldDef field, List<(int away, int home, List<BaseballMatchStats> stats)> results, int matches, bool cancelled)
        {
            var text = new StringBuilder(cancelled ? $"{title}（途中でやめました）\n" : $"{title}\n");
            text.AppendLine($"1組 {matches} 試合・{BaseballRules.Innings} イニング。点・ホームラン・三振・四球・球数・分 は1試合の平均。打球の割合はファウルを除くフェアの打球に対して");
            foreach (var (away, home, stats) in results)
            {
                if (stats.Count == 0)
                {
                    continue;
                }

                text.AppendLine(Line(away, home, stats));
                text.AppendLine("    打球: " + Shares(stats.SelectMany(s => s.Results)));
                text.AppendLine("    穴: " + HoleShares(field, stats));
                text.Append(SpecialShares(stats));
            }

            return text.ToString();
        }

        private static string Line(int away, int home, List<BaseballMatchStats> stats)
        {
            var atBats = stats.Sum(s => s.AtBats[0] + s.AtBats[1]);
            var hits = stats.Sum(s => s.Hits[0] + s.Hits[1]);
            var awayWins = stats.Count(s => s.Winner == BaseballRules.Away);
            var homeWins = stats.Count(s => s.Winner == BaseballRules.Home);
            return $"★{away} 対 ★{home}: 点 {stats.Average(s => s.Runs[0]):0.00} - {stats.Average(s => s.Runs[1]):0.00}（合計 {stats.Average(s => s.TotalRuns):0.0}・0点 {Percent(stats.Count(s => s.TotalRuns == 0), stats.Count)}）" +
                $"  勝ち 先攻{awayWins} 後攻{homeWins} 引き分け{stats.Count - awayWins - homeWins}（点差で終わり {stats.Count(s => s.Mercy)}）" +
                $"  打率 {(atBats > 0 ? (float)hits / atBats : 0f):0.000}  ホームラン {stats.Average(s => s.HomeRuns[0] + s.HomeRuns[1]):0.00}" +
                $"  三振 {stats.Average(s => s.Strikeouts[0] + s.Strikeouts[1]):0.0}  四球 {stats.Average(s => s.Walks[0] + s.Walks[1]):0.0}" +
                $"  球数 {stats.Average(s => s.Pitches):0}  ファウル {stats.Average(s => s.Fouls):0.0}  浮いた {stats.Average(s => s.Lofted):0.0}" +
                $"  バント {stats.Average(s => s.BuntAttempts):0.0}（犠打 {stats.Average(s => s.Sacrifices):0.0}）  ゲッツー {stats.Average(s => s.DoublePlays):0.00}" +
                $"  1試合 {stats.Average(s => s.Seconds) / 60f:0.0}分";
        }

        private static string Shares(IEnumerable<KeyValuePair<BaseballHitResult, int>> counts)
        {
            var totals = new Dictionary<BaseballHitResult, int>();
            foreach (var pair in counts)
            {
                totals.TryGetValue(pair.Key, out var n);
                totals[pair.Key] = n + pair.Value;
            }

            var all = totals.Values.Sum();
            return string.Join("  ", totals.OrderBy(p => p.Key).Select(p => $"{BaseballResults.Label(p.Key)} {Percent(p.Value, all)}"));
        }

        private static string HoleShares(BaseballFieldDef field, List<BaseballMatchStats> stats)
        {
            var inPlay = stats.Sum(s => s.Results.Values.Sum());
            var parts = new List<string>();
            for (var i = 0; i < field.Holes.Count; i++)
            {
                var hole = field.Holes[i];
                var count = stats.Sum(s => s.HoleIns[i]);
                parts.Add($"{i}:{BaseballResults.Label(hole.Result)}({BaseballFieldDef.DegreesOf(hole.Center):0}°) {Percent(count, inPlay)}");
            }

            var stops = Shares(stats.SelectMany(s => s.Stops));
            return string.Join("  ", parts) + $"\n    穴に入らずに止まった打球の内訳: {stops}";
        }

        /// <summary>属性ごとに、魔球（振られた・当てられた・ストライク・ヒットにされた）と必殺打（当てた・その後のファウル・ヒット・ホームラン・アウト）の割合</summary>
        private static string SpecialShares(List<BaseballMatchStats> stats)
        {
            var text = new StringBuilder();
            for (var e = 0; e < BaseballMatchStats.ElementCount; e++)
            {
                int Sum(System.Func<BaseballMatchStats, int[]> pick) => stats.Sum(s => pick(s)[e]);
                var thrown = Sum(s => s.MagicThrown);
                var tries = Sum(s => s.SpecialTries);
                var contact = Sum(s => s.SpecialContact);
                var element = (PenmonScramble.Core.Element)e;
                text.AppendLine($"    {BaseballSpecialNames.Magic(element)} {thrown}球: 振られた {Percent(Sum(s => s.MagicSwung), thrown)} 当てられた {Percent(Sum(s => s.MagicContact), thrown)}" +
                    $" ストライク {Percent(Sum(s => s.MagicStrike), thrown)} ヒットにされた {Percent(Sum(s => s.MagicHitSafe), thrown)}" +
                    $" ／ {BaseballSpecialNames.Hit(element)} {tries}球: 当てた {Percent(contact, tries)} → ファウル {Percent(Sum(s => s.SpecialFoul), contact)}" +
                    $" ヒット {Percent(Sum(s => s.SpecialSafe), contact)}（ホームラン {Percent(Sum(s => s.SpecialHomeRun), contact)}） アウト {Percent(Sum(s => s.SpecialOut), contact)}");
            }

            return text.ToString();
        }

        private static string Percent(int count, int total) => total > 0 ? $"{100f * count / total:0.0}%" : "-";
    }
}
