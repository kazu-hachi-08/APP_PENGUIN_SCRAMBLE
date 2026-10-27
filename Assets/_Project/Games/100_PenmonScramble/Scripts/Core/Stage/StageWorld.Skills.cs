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
            unit.Cooldowns[slot] = skill.Cooldown * unit.CooldownScale;
            SkillUsed?.Invoke(new SkillUsedEvent(unit.Id, slot, skill.Shape));
            UseShape(unit, skill);
            return true;
        }

        private void UseShape(Unit user, SkillDef skill)
        {
            switch (skill.Shape)
            {
                case SkillShape.Melee:
                    UseMelee(user, skill);
                    break;
                case SkillShape.Projectile:
                    SpawnProjectile(user, skill, user.Position + user.Facing * user.Radius, user.Facing);
                    break;
                case SkillShape.Spread:
                    UseSpread(user, skill);
                    break;
                case SkillShape.Dash:
                    user.Dash = new DashState(skill, user.Facing, CalcDamage(user, skill));
                    break;
                case SkillShape.Nova:
                    UseNova(user, skill);
                    break;
                case SkillShape.Zone:
                    _areaEffects.Add(new AreaEffect(_nextId++, user, skill, AreaEffectKind.Zone, user.Position + user.Facing * skill.Range, 0f));
                    break;
                case SkillShape.Drop:
                    UseDrop(user, skill);
                    break;
            }
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
                if (IsOpponent(user, target) && IsInMeleeArc(user, target, skill))
                {
                    ApplyHit(user.Id, target, damage, skill.Knockback, AwayFrom(user.Position, target, user.Facing), skill);
                }
            }

            var swingCenter = user.Position + user.Facing * (skill.Range * 0.5f);
            Burst?.Invoke(new BurstEvent(BurstShape.Swing, user.Element, swingCenter, skill.Range * 0.5f + user.Radius * 0.5f, skill.IsSpecial));
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

        /// <summary>自分の周りの円に入った敵へ、外向きに当てる（周囲。必殺技の大竜巻も同じ）</summary>
        private void UseNova(Unit user, SkillDef skill)
        {
            var damage = CalcDamage(user, skill);
            foreach (var target in _units)
            {
                if (IsOpponent(user, target) && IsWithin(target, user.Position, skill.Range))
                {
                    ApplyHit(user.Id, target, damage, skill.Knockback, AwayFrom(user.Position, target, user.Facing), skill);
                }
            }

            Burst?.Invoke(new BurstEvent(BurstShape.Ring, user.Element, user.Position, skill.Range, skill.IsSpecial));
        }

        /// <summary>中心から target への向き。重なっているときは fallback（向きが決まらないため）</summary>
        private static Vec2 AwayFrom(Vec2 center, Unit target, Vec2 fallback)
        {
            var direction = (target.Position - center).Normalized;
            return direction.IsNearlyZero ? fallback : direction;
        }

        /// <summary>円が範囲に少しでもかかっていれば当てる</summary>
        private static bool IsWithin(Unit target, Vec2 center, float radius)
        {
            var reach = radius + target.Radius;
            return (target.Position - center).LengthSquared <= reach * reach;
        }

        private void ApplyHit(int attackerId, Unit target, int damage, float knockback, Vec2 direction, SkillDef skill)
        {
            // 必殺技のあいだ、出した本人は無敵（§4.3）
            if (target.IsInvincible)
            {
                return;
            }

            target.Hp = Math.Max(0, target.Hp - damage);
            // 足すと連打で飛びすぎるので、新しい吹っ飛びで置き換える
            target.KnockbackVelocity = direction * (knockback * target.KnockbackScale);
            ChargeSpecialGauge(attackerId, target, damage, skill);
            Hit?.Invoke(new HitEvent(attackerId, target.Id, damage, target.Position));
        }

        private bool IsInsideArea(Vec2 position)
        {
            return position.X >= AreaMin.X && position.X <= AreaMax.X && position.Y >= AreaMin.Y && position.Y <= AreaMax.Y;
        }
    }
}
