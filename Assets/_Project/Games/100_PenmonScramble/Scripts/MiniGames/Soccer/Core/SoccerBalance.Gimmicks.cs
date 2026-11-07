namespace PenmonScramble.Soccer.Core
{
    /// <summary>スタジアムの仕掛け（氷の床・水流・風・バンパー・間欠泉）の調整値。置き場所と時間の長さは SoccerPitchTable</summary>
    public static partial class SoccerBalance
    {
        // ---- 氷の床（氷山）----

        /// <summary>氷の上の転がりの摩擦の倍率（よく転がる）</summary>
        public const float IceRollDecelScale = 0.3f;

        /// <summary>氷の上の子の歩き出し・止まりの速さの倍率（すべって止まれない。速さは変えない）</summary>
        public const float IceRunAccelScale = 0.18f;

        /// <summary>氷の上で転んだ子の勢いが消える速さの倍率（転んだまますべっていく）</summary>
        public const float IceDownBrakeScale = 0.25f;

        /// <summary>地面の種類ごとの転がりの摩擦の倍率</summary>
        public static float RollDecelScale(SoccerGround ground) => ground == SoccerGround.Ice ? IceRollDecelScale : 1f;

        /// <summary>地面の種類ごとの歩き出し・止まりの速さの倍率</summary>
        public static float RunAccelScale(SoccerGround ground) => ground == SoccerGround.Ice ? IceRunAccelScale : 1f;

        public static float DownBrakeScale(SoccerGround ground) => ground == SoccerGround.Ice ? IceDownBrakeScale : 1f;

        // ---- 押し流す帯（水流・風）----

        /// <summary>水流が押すのは、この高さより低いボールだけ（水の上を転がる球）</summary>
        public const float CurrentMaxHeight = 0.3f;

        /// <summary>風は浮いた球をこれだけ強く押す（高く蹴るほど流される）</summary>
        public const float WindAirScale = 1.6f;

        /// <summary>
        /// 帯の中の子が流される速さ（歩く速さに足す。勢いには残さない）。ボールを持って運ぶ間も仕掛けが効くように
        /// （CPU 同士の試合の確認で、ボールは持たれている時間が長く、離れた球だけを押しても帯がほとんど試合に絡まなかったため）
        /// </summary>
        public const float CurrentPlayerDrift = 1.5f;
        public const float WindPlayerDrift = 1f;

        public static float PlayerDrift(SoccerFlowKind kind) => kind == SoccerFlowKind.Current ? CurrentPlayerDrift : WindPlayerDrift;

        // ---- バンパー・柱（嵐の崖）----

        /// <summary>バンパーで跳ね返るとき、壁に向かう速さを何倍で返すか（板は WallBounce）</summary>
        public const float BumperBounce = 1.25f;

        /// <summary>バンパーが返す速さの下限（ゆっくり当たっても、はっきり弾き返す）</summary>
        public const float BumperMinSpeed = 7f;

        /// <summary>バンパーで弾かれた後の速さの上限（いちばん強いシュートと同じ）</summary>
        public const float BumperMaxSpeed = MaxShotSpeed;

        // ---- 間欠泉（火山の入り江）----

        /// <summary>跳ね上げる上向きの速さ（いちばん高い所が約 2.5 マス。頭より高く上がって、しばらく誰も取れない）</summary>
        public const float GeyserUpSpeed = 7f;

        /// <summary>跳ね上げるとき、口の真ん中から外へ押し出す速さ（真上に上がって同じ口へ落ち続けないように）</summary>
        public const float GeyserPushSpeed = 2.5f;

        /// <summary>跳ね上げたボールの横の速さを何倍に残すか</summary>
        public const float GeyserKeepSpeed = 0.5f;

        /// <summary>跳ね上げた直後に誰も持てない秒（上がりきる前の低い所で足元に取られないように）</summary>
        public const float GeyserNoPickupSeconds = 0.3f;
    }
}
