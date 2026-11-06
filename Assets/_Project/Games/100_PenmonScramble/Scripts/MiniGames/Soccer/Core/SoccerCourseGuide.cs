using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// ミニゲーム画面のスタジアム選びに出す、サッカーの中身（CPU の強さ・仕掛けの一言・ベストの得失点差・報酬）。
    /// まだ作っていないスタジアム（SoccerStadiumTable に無い）は「準備中」
    /// </summary>
    public sealed class SoccerCourseGuide : IMiniGameCourseGuide
    {
        private const char FullStar = '★';
        private const char EmptyStar = '☆';

        public bool IsReady(int courseNo) => SoccerStadiumTable.Get(courseNo) != null;

        public string Layout(int courseNo)
        {
            var stadium = SoccerStadiumTable.Get(courseNo);
            return stadium != null ? $"CPUの強さ {Stars(stadium.CpuStars)}  {stadium.Gimmick}" : string.Empty;
        }

        public string FormatBest(int courseNo, int best) => FormatGoalDifference(best);

        public string Reward(int courseNo, bool firstPlay)
        {
            var line = $"報酬 {SoccerReward.BaseCoins} コイン ＋ 勝つと {SoccerReward.WinCoins} ＋ 1点ごとに {SoccerReward.CoinsPerGoal}";
            return firstPlay ? $"{line}\n初めて勝つと ボーナス +{SoccerReward.FirstWinBonus}" : line;
        }

        /// <summary>得失点差の見せ方（「+3」「±0」「−1」）</summary>
        public static string FormatGoalDifference(int difference)
        {
            if (difference > 0)
            {
                return $"+{difference}";
            }

            return difference == 0 ? "±0" : $"−{-difference}";
        }

        /// <summary>CPU の強さを5つの星で（「★☆☆☆☆」）</summary>
        public static string Stars(int stars)
        {
            var filled = System.Math.Clamp(stars, SoccerBalance.AiMinStars, SoccerBalance.AiMaxStars);
            return new string(FullStar, filled) + new string(EmptyStar, SoccerBalance.AiMaxStars - filled);
        }
    }
}
