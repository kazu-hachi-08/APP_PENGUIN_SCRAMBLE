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
}
