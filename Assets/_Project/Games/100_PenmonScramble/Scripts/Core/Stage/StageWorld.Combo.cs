using System;
using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 合体技。別のプレイヤーの技が1秒以内に同じ敵へ当たると、その敵の位置で属性の組み合わせの技が起きる
    /// </summary>
    public sealed partial class StageWorld
    {
        private readonly List<PendingCombo> _pendingCombos = new List<PendingCombo>();
        private readonly List<Unit> _shockTargets = new List<Unit>();

        private readonly struct PendingCombo
        {
            public readonly int OwnerId;
            public readonly ComboKind Kind;
            public readonly Vec2 Position;

            public PendingCombo(int ownerId, ComboKind kind, Vec2 position)
            {
                OwnerId = ownerId;
                Kind = kind;
                Position = position;
            }
        }

        /// <summary>
        /// プレイヤーの技が敵に当たるたびに呼ぶ。窓の中に別の人の技が当たっていたら合体技を予約し、そうでなければ自分を覚えさせる。
        /// 必殺技・合体技のダメージでは起こさない（画面中で起き続けないため）
        /// </summary>
        private void RecordComboHit(int attackerId, Unit target, SkillDef skill)
        {
            if (target.Team != Team.Enemy || skill.IsCombo || skill.IsSpecial)
            {
                return;
            }

            var attacker = FindUnit(attackerId);
            if (attacker == null || attacker.Team != Team.Player)
            {
                return;
            }

            var byPartner = target.ComboWindowSeconds > 0f && target.ComboAttackerId != attackerId;
            if (byPartner && target.ComboCooldownSeconds <= 0f)
            {
                var kind = ComboMoves.KindOf(target.ComboElement, attacker.Element);
                _pendingCombos.Add(new PendingCombo(attackerId, kind, target.Position));
                target.ComboCooldownSeconds = Balance.ComboCooldownSeconds;
                target.ComboWindowSeconds = 0f;
                return;
            }

            target.ComboAttackerId = attackerId;
            target.ComboElement = attacker.Element;
            target.ComboWindowSeconds = Balance.ComboWindowSeconds;
        }

        private void FirePendingCombos()
        {
            foreach (var combo in _pendingCombos)
            {
                // 予約してから出すまでの間にゲストが抜けて、消えていることがある
                var owner = FindUnit(combo.OwnerId);
                if (owner == null)
                {
                    continue;
                }

                FireCombo(owner, combo.Kind, combo.Position);
                _hitStopTicks = Math.Max(_hitStopTicks, Balance.ComboHitStopTicks);
                Combo?.Invoke(new ComboEvent(combo.Kind, combo.Position));
            }

            _pendingCombos.Clear();
        }

        /// <summary>ダメージは合体技を起こした（2人目の）プレイヤーの攻撃で決める</summary>
        private void FireCombo(Unit owner, ComboKind kind, Vec2 position)
        {
            switch (kind)
            {
                case ComboKind.Freeze:
                    AddDrop(owner, ComboMoves.Freeze, position, 0f);
                    break;
                case ComboKind.Blizzard:
                    AddArea(owner, ComboMoves.Blizzard, AreaEffectKind.Zone, position);
                    break;
                case ComboKind.IceBurst:
                    BurstIntoShards(owner, position);
                    break;
                case ComboKind.Whirlpool:
                    AddArea(owner, ComboMoves.Whirlpool, AreaEffectKind.Zone, position);
                    break;
                case ComboKind.Shock:
                    ChainShock(owner, position);
                    break;
                case ComboKind.ThunderCloud:
                    AddArea(owner, ComboMoves.ThunderCloud, AreaEffectKind.Cloud, position);
                    break;
                default:
                    AddDrop(owner, ComboMoves.Explosion, position, 0f);
                    break;
            }
        }

        private void AddArea(Unit owner, SkillDef skill, AreaEffectKind kind, Vec2 position)
        {
            _areaEffects.Add(new AreaEffect(_nextId++, owner, skill, kind, position, 0f));
        }

        /// <summary>氷＋雷: その場ではじけて、周りへ破片を飛ばす</summary>
        private void BurstIntoShards(Unit owner, Vec2 position)
        {
            AddDrop(owner, ComboMoves.IceBurst, position, 0f);
            for (var i = 0; i < ComboMoves.IceShardCount; i++)
            {
                var direction = Vec2.Right.Rotated(MathF.PI * 2f * i / ComboMoves.IceShardCount);
                SpawnProjectile(owner, ComboMoves.IceShard, position, direction);
            }
        }

        /// <summary>水＋雷: 近い敵から順に、少しずつずらして電気を伝える</summary>
        private void ChainShock(Unit owner, Vec2 position)
        {
            FindNearestOpponents(owner.Team, position, ComboMoves.ShockRange, _shockTargets);
            var count = Math.Min(ComboMoves.ShockMaxTargets, _shockTargets.Count);
            for (var i = 0; i < count; i++)
            {
                AddDrop(owner, ComboMoves.Shock, _shockTargets[i].Position, ComboMoves.ShockStagger * i);
            }
        }

        /// <summary>雷雲: 一番近い敵へ寄っていき、決まった間隔で雷を落とす</summary>
        private void UpdateCloud(AreaEffect cloud, float dt)
        {
            var target = FindNearestOpponent(cloud.Team, cloud.Position);
            if (target != null)
            {
                var toTarget = target.Position - cloud.Position;
                var step = MathF.Min(toTarget.Length, cloud.Skill.ChaseSpeed * dt);
                cloud.Position += toTarget.Normalized * step;
            }

            cloud.IntervalTimer -= dt;
            if (cloud.IntervalTimer <= 0f)
            {
                HitArea(cloud);
                cloud.IntervalTimer += cloud.Skill.Interval;
                Burst?.Invoke(new BurstEvent(BurstShape.Blast, cloud.Element, cloud.Position, cloud.Radius, false));
            }

            cloud.Remaining -= dt;
            cloud.IsDead = cloud.Remaining <= 0f;
        }
    }
}
