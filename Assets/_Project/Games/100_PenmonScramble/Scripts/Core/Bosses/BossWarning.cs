using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 大ボスの攻撃の前に地面へ出す赤い範囲。From〜To の線から HalfWidth 以内（端は丸い）。
    /// From と To が同じなら円になる。円・帯・突進の道を、この1つの形で表す
    /// </summary>
    public sealed class BossWarning
    {
        internal BossWarning(int id, Vec2 from, Vec2 to, float halfWidth, float seconds)
        {
            Id = id;
            From = from;
            To = to;
            HalfWidth = halfWidth;
            TotalSeconds = seconds;
            Remaining = seconds;
        }

        public int Id { get; }
        public Vec2 From { get; }
        public Vec2 To { get; }
        public float HalfWidth { get; }
        public float TotalSeconds { get; }
        internal float Remaining { get; set; }

        /// <summary>出てから攻撃が来るまでの進み具合（0〜1）。見た目で「もうすぐ来る」を伝えるのに使う</summary>
        public float Progress => TotalSeconds > 0f ? 1f - Remaining / TotalSeconds : 1f;

        /// <summary>円（半径 radius）が範囲に少しでもかかっているか</summary>
        public bool Overlaps(Vec2 center, float radius)
        {
            var reach = HalfWidth + radius;
            return DistanceSquaredToSegment(center) <= reach * reach;
        }

        private float DistanceSquaredToSegment(Vec2 point)
        {
            var segment = To - From;
            var lengthSq = segment.LengthSquared;
            var t = lengthSq > 0f ? System.Math.Clamp(Vec2.Dot(point - From, segment) / lengthSq, 0f, 1f) : 0f;
            return (point - (From + segment * t)).LengthSquared;
        }
    }

    /// <summary>大ボス1体の、いま出している攻撃と次の攻撃までの時間</summary>
    internal sealed class BigBossBrain
    {
        public BigBossBrain(BigBossDef def)
        {
            Def = def;
            RestSeconds = Balance.BigBossFirstAttackDelay;
        }

        public BigBossDef Def { get; }
        public int NextAttackIndex { get; set; }
        public float RestSeconds { get; set; }

        /// <summary>予告を出している攻撃。null なら休み中</summary>
        public BossAttackDef Current { get; set; }
        public float TelegraphSeconds { get; set; }

        /// <summary>突進・波の向き</summary>
        public Vec2 Direction { get; set; }
        public List<BossWarning> Warnings { get; } = new List<BossWarning>();
    }
}
