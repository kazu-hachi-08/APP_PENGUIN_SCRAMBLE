using System;

namespace PenmonScramble.Golf.Core
{
    /// <summary>1コースを回ったときのコインの内訳</summary>
    public readonly struct GolfRewardBreakdown
    {
        public GolfRewardBreakdown(int baseCoins, int underParStrokes, int underParCoins, int holeInOnes, int holeInOneCoins, int firstPlayBonus)
        {
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

        public int Total => BaseCoins + UnderParCoins + HoleInOneCoins + FirstPlayBonus;
    }

    /// <summary>
    /// ゴルフの報酬（Docs/MINIGAME_GOLF_PLAN.md「スコアと報酬」の案）。本編の1ステージ（クリア 100）より控えめにして、
    /// ミニゲームだけでコインを稼ぐのが一番得にならないようにする（フェーズ 7 で決め直す）。マイナスにはしない
    /// </summary>
    public static class GolfReward
    {
        public const int BaseCoins = 40;
        public const int CoinsPerStrokeUnderPar = 15;
        public const int CoinsPerHoleInOne = 50;
        public const int FirstPlayBonus = 100;

        /// <param name="strokes">3ホールの打数の合計</param>
        /// <param name="par">3ホールのパーの合計</param>
        public static GolfRewardBreakdown Compute(int strokes, int par, int holeInOnes, bool firstPlay)
        {
            var under = Math.Max(0, par - strokes);
            var holeInOneCount = Math.Max(0, holeInOnes);
            return new GolfRewardBreakdown(
                BaseCoins,
                under,
                under * CoinsPerStrokeUnderPar,
                holeInOneCount,
                holeInOneCount * CoinsPerHoleInOne,
                firstPlay ? FirstPlayBonus : 0);
        }
    }
}
