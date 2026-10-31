namespace PenmonScramble.Core
{
    /// <summary>
    /// 連れている3体のうちの1体。HP と技の待ち時間は1体ずつ持つ
    /// </summary>
    public sealed class PartyMember
    {
        internal PartyMember(UnitStats stats)
        {
            Stats = stats;
            Hp = stats.MaxHp;
            Cooldowns = new float[stats.Skills.Length];
        }

        public UnitStats Stats { get; }
        public int MaxHp => Stats.MaxHp;
        public int Hp { get; internal set; }
        public bool IsAlive => Hp > 0;

        internal float[] Cooldowns { get; }
    }
}
