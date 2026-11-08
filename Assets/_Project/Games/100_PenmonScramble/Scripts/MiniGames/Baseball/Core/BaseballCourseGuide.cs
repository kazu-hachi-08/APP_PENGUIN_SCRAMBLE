using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// ミニゲーム画面の球場選びに出す、野球の中身（CPU の強さ・盤と仕掛けの一言・ベストの得失点差・報酬）。
    /// まだ作っていない球場（BaseballStadiumTable に無い）は「準備中」
    /// </summary>
    public sealed class BaseballCourseGuide : IMiniGameCourseGuide
    {
        private const char FullStar = '★';
        private const char EmptyStar = '☆';

        public bool IsReady(int courseNo) => BaseballStadiumTable.Get(courseNo) != null;

        public string Layout(int courseNo)
        {
            var stadium = BaseballStadiumTable.Get(courseNo);
            return stadium != null ? MiniGameGuides.TwoLines($"CPUの強さ {Stars(stadium.CpuStars)}", stadium.Gimmick) : string.Empty;
        }

        /// <summary>
        /// みんなでの中身: 協力は人数で上がった CPU の強さ（「CPUの強さ ★★★☆☆（3人）」）、対戦は人どうし。2行目に盤と仕掛けの一言
        /// </summary>
        public string OnlineLayout(int courseNo, MiniGameOnlineMode mode, int humans)
        {
            var stadium = BaseballStadiumTable.Get(courseNo);
            if (stadium == null)
            {
                return string.Empty;
            }

            if (mode == MiniGameOnlineMode.Versus)
            {
                return MiniGameGuides.TwoLines("人どうしの対戦", stadium.Gimmick);
            }

            var stars = BaseballBalance.CoopCpuStars(stadium.CpuStars, humans);
            var people = humans > 1 ? $"（{humans}人）" : string.Empty;
            return MiniGameGuides.TwoLines($"CPUの強さ {Stars(stars)}{people}", stadium.Gimmick);
        }

        public string FormatBest(int courseNo, int best) => FormatRunDifference(best);

        public string Reward(int courseNo, bool firstPlay)
        {
            var line = $"報酬 {BaseballReward.BaseCoins} コイン ＋ 勝つと {BaseballReward.WinCoins} ＋ 1点ごとに {BaseballReward.CoinsPerRun}";
            return firstPlay ? $"{line}\n初めて勝つと ボーナス +{BaseballReward.FirstWinBonus}" : line;
        }

        /// <summary>得失点差の見せ方（「+3」「±0」「−1」。サッカーと同じ）</summary>
        public static string FormatRunDifference(int difference)
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
            var filled = System.Math.Clamp(stars, BaseballBalance.AiMinStars, BaseballBalance.AiMaxStars);
            return new string(FullStar, filled) + new string(EmptyStar, BaseballBalance.AiMaxStars - filled);
        }
    }
}
