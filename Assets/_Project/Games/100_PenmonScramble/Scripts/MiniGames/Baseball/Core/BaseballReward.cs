using System;

namespace PenmonScramble.Baseball.Core
{
    public enum BaseballOutcome
    {
        Loss,
        /// <summary>3回が終わって同点</summary>
        Draw,
        Win,
    }

    /// <summary>1試合のコインの内訳</summary>
    public readonly struct BaseballRewardBreakdown
    {
        public BaseballRewardBreakdown(int baseCoins, BaseballOutcome outcome, int winCoins, int runs, int runCoins, int firstWinBonus)
        {
            BaseCoins = baseCoins;
            Outcome = outcome;
            WinCoins = winCoins;
            Runs = runs;
            RunCoins = runCoins;
            FirstWinBonus = firstWinBonus;
        }

        /// <summary>出ただけでもらえる分</summary>
        public int BaseCoins { get; }

        public BaseballOutcome Outcome { get; }

        /// <summary>勝ち（引き分けは少し）。負けは 0</summary>
        public int WinCoins { get; }

        /// <summary>点に数えた自分のチームの点（BaseballReward.MaxPaidRuns まで）</summary>
        public int Runs { get; }

        public int RunCoins { get; }

        /// <summary>その球場で初めて勝ったときだけ</summary>
        public int FirstWinBonus { get; }

        public int Total => BaseCoins + WinCoins + RunCoins + FirstWinBonus;
    }

    /// <summary>
    /// 野球の報酬。マイナスにはしない。
    /// 1試合は人が入ると 5〜6 分（CPU 同士の試合の確認で 4〜5 分。人が投げる番は球種を選ぶ分だけ長い）で、1時間に 10〜12 試合。
    /// いちばん稼げる「5点以上取って勝ち続ける」で 1試合 110・1時間に 1200 ほど。ゴルフ（800〜1400）・サッカー（〜1400）と同じくらいで、
    /// ストーリーのくり返し（1時間に約 3500）より控えめ（計画書の案のまま。報酬の見直しはフェーズ 7）
    /// </summary>
    public static class BaseballReward
    {
        public const int BaseCoins = 20;
        public const int WinCoins = 50;
        public const int DrawCoins = 20;
        public const int CoinsPerRun = 8;
        public const int MaxPaidRuns = 5;
        public const int FirstWinBonus = 100;

        /// <param name="runs">自分のチームの点</param>
        /// <param name="firstWin">その球場で初めて勝った（呼ぶ側が、記録を書く前の勝った回数が 0 かで決める）</param>
        public static BaseballRewardBreakdown Compute(int runs, BaseballOutcome outcome, bool firstWin)
        {
            var paid = Math.Clamp(runs, 0, MaxPaidRuns);
            var win = outcome == BaseballOutcome.Win ? WinCoins : outcome == BaseballOutcome.Draw ? DrawCoins : 0;
            var bonus = outcome == BaseballOutcome.Win && firstWin ? FirstWinBonus : 0;
            return new BaseballRewardBreakdown(BaseCoins, outcome, win, paid, paid * CoinsPerRun, bonus);
        }

        /// <summary>自分のチームの点と相手の点から、勝ち・引き分け・負け</summary>
        public static BaseballOutcome OutcomeOf(int runs, int conceded)
        {
            if (runs == conceded)
            {
                return BaseballOutcome.Draw;
            }

            return runs > conceded ? BaseballOutcome.Win : BaseballOutcome.Loss;
        }
    }
}
