using System;
using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// 球の動きの計算。GolfBalance.StepSeconds ずつ進める（何歩進めても同じ打ち方なら同じ結果）。
    /// 全部を本当の3Dにせず、地面の上の2次元と「浮いている間だけの高さ」で計算する
    /// （純C#で確かめやすく、オンラインで全員の見え方をそろえやすいため）
    /// </summary>
    public sealed class GolfBallSim
    {
        private const float Dt = GolfBalance.StepSeconds;

        // 球の中心が壁の線の真上にあるとき（向きが決められない）の扱いの境目
        private const float Epsilon = 1e-6f;

        private readonly GolfHoleDef _hole;

        // 速すぎてカップの上を通っている球。抜けたときに「縁で曲がった」を1回だけ出す
        private readonly HashSet<GolfBall> _overCup = new HashSet<GolfBall>();

        public GolfBallSim(GolfHoleDef hole)
        {
            _hole = hole;
        }

        /// <summary>全部の球を1歩進める。起きたことは events に足す</summary>
        public void Step(IReadOnlyList<GolfBall> balls, List<GolfEvent> events)
        {
            foreach (var ball in balls)
            {
                if (ball.IsInPlay)
                {
                    StepBall(ball, events);
                }
            }

            CollideBalls(balls, events);
        }

        /// <summary>
        /// 全部の球が止まったか。止まっていても坂に押されて動き出す球は止まっていない扱い（次の歩で動く）
        /// </summary>
        public bool AllStopped(IReadOnlyList<GolfBall> balls)
        {
            foreach (var ball in balls)
            {
                if (ball.IsMoving || (ball.IsInPlay && WillSlide(ball)))
                {
                    return false;
                }
            }

            return true;
        }

        private bool WillSlide(GolfBall ball)
        {
            var ground = SampleGround(ball.Position);
            return ground.Push.Length > ground.Friction;
        }

        private void StepBall(GolfBall ball, List<GolfEvent> events)
        {
            if (ball.IsAirborne)
            {
                StepAir(ball, events);
                CollideWalls(ball, events);
                return;
            }

            var ground = SampleGround(ball.Position);
            if (ground.Hazard)
            {
                Fall(ball, events);
                return;
            }

            if (TryCup(ball, events))
            {
                return;
            }

            if (ground.Jump != null && ball.Velocity.Length >= GolfBalance.JumpMinSpeed)
            {
                ball.VerticalSpeed = ground.Jump.Launch;
                events.Add(new GolfEvent(GolfEventKind.Jump, ball.Player, ball.Position, ground.Jump.Launch));
            }

            Roll(ball, ground.Push, ground.Friction);
            ball.Position += ball.Velocity * Dt;
            CollideWalls(ball, events);
        }

        /// <summary>球の下の地面がどうなっているか（摩擦・坂の押す力・落ちる所か・ジャンプ台か）</summary>
        private GroundSample SampleGround(Vec2 position)
        {
            var sample = new GroundSample { Friction = GolfBalance.RollDecel, Hazard = !_hole.OnGround(position) };
            foreach (var area in _hole.Areas)
            {
                if (!area.Contains(position))
                {
                    continue;
                }

                switch (area.Kind)
                {
                    case GolfAreaKind.Water:
                    case GolfAreaKind.OutOfBounds:
                        sample.Hazard = true;
                        break;
                    case GolfAreaKind.Sand:
                        sample.Friction = GolfBalance.RollDecel * GolfBalance.SandDecelScale;
                        break;
                    case GolfAreaKind.Slope:
                        sample.Push += area.Push;
                        break;
                    case GolfAreaKind.Jump:
                        sample.Jump = area;
                        break;
                }
            }

            return sample;
        }

        /// <summary>
        /// 坂に押され、摩擦で遅くなる。止まっている球は、坂の押す力が摩擦より強いときだけ動き出す
        /// （摩擦より弱い坂の上で、止まったり動いたりを繰り返さないように）
        /// </summary>
        private static void Roll(GolfBall ball, Vec2 push, float friction)
        {
            var velocity = ball.Velocity;
            if (velocity.IsNearlyZero && push.Length <= friction)
            {
                ball.Velocity = Vec2.Zero;
                return;
            }

            velocity += push * Dt;
            var speed = velocity.Length;
            var drop = friction * Dt;
            velocity = speed <= drop ? Vec2.Zero : velocity * ((speed - drop) / speed);
            ball.Velocity = velocity.ClampLength(GolfBalance.MaxBallSpeed);
        }

        /// <summary>浮いている間は簡単な放物線。摩擦・坂・カップは効かない</summary>
        private static void StepAir(GolfBall ball, List<GolfEvent> events)
        {
            ball.VerticalSpeed -= GolfBalance.Gravity * Dt;
            ball.Height += ball.VerticalSpeed * Dt;
            ball.Position += ball.Velocity * Dt;
            if (ball.Height > 0f)
            {
                return;
            }

            ball.Height = 0f;
            ball.VerticalSpeed = 0f;
            events.Add(new GolfEvent(GolfEventKind.Land, ball.Player, ball.Position));
        }

        /// <summary>
        /// カップの上を通るとき、遅ければ入る。速すぎると縁に引かれて曲がり、通り過ぎる
        /// </summary>
        private bool TryCup(GolfBall ball, List<GolfEvent> events)
        {
            var toCup = _hole.Cup - ball.Position;
            if (toCup.Length > GolfBalance.CupRadius)
            {
                if (_overCup.Remove(ball))
                {
                    events.Add(new GolfEvent(GolfEventKind.LipOut, ball.Player, ball.Position));
                }

                return false;
            }

            var speed = ball.Velocity.Length;
            if (speed < GolfBalance.CupMaxSpeed)
            {
                _overCup.Remove(ball);
                ball.Place(_hole.Cup);
                ball.InCup = true;
                events.Add(new GolfEvent(GolfEventKind.CupIn, ball.Player, _hole.Cup));
                return true;
            }

            _overCup.Add(ball);
            var direction = ball.Velocity / speed;
            var lateral = toCup - direction * Vec2.Dot(toCup, direction);
            var bent = ball.Velocity + lateral.Normalized * (GolfBalance.LipPull * Dt);
            // 曲がるだけで速さは変えない（縁で加速しないように）
            ball.Velocity = bent.Normalized * speed;
            return false;
        }

        private void Fall(GolfBall ball, List<GolfEvent> events)
        {
            events.Add(new GolfEvent(GolfEventKind.Fell, ball.Player, ball.Position));
            _overCup.Remove(ball);
            ball.Place(ball.ReturnPoint);
        }

        private void CollideWalls(GolfBall ball, List<GolfEvent> events)
        {
            foreach (var wall in _hole.Walls)
            {
                if (ball.Height >= wall.Height)
                {
                    continue;
                }

                var closest = GolfGeometry.ClosestOnSegment(wall.A, wall.B, ball.Position);
                var offset = ball.Position - closest;
                var distance = offset.Length;
                if (distance >= GolfBalance.BallRadius)
                {
                    continue;
                }

                var normal = distance > Epsilon ? offset / distance : FacingNormal(wall, ball.Velocity);
                ball.Position = closest + normal * GolfBalance.BallRadius;
                var into = Vec2.Dot(ball.Velocity, normal);
                if (into >= 0f)
                {
                    continue;
                }

                ball.Velocity -= normal * (into * (1f + wall.Bounce));
                ball.Velocity = ball.Velocity.ClampLength(GolfBalance.MaxBallSpeed);
                if (-into >= GolfBalance.HitEventMinSpeed)
                {
                    events.Add(new GolfEvent(GolfEventKind.WallHit, ball.Player, closest, -into));
                }
            }
        }

        /// <summary>壁の線の上に中心が乗ってしまったとき、来た側へ押し返す向き</summary>
        private static Vec2 FacingNormal(GolfWall wall, Vec2 velocity)
        {
            var along = (wall.B - wall.A).Normalized;
            var normal = new Vec2(-along.Y, along.X);
            return Vec2.Dot(velocity, normal) > 0f ? -normal : normal;
        }

        /// <summary>球どうしは同じ重さで弾き合う（打っていない人の球も動く）。カップの中の球・高さの違う球は当たらない</summary>
        private static void CollideBalls(IReadOnlyList<GolfBall> balls, List<GolfEvent> events)
        {
            const float minDistance = GolfBalance.BallRadius * 2f;
            for (var i = 0; i < balls.Count; i++)
            {
                for (var j = i + 1; j < balls.Count; j++)
                {
                    var a = balls[i];
                    var b = balls[j];
                    if (!a.IsInPlay || !b.IsInPlay || Math.Abs(a.Height - b.Height) >= minDistance)
                    {
                        continue;
                    }

                    var offset = b.Position - a.Position;
                    if (offset.LengthSquared >= minDistance * minDistance)
                    {
                        continue;
                    }

                    var distance = offset.Length;
                    var normal = distance > Epsilon ? offset / distance : Vec2.Right;
                    var push = normal * ((minDistance - distance) * 0.5f);
                    a.Position -= push;
                    b.Position += push;

                    var closing = Vec2.Dot(b.Velocity - a.Velocity, normal);
                    if (closing >= 0f)
                    {
                        continue;
                    }

                    var impulse = normal * (-(1f + GolfBalance.BallRestitution) * closing * 0.5f);
                    a.Velocity -= impulse;
                    b.Velocity += impulse;
                    if (-closing >= GolfBalance.HitEventMinSpeed)
                    {
                        events.Add(new GolfEvent(GolfEventKind.BallHit, a.Player, a.Position + normal * GolfBalance.BallRadius, -closing, b.Player));
                    }
                }
            }
        }

        private struct GroundSample
        {
            public float Friction;
            public Vec2 Push;
            public bool Hazard;
            public GolfArea Jump;
        }
    }
}
