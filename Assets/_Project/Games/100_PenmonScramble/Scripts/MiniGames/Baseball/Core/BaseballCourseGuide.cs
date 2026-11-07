using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// ミニゲーム画面の球場選びに出す、野球の中身。
    /// 球場はまだどれも作っていないので「準備中」（フェーズ 4 で球場1、フェーズ 6 で残りを開け、CPU の強さ・盤と仕掛け・報酬を出す）
    /// </summary>
    public sealed class BaseballCourseGuide : IMiniGameCourseGuide
    {
        public bool IsReady(int courseNo) => false;

        public string Layout(int courseNo) => string.Empty;

        public string FormatBest(int courseNo, int best) => FormatRunDifference(best);

        public string Reward(int courseNo, bool firstPlay) => string.Empty;

        /// <summary>得失点差の見せ方（「+3」「±0」「−1」。サッカーと同じ）</summary>
        public static string FormatRunDifference(int difference)
        {
            if (difference > 0)
            {
                return $"+{difference}";
            }

            return difference == 0 ? "±0" : $"−{-difference}";
        }
    }
}
