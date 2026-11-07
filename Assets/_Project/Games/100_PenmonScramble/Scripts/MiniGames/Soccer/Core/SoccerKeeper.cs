using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    public enum SoccerKeeperState
    {
        /// <summary>ゴールの口の前を横に動いて、ボールの来る所へ寄る</summary>
        Guard,
        /// <summary>シュートへ飛びついている（DiveTarget へ向かう）</summary>
        Dive,
        /// <summary>飛びついた後で倒れていて、起き上がるまで動けない</summary>
        Down,
        /// <summary>ボールを持っている。少し持ってから味方の足元へ投げる</summary>
        Hold,
    }

    /// <summary>
    /// おもちゃのキーパー（ペンモンではない。両チームに1体ずつ、強さは同じ）。操作しない・スライディングされない。
    /// ゴールの口の少し前の線（KeeperLineX）の上だけを横に動く。書き換えるのは SoccerWorld だけ
    /// </summary>
    public sealed class SoccerKeeper
    {
        public SoccerKeeper(int team, float lineX, float outward)
        {
            Team = team;
            LineX = lineX;
            Facing = new Vec2(-outward, 0f);
            Position = new Vec2(lineX, 0f);
        }

        /// <summary>守るゴールのチーム（＝このキーパーのチーム）</summary>
        public int Team { get; }

        /// <summary>動ける線の X（ゴールの線から少しピッチの中）</summary>
        public float LineX { get; }

        /// <summary>ピッチの中を向く向き（長さ 1）</summary>
        public Vec2 Facing { get; }

        public Vec2 Position { get; internal set; }

        /// <summary>横に動く速さ（Y だけ）。見た目の歩き・飛びつきの向きに使う</summary>
        public float Speed { get; internal set; }

        public SoccerKeeperState State { get; internal set; }

        /// <summary>今の状態になってからの秒（飛びつき・倒れ・持つの長さを測る）</summary>
        public float StateSeconds { get; internal set; }

        /// <summary>飛びつく先の Y（Dive の間だけ意味がある）</summary>
        public float DiveTargetY { get; internal set; }

        /// <summary>飛びついた向き（−1: Y の負の側・+1: 正の側）。見た目の倒れる向きに使う</summary>
        public float DiveSide { get; internal set; }

        /// <summary>今のボールの道筋を止めると決めたか（SoccerKeeperSave で決める。道筋が変わったら決め直す）</summary>
        public bool WillSave { get; internal set; }

        /// <summary>今のボールの道筋を、もう決めたか</summary>
        internal bool Decided { get; set; }

        /// <summary>投げた直後は、このキーパーはボールを取らない（投げた瞬間にまた取らないように）</summary>
        internal float CatchLockSeconds { get; set; }

        internal void Enter(SoccerKeeperState state)
        {
            State = state;
            StateSeconds = 0f;
        }

        internal void Reset()
        {
            Position = new Vec2(LineX, 0f);
            Speed = 0f;
            Decided = false;
            WillSave = false;
            CatchLockSeconds = 0f;
            Enter(SoccerKeeperState.Guard);
        }
    }
}
