using System;
using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 3体の交代と必殺技（§4.1・§4.3）
    /// </summary>
    public sealed partial class StageWorld
    {
        // カットインが終わってから効き目を出す必殺技（出した Unit の Id）
        private readonly List<int> _pendingSpecials = new List<int>();
        private int _hitStopTicks;

        private void TrySwitch(Unit unit)
        {
            if (unit.SwitchCooldown > 0f || !unit.SwitchToNextAlive())
            {
                return;
            }

            unit.SwitchCooldown = Balance.SwitchCooldownSeconds;
        }

        private bool TryUseSpecial(Unit unit)
        {
            if (!unit.IsSpecialReady)
            {
                return false;
            }

            unit.SpecialGauge = 0f;
            unit.InvincibleSeconds = Balance.SpecialInvincibleSeconds;
            _hitStopTicks = Math.Max(_hitStopTicks, Balance.SpecialCutInTicks);
            _pendingSpecials.Add(unit.Id);
            SpecialUsed?.Invoke(new SpecialUsedEvent(unit.Id, unit.SpeciesNo, unit.Element));
            return true;
        }

        /// <summary>敵に与えたダメージでゲージをためる。敵1体分の HP を削ると SpecialGainPerEnemy たまる</summary>
        private void ChargeSpecialGauge(int attackerId, Unit target, int damage, SkillDef skill)
        {
            if (skill.IsSpecial || target.Team != Team.Enemy)
            {
                return;
            }

            var attacker = FindUnit(attackerId);
            if (attacker == null || attacker.Team != Team.Player)
            {
                return;
            }

            var gain = (float)damage / Math.Max(1, target.MaxHp) * Balance.SpecialGainPerEnemy;
            attacker.SpecialGauge = MathF.Min(1f, attacker.SpecialGauge + gain);
        }

        private void FirePendingSpecials()
        {
            foreach (var unitId in _pendingSpecials)
            {
                // カットインの間にゲストが抜けて、消えていることがある
                var unit = FindUnit(unitId);
                if (unit != null)
                {
                    FireSpecial(unit);
                }
            }

            _pendingSpecials.Clear();
        }

        private void FireSpecial(Unit user)
        {
            var skill = SpecialMoves.For(user.Element);
            switch (user.Element)
            {
                case Element.Ice:
                    FreezeAndShatter(user, skill);
                    break;
                case Element.Water:
                    SendBigWave(user, skill);
                    break;
                case Element.Wind:
                    UseNova(user, skill);
                    break;
                case Element.Thunder:
                    RainThunder(user, skill);
                    break;
            }
        }

        /// <summary>氷: 敵を全部凍らせて、溶けるときにその場で砕く</summary>
        private void FreezeAndShatter(Unit user, SkillDef skill)
        {
            foreach (var target in _units)
            {
                if (!IsOpponent(user, target))
                {
                    continue;
                }

                target.FrozenSeconds = SpecialMoves.IceFreezeSeconds;
                target.KnockbackVelocity = Vec2.Zero;
                target.Dash = null;
                AddDrop(user, skill, target.Position, skill.Delay);
            }
        }

        /// <summary>水: 向いている側と反対の端から、縦一列の大きな弾を流す（向いている側へ押し流す）</summary>
        private void SendBigWave(Unit user, SkillDef skill)
        {
            var toRight = user.Facing.X >= 0f;
            var direction = toRight ? Vec2.Right : -Vec2.Right;
            var x = toRight ? AreaMin.X + skill.ProjectileRadius : AreaMax.X - skill.ProjectileRadius;
            for (var y = AreaMin.Y + SpecialMoves.WaterWaveSpacing * 0.5f; y < AreaMax.Y; y += SpecialMoves.WaterWaveSpacing)
            {
                SpawnProjectile(user, skill, new Vec2(x, y), direction);
            }
        }

        /// <summary>雷: 全部の敵の頭上と、ばらばらの場所に、少しずつずらして落とす</summary>
        private void RainThunder(Unit user, SkillDef skill)
        {
            var bolt = 0;
            foreach (var target in _units)
            {
                if (IsOpponent(user, target))
                {
                    AddDrop(user, skill, target.Position, skill.Delay + SpecialMoves.ThunderBoltStagger * bolt++);
                }
            }

            for (var i = 0; i < SpecialMoves.ThunderExtraBolts; i++)
            {
                AddDrop(user, skill, RandomPointInArea(), skill.Delay + SpecialMoves.ThunderBoltStagger * bolt++);
            }
        }

        private Vec2 RandomPointInArea()
        {
            var x = AreaMin.X + (float)_random.NextDouble() * (AreaMax.X - AreaMin.X);
            var y = AreaMin.Y + (float)_random.NextDouble() * (AreaMax.Y - AreaMin.Y);
            return new Vec2(x, y);
        }
    }
}
