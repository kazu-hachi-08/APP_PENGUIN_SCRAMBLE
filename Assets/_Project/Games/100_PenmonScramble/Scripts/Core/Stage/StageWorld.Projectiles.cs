namespace PenmonScramble.Core
{
    /// <summary>
    /// 飛ぶ技（弾・拡散、必殺技の大波）
    /// </summary>
    public sealed partial class StageWorld
    {
        private void SpawnProjectile(Unit user, SkillDef skill, Vec2 position, Vec2 direction)
        {
            _projectiles.Add(new Projectile(_nextId++, user, skill, position, direction));
        }

        /// <summary>扇の端から端まで、同じ角度ずつずらして撃つ</summary>
        private void UseSpread(Unit user, SkillDef skill)
        {
            var count = System.Math.Max(1, skill.Count);
            var step = count > 1 ? skill.SpreadDegrees / (count - 1) : 0f;
            var start = -skill.SpreadDegrees * 0.5f;
            for (var i = 0; i < count; i++)
            {
                var direction = user.Facing.Rotated((start + step * i) * DegToRad);
                // 自分の体の縁から出す。中心から出すと、くっついている敵の後ろへ抜けて見えるため
                SpawnProjectile(user, skill, user.Position + direction * user.Radius, direction);
            }
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

                if (!IsWithin(target, projectile.Position, projectile.Radius))
                {
                    continue;
                }

                projectile.HitUnitIds.Add(target.Id);
                ApplyHit(projectile.OwnerId, target, projectile.Damage, projectile.Skill.Knockback, projectile.Velocity.Normalized, projectile.Skill);
                projectile.IsDead = !projectile.Skill.Pierce;
            }
        }
    }
}
