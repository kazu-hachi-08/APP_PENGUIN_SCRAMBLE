namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// 球の動きの調整値（長さは m・時間は秒）。ホスト1台で計算して全員に配るので、Unity の物理は使わず、ここの数で決める
    /// </summary>
    public static class GolfBalance
    {
        /// <summary>
        /// 1歩の時間。描画のフレームに関係なく、いつも同じ刻みで進める（同じ打ち方なら同じ結果になるように）
        /// </summary>
        public const float StepSeconds = 1f / 120f;

        public const float BallRadius = 0.18f;

        /// <summary>
        /// 球の速さの上限。1歩で進む距離（MaxBallSpeed × StepSeconds）が球の半径より短ければ、薄い壁をすり抜けない
        /// </summary>
        public const float MaxBallSpeed = 13f;

        /// <summary>ゲージ 0% と 100% のときの打ち出しの速さ。平らな地面なら 100% で 約 30m 転がる</summary>
        public const float MinShotSpeed = 1.2f;
        public const float MaxShotSpeed = 10f;

        /// <summary>転がりの摩擦（毎秒これだけ遅くなる）。速さに比べない一定の減り方にして、止まる位置を読みやすくする</summary>
        public const float RollDecel = 1.6f;

        /// <summary>砂地の摩擦は何倍か（止まりやすい）</summary>
        public const float SandDecelScale = 3.2f;

        public const float Gravity = 9.8f;

        /// <summary>ジャンプ台で跳ぶのに要る速さ（止まりかけの球が台の上で跳ね続けないように）</summary>
        public const float JumpMinSpeed = 1.5f;

        /// <summary>カップ: 球の中心がこの距離に入り、速さが CupMaxSpeed より遅ければ入る</summary>
        public const float CupRadius = 0.3f;
        public const float CupMaxSpeed = 3.2f;

        /// <summary>速すぎてカップの上を通るとき、縁に引かれて曲がる強さ（入らずに少しそれる）</summary>
        public const float LipPull = 9f;

        /// <summary>壁の跳ね返りの強さ（壁に向かう速さを何倍で返すか）。壁の種類で変える</summary>
        public const float WallBounce = 0.72f;
        public const float SoftWallBounce = 0.45f;
        public const float BumperBounce = 1.05f;

        /// <summary>壁の高さ。浮いた球はこれより高ければ越える</summary>
        public const float TallWallHeight = 0.6f;
        public const float LowWallHeight = 0.2f;

        /// <summary>球どうしの跳ね返り（1 で速さを失わない）</summary>
        public const float BallRestitution = 0.92f;

        /// <summary>音や揺れのイベントを出す、ぶつかる速さの下限（壁沿いに転がる間ずっと鳴らないように）</summary>
        public const float HitEventMinSpeed = 0.4f;

        /// <summary>1打で転がる時間の上限。坂の作りの誤りなどで止まらなくても、打順が進むように</summary>
        public const float MaxRollSeconds = 25f;

        // ---- ギミック（フェーズ 6） ----

        /// <summary>氷の床の摩擦は何倍か（よく滑る）</summary>
        public const float IceFloorDecelScale = 0.35f;

        /// <summary>ワープの泡の入口の大きさ（球の中心がこの距離に入ると出口へ飛ぶ）</summary>
        public const float WarpRadius = 0.42f;

        /// <summary>
        /// 動く物が通る範囲の中で止まった球を、範囲の外へよける距離（球の縁から範囲の縁まで）。
        /// 狙っている間は球を動かさないので、動く物が止まった球をすり抜けないよう、止まる所を範囲の外にする
        /// </summary>
        public const float SweepClearance = 0.08f;

        /// <summary>ガイド線の長さの上限と、跳ね返った後に見せる短い線の長さ</summary>
        public const float GuideMaxLength = 9f;
        public const float GuideBounceLength = 1.6f;

        /// <summary>ガイド線を探る刻みと、当たった所を挟み込んで詰める回数</summary>
        public const float GuideMarchStep = 0.05f;
        public const int GuideRefineIterations = 10;
    }
}
