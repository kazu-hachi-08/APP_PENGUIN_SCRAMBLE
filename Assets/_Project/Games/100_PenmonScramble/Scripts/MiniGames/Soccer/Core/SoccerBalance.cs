namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// ボールとペンモンの動きの調整値（長さはマス＝m・時間は秒）。ホストが計算して全員に配るので、Unity の物理は使わず、ここの数で決める。
    /// 「（案）」の値は試遊で決め直す
    /// </summary>
    public static partial class SoccerBalance
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

        // ---- パス ----

        /// <summary>向いている向き（スティック）からこの角度（度）までにいる味方へパスする（案）</summary>
        public const float PassConeDegrees = 60f;

        /// <summary>受ける子の足元に着いたときに残る速さ（止まりかけの球を取りに戻らなくてよいように）</summary>
        public const float PassArriveSpeed = 3f;

        /// <summary>パスの速さの範囲（近くへも弱すぎず、遠くへも速すぎて取りこぼさない）</summary>
        public const float PassMinSpeed = 6f;
        public const float PassMaxSpeed = 15f;

        /// <summary>この距離より近い味方へはパスしない（足元どうしで渡しても意味がない）</summary>
        public const float PassMinDistance = 1.2f;

        /// <summary>前に味方がいないときに、向いている向きへ弱く蹴り出す速さ</summary>
        public const float PassFallbackSpeed = 7f;

        // ---- スライディング ----

        /// <summary>すべる速さと長さ（約 3.5 マス。案）。すべる間は向きを変えられない</summary>
        public const float SlideSpeed = 10f;
        public const float SlideSeconds = 0.35f;

        /// <summary>持っている相手のボールにこの距離（子の中心からボールの中心）まで届けば奪う</summary>
        public const float SlideStealReach = PlayerRadius + BallRadius + 0.3f;

        /// <summary>奪われた子が転んで動けない秒（案）</summary>
        public const float TackledDownSeconds = 1f;

        /// <summary>外したときに自分が起き上がれない秒（むやみに出させない。案）</summary>
        public const float SlideMissSeconds = 0.5f;

        /// <summary>すべり始めてから次にすべれるまでの秒（案）</summary>
        public const float SlideCooldownSeconds = 1.2f;

        /// <summary>転んでいる・起き上がる間に、歩く勢いが消える速さ（毎秒）</summary>
        public const float DownBrake = 30f;

        // ---- ダッシュ（持っていないときの技2）----

        /// <summary>ダッシュの長さ・速さの倍率・次に出せるまでの秒（転がるボールを追いかけるため。案）</summary>
        public const float DashSeconds = 0.5f;
        public const float DashSpeedScale = 1.45f;
        public const float DashCooldownSeconds = 3f;

        // ---- 動かす子の切り替え ----

        /// <summary>自動で替わってから、次に自動で替わるまでの秒（替わり続けてガタつかないように。案）</summary>
        public const float SwitchHoldSeconds = 0.8f;

        /// <summary>今の子よりこの秒以上早くボールに着ける子にだけ自動で替わる（同じくらいの所で行ったり来たりしないように）</summary>
        public const float SwitchMarginSeconds = 0.5f;

        /// <summary>転んでいる子は、ボールに着く秒にこれを足して比べる（替わってもすぐ動けないので後回し）</summary>
        public const float SwitchDownPenaltySeconds = 0.8f;

        /// <summary>自分が出したパスが転がっている扱いにする、いちばん長い秒（誰も持たないまま止まったら、また替われるように）</summary>
        public const float SwitchPassFlightSeconds = 2f;

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

        // ---- キーパー（両チーム同じ強さ。スタジアムでも変えない）----

        /// <summary>体の当たりの半径。ペンモンはキーパーを通り抜けない</summary>
        public const float KeeperRadius = 0.45f;

        /// <summary>動ける線の、ゴールの線からピッチの中への距離</summary>
        public const float KeeperLineOffset = 0.7f;

        /// <summary>動ける幅は、ゴールの口の端からこれだけ内側まで</summary>
        public const float KeeperGuardInset = 0.4f;

        /// <summary>構えて横に動く速さ</summary>
        public const float KeeperGuardSpeed = 3.5f;

        /// <summary>シュートが来ていないとき、ボールの Y の何倍の所に立つか（ゴールの真ん中とボールの間に入る）</summary>
        public const float KeeperFollowRatio = 0.45f;

        /// <summary>シュートに気づいて動き出すまでの秒（近くからの球ほど間に合わない）</summary>
        public const float KeeperReactSeconds = 0.2f;

        /// <summary>飛びつく速さと、飛びついて動ける距離の上限</summary>
        public const float KeeperDiveSpeed = 4f;
        public const float KeeperDiveReach = 1.7f;

        /// <summary>立ったまま腕で届く距離（体の中心から）</summary>
        public const float KeeperBodyReach = 0.55f;

        /// <summary>ボールを取れる距離（体の中心からボールの縁まで）と高さ（バーより少し上まで手が届く）</summary>
        public const float KeeperCatchRadius = 0.55f;
        public const float KeeperReachHeight = 1.7f;

        /// <summary>止めるか決めるのは、ボールがキーパーの線に着くこの秒前から（遠くの球は寄るだけで、近づいてから決める）</summary>
        public const float KeeperDiveLeadSeconds = 0.6f;

        /// <summary>止める割合: 余裕（動ける距離 − 要る距離）がこの長さ変わると 0.5 から 1（または 0）まで動く（案）</summary>
        public const float KeeperSaveSpread = 1f;

        /// <summary>バーの高さの球は、止める割合をこれだけ下げる（浮いた球を蹴る意味を持たせる）</summary>
        public const float KeeperHighBallPenalty = 0.3f;

        /// <summary>止める割合の下限と上限（どんな球にも少しだけ運の余地を残す）</summary>
        public const float KeeperMinSave = 0.03f;
        public const float KeeperMaxSave = 0.97f;

        /// <summary>止めないと決めたときに、ボールの縁から空けるすき間（指先をかすめて入る）</summary>
        public const float KeeperMissGap = 0.12f;

        /// <summary>これより遅い球は、止めないと決めた後でも取る（ゆるい転がりがキーパーをすり抜けないように）</summary>
        public const float KeeperEasyCatchSpeed = 5f;

        /// <summary>止められなかった球が体に当たったとき、勢いを何倍で返すか（はじく）</summary>
        public const float KeeperParryBounce = 0.5f;

        /// <summary>飛びついてから倒れているまでの長さの上限と、倒れてから起き上がるまでの秒</summary>
        public const float KeeperDiveMaxSeconds = 0.6f;
        public const float KeeperDownSeconds = 0.7f;

        /// <summary>取ってから投げるまでの秒・持っているボールの位置（体の前・胸の高さ）</summary>
        public const float KeeperHoldSeconds = 0.9f;
        public const float KeeperHoldDistance = 0.5f;
        public const float KeeperHoldHeight = 0.45f;

        /// <summary>投げた後、このキーパーがボールを取らない秒</summary>
        public const float KeeperThrowLockSeconds = 0.6f;

        /// <summary>味方の足元へ投げる速さ（着いたときにこれだけ残る）と、速さの範囲</summary>
        public const float KeeperThrowArriveSpeed = 1.5f;
        public const float KeeperThrowMinSpeed = 5f;
        public const float KeeperThrowMaxSpeed = 12f;

        /// <summary>味方がいないときに投げる距離（前へ、相手のいない側へ少し寄せる）</summary>
        public const float KeeperThrowFallbackDistance = 7f;
        public const float KeeperThrowFallbackSide = 3f;
    }
}
