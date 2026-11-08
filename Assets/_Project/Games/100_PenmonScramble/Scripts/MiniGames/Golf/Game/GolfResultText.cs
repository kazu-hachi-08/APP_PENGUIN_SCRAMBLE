using PenmonScramble.Golf.Core;
using PenmonScramble.MiniGames.Game;

namespace PenmonScramble.Golf.Game
{
    /// <summary>ゴルフの結果に出すもの</summary>
    public readonly struct GolfResultInfo
    {
        public GolfResultInfo(int strokes, int par, GolfRewardBreakdown reward, bool newBest, bool firstPlay, int previousBest, bool recorded)
        {
            Strokes = strokes;
            Par = par;
            Reward = reward;
            NewBest = newBest;
            FirstPlay = firstPlay;
            PreviousBest = previousBest;
            Recorded = recorded;
        }

        public int Strokes { get; }
        public int Par { get; }
        public GolfRewardBreakdown Reward { get; }
        public bool NewBest { get; }
        public bool FirstPlay { get; }

        /// <summary>今回の前のベスト（初めてなら 0）</summary>
        public int PreviousBest { get; }

        /// <summary>記録に書いた（開発用の試しのホールでは書かない）</summary>
        public bool Recorded { get; }
    }

    /// <summary>ゴルフの結果を、結果の欄（MiniGameResultPanel）の中身にする: 合計の打数とパーとの差・ベスト更新・コインの内訳</summary>
    public static class GolfResultText
    {
        public static MiniGameResultView Build(GolfResultInfo info)
        {
            var reward = info.Reward;
            var rank = reward.Rank > 0 ? $"{reward.Rank}位  " : string.Empty;
            var view = new MiniGameResultView
            {
                Score = $"{rank}{info.Strokes} 打（{GolfScoreNames.Difference(info.Strokes, info.Par)}）",
                Best = BestLine(info),
                BestHighlighted = info.NewBest,
                CoinTotal = info.Recorded ? $"+{reward.Total:N0} コイン" : "試しのホールは記録しません",
            };

            view.AddReward("回った", reward.BaseCoins);
            if (reward.UnderParStrokes > 0)
            {
                view.AddReward($"パーより {reward.UnderParStrokes} 打少ない", reward.UnderParCoins);
            }

            if (reward.HoleInOnes > 0)
            {
                view.AddReward($"ホールインワン ×{reward.HoleInOnes}", reward.HoleInOneCoins);
            }

            if (reward.FirstPlayBonus > 0)
            {
                view.AddReward("初めて回った", reward.FirstPlayBonus);
            }

            if (reward.RankBonus > 0)
            {
                view.AddReward($"{reward.Rank}位", reward.RankBonus);
            }

            return view;
        }

        private static string BestLine(GolfResultInfo info)
        {
            if (!info.Recorded)
            {
                return string.Empty;
            }

            return MiniGameResultView.BestSentence(info.FirstPlay, info.NewBest, $"{info.PreviousBest} 打");
        }
    }
}
