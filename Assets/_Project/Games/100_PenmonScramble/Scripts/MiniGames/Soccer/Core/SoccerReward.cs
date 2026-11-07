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
    /// サッカーの報酬。マイナスにはしない。
    /// 1試合は合図・ゴールの間・結果まで入れて 3分40秒ほど（1時間に 16 試合）。ストーリーのくり返し（1時間に約 3500）より控えめな
    /// ゴルフ（800〜1400）に合わせ、いちばん稼げる「5点以上取って勝ち続ける」でも 1試合 85・1時間に 1400 ほどにした
    /// （計画書の案の 勝ち 50・1点 8 では 110・1時間に 1800 で、海岸の CPU ★1 にはまず勝てるのでゴルフより稼げてしまった）
    /// </summary>
    public static class SoccerReward
    {
        public const int BaseCoins = 20;
        public const int WinCoins = 40;
        public const int ShootoutWinCoins = 25;
        public const int CoinsPerGoal = 5;
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
