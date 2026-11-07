namespace PenmonScramble.Baseball.Core
{
    /// <summary>1球の結果をカウントに入れたら、打席がどうなったか</summary>
    public enum BaseballPlateResult
    {
        /// <summary>打席が続く（ボール・ストライク・ファウル）</summary>
        Continue,
        Strikeout,
        Walk,
        /// <summary>打ってヒット（ホームランも）</summary>
        Hit,
        /// <summary>打ってアウト（ゴロ・フライ・ゲッツー。走者の扱いはフェーズ 3 の BaseballBases）</summary>
        Out,
    }

    /// <summary>
    /// ボール・ストライク・アウトのカウント（Docs/MINIGAME_BASEBALL_PLAN.md「打つ」）。
    /// 3ストライクで三振・4ボールで四球。ファウルは 2ストライク未満ならストライクを1つ足す（バントのファウルも同じ）。
    /// 走者・点はフェーズ 3 の BaseballBases が持つ（ゲッツーもここではアウト1つ）
    /// </summary>
    public sealed class BaseballCount
    {
        public int Balls { get; private set; }
        public int Strikes { get; private set; }
        public int Outs { get; private set; }

        /// <summary>3アウトになった（攻守交代）</summary>
        public bool IsInningOver => Outs >= BaseballRules.OutsPerInning;

        public BaseballPlateResult Apply(BaseballPitchOutcome outcome)
        {
            switch (outcome.Kind)
            {
                case BaseballPitchOutcomeKind.CalledStrike:
                case BaseballPitchOutcomeKind.SwingingStrike:
                    return AddStrike();
                case BaseballPitchOutcomeKind.Ball:
                    return AddBall();
                case BaseballPitchOutcomeKind.Foul:
                    if (Strikes < BaseballRules.StrikesForOut - 1)
                    {
                        Strikes++;
                    }

                    return BaseballPlateResult.Continue;
                case BaseballPitchOutcomeKind.InPlay:
                    return InPlay(outcome.Hit);
                default:
                    return BaseballPlateResult.Continue;
            }
        }

        /// <summary>次の打者のためにボール・ストライクを戻す（アウトは残す）</summary>
        public void ResetCount()
        {
            Balls = 0;
            Strikes = 0;
        }

        /// <summary>攻守交代でアウトも戻す</summary>
        public void ClearOuts()
        {
            ResetCount();
            Outs = 0;
        }

        private BaseballPlateResult AddStrike()
        {
            Strikes++;
            if (Strikes < BaseballRules.StrikesForOut)
            {
                return BaseballPlateResult.Continue;
            }

            Outs++;
            ResetCount();
            return BaseballPlateResult.Strikeout;
        }

        private BaseballPlateResult AddBall()
        {
            Balls++;
            if (Balls < BaseballRules.BallsForWalk)
            {
                return BaseballPlateResult.Continue;
            }

            ResetCount();
            return BaseballPlateResult.Walk;
        }

        private BaseballPlateResult InPlay(BaseballHitResult hit)
        {
            ResetCount();
            if (!BaseballResults.IsOut(hit))
            {
                return BaseballPlateResult.Hit;
            }

            Outs++;
            return BaseballPlateResult.Out;
        }
    }
}
