using PenmonScramble.Baseball.Core;
using PenmonScramble.MiniGames.Game;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>野球の結果に出すもの（自分のチームから見た）</summary>
    public readonly struct BaseballResultInfo
    {
        public BaseballResultInfo(int runs, int conceded, BaseballOutcome outcome, BaseballRewardBreakdown reward, bool recorded, int previousBest, bool hadBest, bool firstPlay = false, bool newBest = false)
        {
            Runs = runs;
            Conceded = conceded;
            Outcome = outcome;
            Reward = reward;
            Recorded = recorded;
            PreviousBest = previousBest;
            HadBest = hadBest;
            FirstPlay = firstPlay;
            NewBest = newBest;
        }

        public int Runs { get; }
        public int Conceded { get; }
        public BaseballOutcome Outcome { get; }
        public BaseballRewardBreakdown Reward { get; }

        /// <summary>記録に書いた（開発用の試しの試合では書かない）</summary>
        public bool Recorded { get; }

        public int PreviousBest { get; }
        public bool HadBest { get; }
        public bool FirstPlay { get; }
        public bool NewBest { get; }

        public int RunDifference => Runs - Conceded;

        public BaseballResultInfo WithRecord(bool firstPlay, bool newBest) =>
            new BaseballResultInfo(Runs, Conceded, Outcome, Reward, Recorded, PreviousBest, HadBest, firstPlay, newBest);
    }

    /// <summary>
    /// 野球の結果を、結果の欄（MiniGameResultPanel）の中身にする:
    /// 「勝ち！ 3 − 1」・得失点差とベスト・コインの内訳（出た・勝ち / 引き分け・点・初めて勝った）
    /// </summary>
    public static class BaseballResultText
    {
        public static MiniGameResultView Build(BaseballResultInfo info)
        {
            var reward = info.Reward;
            var view = new MiniGameResultView
            {
                Score = $"{Headline(info.Outcome)} {info.Runs} − {info.Conceded}",
                Best = BestLine(info),
                BestHighlighted = info.Recorded && (info.NewBest || info.FirstPlay),
                CoinTotal = info.Recorded ? $"+{reward.Total:N0} コイン" : "試しの試合は記録しません",
            };

            view.AddReward("試合に出た", reward.BaseCoins);
            if (reward.WinCoins > 0)
            {
                view.AddReward(reward.Outcome == BaseballOutcome.Draw ? "引き分け" : "勝ち", reward.WinCoins);
            }

            if (reward.Runs > 0)
            {
                view.AddReward($"{reward.Runs} 点", reward.RunCoins);
            }

            if (reward.FirstWinBonus > 0)
            {
                view.AddReward("この球場で初めて勝った", reward.FirstWinBonus);
            }

            return view;
        }

        private static string Headline(BaseballOutcome outcome)
        {
            switch (outcome)
            {
                case BaseballOutcome.Win:
                    return "勝ち！";
                case BaseballOutcome.Draw:
                    return "引き分け";
                default:
                    return "負け…";
            }
        }

        /// <summary>「得失点差 +2」と、その下の行にベスト（「ベスト更新！」など）</summary>
        private static string BestLine(BaseballResultInfo info)
        {
            var line = $"得失点差 {BaseballCourseGuide.FormatRunDifference(info.RunDifference)}";
            if (!info.Recorded)
            {
                return line;
            }

            if (info.FirstPlay)
            {
                return $"{line}\nはじめての記録！";
            }

            return info.NewBest
                ? $"{line}\nベスト更新！（前は {BaseballCourseGuide.FormatRunDifference(info.PreviousBest)}）"
                : $"{line}\nベスト {BaseballCourseGuide.FormatRunDifference(info.PreviousBest)}";
        }
    }
}
