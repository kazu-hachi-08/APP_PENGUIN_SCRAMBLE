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
        }
    }
}
