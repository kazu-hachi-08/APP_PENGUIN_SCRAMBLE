using PenmonScramble.Core;

namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// 1人の球。位置は地面の上（GolfHoleDef と同じ座標）、高さは浮いているときだけ 0 より大きい
    /// </summary>
    public sealed class GolfBall
    {
        public int Player { get; }
        public Vec2 Position { get; internal set; }
        public Vec2 Velocity { get; internal set; }
        public float Height { get; internal set; }
        public float VerticalSpeed { get; internal set; }

        /// <summary>カップに入った（もう当たらないし動かない）</summary>
        public bool InCup { get; internal set; }

        /// <summary>コースの上にある（打数の上限で終わった人・抜けた人の球は外す）</summary>
        public bool Active { get; internal set; } = true;

        /// <summary>池・場外に落ちたときに戻す位置（打つ前の位置）</summary>
        public Vec2 ReturnPoint { get; internal set; }

        // ---- 特殊ショットの効き目（打った1打の間だけ。止まる・落ちると消える） ----

        /// <summary>いま効いている特殊ショット（打った人の球だけ）。無ければ None</summary>
        public GolfSpecialKind Special { get; internal set; }

        /// <summary>氷: 凍った道があと何 m 続くか。0 より大きい間は止まりにくく、池・砂地を渡れる</summary>
        public float IceLeft { get; internal set; }

        /// <summary>水: あと何回跳ねるか</summary>
        public int SkipsLeft { get; internal set; }

        /// <summary>水: 次に跳ねるときに上へ跳ぶ速さ</summary>
        internal float SkipSpeed { get; set; }

        /// <summary>風: 曲がりながら進む距離の残り（m）</summary>
        public float CurveLeft { get; internal set; }

        /// <summary>風: 進んだ 1m あたりに曲がる角度（ラジアン。正で左回り）</summary>
        internal float Curvature { get; set; }

        /// <summary>雷: まだ加速できる（打った後にもう一度押すのを待っている）</summary>
        public bool BoostReady { get; internal set; }

        /// <summary>雷: 加速の★（強さと、変えられる角度）</summary>
        internal int BoostStars { get; set; }

        public GolfBall(int player, Vec2 position)
        {
            Player = player;
            Position = position;
            ReturnPoint = position;
        }

        public bool IsAirborne => Height > 0f || VerticalSpeed > 0f;

        /// <summary>コースの上で、ほかの球や壁と当たる</summary>
        public bool IsInPlay => Active && !InCup;

        public bool IsMoving => IsInPlay && (!Velocity.IsNearlyZero || IsAirborne);

        /// <summary>置き直す（ティー・落ちたときの戻し・テスト）。止めた状態にする</summary>
        public void Place(Vec2 position)
        {
            Position = position;
            Velocity = Vec2.Zero;
            Height = 0f;
            VerticalSpeed = 0f;
            ClearSpecial();
        }

        /// <summary>打ち出す（地面を転がり始める）</summary>
        public void Launch(Vec2 velocity)
        {
            Velocity = velocity.ClampLength(GolfBalance.MaxBallSpeed);
        }

        internal void Stop()
        {
            Velocity = Vec2.Zero;
            VerticalSpeed = 0f;
            Height = 0f;
            ClearSpecial();
        }

        /// <summary>打ち出した直後に特殊ショットの効き目を載せる</summary>
        internal void ApplySpecial(GolfSpecial special)
        {
            ClearSpecial();
            Special = special.Kind;
            switch (special.Kind)
            {
                case GolfSpecialKind.IceRoad:
                    IceLeft = GolfSpecialBalance.IceLength(special.Stars);
                    break;
                case GolfSpecialKind.SkipBounce:
                    // 打ち出しが1回目の跳び。そのあと★の回数だけ跳ねる
                    VerticalSpeed = GolfSpecialBalance.SkipLaunch;
                    SkipSpeed = GolfSpecialBalance.SkipLaunch * GolfSpecialBalance.SkipVerticalKeep;
                    SkipsLeft = GolfSpecialBalance.SkipBounces(special.Stars);
                    break;
                case GolfSpecialKind.WindCurve:
                    CurveLeft = GolfSpecialBalance.CurveLength;
                    // 右（+1）は時計回り＝角度が減る向き
                    Curvature = -special.Curve * GolfSpecialBalance.Curvature(special.Stars);
                    break;
                case GolfSpecialKind.ThunderBoost:
                    BoostReady = true;
                    BoostStars = special.Stars;
                    break;
            }
        }

        internal void ClearSpecial()
        {
            Special = GolfSpecialKind.None;
            IceLeft = 0f;
            SkipsLeft = 0;
            SkipSpeed = 0f;
            CurveLeft = 0f;
            Curvature = 0f;
            BoostReady = false;
            BoostStars = 0;
        }
    }
}
