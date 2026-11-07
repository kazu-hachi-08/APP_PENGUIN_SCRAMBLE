namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// 野球のルールの数（刻み・カウント・1球の持ち時間）。球とバットの調整値は BaseballBalance、盤の形は BaseballFieldDef。
    /// イニング・点差など試合のルールはフェーズ 3 で足す
    /// </summary>
    public static class BaseballRules
    {
        /// <summary>
        /// 1歩の時間。本編・サッカーと同じ 1/30 秒の固定の刻み（描画のフレームに関係なく、同じ入力なら同じ結果になるように）。
        /// 速い球が穴やバットをすり抜けないよう、球はこの中をさらに細かく分けて進める（BaseballBalance.BallSubstepLength）
        /// </summary>
        public const float StepSeconds = 1f / 30f;

        /// <summary>このボールの数で四球</summary>
        public const int BallsForWalk = 4;

        /// <summary>このストライクの数で三振（ファウルではここまで足さない）</summary>
        public const int StrikesForOut = 3;

        /// <summary>このアウトの数で攻守交代</summary>
        public const int OutsPerInning = 3;

        /// <summary>1球の持ち時間（秒。案）。過ぎたら、そのとき選んでいる球種でまっすぐ投げる</summary>
        public const float PitchClockSeconds = 8f;
    }
}
