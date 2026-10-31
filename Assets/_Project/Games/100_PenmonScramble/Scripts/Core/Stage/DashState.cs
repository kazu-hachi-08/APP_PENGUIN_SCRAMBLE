using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>突進中の状態。すべり終わるまで、歩くことも別の技を出すこともできない</summary>
    internal sealed class DashState
    {
        public DashState(SkillDef skill, Vec2 direction, int damage)
            : this(skill, direction, damage, skill.Range)
        {
        }

        /// <param name="distance">すべる距離。大ボスは予告の道の長さ（柵まで）にそろえる</param>
        public DashState(SkillDef skill, Vec2 direction, int damage, float distance)
        {
            Skill = skill;
            Direction = direction;
            Damage = damage;
            RemainingDistance = distance;
        }

        public SkillDef Skill { get; }
        public Vec2 Direction { get; }
        public int Damage { get; }
        public float RemainingDistance { get; set; }

        /// <summary>1回の突進で、同じ敵に何度も当たらないようにする</summary>
        public HashSet<int> HitUnitIds { get; } = new HashSet<int>();
    }
}
