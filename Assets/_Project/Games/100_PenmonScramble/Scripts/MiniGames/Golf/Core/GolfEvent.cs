using PenmonScramble.Core;

namespace PenmonScramble.Golf.Core
{
    public enum GolfEventKind
    {
        /// <summary>打った（Strength はゲージの強さ 0〜1）</summary>
        Shot,
        /// <summary>壁に当たった（Strength は壁に向かう速さ）</summary>
        WallHit,
        /// <summary>球どうしが当たった（Other は当たった相手。Strength はぶつかる速さ）</summary>
        BallHit,
        /// <summary>ジャンプ台で跳んだ</summary>
        Jump,
        /// <summary>浮いた球が地面に着いた</summary>
        Land,
        /// <summary>水切りで跳ねた（Strength は池の上なら 1）</summary>
        Skip,
        /// <summary>特殊ショットを打った（Other は GolfSpecialKind、Strength は★）</summary>
        Special,
        /// <summary>雷の特殊ショットで加速した</summary>
        Boost,
        /// <summary>カップの縁で曲がって入らなかった</summary>
        LipOut,
        /// <summary>カップに入った</summary>
        CupIn,
        /// <summary>池・場外に落ちて、打つ前の位置へ戻った（打った本人なら 1 打足す）</summary>
        Fell,
        /// <summary>打順が回ってきた</summary>
        TurnStart,
        /// <summary>打数の上限まで打っても入らず、終わった</summary>
        GaveUp,
        /// <summary>全員が終わった</summary>
        HoleFinished,
    }

    /// <summary>ホールで起きたこと。表示・音・通信へはこれで渡す（Core は見せ方を知らない）</summary>
    public readonly struct GolfEvent
    {
        public readonly GolfEventKind Kind;
        public readonly int Player;
        public readonly int Other;
        public readonly Vec2 Position;
        public readonly float Strength;

        public GolfEvent(GolfEventKind kind, int player, Vec2 position, float strength = 0f, int other = -1)
        {
            Kind = kind;
            Player = player;
            Other = other;
            Position = position;
            Strength = strength;
        }

        public override string ToString() => $"{Kind} p{Player} {Position} {Strength:0.##}";
    }
}
