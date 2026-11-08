using PenmonScramble.MiniGames.Game;
using PenmonScramble.Soccer.Core;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>サッカーの結果に出すもの（自分のチームから見た。ひとりでは赤）</summary>
    public readonly struct SoccerResultInfo
    {
        public SoccerResultInfo(int team, int goals, int conceded, SoccerOutcome outcome, SoccerShootout shootout, SoccerRewardBreakdown reward, bool recorded, int previousBest, bool hadBest, bool firstPlay = false, bool newBest = false)
        {
            Team = team;
            Goals = goals;
            Conceded = conceded;
            Outcome = outcome;
            Shootout = shootout;
            Reward = reward;
            Recorded = recorded;
            PreviousBest = previousBest;
            HadBest = hadBest;
            FirstPlay = firstPlay;
            NewBest = newBest;
        }

        /// <summary>自分のチーム</summary>
        public int Team { get; }

        public int Goals { get; }
        public int Conceded { get; }
        public SoccerOutcome Outcome { get; }

        /// <summary>PK戦（しなかったら null）</summary>
        public SoccerShootout Shootout { get; }

        public SoccerRewardBreakdown Reward { get; }

        /// <summary>記録に書いた（開発用の試しのピッチ・PK戦の確認では書かない）</summary>
        public bool Recorded { get; }

        public int PreviousBest { get; }
        public bool HadBest { get; }
        public bool FirstPlay { get; }
        public bool NewBest { get; }

        /// <summary>得失点差（PK戦で決まっても、3分の間の点だけで数える）</summary>
        public int GoalDifference => Goals - Conceded;

        public SoccerResultInfo WithRecord(bool firstPlay, bool newBest) =>
            new SoccerResultInfo(Team, Goals, Conceded, Outcome, Shootout, Reward, Recorded, PreviousBest, HadBest, firstPlay, newBest);
    }

    /// <summary>
    /// サッカーの結果を、結果の欄（MiniGameResultPanel）の中身にする:
    /// 「勝ち！ 3 − 1」（PK戦なら「PK勝ち！ 2 − 2」）・得失点差とベスト（PK戦の点）・コインの内訳
    /// </summary>
    public static class SoccerResultText
    {
        public static MiniGameResultView Build(SoccerResultInfo info)
        {
            var reward = info.Reward;
            var view = new MiniGameResultView
            {
                Score = $"{Headline(info)} {info.Goals} − {info.Conceded}",
                Best = BestLine(info),
                BestHighlighted = info.Recorded && (info.NewBest || info.FirstPlay),
                CoinTotal = info.Recorded ? $"+{reward.Total:N0} コイン" : "試しの試合は記録しません",
            };

            view.AddReward("試合に出た", reward.BaseCoins);
            if (reward.WinCoins > 0)
            {
                view.AddReward(reward.Outcome == SoccerOutcome.ShootoutWin ? "PK戦で勝ち" : "勝ち", reward.WinCoins);
            }

            if (reward.Goals > 0)
            {
                view.AddReward($"{reward.Goals} 点", reward.GoalCoins);
            }

            if (reward.FirstWinBonus > 0)
            {
                view.AddReward("このスタジアムで初めて勝った", reward.FirstWinBonus);
            }

            return view;
        }

        private static string Headline(SoccerResultInfo info)
        {
            switch (info.Outcome)
            {
                case SoccerOutcome.Win:
                    return "勝ち！";
                case SoccerOutcome.ShootoutWin:
                    return "PK勝ち！";
                default:
                    return info.Shootout != null ? "PK負け…" : "負け…";
            }
        }

        /// <summary>「得失点差 +2」と、その下の行にベスト（「ベスト更新！」など）。PK戦をしたら PK の点も添える</summary>
        private static string BestLine(SoccerResultInfo info)
        {
            var line = $"得失点差 {SoccerCourseGuide.FormatGoalDifference(info.GoalDifference)}";
            if (info.Shootout != null)
            {
                line += $"（PK {info.Shootout.Score(info.Team)} − {info.Shootout.Score(SoccerRules.Opponent(info.Team))}）";
            }

            if (!info.Recorded)
            {
                return line;
            }

            var best = MiniGameResultView.BestSentence(info.FirstPlay, info.NewBest, SoccerCourseGuide.FormatGoalDifference(info.PreviousBest));
            return $"{line}\n{best}";
        }
    }
}
