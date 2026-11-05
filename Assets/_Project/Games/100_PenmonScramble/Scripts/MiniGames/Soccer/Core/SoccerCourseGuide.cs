using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// ミニゲーム画面のスタジアム選びに出す、サッカーの中身。
    /// スタジアムはまだどれも作っていないので「準備中」（フェーズ 4 でスタジアム1、フェーズ 6 で残りを開け、CPU の強さ・仕掛け・報酬を出す）
    /// </summary>
    public sealed class SoccerCourseGuide : IMiniGameCourseGuide
    {
        public bool IsReady(int courseNo) => false;

        public string Layout(int courseNo) => string.Empty;

        public string FormatBest(int courseNo, int best) => FormatGoalDifference(best);

        public string Reward(int courseNo, bool firstPlay) => string.Empty;

        /// <summary>得失点差の見せ方（「+3」「±0」「−1」）</summary>
        public static string FormatGoalDifference(int difference)
        {
            if (difference > 0)
            {
                return $"+{difference}";
            }

            return difference == 0 ? "±0" : $"−{-difference}";
        }
    }
}
