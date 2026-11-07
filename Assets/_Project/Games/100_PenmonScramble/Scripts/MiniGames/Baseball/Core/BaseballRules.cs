namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// 野球のルールの数（刻み）。球とバットの調整値は BaseballBalance、盤の形は BaseballFieldDef。
    /// カウント・イニングなど試合のルールはフェーズ 3 で足す
    /// </summary>
    public static class BaseballRules
    {
        /// <summary>
        /// 1歩の時間。本編・サッカーと同じ 1/30 秒の固定の刻み（描画のフレームに関係なく、同じ入力なら同じ結果になるように）。
        /// 速い球が穴やバットをすり抜けないよう、球はこの中をさらに細かく分けて進める（BaseballBalance.BallSubstepLength）
        /// </summary>
        public const float StepSeconds = 1f / 30f;
    }
}
