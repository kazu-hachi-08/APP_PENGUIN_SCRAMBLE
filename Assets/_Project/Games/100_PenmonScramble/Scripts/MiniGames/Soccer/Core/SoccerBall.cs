using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// ボール。地面の上の2次元の位置と速さに、浮いている間だけの高さを足して持つ
    /// （全部を本当の3Dにせず、純C#で確かめやすく、オンラインで全員の見え方をそろえやすくするため）。
    /// 書き換えるのは SoccerWorld だけ
    /// </summary>
    public sealed class SoccerBall
    {
        public const int NoPlayer = -1;

        public Vec2 Position { get; internal set; }

        public Vec2 Velocity { get; internal set; }

        /// <summary>ボールの下の端の高さ（地面なら 0）</summary>
        public float Height { get; internal set; }

        /// <summary>上向きの速さ（浮いている間だけ）</summary>
        public float VerticalSpeed { get; internal set; }

        /// <summary>持っている子の番号。誰も持っていなければ NoPlayer</summary>
        public int Holder { get; internal set; } = NoPlayer;

        /// <summary>最後に触った子（ゴールを決めた子を出すのに使う）</summary>
        public int LastToucher { get; internal set; } = NoPlayer;

        /// <summary>蹴った直後で、まだ持てない子と、その残りの秒</summary>
        public int LockedPlayer { get; internal set; } = NoPlayer;

        public float LockSeconds { get; internal set; }

        /// <summary>誰も触っていない秒（SoccerBalance.IdleNudgeSeconds で自分で転がる）</summary>
        public float IdleSeconds { get; internal set; }

        public bool IsHeld => Holder != NoPlayer;

        public bool IsAirborne => Height > 0f || VerticalSpeed > 0f;

        internal void Place(Vec2 position)
        {
            Position = position;
            Velocity = Vec2.Zero;
            Height = 0f;
            VerticalSpeed = 0f;
            Holder = NoPlayer;
            LockedPlayer = NoPlayer;
            LockSeconds = 0f;
            IdleSeconds = 0f;
        }
    }
}
