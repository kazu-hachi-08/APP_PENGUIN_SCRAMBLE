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

    /// <summary>
    /// 大ボス1体（または巨大ペンモン1体の大技）の、いま出している攻撃と次の攻撃までの時間。
    /// 巨大ペンモンは大技1つだけを持ち、大技の合間はふつうの技で戦う（StageWorld.Giant）
    /// </summary>
    internal sealed class BigBossBrain
    {
        private static readonly BossAttackDef[] NoAttacks = new BossAttackDef[0];

        private BigBossBrain(IReadOnlyList<BossAttackDef> attacks, float restSeconds, float firstDelay, float telegraphScale)
        {
            Attacks = attacks ?? NoAttacks;
            BaseRestSeconds = restSeconds;
            RestSeconds = firstDelay;
            TelegraphScale = telegraphScale;
        }

        /// <param name="telegraphScale">難易度の予告の倍率（Balance.DifficultyTelegraphScale）</param>
        public static BigBossBrain ForBigBoss(BigBossDef def, bool serious, float telegraphScale)
        {
            return new BigBossBrain(def.Attacks, def.RestSeconds, Balance.BigBossFirstAttackDelay, telegraphScale)
            {
                Def = def,
                Rage = serious ? def.Rage : null,
                IsSerious = serious,
            };
        }

        public static BigBossBrain ForGiant(BossAttackDef attack, float telegraphScale)
        {
            return new BigBossBrain(new[] { attack }, Balance.GiantBigAttackInterval, Balance.GiantFirstBigAttackDelay, telegraphScale)
            {
                IsGiant = true,
            };
        }

        /// <summary>大ボスの定義。巨大ペンモンは null</summary>
        public BigBossDef Def { get; private set; }

        /// <summary>この順に繰り返す</summary>
        public IReadOnlyList<BossAttackDef> Attacks { get; }
        public bool IsGiant { get; private set; }

        /// <summary>本気（頭に「本気」の札）。HP が半分を切ると IsEnraged になる</summary>
        public bool IsSerious { get; private set; }
        public bool IsEnraged { get; set; }

        /// <summary>本気で増える攻撃。本気でなければ null</summary>
        public BossRage Rage { get; private set; }

        public int NextAttackIndex { get; set; }
        public float RestSeconds { get; set; }

        /// <summary>攻撃が終わってから次の予告までの、もとの休み（秒）</summary>
        public float BaseRestSeconds { get; }

        /// <summary>難易度による予告の倍率。吠えた後はさらに短くする（CurrentTelegraphScale）</summary>
        public float TelegraphScale { get; }

        public float CurrentTelegraphScale => IsEnraged ? TelegraphScale * Balance.RageTelegraphScale : TelegraphScale;
        public float CurrentRestSeconds => IsEnraged ? BaseRestSeconds * Balance.RageRestScale : BaseRestSeconds;

        /// <summary>予告を出している攻撃。null なら休み中</summary>
        public BossAttackDef Current { get; set; }
        public float TelegraphSeconds { get; set; }

        /// <summary>いま出している予告の、出したときの長さ（秒）</summary>
        public float TelegraphTotal { get; set; }

        /// <summary>本気で、休まずに続けて出す残りの回数</summary>
        public int RepeatsLeft { get; set; }

        /// <summary>突進・波の向き</summary>
        public Vec2 Direction { get; set; }

        /// <summary>突進ですべる距離（予告の道の長さ）</summary>
        public float DashDistance { get; set; }
        public List<BossWarning> Warnings { get; } = new List<BossWarning>();

        /// <summary>吠えた後に、attack を出すとき増やす数（同時・続けて のどちらかは Rage.Simultaneous）。増えなければ 0</summary>
        public int ExtraFor(BossAttackDef attack) => IsEnraged && Rage != null && Rage.Kind == attack.Kind ? Rage.Extra : 0;
    }
}
