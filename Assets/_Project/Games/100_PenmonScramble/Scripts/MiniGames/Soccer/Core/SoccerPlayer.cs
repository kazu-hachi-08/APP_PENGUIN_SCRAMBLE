using System;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    public enum SoccerPlayerState
    {
        /// <summary>歩ける・ボールを持てる</summary>
        Normal,
        /// <summary>スライディング中（向きを変えられない。持っている相手に届けば奪う）</summary>
        Slide,
        /// <summary>スライディングでボールを奪われて転んでいる（動けない・持てない）</summary>
        Down,
        /// <summary>スライディングを外して起き上がるところ（動けない・持てない）</summary>
        Recover,
        /// <summary>氷の必殺シュートに当たって凍っている（動けない・持てない）</summary>
        Frozen,
    }

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

        /// <summary>属性と★（必殺シュートの種類と強さだけに効く。歩く速さは種類で変えない）</summary>
        public Element Element { get; internal set; } = Element.Ice;

        public int Stars { get; internal set; } = 1;

        /// <summary>凍っている残りの秒（Frozen の間だけ意味がある）</summary>
        public float FrozenSeconds { get; internal set; }

        public Vec2 Position { get; internal set; }

        public Vec2 Velocity { get; internal set; }

        /// <summary>向いている向き（長さ 1）。止まっても最後に歩いた向きのまま</summary>
        public Vec2 Facing { get; internal set; }

        /// <summary>シュートをためている秒。ためていなければ 0</summary>
        public float ChargeSeconds { get; internal set; }

        public bool IsCharging { get; internal set; }

        /// <summary>ためゲージ（0〜1）。表示用</summary>
        public float Charge01 => Math.Min(ChargeSeconds / SoccerBalance.ShotChargeSeconds, 1f);

        public SoccerPlayerState State { get; internal set; }

        /// <summary>今の状態になってからの秒</summary>
        public float StateSeconds { get; internal set; }

        /// <summary>次にスライディングできるまでの秒</summary>
        public float SlideCooldown { get; internal set; }

        /// <summary>ダッシュの残りの秒（0 ならダッシュしていない）と、次にダッシュできるまでの秒</summary>
        public float DashSeconds { get; internal set; }

        public float DashCooldown { get; internal set; }

        /// <summary>歩ける・ボールを持てる・技を出せる</summary>
        public bool CanAct => State == SoccerPlayerState.Normal;

        /// <summary>転んでいる・起き上がるところ・凍っている（ボールを持てない・パスを受けない）</summary>
        public bool IsDown => State == SoccerPlayerState.Down || State == SoccerPlayerState.Recover || State == SoccerPlayerState.Frozen;

        public bool IsFrozen => State == SoccerPlayerState.Frozen;

        public bool IsDashing => DashSeconds > 0f;

        // 前の歩でボタンを押していたか（押した瞬間を SoccerWorld が見分ける）
        internal bool WasShooting { get; set; }

        internal bool WasPassing { get; set; }

        internal bool WasSpecial { get; set; }

        internal void ResetCharge()
        {
            ChargeSeconds = 0f;
            IsCharging = false;
        }

        internal void Enter(SoccerPlayerState state)
        {
            State = state;
            StateSeconds = 0f;
        }

        /// <summary>キックオフで並び直すとき: 転んだ・すべった・ためた・待ちを全部消す</summary>
        internal void ResetActions()
        {
            ResetCharge();
            Enter(SoccerPlayerState.Normal);
            SlideCooldown = 0f;
            DashSeconds = 0f;
            DashCooldown = 0f;
            FrozenSeconds = 0f;
        }
    }
}
