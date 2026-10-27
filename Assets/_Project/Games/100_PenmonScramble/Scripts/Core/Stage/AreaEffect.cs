namespace PenmonScramble.Core
{
    public enum AreaEffectKind : byte
    {
        /// <summary>しばらく残り、中の敵に少しずつダメージ（設置）</summary>
        Zone,

        /// <summary>少し待ってから1回だけ当たる（落下）。待っている間は落ちる場所を見せる</summary>
        Drop,
    }

    /// <summary>地面に置かれた範囲（設置・落下の技、必殺技）</summary>
    public sealed class AreaEffect
    {
        internal AreaEffect(int id, Unit owner, SkillDef skill, AreaEffectKind kind, Vec2 position, float delay)
        {
            Id = id;
            OwnerId = owner.Id;
            Team = owner.Team;
            Element = owner.Element;
            Skill = skill;
            Kind = kind;
            Position = position;
            // 撃った子が倒れても範囲は残るので、ここで決めておく
            Damage = StageWorld.CalcDamage(owner, skill);
            Remaining = kind == AreaEffectKind.Zone ? skill.Duration : delay;
        }

        public int Id { get; }
        public int OwnerId { get; }
        public Team Team { get; }
        public Element Element { get; }
        public SkillDef Skill { get; }
        public AreaEffectKind Kind { get; }
        public Vec2 Position { get; }
        public float Radius => Skill.AreaRadius;
        public int Damage { get; }

        /// <summary>設置: 消えるまで / 落下: 落ちるまで（秒）</summary>
        internal float Remaining { get; set; }

        /// <summary>設置: 次にダメージを与えるまで（秒）。0 から始めて、置いた瞬間にも当てる</summary>
        internal float IntervalTimer { get; set; }

        internal bool IsDead { get; set; }
    }
}
