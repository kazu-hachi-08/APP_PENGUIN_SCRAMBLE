using System;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 移動・吹っ飛び・押し合い・壁。Unity の物理は使わず、円同士で計算する
    /// </summary>
    public sealed partial class StageWorld
    {
        /// <summary>move は長さ 0〜1。向きは最後に動いた方向</summary>
        private static void MoveBy(Unit unit, Vec2 move, float dt)
        {
            if (move.IsNearlyZero)
            {
                return;
            }

            var speed = unit.IsSlowed ? unit.MoveSpeed * Balance.SlowSpeedRatio : unit.MoveSpeed;
            unit.Position += move * (speed * dt);
            unit.Facing = move.Normalized;
        }

        private void ApplyKnockback(float dt)
        {
            foreach (var unit in _units)
            {
                ApplyGust(unit, dt);
                var velocity = unit.KnockbackVelocity;
                if (velocity.IsNearlyZero)
                {
                    continue;
                }

                unit.Position += velocity * dt;
                var speed = MathF.Max(0f, velocity.Length - Balance.KnockbackDeceleration * dt);
                unit.KnockbackVelocity = velocity.Normalized * speed;
            }
        }

        /// <summary>重なった円を半分ずつ押し戻す。数十体なので総当たりで足りる</summary>
        private void ResolveOverlaps()
        {
            for (var i = 0; i < _units.Count; i++)
            {
                for (var j = i + 1; j < _units.Count; j++)
                {
                    Separate(_units[i], _units[j]);
                }
            }
        }

        private static void Separate(Unit a, Unit b)
        {
            // もぐっている大ボスの上は通れる（足元から飛び出すのを待つ間に、押し出されて逃げられなくならないように）
            if (a.IsSubmerged || b.IsSubmerged)
            {
                return;
            }

            var delta = b.Position - a.Position;
            var minDistance = a.Radius + b.Radius;
            var distanceSq = delta.LengthSquared;
            if (distanceSq >= minDistance * minDistance)
            {
                return;
            }

            // 完全に同じ位置だと押す向きが決まらないので、横へ押す
            var normal = delta.IsNearlyZero ? Vec2.Right : delta.Normalized;
            var push = normal * ((minDistance - MathF.Sqrt(distanceSq)) * 0.5f);
            a.Position -= push;
            b.Position += push;
        }

        private void KeepInsideArea()
        {
            foreach (var unit in _units)
            {
                var before = unit.Position;
                var shape = WallFor(unit.Position, unit.Radius);
                (unit.Position, unit.KnockbackVelocity) = shape.Confine(unit.Position, unit.KnockbackVelocity, unit.Radius, Balance.WallRestitution);
                if (unit.Gust != null && (unit.Position - before).LengthSquared > Balance.GustWallHitDistance * Balance.GustWallHitDistance)
                {
                    HitWallByGust(unit);
                }
            }
        }

        /// <summary>突風に押されている間は、決まった速さで流す。倒れた・押す時間が終わったら止める</summary>
        private static void ApplyGust(Unit unit, float dt)
        {
            var gust = unit.Gust;
            if (gust == null)
            {
                return;
            }

            if (!unit.IsAlive || gust.Remaining <= 0f)
            {
                unit.Gust = null;
                return;
            }

            unit.Position += gust.Velocity * dt;
            gust.Remaining -= dt;
        }

        /// <summary>突風で柵に押し付けられた: 1回だけ痛く、柵から少し跳ね返って押しが終わる</summary>
        private void HitWallByGust(Unit unit)
        {
            var gust = unit.Gust;
            unit.Gust = null;
            ApplyHit(gust.OwnerId, unit, gust.WallDamage, gust.Skill.Knockback, -gust.Velocity.Normalized, gust.Skill);
            Burst?.Invoke(new BurstEvent(BurstShape.Blast, Element.Wind, unit.Position, unit.Radius * 1.5f, false));
        }

        /// <summary>
        /// 押し戻しに使う形。範囲が1つならそれ。いくつかを合わせた範囲では、どれかの中にいればその形（押し戻さない）、
        /// どれにも入っていなければ一番近い形へ戻す（闘技場が始まったときに外にいた人は、楕円の中へ引き寄せられる）
        /// </summary>
        private AreaShape WallFor(Vec2 position, float margin)
        {
            if (_bounds.Count == 1)
            {
                return _bounds[0];
            }

            var nearest = _bounds[0];
            var nearestSq = float.MaxValue;
            foreach (var shape in _bounds)
            {
                if (shape.Contains(position, margin))
                {
                    return shape;
                }

                var sq = (shape.ClosestInside(position, margin) - position).LengthSquared;
                if (sq < nearestSq)
                {
                    nearestSq = sq;
                    nearest = shape;
                }
            }

            return nearest;
        }

        /// <summary>動ける範囲を置き換える。最初の形を「いまのエリア」とする</summary>
        private void SetBounds(params AreaShape[] shapes)
        {
            _bounds.Clear();
            _bounds.AddRange(shapes);
            var min = shapes[0].Min;
            var max = shapes[0].Max;
            foreach (var shape in shapes)
            {
                min = new Vec2(MathF.Min(min.X, shape.Min.X), MathF.Min(min.Y, shape.Min.Y));
                max = new Vec2(MathF.Max(max.X, shape.Max.X), MathF.Max(max.Y, shape.Max.Y));
            }

            AreaMin = min;
            AreaMax = max;
        }

        /// <summary>動ける範囲の中か（弾が外へ出たら消すのに使う）</summary>
        private bool IsInsideArea(Vec2 position)
        {
            foreach (var shape in _bounds)
            {
                if (shape.Contains(position, 0f))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
