using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 地面に置く技（設置・落下）と突進
    /// </summary>
    public sealed partial class StageWorld
    {
        // 落下の的を探すときに使い回す（毎回 new しないため）
        private readonly List<Unit> _dropTargets = new List<Unit>();

        /// <summary>近くの敵から順に Count か所へ落とす。届く所に敵がいなければ、前に1つ落とす</summary>
        private void UseDrop(Unit user, SkillDef skill)
        {
            FindNearestOpponents(user.Team, user.Position, skill.Range, _dropTargets);
            if (_dropTargets.Count == 0)
            {
                AddDrop(user, skill, user.Position + user.Facing * Balance.DropFallbackDistance, skill.Delay);
                return;
            }

            var count = System.Math.Min(System.Math.Max(1, skill.Count), _dropTargets.Count);
            for (var i = 0; i < count; i++)
            {
                AddDrop(user, skill, _dropTargets[i].Position, skill.Delay);
            }
        }

        private void AddDrop(Unit owner, SkillDef skill, Vec2 position, float delay)
        {
            _areaEffects.Add(new AreaEffect(_nextId++, owner, skill, AreaEffectKind.Drop, position, delay));
        }

        /// <summary>center から range の中にいる、team の相手を、近い順に results へ入れる</summary>
        private void FindNearestOpponents(Team team, Vec2 center, float range, List<Unit> results)
        {
            results.Clear();
            foreach (var other in _units)
            {
                if (other.Team != team && other.IsAlive && IsWithin(other, center, range))
                {
                    results.Add(other);
                }
            }

            results.Sort((a, b) => (a.Position - center).LengthSquared.CompareTo((b.Position - center).LengthSquared));
        }

        private void UpdateAreaEffects(float dt)
        {
            foreach (var effect in _areaEffects)
            {
                switch (effect.Kind)
                {
                    case AreaEffectKind.Zone:
                    case AreaEffectKind.Lava:
                        UpdateZone(effect, dt);
                        break;
                    case AreaEffectKind.Fin:
                        UpdateFinRunner(effect, dt);
                        break;
                    case AreaEffectKind.Drop:
                        UpdateDrop(effect, dt);
                        break;
                    case AreaEffectKind.Vortex:
                        UpdateVortex(effect, dt);
                        break;
                    default:
                        UpdateCloud(effect, dt);
                        break;
                }
            }

            _areaEffects.RemoveAll(e => e.IsDead);
        }

        private void UpdateZone(AreaEffect zone, float dt)
        {
            zone.IntervalTimer -= dt;
            if (zone.IntervalTimer <= 0f)
            {
                HitArea(zone);
                zone.IntervalTimer += zone.Skill.Interval;
            }

            AgeEffect(zone, dt);
        }

        /// <summary>範囲の残り時間を進め、切れたら消す。残り時間の持ち方は範囲の種類によらず同じ（雷雲・背びれの走りも同じ）</summary>
        private static void AgeEffect(AreaEffect effect, float dt)
        {
            effect.Remaining -= dt;
            effect.IsDead = effect.Remaining <= 0f;
        }

        private void UpdateDrop(AreaEffect drop, float dt)
        {
            drop.Remaining -= dt;
            if (drop.Remaining > 0f)
            {
                return;
            }

            HitArea(drop);
            drop.IsDead = true;
            Burst?.Invoke(new BurstEvent(BurstShape.Blast, drop.Element, drop.Position, drop.Radius, drop.Skill.IsSpecial));
        }

        /// <summary>範囲の中の敵に当てる。Pull があれば外へ飛ばす代わりに中心へ吸い込む（うず潮）</summary>
        private void HitArea(AreaEffect effect)
        {
            var skill = effect.Skill;
            foreach (var target in _units)
            {
                if (target.Team == effect.Team || !target.IsAlive || !IsWithin(target, effect.Position, effect.Radius))
                {
                    continue;
                }

                var outward = AwayFrom(effect.Position, target, Vec2.Right);
                // 竜巻は UpdateVortex で位置を寄せているので、当たったときは飛ばさない（飛ばすと一瞬動けなくなり、抜け出せなくなる）
                var pulls = skill.Pull > 0f && effect.Kind != AreaEffectKind.Vortex;
                ApplyHit(effect.OwnerId, target, effect.Damage, pulls ? skill.Pull : skill.Knockback, pulls ? -outward : outward, skill);
            }
        }

        /// <summary>
        /// 竜巻: 円とそのまわり（VortexReachRatio 倍）の相手を、毎回少しずつ中心へ寄せる。吹っ飛ばさずに位置を動かすので、
        /// 歩いて逆らえば抜け出せる（引く速さは歩く速さより遅い）。円の中にいる相手には、間隔ごとに当たる
        /// </summary>
        private void UpdateVortex(AreaEffect vortex, float dt)
        {
            DriftVortex(vortex, dt);
            var reach = vortex.Radius * Balance.VortexReachRatio;
            foreach (var target in _units)
            {
                if (target.Team == vortex.Team || !target.IsAlive || target.IsSubmerged || !IsWithin(target, vortex.Position, reach))
                {
                    continue;
                }

                var toCenter = vortex.Position - target.Position;
                var step = System.MathF.Min(toCenter.Length, vortex.Skill.Pull * dt);
                target.Position += DirectionOr(toCenter, Vec2.Zero) * step;
            }

            UpdateZone(vortex, dt);
        }

        /// <summary>前へすべり、ぶつかった敵を進む向きへ飛ばす。壁に当たってもすべる距離は減っていく</summary>
        private void UpdateDash(Unit unit, float dt)
        {
            var dash = unit.Dash;
            var step = System.MathF.Min(dash.RemainingDistance, dash.Skill.DashSpeed * dt);
            unit.Position += dash.Direction * step;
            unit.Facing = dash.Direction;
            dash.RemainingDistance -= step;

            foreach (var target in _units)
            {
                if (!IsOpponent(unit, target) || dash.HitUnitIds.Contains(target.Id))
                {
                    continue;
                }

                if (IsWithin(target, unit.Position, unit.Radius + dash.Skill.AreaRadius))
                {
                    dash.HitUnitIds.Add(target.Id);
                    ApplyHit(unit.Id, target, dash.Damage, dash.Skill.Knockback, dash.Direction, dash.Skill);
                }
            }

            if (dash.RemainingDistance <= 0f)
            {
                unit.Dash = null;
            }
        }
    }
}
