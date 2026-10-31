using System;
using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 大ボスの動き: 休みの間は一番近いプレイヤーへ寄り、時間が来たら赤い範囲で予告 → 攻撃、を順に繰り返す。
    /// 予告が出ている間は動かない（予告の場所と攻撃の場所がずれないように）
    /// </summary>
    public sealed partial class StageWorld
    {
        // 波の帯の上に並べる「はじける」演出の間隔（マス）
        private const float WaveBurstSpacing = 2f;

        private readonly List<BossWarning> _warnings = new List<BossWarning>();

        /// <summary>いま出ている赤い範囲（全部の大ボスの分）</summary>
        public IReadOnlyList<BossWarning> Warnings => _warnings;

        private void UpdateBigBoss(Unit boss, BigBossBrain brain, float dt)
        {
            if (brain.Current == null)
            {
                RestAndApproach(boss, brain, dt);
                return;
            }

            brain.TelegraphSeconds -= dt;
            foreach (var warning in brain.Warnings)
            {
                warning.Remaining = MathF.Max(0f, warning.Remaining - dt);
            }

            if (brain.TelegraphSeconds > 0f)
            {
                return;
            }

            ExecuteBossAttack(boss, brain);
            ClearWarnings(brain);
            brain.Current = null;
            brain.RestSeconds = brain.Def.RestSeconds;
        }

        private void RestAndApproach(Unit boss, BigBossBrain brain, float dt)
        {
            brain.RestSeconds -= dt;
            var target = FindNearestOpponent(boss);
            if (target == null)
            {
                return;
            }

            var toTarget = target.Position - boss.Position;
            if (toTarget.LengthSquared > Balance.BigBossKeepDistance * Balance.BigBossKeepDistance)
            {
                MoveBy(boss, toTarget.Normalized, dt);
            }
            else
            {
                boss.Facing = DirectionOr(toTarget, boss.Facing);
            }

            if (brain.RestSeconds <= 0f)
            {
                StartBossAttack(boss, brain, target);
            }
        }

        private void StartBossAttack(Unit boss, BigBossBrain brain, Unit target)
        {
            var attack = brain.Def.Attacks[brain.NextAttackIndex % brain.Def.Attacks.Count];
            brain.NextAttackIndex++;
            brain.Current = attack;
            brain.TelegraphSeconds = attack.TelegraphSeconds;
            var skill = attack.Skill;
            switch (attack.Kind)
            {
                case BossAttackKind.Charge:
                    brain.Direction = attack.CrossScreen ? HorizontalToward(boss, target) : AwayFrom(boss.Position, target, boss.Facing);
                    boss.Facing = brain.Direction;
                    // 柵に当たったら止まる。予告の道と実際に走る道をそろえ、柵に沿って予告の外へすべり込まないようにする
                    var wall = Area.DistanceToWall(boss.Position, brain.Direction, boss.Radius);
                    brain.DashDistance = attack.CrossScreen ? wall : MathF.Min(skill.Range, wall);
                    AddWarning(brain, boss.Position, boss.Position + brain.Direction * brain.DashDistance, boss.Radius + skill.AreaRadius);
                    break;
                case BossAttackKind.Rain:
                    StartRain(brain, skill);
                    break;
                case BossAttackKind.Ambush:
                    // もぐっている間は攻撃が効かない。飛び出す場所は、もぐった瞬間の一番近いプレイヤーの足元
                    boss.IsSubmerged = true;
                    boss.KnockbackVelocity = Vec2.Zero;
                    AddWarning(brain, target.Position, target.Position, skill.AreaRadius);
                    break;
                default:
                    StartWave(boss, brain, target);
                    break;
            }
        }

        /// <summary>
        /// 元気なプレイヤー全員の足元と、エリアのばらばらの場所に円を出す（ソロでもよける所が分かりにくくならない数）。
        /// ばらばらの円は広い場ほど増やす。広さの比の平方根にして、闘技場（ふつうのエリアの約3倍）で 3 → 5 くらいにとどめる
        /// （広さどおりに増やすと、円だらけで逃げ場が読めなくなるため）
        /// </summary>
        private void StartRain(BigBossBrain brain, SkillDef skill)
        {
            foreach (var unit in _units)
            {
                if (unit.Team == Team.Player && unit.IsAlive)
                {
                    AddWarning(brain, unit.Position, unit.Position, skill.AreaRadius);
                }
            }

            var count = (int)MathF.Round(skill.Count * MathF.Sqrt(Area.Size / (Balance.AreaWidth * Balance.AreaHeight)));
            for (var i = 0; i < count; i++)
            {
                var point = RandomPointInArea(skill.AreaRadius);
                AddWarning(brain, point, point, skill.AreaRadius);
            }
        }

        /// <summary>一番近いプレイヤーがいる側（上半分か下半分）を、エリアの端から端まで帯で知らせる。闘技場では、帯の高さで柵から柵まで</summary>
        private void StartWave(Unit boss, BigBossBrain brain, Unit target)
        {
            var centerY = Area.Center.Y;
            var quarter = Area.Height * 0.25f;
            var y = target.Position.Y >= centerY ? centerY + quarter : centerY - quarter;
            var (minX, maxX) = Area.SpanX(y, 0f);
            brain.Direction = HorizontalToward(boss, target);
            AddWarning(brain, new Vec2(minX, y), new Vec2(maxX, y), quarter);
        }

        private void ExecuteBossAttack(Unit boss, BigBossBrain brain)
        {
            var skill = brain.Current.Skill;
            var damage = CalcDamage(boss, skill);
            switch (brain.Current.Kind)
            {
                case BossAttackKind.Charge:
                    boss.Dash = new DashState(skill, brain.Direction, damage, brain.DashDistance);
                    break;
                case BossAttackKind.Rain:
                    foreach (var warning in brain.Warnings)
                    {
                        HitPlayersIn(boss, warning, damage, skill, null);
                        Burst?.Invoke(new BurstEvent(BurstShape.Blast, boss.Element, warning.From, warning.HalfWidth, false));
                    }

                    break;
                case BossAttackKind.Ambush:
                    var spot = brain.Warnings[0];
                    boss.Position = spot.From;
                    boss.IsSubmerged = false;
                    HitPlayersIn(boss, spot, damage, skill, null);
                    Burst?.Invoke(new BurstEvent(BurstShape.Ring, boss.Element, spot.From, spot.HalfWidth, false));
                    break;
                default:
                    ExecuteWave(boss, brain.Warnings[0], damage, skill, brain.Direction);
                    break;
            }
        }

        private void ExecuteWave(Unit boss, BossWarning band, int damage, SkillDef skill, Vec2 direction)
        {
            HitPlayersIn(boss, band, damage, skill, direction);
            for (var x = band.From.X; x <= band.To.X; x += WaveBurstSpacing)
            {
                Burst?.Invoke(new BurstEvent(BurstShape.Blast, boss.Element, new Vec2(x, band.From.Y), band.HalfWidth, false));
            }
        }

        /// <param name="direction">飛ばす向き。null なら範囲の中心から外へ</param>
        private void HitPlayersIn(Unit boss, BossWarning warning, int damage, SkillDef skill, Vec2? direction)
        {
            foreach (var target in _units)
            {
                if (IsOpponent(boss, target) && warning.Overlaps(target.Position, target.Radius))
                {
                    ApplyHit(boss.Id, target, damage, skill.Knockback, direction ?? AwayFrom(warning.From, target, boss.Facing), skill);
                }
            }
        }

        private void AddWarning(BigBossBrain brain, Vec2 from, Vec2 to, float halfWidth)
        {
            var warning = new BossWarning(_nextId++, from, to, halfWidth, brain.Current.TelegraphSeconds);
            brain.Warnings.Add(warning);
            _warnings.Add(warning);
        }

        /// <summary>攻撃が終わった・大ボスが倒れたときに、その大ボスの予告を消す</summary>
        private void ClearWarnings(BigBossBrain brain)
        {
            foreach (var warning in brain.Warnings)
            {
                _warnings.Remove(warning);
            }

            brain.Warnings.Clear();
        }

        private static Vec2 HorizontalToward(Unit from, Unit target)
        {
            return new Vec2(target.Position.X >= from.Position.X ? 1f : -1f, 0f);
        }

        private Vec2 RandomPointInArea(float margin) => Area.RandomPoint(_random, margin);
    }
}
