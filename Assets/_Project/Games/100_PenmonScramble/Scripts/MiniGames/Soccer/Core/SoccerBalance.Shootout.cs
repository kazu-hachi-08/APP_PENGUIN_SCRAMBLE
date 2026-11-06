namespace PenmonScramble.Soccer.Core
{
    /// <summary>PK戦の調整値（案。フェーズ 7 の試遊で決め直す）</summary>
    public static partial class SoccerBalance
    {
        /// <summary>ボールを置く所の、ゴールの線からの距離</summary>
        public const float PenaltyDistance = 6f;

        /// <summary>蹴る子はボールのこれだけ後ろに立つ</summary>
        public const float PenaltyRunUp = 0.9f;

        /// <summary>構え（何本目かを見せる）・狙う持ち時間・決まった後に見せる秒</summary>
        public const float PenaltyReadySeconds = 1f;
        public const float PenaltyAimSeconds = 5f;
        public const float PenaltyResultSeconds = 1.5f;

        /// <summary>スティックをこれより倒したら左右・高いを選んだことにする</summary>
        public const float PenaltyStickDeadZone = 0.35f;

        /// <summary>ためゲージが満タンになるまで（満タンのまま止まる）</summary>
        public const float PenaltyChargeSeconds = 1f;

        /// <summary>ためがこれより上は赤い所（強いが、枠を外れることがある）</summary>
        public const float PenaltyRedZone = 0.85f;

        /// <summary>赤い所のいちばん上（満タン）で外れる割合と、高い所を狙うといつも足される外れる割合</summary>
        public const float PenaltyMaxMissChance = 0.6f;
        public const float PenaltyHighMissChance = 0.08f;

        /// <summary>持ち時間が過ぎたときの、弱く蹴る強さ</summary>
        public const float PenaltyTimeoutPower = 0.3f;

        /// <summary>
        /// 守る向きが合ったときに止める割合 ＝ PenaltySaveBase − 強さ × PenaltySavePowerWeight（高い所なら PenaltyHighSaveDrop も引く）。
        /// 向きが違えば入る
        /// </summary>
        public const float PenaltySaveBase = 1.1f;
        public const float PenaltySavePowerWeight = 0.7f;
        public const float PenaltyHighSaveDrop = 0.2f;
        public const float PenaltyMinSave = 0.1f;
        public const float PenaltyMaxSave = 0.95f;

        /// <summary>強さ 0 と 1 の球の速さ（ゴールまで飛ぶ秒が決まる）</summary>
        public const float PenaltyMinSpeed = 9f;
        public const float PenaltyMaxSpeed = 22f;

        /// <summary>狙う所: 柱の内側への余白・高い所のボールの下の端の高さ（バーの下）</summary>
        public const float PenaltyPostInset = 0.75f;
        public const float PenaltyHighHeight = 0.75f;

        /// <summary>外れた球が飛ぶ所: 柱のこれだけ外・バーの上の高さ</summary>
        public const float PenaltyWideOffset = 0.9f;
        public const float PenaltyOverHeight = 1.9f;

        /// <summary>キーパーが飛ぶ速さ（PK戦の見た目）</summary>
        public const float PenaltyKeeperDiveSpeed = 6f;

        /// <summary>CPU が蹴る: 左右・真ん中を選ぶ割合（真ん中）・高い所を狙う割合・狙う強さのまん中・構えてからため始めるまでの秒</summary>
        public const float AiPenaltyCenterChance = 0.2f;
        public const float AiPenaltyHighChance = 0.35f;
        public const float AiPenaltyPower = 0.74f;
        public const float AiPenaltyMinWait = 0.6f;
        public const float AiPenaltyMaxWait = 1.5f;
    }
}
