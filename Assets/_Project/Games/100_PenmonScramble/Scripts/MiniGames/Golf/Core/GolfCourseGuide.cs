using System.Linq;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Golf.Core
{
    /// <summary>ミニゲーム画面のコース選びに出す、ゴルフのコースの中身（ホールとパー・ベストのパーとの差・報酬）</summary>
    public sealed class GolfCourseGuide : IMiniGameCourseGuide
    {
        public bool IsReady(int courseNo) => GolfCourseTable.Get(courseNo) != null;

        public string Layout(int courseNo)
        {
            var course = GolfCourseTable.Get(courseNo);
            if (course == null)
            {
                return string.Empty;
            }

            var pars = string.Join("・", course.Holes.Select(h => h.Par));
            return $"{course.Holes.Count}ホール  パー {pars}（合計 {course.TotalPar}）";
        }

        public string FormatBest(int courseNo, int best)
        {
            var course = GolfCourseTable.Get(courseNo);
            return course != null ? $"{best}打（{GolfScoreNames.Difference(best, course.TotalPar)}）" : $"{best}打";
        }

        public string Reward(int courseNo, bool firstPlay)
        {
            var line = $"報酬 {GolfReward.BaseCoins} コイン ＋ パーより少ない1打ごとに {GolfReward.CoinsPerStrokeUnderPar}";
            return firstPlay ? $"{line}\n初めて回ると ボーナス +{GolfReward.FirstPlayBonus}" : line;
        }
    }
}
