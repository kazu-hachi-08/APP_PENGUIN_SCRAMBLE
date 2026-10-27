using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 飛んでいる技の弾
    /// </summary>
    public sealed class Projectile
    {
        internal Projectile(int id, Unit owner, SkillDef skill, Vec2 position, Vec2 direction)
        {
            Id = id;
            OwnerId = owner.Id;
            Team = owner.Team;
            Skill = skill;
            Damage = StageWorld.CalcDamage(owner, skill);
            Position = position;
            Velocity = direction * skill.ProjectileSpeed;
            RemainingDistance = skill.Range;
        }

        public int Id { get; }
        public int OwnerId { get; }
        public Team Team { get; }
        public SkillDef Skill { get; }
        public float Radius => Skill.ProjectileRadius;

        /// <summary>撃った瞬間のダメージ。撃った子が倒れても弾は残るので、ここで決めておく</summary>
        public int Damage { get; }

        public Vec2 Position { get; internal set; }
        public Vec2 Velocity { get; }
        internal float RemainingDistance { get; set; }
        internal bool IsDead { get; set; }

        /// <summary>貫通する弾が、同じ敵に毎回当たらないようにする</summary>
        internal HashSet<int> HitUnitIds { get; } = new HashSet<int>();
    }
}
