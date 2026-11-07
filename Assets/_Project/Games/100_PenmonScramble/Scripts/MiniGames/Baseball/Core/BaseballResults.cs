namespace PenmonScramble.Baseball.Core
{
    /// <summary>結果の見せ方（大きな文字）と種類分け</summary>
    public static class BaseballResults
    {
        public static bool IsOut(BaseballHitResult result)
        {
            return result == BaseballHitResult.GroundOut || result == BaseballHitResult.DoublePlay || result == BaseballHitResult.FlyOut;
        }

        public static string Label(BaseballHitResult result)
        {
            return result switch
            {
                BaseballHitResult.Single => "ヒット",
                BaseballHitResult.Double => "ツーベース",
                BaseballHitResult.Triple => "スリーベース",
                BaseballHitResult.HomeRun => "ホームラン！",
                BaseballHitResult.GroundOut => "ゴロアウト",
                BaseballHitResult.DoublePlay => "ゲッツー",
                BaseballHitResult.FlyOut => "フライアウト",
                BaseballHitResult.Foul => "ファウル",
                _ => string.Empty,
            };
        }

        /// <summary>1球の結果の見せ方（打った球は打球の結果）</summary>
        public static string Label(BaseballPitchOutcome outcome)
        {
            return outcome.Kind switch
            {
                BaseballPitchOutcomeKind.CalledStrike => "ストライク",
                BaseballPitchOutcomeKind.SwingingStrike => "空振り",
                BaseballPitchOutcomeKind.Ball => "ボール",
                BaseballPitchOutcomeKind.Foul => Label(BaseballHitResult.Foul),
                BaseballPitchOutcomeKind.InPlay => Label(outcome.Hit),
                _ => string.Empty,
            };
        }
    }
}
