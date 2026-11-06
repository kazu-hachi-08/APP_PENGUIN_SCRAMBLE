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
        /// <summary>パスした（Player は蹴った子、Other は受ける味方。前に味方がいなければ NoPlayer。Strength は蹴り出した速さ）</summary>
        Pass,
        /// <summary>スライディングを始めた（Player はすべる子）</summary>
        Slide,
        /// <summary>スライディングでボールを奪った（Player は奪った子、Other は転んだ子）</summary>
        Tackle,
        /// <summary>スライディングを外して転んだ（Player はすべった子）</summary>
        SlideMiss,
        /// <summary>ダッシュした（Player は走る子）</summary>
        Dash,
        /// <summary>必殺シュートのカットインが始まった（Player は蹴る子、Strength は★。蹴るのはカットインの後）</summary>
        SpecialCutIn,
        /// <summary>必殺シュートを蹴った（Player は蹴った子、Strength は★。同じ歩に Kick も出る）</summary>
        SpecialKick,
        /// <summary>氷の必殺シュートで凍った（Player は凍った子、Strength は凍る秒）</summary>
        Freeze,

        // ---- 試合の流れ（SoccerMatch・SoccerShootout）----

        /// <summary>キックオフの合図が終わってボールに触れるようになった（Team は蹴る側）</summary>
        PlayStart,
        /// <summary>残りの秒が1つ減った（Strength は残りの秒。残り 10 秒から）</summary>
        Countdown,
        /// <summary>時間切れ</summary>
        TimeUp,
        /// <summary>同点で PK戦が始まった</summary>
        ShootoutStart,
        /// <summary>PK戦の1本の構え（Player は蹴る子、Team は蹴る側、Strength は何本目（1 から））</summary>
        PenaltyReady,
        /// <summary>PK戦で蹴った（Player は蹴る子、Strength は強さ 0〜1、Lofted は高い所を狙った）</summary>
        PenaltyKick,
        /// <summary>PK戦の1本が決まった・止められた・外れた（Player は蹴った子、Team は蹴った側）</summary>
        PenaltyGoal,
        PenaltySave,
        PenaltyMiss,
        /// <summary>試合が終わった（Team は勝った側。PK戦で決まったら Lofted）</summary>
        MatchEnd,
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

        /// <summary>もう1人の子（パスを受ける味方・奪われた子）。いなければ SoccerBall.NoPlayer</summary>
        public readonly int Other;

        public SoccerEvent(SoccerEventKind kind, int player, int team, Vec2 position, float strength = 0f, bool lofted = false, int other = SoccerBall.NoPlayer)
        {
            Other = other;
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
