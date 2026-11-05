namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// ボールとペンモンの動きの調整値（長さはマス＝m・時間は秒）。ホストが計算して全員に配るので、Unity の物理は使わず、ここの数で決める。
    /// 「（案）」の値はフェーズ 2〜4 の試遊で決め直す
    /// </summary>
    public static class SoccerBalance
    {
        // ---- ペンモン ----

        /// <summary>歩く速さ。種類の速さは効かせない（オンラインで育成の差で勝負が決まらないように）</summary>
        public const float RunSpeed = 6f;

        /// <summary>歩き出し・止まりの速さの変わり方（毎秒）。すぐ向きを変えられるくらい大きく</summary>
        public const float RunAccel = 40f;

        /// <summary>ボールを持っている子の歩く速さは何倍か（持っていない子が追いつけるように）</summary>
        public const float CarrySpeedScale = 0.85f;

        /// <summary>体の当たりの半径</summary>
        public const float PlayerRadius = 0.4f;

        /// <summary>持っているボールの中心と子の中心の距離（足元の少し前）</summary>
        public const float DribbleDistance = 0.62f;

        /// <summary>この距離に入った離れたボールを持つ（体の半径＋ボールの半径＋少し）</summary>
        public const float PickupReach = 0.72f;

        /// <summary>これより高い所のボールは持てない（頭の高さ）</summary>
        public const float PickupMaxHeight = 0.9f;

        /// <summary>蹴った子は、離れたボールをこの間だけ持てない（蹴った瞬間にまた拾わないように）</summary>
        public const float RepickLockSeconds = 0.4f;

        // ---- シュート ----

        /// <summary>ためゲージが満タンになるまでの時間（案）</summary>
        public const float ShotChargeSeconds = 0.8f;

        /// <summary>ため 0 と満タンのときの蹴り出しの速さ（案）</summary>
        public const float MinShotSpeed = 7f;
        public const float MaxShotSpeed = 19f;

        /// <summary>満タンで蹴ったときの浮く速さ。いちばん高い所が約 1.3 マス（バーより少し低い）になる（案）</summary>
        public const float LoftUpSpeed = 5f;

        /// <summary>ためがこれ以上なら浮いた球（満タン。少しの誤差でも浮くように 1 より少し下）</summary>
        public const float LoftCharge = 0.98f;

        /// <summary>シュートの向きを相手のゴールの口へ寄せる最大の角度（度。案）</summary>
        public const float AimAssistDegrees = 15f;

        // ---- ボール ----

        public const float BallRadius = 0.22f;

        /// <summary>転がりの摩擦（毎秒これだけ遅くなる）。速さに比べない一定の減り方にして、止まる位置を読みやすくする</summary>
        public const float RollDecel = 6f;

        /// <summary>これより遅い転がりは止める（いつまでも少しずつ動かないように）</summary>
        public const float StopSpeed = 0.05f;

        public const float Gravity = 9.8f;

        /// <summary>浮いた球が地面で跳ねるとき、落ちる速さを何倍で返すか</summary>
        public const float GroundBounce = 0.45f;

        /// <summary>これより弱い跳ね返りは跳ねずに転がる</summary>
        public const float MinBounceSpeed = 1f;

        /// <summary>壁（ボード）で跳ね返るとき、壁に向かう速さを何倍で返すか（案）</summary>
        public const float WallBounce = 0.7f;

        /// <summary>ゴールの網は勢いを吸う</summary>
        public const float NetBounce = 0.2f;

        /// <summary>
        /// ボールを細かく分けて進めるときの1回の長さの上限。半径より十分短くして、速い球でも壁をすり抜けないようにする
        /// </summary>
        public const float BallSubstepLength = BallRadius * 0.5f;

        /// <summary>音や揺れのイベントを出す、壁にぶつかる速さの下限（壁沿いに転がる間ずっと鳴らないように）</summary>
        public const float HitEventMinSpeed = 1f;

        /// <summary>
        /// 誰も触らないままこの時間がたったら、ボールが自分で一番近い子の方へ少し転がる
        /// （壁ぎわで止まって誰も取りに行かない詰まりを防ぐ。案）
        /// </summary>
        public const float IdleNudgeSeconds = 8f;

        public const float IdleNudgeSpeed = 4f;

        /// <summary>地面の種類ごとの転がりの摩擦の倍率。今は芝だけ（氷の床などの仕掛けはフェーズ 6 でここに分けて足す）</summary>
        public static float RollDecelScale(SoccerGround ground) => 1f;
    }
}
