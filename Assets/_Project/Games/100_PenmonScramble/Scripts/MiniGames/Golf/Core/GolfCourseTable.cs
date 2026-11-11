using System.Collections.Generic;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// 1コース（1回の遊び＝3ホール）。番号・名前・景色のワールドは MiniGameTable の行と同じ（記録・解放はそちらで決める）
    /// </summary>
    public sealed class GolfCourseDef
    {
        public GolfCourseDef(int no, string name, int world, IReadOnlyList<GolfHoleDef> holes, string gimmick = "")
        {
            No = no;
            Name = name;
            World = world;
            Holes = holes;
            Gimmick = gimmick;
        }

        public int No { get; }
        public string Name { get; }

        /// <summary>景色（地面の絵と色）のもとにするストーリーのワールド</summary>
        public int World { get; }

        public IReadOnlyList<GolfHoleDef> Holes { get; }

        /// <summary>仕掛けの一言（コース選びに出す。サッカー・野球の仕掛けの一言と同じ役目）</summary>
        public string Gimmick { get; }

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
    /// まだ作っていないコースは Get が null を返し、ミニゲーム画面で「準備中」になる
    /// </summary>
    public static class GolfCourseTable
    {
        private static readonly Dictionary<int, IReadOnlyList<GolfHoleDef>> Holes = new Dictionary<int, IReadOnlyList<GolfHoleDef>>
        {
            { 1, GolfCourse1.Holes },
            { 2, GolfCourse2.Holes },
            { 3, GolfCourse3.Holes },
            { 4, GolfCourse4.Holes },
            { 5, GolfCourse5.Holes },
        };

        // コース選びに出す仕掛けの一言。3ホールに出てくる仕掛けのうち、そのコースらしいものを短く
        private static readonly Dictionary<int, string> Gimmicks = new Dictionary<int, string>
        {
            { 1, "流氷の壁と海" },
            { 2, "氷の床・凍った坂・つらら" },
            { 3, "海流とワープの泡" },
            { 4, "崖へ吹く風と回る風車" },
            { 5, "溶岩の川・間欠泉・動く岩" },
        };

        /// <summary>番号のコース。表に無い・まだ作っていなければ null</summary>
        public static GolfCourseDef Get(int courseNo)
        {
            var row = MiniGameTable.Get(MiniGameTable.GolfId)?.Course(courseNo);
            if (row == null || !Holes.TryGetValue(courseNo, out var holes))
            {
                return null;
            }

            return new GolfCourseDef(row.No, row.Name, row.World, holes, Gimmicks.TryGetValue(courseNo, out var gimmick) ? gimmick : string.Empty);
        }

        /// <summary>開発用の試しのホールだけの1ホールのコース（記録には書かない）</summary>
        public static GolfCourseDef TestCourse()
        {
            return new GolfCourseDef(0, "試しのホール", 1, new[] { GolfTestHoles.TestHole });
        }
    }
}
