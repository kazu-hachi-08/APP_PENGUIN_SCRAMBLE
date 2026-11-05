using System;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>フィールドの子1体（ペンモン）。書き換えるのは SoccerWorld だけ</summary>
    public sealed class SoccerPlayer
    {
        public SoccerPlayer(int index, int team)
        {
            Index = index;
            Team = team;
            Facing = new Vec2(SoccerPitchDef.AttackDirection(team), 0f);
        }

        /// <summary>SoccerWorld.Players の中の番号（入力・イベントはこの番号で指す）</summary>
        public int Index { get; }

        public int Team { get; }

        public Vec2 Position { get; internal set; }

        public Vec2 Velocity { get; internal set; }

        /// <summary>向いている向き（長さ 1）。止まっても最後に歩いた向きのまま</summary>
        public Vec2 Facing { get; internal set; }

        /// <summary>シュートをためている秒。ためていなければ 0</summary>
        public float ChargeSeconds { get; internal set; }

        public bool IsCharging { get; internal set; }

        /// <summary>ためゲージ（0〜1）。表示用</summary>
        public float Charge01 => Math.Min(ChargeSeconds / SoccerBalance.ShotChargeSeconds, 1f);

        internal void ResetCharge()
        {
            ChargeSeconds = 0f;
            IsCharging = false;
        }
    }
}
