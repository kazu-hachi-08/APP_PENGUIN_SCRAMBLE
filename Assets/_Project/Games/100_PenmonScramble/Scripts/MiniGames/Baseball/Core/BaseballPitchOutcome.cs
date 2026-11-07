namespace PenmonScramble.Baseball.Core
{
    public enum BaseballPitchOutcomeKind
    {
        /// <summary>まだ決まっていない（投げる前・球が動いている）</summary>
        None,
        /// <summary>見送ってゾーンの中</summary>
        CalledStrike,
        /// <summary>振って当たらなかった（バントの構えのまま見送ったのも）</summary>
        SwingingStrike,
        /// <summary>見送ってゾーンの外</summary>
        Ball,
        /// <summary>当たったがファウル</summary>
        Foul,
        /// <summary>当たってフェアの結果が出た（Hit がその結果）</summary>
        InPlay,
    }

    /// <summary>1球の結果。カウント・走者はフェーズ 3 の BaseballAtBat がこれを見て進める</summary>
    public readonly struct BaseballPitchOutcome
    {
        public static readonly BaseballPitchOutcome None = new BaseballPitchOutcome(BaseballPitchOutcomeKind.None, BaseballHitResult.Single, false);

        public BaseballPitchOutcome(BaseballPitchOutcomeKind kind, BaseballHitResult hit, bool bunt)
        {
            Kind = kind;
            Hit = hit;
            Bunt = bunt;
        }

        public BaseballPitchOutcomeKind Kind { get; }

        /// <summary>InPlay のときの打球の結果</summary>
        public BaseballHitResult Hit { get; }

        /// <summary>バントで当てた球か（犠打の扱いはフェーズ 3）</summary>
        public bool Bunt { get; }

        public bool IsDecided => Kind != BaseballPitchOutcomeKind.None;

        public override string ToString() => Kind == BaseballPitchOutcomeKind.InPlay ? $"{Kind}({Hit}{(Bunt ? ", bunt" : string.Empty)})" : Kind.ToString();
    }
}
