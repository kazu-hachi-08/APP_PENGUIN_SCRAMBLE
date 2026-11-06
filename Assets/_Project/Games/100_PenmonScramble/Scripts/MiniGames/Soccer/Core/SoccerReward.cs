using System;

namespace PenmonScramble.Soccer.Core
{
    public enum SoccerOutcome
    {
        Loss,
        Win,
        /// <summary>PK戦で勝った（3分の間の得失点差は 0）</summary>
        ShootoutWin,
    }

    /// <summary>1試合のコインの内訳</summary>
    public readonly struct SoccerRewardBreakdown
    {
        public SoccerRewardBreakdown(int baseCoins, SoccerOutcome outcome, int winCoins, int goals, int goalCoins, int firstWinBonus)
        {
            BaseCoins = baseCoins;
            Outcome = outcome;
            WinCoins = winCoins;
            Goals = goals;
            GoalCoins = goalCoins;
            FirstWinBonus = firstWinBonus;
        }

        /// <summary>出ただけでもらえる分</summary>
        public int BaseCoins { get; }

        public SoccerOutcome Outcome { get; }

        /// <summary>勝ち（PK勝ちは少なめ）。負けは 0</summary>
        public int WinCoins { get; }

        /// <summary>点に数えた自分のチームの点（SoccerReward.MaxPaidGoals まで）</summary>
        public int Goals { get; }

        public int GoalCoins { get; }

        /// <summary>そのスタジアムで初めて勝ったときだけ</summary>
        public int FirstWinBonus { get; }

        public int Total => BaseCoins + WinCoins + GoalCoins + FirstWinBonus;
    }

    /// <summary>
    /// サッカーの報酬（案。計画書「報酬」）。本編のストーリーより控えめにする（数値はフェーズ 7 でゴルフと比べて決め直す）。マイナスにはしない
    /// </summary>
    public static class SoccerReward
    {
        public const int BaseCoins = 20;
        public const int WinCoins = 50;
        public const int ShootoutWinCoins = 30;
        public const int CoinsPerGoal = 8;
        public const int MaxPaidGoals = 5;
        public const int FirstWinBonus = 100;

        /// <param name="goals">自分のチームの3分の間の点（PK戦の点は数えない）</param>
        /// <param name="firstWin">そのスタジアムで初めて勝った（呼ぶ側が、記録を書く前の勝った回数が 0 かで決める）</param>
        public static SoccerRewardBreakdown Compute(int goals, SoccerOutcome outcome, bool firstWin)
        {
            var paid = Math.Clamp(goals, 0, MaxPaidGoals);
            var win = outcome == SoccerOutcome.Win ? WinCoins : outcome == SoccerOutcome.ShootoutWin ? ShootoutWinCoins : 0;
            var won = outcome != SoccerOutcome.Loss;
            return new SoccerRewardBreakdown(BaseCoins, outcome, win, paid, paid * CoinsPerGoal, won && firstWin ? FirstWinBonus : 0);
        }
    }
}
