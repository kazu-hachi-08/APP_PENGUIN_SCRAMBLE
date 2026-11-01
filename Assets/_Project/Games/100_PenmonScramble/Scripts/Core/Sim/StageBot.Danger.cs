using System;

namespace PenmonScramble.Core
{
    /// <summary>
    /// ボットの「よける」: 大ボス・巨大ペンモンの赤い予告、雑魚とボスの構え、突進、敵の竜巻・背びれ・溶岩を危ない所として見る。
    /// 見えてから ReactionSeconds たったものだけを避ける（人が気づいて動き出すまでの遅れ）
    /// </summary>
    public sealed partial class StageBot
    {
        // 危ない所の縁から、さらにこれだけ離れる（マス）。ぎりぎりで止まって体の端が当たらないように
        private const float SafetyMargin = 0.2f;
        // 逃げ先を探す距離（マス）と向きの数。近い輪から順に、安全な所が見つかった輪で決める
        private static readonly float[] EscapeRings = { 1f, 2f, 3f, 4.5f, 6f, 8f };
        private const int EscapeDirections = 16;
        // 歩く先がこれだけ先で危ない所に入るなら、立ち止まる（マス）
        private const float LookAhead = 0.4f;
        // 構えた敵の弾・突進の道の幅に足す分（マス）
        private const float LaneHalfWidth = 0.4f;
        // 竜巻の引き寄せる範囲から、さらに離れる分（マス）
        private const float VortexExtraMargin = 1f;

        /// <summary>いまの場所が危なければ、一番近い安全な所への向きを返す。危なくなければ false</summary>
        private bool TryEscape(StageWorld world, Unit player, float time, out Vec2 move)
        {
            move = Vec2.Zero;
            Notice(world, time);
            var radius = player.Radius + SafetyMargin;
            if (!IsDangerous(world, player, player.Position, radius))
            {
                return false;
            }

            var center = world.Area.Center;
            foreach (var ring in EscapeRings)
            {
                var found = false;
                var bestScore = float.MaxValue;
                for (var i = 0; i < EscapeDirections; i++)
                {
                    var direction = Vec2.Right.Rotated(MathF.PI * 2f * i / EscapeDirections);
                    var point = player.Position + direction * ring;
                    if (!world.Area.Contains(point, player.Radius) || IsDangerous(world, player, point, radius))
                    {
                        continue;
                    }

                    // 同じ輪の中では、柵から遠い（真ん中に近い）所を選ぶ。柵ぎわは次の攻撃で逃げ場が無くなるため
                    var score = (point - center).LengthSquared;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        move = direction;
                        found = true;
                    }
                }

                if (found)
                {
                    return true;
                }
            }

            // どこも危ないときは真ん中へ（止まっているよりは当たりにくい）
            move = (center - player.Position).Normalized;
            return true;
        }

        /// <summary>この向きに歩くとすぐ危ない所に入るなら、止まる</summary>
        private Vec2 AvoidSteppingIn(StageWorld world, Unit player, Vec2 move)
        {
            if (move.IsNearlyZero)
            {
                return move;
            }

            var ahead = player.Position + move.Normalized * LookAhead;
            return IsDangerous(world, player, ahead, player.Radius + SafetyMargin) ? Vec2.Zero : move;
        }

        /// <summary>見えている危ないものの、初めて見た時刻を覚える。消えたものは忘れる</summary>
        private void Notice(StageWorld world, float time)
        {
            _stale.Clear();
            _stale.AddRange(_firstSeen.Keys);
            foreach (var warning in world.Warnings)
            {
                See(warning.Id, time);
            }

            foreach (var unit in world.Units)
            {
                if (unit.Team == Team.Enemy && (unit.IsWindingUp || unit.IsDashing))
                {
                    See(unit.Id, time);
                }
            }

            foreach (var effect in world.AreaEffects)
            {
                if (effect.Team == Team.Enemy)
                {
                    See(effect.Id, time);
                }
            }

            foreach (var id in _stale)
            {
                _firstSeen.Remove(id);
            }
        }

        private void See(int id, float time)
        {
            if (!_firstSeen.ContainsKey(id))
            {
                // 見落としたものは、消えるまでずっと気づかない
                _firstSeen[id] = Overlooks() ? float.MaxValue : time;
            }

            _stale.Remove(id);
        }

