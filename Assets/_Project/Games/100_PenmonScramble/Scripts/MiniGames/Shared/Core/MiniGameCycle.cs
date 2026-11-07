namespace PenmonScramble.MiniGames.Core
{
    /// <summary>
    /// 決まった間隔のくり返し（Period 秒ごとに、はじめの Active 秒だけ「動いている」）。Offset 秒ずらして始める。
    /// 時間だけで決まるので、ホストとゲストで同じ時刻なら同じ様子になる（ゴルフの間欠泉で作り、サッカーの仕掛けでも使うので共通側へ移した）
    /// </summary>
    public readonly struct MiniGameCycle
    {
        public readonly float Period;
        public readonly float Active;
        public readonly float Offset;

        public MiniGameCycle(float period, float active, float offset = 0f)
        {
            Period = period;
            Active = active;
            Offset = offset;
        }

        /// <summary>1回りの中のどこか（0〜Period）</summary>
        public float Local(float time)
        {
            if (Period <= 0f)
            {
                return 0f;
            }

            var t = (time + Offset) % Period;
            return t < 0f ? t + Period : t;
        }

        public bool IsActive(float time) => Period > 0f && Local(time) < Active;

        /// <summary>次に動き出すまでの秒（動いている間は 0）。見た目の「もうすぐ噴く」に使う</summary>
        public float UntilActive(float time) => IsActive(time) ? 0f : Period - Local(time);
    }
}
