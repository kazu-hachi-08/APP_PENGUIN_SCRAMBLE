using System;
using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// 球の動きの計算。GolfBalance.StepSeconds ずつ進める（何歩進めても同じ打ち方なら同じ結果）。
    /// 全部を本当の3Dにせず、地面の上の2次元と「浮いている間だけの高さ」で計算する
    /// （純C#で確かめやすく、オンラインで全員の見え方をそろえやすいため）。
    /// 動く物・間欠泉は時刻（ホールが始まってからの秒）だけで決まるので、Step に時刻を渡す
    /// </summary>
    public sealed class GolfBallSim
    {
        private const float Dt = GolfBalance.StepSeconds;

        // 球の中心が壁の線の真上にあるとき（向きが決められない）の扱いの境目
        private const float Epsilon = 1e-6f;

        // 壁に押しつけられた球を止まった扱いにする速さの上限と、壁に触れているとみなす隙間
        private const float PinnedSpeed = 0.08f;
        private const float PinnedReach = 0.02f;

        private readonly GolfHoleDef _hole;

        // 速すぎてカップの上を通っている球。抜けたときに「縁で曲がった」を1回だけ出す
        private readonly HashSet<GolfBall> _overCup = new HashSet<GolfBall>();

        // この歩の時刻と、その時刻の動く物の壁（球ごとに作り直さないよう、歩の始めに1回だけ置く）
        private readonly List<MovingWall> _movingWalls = new List<MovingWall>();
        private float _time;

        public GolfBallSim(GolfHoleDef hole)
        {
            _hole = hole;
        }

        /// <summary>全部の球を1歩進める。起きたことは events に足す</summary>
        /// <param name="time">この歩の時刻（ホールが始まってからの秒）。動く物・間欠泉の様子を決める</param>
        public void Step(IReadOnlyList<GolfBall> balls, List<GolfEvent> events, float time = 0f)
        {
            SetTime(time);
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
        public bool AllStopped(IReadOnlyList<GolfBall> balls, float time = 0f)
        {
            SetTime(time);
            foreach (var ball in balls)
            {
                if (!ball.IsInPlay || IsPinned(ball))
                {
                    continue;
                }

                if (ball.IsMoving || WillSlide(ball))
                {
                    return false;
                }
            }

            return true;
        }

        private bool WillSlide(GolfBall ball)
        {
            var ground = SampleGround(ball, ball.Position);
            return ground.Push.Length > ground.Friction;
        }

        /// <summary>
        /// 坂・押し流す帯に壁へ押しつけられて、ほとんど動いていない球（止まった扱い）。
        /// 壁に押しつけられた球は、壁で跳ね返されては押し戻されるのをくり返し、速さがちょうど 0 にならないため
        /// </summary>
        private bool IsPinned(GolfBall ball)
        {
            if (ball.IsAirborne || ball.Velocity.Length > PinnedSpeed)
            {
                return false;
            }

            var ground = SampleGround(ball, ball.Position);
            if (ground.Push.Length <= ground.Friction)
            {
                return false;
            }

            foreach (var wall in _hole.Walls)
            {
                var offset = ball.Position - MiniGameGeometry.ClosestOnSegment(wall.A, wall.B, ball.Position);
                if (offset.Length > GolfBalance.BallRadius + PinnedReach || offset.Length <= Epsilon)
                {
                    continue;
                }

                var normal = offset.Normalized;
                var into = Vec2.Dot(ground.Push, normal);
                var along = ground.Push - normal * into;
                if (into < 0f && along.Length <= ground.Friction)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 止まった球が池・場外の上にあれば落とす（凍った道の上で止まった球は、氷が消えると池に落ちる）。
        /// 打順の終わりに呼ぶ。起きたことは events に足す
        /// </summary>
        public void SettleHazards(IReadOnlyList<GolfBall> balls, List<GolfEvent> events)
        {
            foreach (var ball in balls)
            {
                if (ball.IsInPlay && IsHazard(ball.Position))
                {
                    Fall(ball, events);
                }
            }
        }

        /// <summary>
        /// 止まった球が動く物の通る範囲に掛かっていれば、範囲の外へよける（打順の終わりに呼ぶ）。
        /// 狙っている間は球を動かさないので、動く物が止まった球をすり抜けて見えないように
        /// </summary>
        public void ClearSweeps(IReadOnlyList<GolfBall> balls, List<GolfEvent> events)
        {
            if (_hole.Movers.Count == 0)
            {
                return;
            }

            foreach (var ball in balls)
            {
                if (ball.IsInPlay && MoveOutOfSweeps(ball))
                {
                    events.Add(new GolfEvent(GolfEventKind.Nudged, ball.Player, ball.Position));
                }
            }
        }

        /// <summary>範囲が重なっていても抜けられるよう、何回かくり返す。よけた先が壁に掛かれば壁から離す</summary>
        private bool MoveOutOfSweeps(GolfBall ball)
        {
            const int maxPasses = 4;
            var moved = false;
            for (var pass = 0; pass < maxPasses; pass++)
            {
                var pushed = false;
                foreach (var mover in _hole.Movers)
                {
                    if (mover.TryPushOutOfSweep(ball.Position, out var away))
                    {
                        ball.Position = away;
                        pushed = true;
                    }
                }

                if (!pushed)
                {
                    break;
                }

                moved = true;
                SeparateFromStaticWalls(ball);
            }

            return moved;
        }

        private void SeparateFromStaticWalls(GolfBall ball)
        {
            foreach (var wall in _hole.Walls)
            {
                var closest = MiniGameGeometry.ClosestOnSegment(wall.A, wall.B, ball.Position);
                var offset = ball.Position - closest;
                if (offset.Length < GolfBalance.BallRadius && offset.Length > Epsilon)
                {
                    ball.Position = closest + offset.Normalized * GolfBalance.BallRadius;
                }
            }
        }

        /// <summary>この位置が池・場外か（特殊ショットの効き目は見ない）</summary>
        public bool IsHazard(Vec2 position)
        {
            if (!_hole.OnGround(position))
            {
                return true;
            }

            foreach (var area in _hole.Areas)
            {
                if (area.IsHazard && area.Contains(position))
                {
                    return true;
                }
            }

            return false;
        }

        private void StepBall(GolfBall ball, List<GolfEvent> events)
        {
            Curve(ball);
            if (ball.IsAirborne)
            {
                StepAir(ball, events);
                CollideWalls(ball, events);
                return;
            }

            var ground = SampleGround(ball, ball.Position);
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
                var kind = ground.Jump.Kind == GolfAreaKind.Geyser ? GolfEventKind.Geyser : GolfEventKind.Jump;
                events.Add(new GolfEvent(kind, ball.Player, ball.Position, ground.Jump.Launch));
            }

            Roll(ball, ground.Push, ground.Friction);
            var step = ball.Velocity * Dt;
            ball.Position += step;
            MeltIce(ball, step.Length);
            CollideWalls(ball, events);
            TryWarp(ball, events);
        }

        /// <summary>ワープの泡: 地面を転がる球が入口に入ると、速さと向きはそのままで出口へ</summary>
        private void TryWarp(GolfBall ball, List<GolfEvent> events)
        {
            for (var i = 0; i < _hole.Warps.Count; i++)
            {
                var warp = _hole.Warps[i];
                if (!warp.Catches(ball.Position))
                {
                    continue;
                }

                events.Add(new GolfEvent(GolfEventKind.Warp, ball.Player, warp.Entry, i));
                _overCup.Remove(ball);
                ball.Position = warp.Exit;
                return;
            }
        }

        /// <summary>
        /// 球の下の地面がどうなっているか（摩擦・坂の押す力・落ちる所か・ジャンプ台か）。
        /// 氷の特殊ショットの道の上では、池も砂地も凍って、よく滑る地面になる（場外はそのまま落ちる）
        /// </summary>
        private GroundSample SampleGround(GolfBall ball, Vec2 position)
        {
            var iced = ball.IceLeft > 0f;
            var friction = iced ? GolfBalance.RollDecel * GolfSpecialBalance.IceDecelScale : GolfBalance.RollDecel;
            var floor = GolfBalance.RollDecel * GolfBalance.IceFloorDecelScale;
            var sample = new GroundSample { Friction = friction, Hazard = !_hole.OnGround(position) };
            foreach (var area in _hole.Areas)
            {
                if (!area.Contains(position))
                {
                    continue;
                }

                switch (area.Kind)
                {
                    case GolfAreaKind.Water:
                        sample.Hazard |= !iced;
                        break;
                    case GolfAreaKind.OutOfBounds:
                        sample.Hazard = true;
                        break;
                    case GolfAreaKind.Sand:
                        if (!iced)
                        {
                            sample.Friction = GolfBalance.RollDecel * GolfBalance.SandDecelScale;
                        }

                        break;
                    case GolfAreaKind.Slope:
                    case GolfAreaKind.Flow:
                        sample.Push += area.Push;
                        break;
                    case GolfAreaKind.Ice:
                        sample.Friction = Math.Min(sample.Friction, floor);
                        break;
                    case GolfAreaKind.Jump:
                    case GolfAreaKind.Geyser:
                        if (area.LaunchesAt(_time))
                        {
                            sample.Jump = area;
                        }

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
        private void StepAir(GolfBall ball, List<GolfEvent> events)
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
            if (TrySkip(ball, events))
            {
                return;
            }

            events.Add(new GolfEvent(GolfEventKind.Land, ball.Player, ball.Position));
        }

        /// <summary>
        /// 水切り: 着いた所で、まだ跳ねる回数があればまた跳ぶ（池の上でも跳ねる＝水切り）。
        /// 場外（地面の外・溶岩など）では跳ねずに落ちる
        /// </summary>
        private bool TrySkip(GolfBall ball, List<GolfEvent> events)
        {
            if (ball.SkipsLeft <= 0 || OutOfBounds(ball.Position))
            {
                return false;
            }

            ball.SkipsLeft--;
            ball.VerticalSpeed = ball.SkipSpeed;
            ball.SkipSpeed *= GolfSpecialBalance.SkipVerticalKeep;
            ball.Velocity *= GolfSpecialBalance.SkipForwardKeep;
            var onWater = InArea(ball.Position, GolfAreaKind.Water);
            events.Add(new GolfEvent(GolfEventKind.Skip, ball.Player, ball.Position, onWater ? 1f : 0f));
            return true;
        }

        private bool OutOfBounds(Vec2 position) => !_hole.OnGround(position) || InArea(position, GolfAreaKind.OutOfBounds);

        private bool InArea(Vec2 position, GolfAreaKind kind)
        {
            foreach (var area in _hole.Areas)
            {
                if (area.Kind == kind && area.Contains(position))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>風: 進んだ距離に比べて向きを回す（球の速さは変えない）。地面でも空中でも曲がる</summary>
        private static void Curve(GolfBall ball)
        {
            if (ball.CurveLeft <= 0f || ball.Velocity.IsNearlyZero)
            {
                return;
            }

            var distance = Math.Min(ball.Velocity.Length * Dt, ball.CurveLeft);
            ball.Velocity = ball.Velocity.Rotated(ball.Curvature * distance);
            ball.CurveLeft -= distance;
        }

        /// <summary>氷: 地面を進んだ分だけ凍った道が短くなる</summary>
        private static void MeltIce(GolfBall ball, float distance)
        {
            if (ball.IceLeft > 0f)
            {
                ball.IceLeft = Math.Max(0f, ball.IceLeft - distance);
            }
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
                CollideWall(ball, wall, null, events);
            }

            foreach (var moving in _movingWalls)
            {
                CollideWall(ball, moving.Wall, moving.Mover, events);
            }
        }

        /// <summary>
        /// 壁1本との当たり。動く壁（mover が null でない）なら、壁の動く速さに対して跳ね返る
        /// （止まった球も、動く壁に押されれば動き出す）
        /// </summary>
        private void CollideWall(GolfBall ball, GolfWall wall, GolfMover mover, List<GolfEvent> events)
        {
            if (ball.Height >= wall.Height)
            {
                return;
            }

            var closest = MiniGameGeometry.ClosestOnSegment(wall.A, wall.B, ball.Position);
            var offset = ball.Position - closest;
            var distance = offset.Length;
            if (distance >= GolfBalance.BallRadius)
            {
                return;
            }

            var wallVelocity = mover?.VelocityAt(closest, _time) ?? Vec2.Zero;
            var normal = distance > Epsilon ? offset / distance : FacingNormal(wall, ball.Velocity - wallVelocity);
            ball.Position = closest + normal * GolfBalance.BallRadius;
            var into = Vec2.Dot(ball.Velocity - wallVelocity, normal);
            if (into >= 0f)
            {
                return;
            }

            ball.Velocity -= normal * (into * (1f + wall.Bounce));
            ball.Velocity = ball.Velocity.ClampLength(GolfBalance.MaxBallSpeed);
            if (-into >= GolfBalance.HitEventMinSpeed)
            {
                events.Add(new GolfEvent(GolfEventKind.WallHit, ball.Player, closest, -into));
            }
        }

        /// <summary>時刻を決めて、その時刻の動く物の壁を置き直す</summary>
        private void SetTime(float time)
        {
            _time = time;
            _movingWalls.Clear();
            foreach (var mover in _hole.Movers)
            {
                for (var i = 0; i < mover.LocalWalls.Count; i++)
                {
                    _movingWalls.Add(new MovingWall(mover.WallAt(i, time), mover));
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

        private readonly struct MovingWall
        {
            public readonly GolfWall Wall;
            public readonly GolfMover Mover;

            public MovingWall(GolfWall wall, GolfMover mover)
            {
                Wall = wall;
                Mover = mover;
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
