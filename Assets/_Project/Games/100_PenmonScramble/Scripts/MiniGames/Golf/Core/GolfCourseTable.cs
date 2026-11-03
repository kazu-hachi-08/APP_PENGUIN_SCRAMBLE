using System.Collections.Generic;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// 1コース（1回の遊び＝3ホール）。番号・名前・景色のワールドは MiniGameTable の行と同じ（記録・解放はそちらで決める）
    /// </summary>
    public sealed class GolfCourseDef
    {
        public GolfCourseDef(int no, string name, int world, IReadOnlyList<GolfHoleDef> holes)
        {
            No = no;
            Name = name;
            World = world;
            Holes = holes;
        }

        public int No { get; }
        public string Name { get; }

        /// <summary>景色（地面の絵と色）のもとにするストーリーのワールド</summary>
        public int World { get; }

        public IReadOnlyList<GolfHoleDef> Holes { get; }

        public int TotalPar
        {
            get
            {
                var par = 0;
                foreach (var hole in Holes)
                {
                    par += hole.Par;
                }

                return par;
            }
        }
    }

    /// <summary>
    /// ゴルフのコースの定義表。ホールの形はコースごとのファイル（GolfCourse1 など）。
    /// まだ作っていないコース（2〜5 はフェーズ 6）は Get が null を返し、ミニゲーム画面で「準備中」になる
    /// </summary>
    public static class GolfCourseTable
    {
        private static readonly Dictionary<int, IReadOnlyList<GolfHoleDef>> Holes = new Dictionary<int, IReadOnlyList<GolfHoleDef>>
        {
            { 1, GolfCourse1.Holes },
        };

        /// <summary>番号のコース。表に無い・まだ作っていなければ null</summary>
        public static GolfCourseDef Get(int courseNo)
        {
            var row = MiniGameTable.Get(MiniGameTable.GolfId)?.Course(courseNo);
            if (row == null || !Holes.TryGetValue(courseNo, out var holes))
            {
                return null;
            }

            return new GolfCourseDef(row.No, row.Name, row.World, holes);
        }

        /// <summary>開発用の試しのホールだけの1ホールのコース（記録には書かない）</summary>
        public static GolfCourseDef TestCourse()
        {
            return new GolfCourseDef(0, "試しのホール", 1, new[] { GolfTestHoles.TestHole });
        }
    }
}
