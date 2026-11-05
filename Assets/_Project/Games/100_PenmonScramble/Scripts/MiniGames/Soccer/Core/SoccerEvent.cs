using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    public enum SoccerEventKind
    {
        /// <summary>キックオフの並びに戻した（Team は蹴る側）</summary>
        Kickoff,
        /// <summary>ボールを持った</summary>
        Possess,
        /// <summary>蹴った（Strength はため 0〜1。浮いた球なら Lofted。Position は蹴り出した所）</summary>
        Kick,
        /// <summary>壁（板・バー）に当たった（Strength は壁に向かう速さ）</summary>
        WallHit,
        /// <summary>ゴールの網に当たった（Strength は網に向かう速さ）</summary>
        NetHit,
        /// <summary>浮いた球が地面に着いた（Strength は落ちる速さ）</summary>
        Land,
        /// <summary>点が入った（Team は点を取った側、Player は最後に触った子）</summary>
        Goal,
        /// <summary>誰も触らないので、ボールが一番近い子の方へ自分で転がった（Player はその子）</summary>
        Nudge,
        /// <summary>キーパーがシュートへ飛びついた（Team はキーパー、Position は飛びつく先、Strength は向き −1 / +1、Lofted は止めると決めたか）</summary>
        KeeperDive,
        /// <summary>キーパーがボールを取った（Team はキーパー、Player は持っていたのを取られた子。離れた球なら NoPlayer）</summary>
        KeeperCatch,
        /// <summary>キーパーが止められなかった球が体に当たってはじいた（Strength は当たった速さ）</summary>
        KeeperParry,
        /// <summary>キーパーがボールを投げた（Player は受け取る味方。いなければ NoPlayer）</summary>
        KeeperThrow,
    }

    /// <summary>試合で起きたこと。表示・音・通信へはこれで渡す（Core は見せ方を知らない）</summary>
    public readonly struct SoccerEvent
    {
        public readonly SoccerEventKind Kind;
        public readonly int Player;
        public readonly int Team;
        public readonly Vec2 Position;
        public readonly float Strength;
        public readonly bool Lofted;

        public SoccerEvent(SoccerEventKind kind, int player, int team, Vec2 position, float strength = 0f, bool lofted = false)
        {
            Kind = kind;
            Player = player;
            Team = team;
            Position = position;
            Strength = strength;
            Lofted = lofted;
        }

        public override string ToString() => $"{Kind} p{Player} t{Team} {Position} {Strength:0.##}";
    }
}
