namespace PenmonScramble.Core
{
    /// <summary>敵が技を出す前の構え。構えている間は動かず向きも変えず、時間が来たら構えたときの向きで技を出す</summary>
    internal sealed class WindUpState
    {
        public WindUpState(int slot, float seconds)
        {
            Slot = slot;
            TotalSeconds = seconds;
            RemainingSeconds = seconds;
        }

        /// <summary>出す技の番号（0 が技1）</summary>
        public int Slot { get; }
        public float TotalSeconds { get; }
        public float RemainingSeconds { get; set; }

        /// <summary>構え始めてからの割合（0〜1。1 で技が出る）</summary>
        public float Progress => TotalSeconds > 0f ? 1f - RemainingSeconds / TotalSeconds : 1f;
    }
}
