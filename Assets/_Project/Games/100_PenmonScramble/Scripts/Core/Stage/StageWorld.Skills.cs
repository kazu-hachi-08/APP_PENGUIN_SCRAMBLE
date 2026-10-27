using System;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 技の発動と当たり。技は「形」ごとの処理を使い回す（§4.2）
    /// </summary>
    public sealed partial class StageWorld
    {
        private const float DegToRad = MathF.PI / 180f;

        internal static int CalcDamage(Unit attacker, SkillDef skill)
        {
            return Math.Max(Balance.MinDamage, (int)MathF.Round(skill.Power * attacker.Attack));
        }

        private bool TryUseSkill(Unit unit, int slot, bool faceNearestWhenIdle)
        {
            if (unit.Cooldowns[slot] > 0f)
            {
                return false;
            }

            if (faceNearestWhenIdle)
            {
                FaceNearestOpponent(unit);
            }

            var skill = unit.Skills[slot];
            unit.Cooldowns[slot] = skill.Cooldown;
            SkillUsed?.Invoke(new SkillUsedEvent(unit.Id, slot, skill.Shape));

            switch (skill.Shape)
            {
                case SkillShape.Melee:
                    UseMelee(unit, skill);
                    break;
                case SkillShape.Projectile:
                    SpawnProjectile(unit, skill);
                    break;
            }

            return true;
        }

        /// <summary>止まったまま技を出したとき、狙いをつけなくても敵へ向くようにする（§3）</summary>
        private void FaceNearestOpponent(Unit unit)
        {
            var nearest = FindNearestOpponent(unit);
            if (nearest == null)
            {
                return;
            }

            var direction = (nearest.Position - unit.Position).Normalized;
            if (!direction.IsNearlyZero)
            {
                unit.Facing = direction;
            }
        }

        private void UseMelee(Unit user, SkillDef skill)
        {
            var damage = CalcDamage(user, skill);
            foreach (var target in _units)
            {
                if (target.Team != user.Team && target.IsAlive && IsInMeleeArc(user, target, skill))
                {
                    var direction = (target.Position - user.Position).Normalized;
                    ApplyHit(user.Id, target, damage, skill.Knockback, direction.IsNearlyZero ? user.Facing : direction);
                }
            }
        }

        private static bool IsInMeleeArc(Unit user, Unit target, SkillDef skill)
        {
            var toTarget = target.Position - user.Position;
            var distance = toTarget.Length;
            if (distance - target.Radius > skill.Range)
            {
                return false;
            }

            // 中心が自分の体の中にあるほど近い相手は、向きに関係なく当てる（向きの計算が不安定になるため）
            if (distance < user.Radius)
            {
                return true;
            }

            var cos = Vec2.Dot(user.Facing, toTarget / distance);
            return cos >= MathF.Cos(skill.HalfAngleDegrees * DegToRad);
        }

        private void SpawnProjectile(Unit user, SkillDef skill)
        {
            // 自分の体の縁から出す。中心から出すと、くっついている敵の後ろへ抜けて見えるため
            var position = user.Position + user.Facing * user.Radius;
            _projectiles.Add(new Projectile(_nextId++, user, skill, position, user.Facing));
        }

        private void UpdateProjectiles(float dt)
        {
            foreach (var projectile in _projectiles)
            {
                var step = projectile.Velocity * dt;
                projectile.Position += step;
                projectile.RemainingDistance -= step.Length;
                HitUnitsWith(projectile);

                if (projectile.RemainingDistance <= 0f || !IsInsideArea(projectile.Position))
                {
                    projectile.IsDead = true;
                }
            }
        }

        private void HitUnitsWith(Projectile projectile)
        {
            foreach (var target in _units)
            {
                if (projectile.IsDead)
                {
                    return;
                }

                if (target.Team == projectile.Team || !target.IsAlive || projectile.HitUnitIds.Contains(target.Id))
                {
                    continue;
                }

                var reach = projectile.Radius + target.Radius;
                if ((target.Position - projectile.Position).LengthSquared > reach * reach)
                {
                    continue;
                }

                projectile.HitUnitIds.Add(target.Id);
                ApplyHit(projectile.OwnerId, target, projectile.Damage, projectile.Skill.Knockback, projectile.Velocity.Normalized);
                projectile.IsDead = !projectile.Skill.Pierce;
            }
        }

        private void ApplyHit(int attackerId, Unit target, int damage, float knockback, Vec2 direction)
        {
            target.Hp = Math.Max(0, target.Hp - damage);
            // 足すと連打で飛びすぎるので、新しい吹っ飛びで置き換える
            target.KnockbackVelocity = direction * knockback;
            Hit?.Invoke(new HitEvent(attackerId, target.Id, damage, target.Position));
        }

        private bool IsInsideArea(Vec2 position)
        {
            return position.X >= AreaMin.X && position.X <= AreaMax.X && position.Y >= AreaMin.Y && position.Y <= AreaMax.Y;
        }
    }
}
