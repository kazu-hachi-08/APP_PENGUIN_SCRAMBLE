using System;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 移動・吹っ飛び・押し合い・壁。Unity の物理は使わず、円同士で計算する（§4.4）
    /// </summary>
    public sealed partial class StageWorld
    {
        /// <summary>move は長さ 0〜1。向きは最後に動いた方向（§3）</summary>
        private static void MoveBy(Unit unit, Vec2 move, float dt)
        {
            if (move.IsNearlyZero)
            {
                return;
            }

            unit.Position += move * (unit.MoveSpeed * dt);
            unit.Facing = move.Normalized;
        }

        private void ApplyKnockback(float dt)
        {
            foreach (var unit in _units)
            {
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
                var (x, vx) = Bounce(unit.Position.X, unit.KnockbackVelocity.X, AreaMin.X + unit.Radius, AreaMax.X - unit.Radius);
                var (y, vy) = Bounce(unit.Position.Y, unit.KnockbackVelocity.Y, AreaMin.Y + unit.Radius, AreaMax.Y - unit.Radius);
                unit.Position = new Vec2(x, y);
                unit.KnockbackVelocity = new Vec2(vx, vy);
            }
        }

        /// <summary>1軸ぶんの壁。外へ向かって飛んでいたら跳ね返す（§4.4）</summary>
        private static (float position, float velocity) Bounce(float position, float velocity, float min, float max)
        {
            if (position < min)
            {
                return (min, velocity < 0f ? -velocity * Balance.WallRestitution : velocity);
            }

            if (position > max)
            {
                return (max, velocity > 0f ? -velocity * Balance.WallRestitution : velocity);
            }

            return (position, velocity);
        }
    }
}
