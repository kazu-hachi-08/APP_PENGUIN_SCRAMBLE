namespace PenmonScramble.Core
{
    /// <summary>技が当たった。ダメージ数字・白く光る演出に使う</summary>
    public readonly struct HitEvent
    {
        public readonly int AttackerId;
        public readonly int TargetId;
        public readonly int Damage;
        public readonly Vec2 Position;

        public HitEvent(int attackerId, int targetId, int damage, Vec2 position)
        {
            AttackerId = attackerId;
            TargetId = targetId;
            Damage = damage;
            Position = position;
        }
    }

    /// <summary>HP が0になって場から消えた</summary>
    public readonly struct DefeatedEvent
    {
        public readonly int UnitId;
        public readonly Team Team;
        public readonly Vec2 Position;

        public DefeatedEvent(int unitId, Team team, Vec2 position)
        {
            UnitId = unitId;
            Team = team;
            Position = position;
        }
    }

    /// <summary>技を出した。つぶれて伸びる演出・効果音に使う</summary>
    public readonly struct SkillUsedEvent
    {
        public readonly int UnitId;
        public readonly int Slot;
        public readonly SkillShape Shape;

        public SkillUsedEvent(int unitId, int slot, SkillShape shape)
        {
            UnitId = unitId;
            Slot = slot;
            Shape = shape;
        }
    }

    /// <summary>必殺技を出した。カットイン・画面の揺れに使う。効き目はカットインの後に出る</summary>
    public readonly struct SpecialUsedEvent
    {
        public readonly int UnitId;
        public readonly int SpeciesNo;
        public readonly Element Element;

        public SpecialUsedEvent(int unitId, int speciesNo, Element element)
        {
            UnitId = unitId;
            SpeciesNo = speciesNo;
            Element = element;
        }
    }

    /// <summary>合体技が起きた。「合体！○○」の文字・大きめのエフェクト・画面の揺れに使う</summary>
    public readonly struct ComboEvent
    {
        public readonly ComboKind Kind;
        public readonly Vec2 Position;

        public ComboEvent(ComboKind kind, Vec2 position)
        {
            Kind = kind;
            Position = position;
        }
    }

    public enum BurstShape : byte
    {
        /// <summary>近接の振り</summary>
        Swing,

        /// <summary>周囲に広がる輪</summary>
        Ring,

        /// <summary>落下が落ちた・凍った敵が砕けた</summary>
        Blast,
    }

    /// <summary>
    /// 一瞬だけ当たる範囲の技が出た。範囲の見た目に使う（当たったかどうかは HitEvent）。
    /// 技の形ごとの見た目を、技の定義を引かずに描けるようにするため
    /// </summary>
    public readonly struct BurstEvent
    {
        public readonly BurstShape Shape;
        public readonly Element Element;
        public readonly Vec2 Position;
        public readonly float Radius;

        /// <summary>必殺技の一部。大きく画面を揺らす</summary>
        public readonly bool IsSpecial;

        public BurstEvent(BurstShape shape, Element element, Vec2 position, float radius, bool isSpecial)
        {
            Shape = shape;
            Element = element;
            Position = position;
            Radius = radius;
            IsSpecial = isSpecial;
        }
    }
}
