using System.Collections.Generic;
using System.Text;
using System.Threading;
using PenmonScramble.Golf.Core;
using PenmonScramble.MiniGames.Core;
using UnityEditor;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// コースの確認。試しの打ち手に向きと強さ（動くギミックのあるホールは打つ時刻も）を総当たりさせ、
    /// 全ホールで「パー以内で入る打ち方がある」「詰んで出られない所が無い」「止まらない所が無い」「ティーとカップの置き方」を確かめる。
    /// 全コースで数分かかる（裏の糸で回し、ホールごとに進み具合を出す）。Play しなくてよい。結果は Console に出す
    /// </summary>
    public static class GolfCourseCheckMenu
    {
        [MenuItem(EditorMenu.Golf + "コースの確認", priority = EditorMenu.GolfPriority + 20)]
        public static void Run()
        {
            var holes = AllHoles();
            using var cancel = new CancellationTokenSource();
            var results = new List<(GolfCourseDef course, GolfHoleCheck check)>();
            var done = 0;
            var job = new Thread(() =>
            {
                foreach (var (course, hole) in holes)
                {
                    if (cancel.IsCancellationRequested)
                    {
                        return;
                    }

                    var check = GolfCourseCheck.CheckHole(hole);
                    lock (results)
                    {
                        results.Add((course, check));
                    }

                    done++;
                }
            }) { IsBackground = true };
            job.Start();
            BalanceLabMenu.WaitWithProgress(job, cancel, "ゴルフ: コースの確認", () => $"試しの打ち手で打っています… {done}/{holes.Count} ホール", () => (float)done / holes.Count);
            Report(results, cancel.IsCancellationRequested);
        }

        private static List<(GolfCourseDef course, GolfHoleDef hole)> AllHoles()
        {
            var holes = new List<(GolfCourseDef, GolfHoleDef)>();
            foreach (var row in MiniGameTable.Get(MiniGameTable.GolfId).Courses)
            {
                var course = GolfCourseTable.Get(row.No);
                if (course == null)
                {
                    continue;
                }

                foreach (var hole in course.Holes)
                {
                    holes.Add((course, hole));
                }
            }

            return holes;
        }

        private static void Report(List<(GolfCourseDef course, GolfHoleCheck check)> results, bool cancelled)
        {
            var text = new StringBuilder(BalanceLabMenu.ReportHeader("[Golf] コースの確認", cancelled) + "\n");
            var failed = 0;
            foreach (var (course, check) in results)
            {
                text.AppendLine($"コース{course.No} {course.Name}  {check}");
                failed += check.Passed ? 0 : 1;
            }

            if (failed > 0)
            {
                Debug.LogWarning(text.Insert(0, $"{failed} ホールに問題があります。").ToString());
                return;
            }

            Debug.Log(text.ToString());
        }
    }
}
