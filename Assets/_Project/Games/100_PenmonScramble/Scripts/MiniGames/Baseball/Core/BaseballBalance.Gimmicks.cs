namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// 球場の仕掛け（氷の床・水流・風・間欠泉・動く穴）の調整値。置き場所と時間の長さは BaseballFieldTable。
    /// 数は CPU の試合の確認（全球場）で、点の入り方が極端にならないよう決めた
    /// </summary>
    public static partial class BaseballBalance
    {
        // ---- 氷の床（氷山）----

        /// <summary>氷の床の上の転がりの摩擦の倍率（よく転がる。外野の奥まで転がって長打になりやすい）</summary>
        public const float IceFloorFriction = 0.45f;

        // ---- 水流の帯（深海）----

        /// <summary>水流が転がる球を押す強さ（毎秒。転がりの摩擦より強くして、帯の中の遅い球を流し続ける）</summary>
        public const float CurrentAccel = 14f;

        /// <summary>水流が押す速さの上限（弧に沿う向きの速さ）</summary>
        public const float CurrentMaxSpeed = 3.2f;

        /// <summary>水流の向きが入れ替わる秒</summary>
        public const float CurrentFlipSeconds = 5f;

        // ---- 風（嵐の崖）----

        /// <summary>風が浮いた球を横へ速くする強さ（毎秒。浮いた打球は約 1.2 秒飛ぶので、2 マスほど流される）</summary>
        public const float WindAccel = 3f;

        /// <summary>風が吹く秒と止む秒（吹くたびに左右が入れ替わる）</summary>
        public const float WindBlowSeconds = 4f;
        public const float WindCalmSeconds = 2f;

        // ---- 動く穴（嵐の崖）----

        /// <summary>動くアウトの穴が左右へ動く角度（度）と、1往復の秒</summary>
        public const float SwayDegrees = 6f;
        public const float SwaySeconds = 3.2f;

        // ---- 間欠泉（火山の入り江）----

        /// <summary>跳ね上げる上向きの速さ（いちばん高い所が約 0.9 マス。高いフェンスは越えない）</summary>
        public const float GeyserUpSpeed = 4.6f;

        /// <summary>跳ね上げるとき、口の真ん中から外へ押し出す速さ（同じ口へ落ち続けないように）</summary>
        public const float GeyserPushSpeed = 2.5f;

        /// <summary>跳ね上げた球の横の速さを何倍に残すか</summary>
        public const float GeyserKeepSpeed = 0.6f;

        /// <summary>間欠泉の口の半径・噴く間隔・噴いている秒</summary>
        public const float GeyserRadius = 0.55f;
        public const float GeyserPeriod = 3f;
        public const float GeyserActive = 1.2f;
    }
}
