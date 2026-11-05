using System;

namespace PenmonScramble.Golf.Core
{
    /// <summary>1コースを回ったときのコインの内訳</summary>
    public readonly struct GolfRewardBreakdown
    {
        public GolfRewardBreakdown(int baseCoins, int underParStrokes, int underParCoins, int holeInOnes, int holeInOneCoins, int firstPlayBonus, int rank = 0, int rankBonus = 0)
        {
            Rank = rank;
            RankBonus = rankBonus;
            BaseCoins = baseCoins;
            UnderParStrokes = underParStrokes;
            UnderParCoins = underParCoins;
            HoleInOnes = holeInOnes;
            HoleInOneCoins = holeInOneCoins;
            FirstPlayBonus = firstPlayBonus;
        }

        /// <summary>回っただけでもらえる分</summary>
        public int BaseCoins { get; }

        /// <summary>パーより少なかった打数（パー以上なら 0）</summary>
        public int UnderParStrokes { get; }

        public int UnderParCoins { get; }
        public int HoleInOnes { get; }
        public int HoleInOneCoins { get; }

        /// <summary>そのコースを初めて回ったときだけ。2回目からは 0</summary>
        public int FirstPlayBonus { get; }

        /// <summary>オンラインの順位（1 から）。ひとりで回ったときは 0</summary>
        public int Rank { get; }

        /// <summary>オンラインの順位のボーナス。ひとりで回ったとき・4位は 0</summary>
        public int RankBonus { get; }

        public int Total => BaseCoins + UnderParCoins + HoleInOneCoins + FirstPlayBonus + RankBonus;
    }

    /// <summary>
    /// ゴルフの報酬（仕様書 §13.3「報酬」）。本編のストーリー（1回 100・1時間に約 3500）より控えめにして、
    /// ミニゲームだけでコインを稼ぐのが一番得にならないようにする（3ホールに2〜3分かかるので、パー前後なら1時間に 1500 に届かない）。マイナスにはしない
    /// </summary>
    public static class GolfReward
    {
        public const int BaseCoins = 40;
        public const int CoinsPerStrokeUnderPar = 15;
        public const int CoinsPerHoleInOne = 50;
        public const int FirstPlayBonus = 100;

        /// <summary>オンラインの順位のボーナス（1位・2位・3位）。同じ打数は同じ順位</summary>
        private static readonly int[] RankBonuses = { 40, 20, 10 };

        public static int RankBonusFor(int rank) => rank >= 1 && rank <= RankBonuses.Length ? RankBonuses[rank - 1] : 0;

        /// <param name="strokes">3ホールの打数の合計</param>
        /// <param name="par">3ホールのパーの合計</param>
        /// <param name="rank">オンラインの順位（1 から）。ひとりで回ったときは 0（順位のボーナスは付かない）</param>
        public static GolfRewardBreakdown Compute(int strokes, int par, int holeInOnes, bool firstPlay, int rank = 0)
        {
            var under = Math.Max(0, par - strokes);
            var holeInOneCount = Math.Max(0, holeInOnes);
            return new GolfRewardBreakdown(
                BaseCoins,
                under,
                under * CoinsPerStrokeUnderPar,
                holeInOneCount,
                holeInOneCount * CoinsPerHoleInOne,
                firstPlay ? FirstPlayBonus : 0,
                Math.Max(0, rank),
                RankBonusFor(rank));
        }
    }
}