        private bool Overlooks() => _skill == BotSkill.Casual && _random.NextDouble() < CasualOverlookChance;

        private bool IsNoticed(int id, float now)
        {
            return _firstSeen.TryGetValue(id, out var seen) && now - seen >= ReactionSeconds;
        }

        private bool IsDangerous(StageWorld world, Unit player, Vec2 point, float radius)
        {
            var now = _now;
            foreach (var warning in world.Warnings)
            {
                if (IsNoticed(warning.Id, now) && warning.Overlaps(point, radius))
                {
                    return true;
                }
            }

            foreach (var unit in world.Units)
            {
                if (unit.Team == Team.Enemy && IsNoticed(unit.Id, now) && IsInAttack(unit, point, radius))
                {
                    return true;
                }
            }

            foreach (var effect in world.AreaEffects)
            {
                if (effect.Team == Team.Enemy && IsNoticed(effect.Id, now) && IsInEffect(effect, point, radius))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>構えている敵の技・突進している敵の道に入っているか</summary>
        private static bool IsInAttack(Unit enemy, Vec2 point, float radius)
        {
            if (enemy.IsDashing)
            {
                return IsInLane(enemy.Position, enemy.Dash.Direction, enemy.Dash.RemainingDistance, enemy.Radius + LaneHalfWidth, point, radius);
            }

            if (!enemy.IsWindingUp)
            {
                return false;
            }

            var skill = enemy.Skills[enemy.WindUp.Slot];
            switch (skill.Shape)
            {
                case SkillShape.Projectile:
                case SkillShape.Dash:
                    return IsInLane(enemy.Position, enemy.Facing, skill.Range, enemy.Radius + LaneHalfWidth, point, radius);
                case SkillShape.Spread:
                case SkillShape.Melee:
                    return IsInCone(enemy, skill, point, radius);
                case SkillShape.Drop:
                case SkillShape.Zone:
                    return IsWithin(point, enemy.Position + enemy.Facing * skill.Range, skill.AreaRadius + radius) || IsWithin(point, enemy.Position, enemy.Radius + radius);
                default:
                    return IsWithin(point, enemy.Position, enemy.Radius + skill.Range + radius);
            }
        }

        private static bool IsInCone(Unit enemy, SkillDef skill, Vec2 point, float radius)
        {
            var toPoint = point - enemy.Position;
            var distance = toPoint.Length;
            if (distance - radius > enemy.Radius + skill.Range)
            {
                return false;
            }

            if (distance <= enemy.Radius + radius)
            {
                return true;
            }

            var halfAngle = skill.Shape == SkillShape.Spread ? skill.SpreadDegrees * 0.5f : skill.HalfAngleDegrees;
            var widen = MathF.Asin(Math.Min(1f, radius / distance));
            return Vec2.Dot(enemy.Facing, toPoint / distance) >= MathF.Cos(halfAngle * MathF.PI / 180f + widen);
        }

        private static bool IsInEffect(AreaEffect effect, Vec2 point, float radius)
        {
            switch (effect.Kind)
            {
                case AreaEffectKind.Vortex:
                    // 引き寄せられる所のさらに外で待つ（縁で立ち止まると、引かれて中へ入ってしまう）
                    return IsWithin(point, effect.Position, effect.Radius * Balance.VortexReachRatio + VortexExtraMargin + radius);
                case AreaEffectKind.Fin:
                    return IsInLane(effect.Position, effect.Velocity.Normalized, effect.Velocity.Length * 0.6f, effect.Radius, point, radius);
                default:
                    return IsWithin(point, effect.Position, effect.Radius + radius);
            }
        }

        /// <summary>origin から direction へ length の道（幅の半分 halfWidth、端は丸い）に、円がかかっているか</summary>
        private static bool IsInLane(Vec2 origin, Vec2 direction, float length, float halfWidth, Vec2 point, float radius)
        {
            var t = Math.Clamp(Vec2.Dot(point - origin, direction), 0f, length);
            var reach = halfWidth + radius;
            return (point - (origin + direction * t)).LengthSquared <= reach * reach;
        }

        private static bool IsWithin(Vec2 point, Vec2 center, float reach) => (point - center).LengthSquared <= reach * reach;
    }
}
