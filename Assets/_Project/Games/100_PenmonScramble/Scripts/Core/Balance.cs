namespace PenmonScramble.Core
{
    /// <summary>
    /// ゲームの調整値。数値はここにまとめ、ルールのコードには直接書かない。
    /// </summary>
    public static class Balance
    {
        /// <summary>
        /// 1回の更新で進める時間（秒）。オンラインでホストとゲストの結果をそろえるため、描画のフレームとは切り離して固定にする
        /// </summary>
        public const float TickSeconds = 1f / 30f;
    }
}
