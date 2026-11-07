using System;
using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// 打球の転がりと放物線（Unity の物理は使わない）。打ってから結果が決まるまで:
    /// 浮いた球は放物線で飛び（浮いている間は穴に落ちない）、フェンスより高く越えたらホームラン・低ければ跳ね返る。
    /// 転がる球は摩擦とすり鉢の引きで遅くなり、穴の上を遅く通れば落ち（速いと縁で曲がって通り過ぎる）、穴に入らずに止まったら止まった場所の札で決まる。
    /// ファウルラインの外へ出たらファウル
    /// </summary>
    public sealed class BaseballBallSim
    {
        private readonly BaseballFieldDef _field;
        private readonly HashSet<int> _overHoles = new HashSet<int>();
        private float _elapsed;

        public BaseballBallSim(BaseballFieldDef field)
        {
            _field = field;
        }

        public bool IsActive { get; private set; }

        public Vec2 Position { get; internal set; }
        public Vec2 Velocity { get; internal set; }

        /// <summary>地面からの高さ（球の底。0 なら転がっている）</summary>
        public float Height { get; internal set; }

        public float VerticalSpeed { get; internal set; }

        public bool IsFlying => Height > 0f || VerticalSpeed > 0f;

        /// <summary>決まった結果（決まるまでは null）</summary>
        public BaseballHitResult? Result { get; private set; }

        /// <summary>穴に入ったときの穴の番号（入っていなければ −1）</summary>
        public int HoleIndex { get; private set; } = -1;

        public void Launch(Vec2 from, BaseballHit hit)
        {
            Position = from;
            Velocity = hit.Direction * hit.Speed;
            Height = 0f;
            VerticalSpeed = hit.VerticalSpeed;
            Result = null;
            HoleIndex = -1;
            _overHoles.Clear();
            _elapsed = 0f;
            IsActive = true;
        }

        public void Stop()
        {
            IsActive = false;
        }

        /// <summary>1歩（dt 秒）進める。速い球が穴をすり抜けないよう細かく分けて進める</summary>
        public void Step(float dt, List<BaseballEvent> events)
        {
            if (!IsActive)
            {
                return;
            }

            _elapsed += dt;
            var substeps = Math.Max(1, (int)MathF.Ceiling(Velocity.Length * dt / BaseballBalance.BallSubstepLength));
            var sub = dt / substeps;
            for (var i = 0; i < substeps && IsActive; i++)
            {
                SubStep(sub, events);
            }

            if (IsActive && _elapsed >= BaseballBalance.MaxBallSeconds)
            {
                Settle(events);
            }
        }

        private void SubStep(float dt, List<BaseballEvent> events)
        {
            if (IsFlying)
            {
                Fly(dt, events);
            }
            else
            {
                Roll(dt);
            }

            if (!IsActive || CheckFence(events) || CheckFoul(events))
            {
                return;
            }

            if (!IsFlying && (TryHoles(dt, events) || TryStop(events)))
            {
                return;
            }
        }

        private void Fly(float dt, List<BaseballEvent> events)
        {
            Position += Velocity * dt;
            VerticalSpeed -= BaseballBalance.Gravity * dt;
            Height += VerticalSpeed * dt;
            if (Height > 0f)
            {
                return;
            }

            // 着いた所がファウルの溝ならそこでファウル（CheckFoul が見る）。跳ねる勢いが残っていれば小さく跳ねる
            var falling = -VerticalSpeed;
            Height = 0f;
            Velocity *= BaseballBalance.LandKeep;
            var bounce = falling * BaseballBalance.LandBounce;
            VerticalSpeed = bounce >= BaseballBalance.MinBounceSpeed ? bounce : 0f;
            events.Add(new BaseballEvent(BaseballEventKind.Land, Position, falling));
        }

        private void Roll(float dt)
        {
            var speed = Velocity.Length;
            var pull = (_field.BowlCenter - Position).Normalized * (BaseballBalance.BowlPull * dt);
            var slowed = Math.Max(speed - BaseballBalance.RollFriction * dt, 0f);
            Velocity = (speed > 0f ? Velocity * (slowed / speed) : Vec2.Zero) + pull;
            Position += Velocity * dt;
        }

        /// <summary>フェンス（円弧）。浮いた球が高く越えたらホームラン、低ければ跳ね返す</summary>
        private bool CheckFence(List<BaseballEvent> events)
        {
            var limit = _field.FenceRadius - BaseballBalance.BallRadius;
            var distance = Position.Length;
            if (distance < limit || !_field.IsFair(Position))
            {
                return false;
            }

            if (Height >= _field.FenceHeight)
            {
                Finish(BaseballHitResult.HomeRun, -1);
                events.Add(new BaseballEvent(BaseballEventKind.HomeRun, Position, result: BaseballHitResult.HomeRun));
                return true;
            }

            var normal = Position / distance;
            var into = Vec2.Dot(Velocity, normal);
            Position = normal * limit;
            if (into > 0f)
            {
                // フェンスへ向かう分を反対へ返し、全体の勢いを落とす
                Velocity = (Velocity - normal * (2f * into)) * BaseballBalance.FenceBounce;
                events.Add(new BaseballEvent(BaseballEventKind.FenceHit, Position, into));
            }

            return false;
        }

        /// <summary>
        /// ファウルラインの外（ホームの後ろも）。浮いている球は着くまで待つ（ラインの上を飛んで戻ってくることがあるため）。
        /// ホームのすぐそばはまだ決めない（打った直後の球はホームの横にいる）
        /// </summary>
        private bool CheckFoul(List<BaseballEvent> events)
        {
            if (IsFlying || Position.Length < BaseballBalance.FoulGraceDistance || _field.IsFair(Position))
            {
                return false;
            }

            Finish(BaseballHitResult.Foul, -1);
            events.Add(new BaseballEvent(BaseballEventKind.Foul, Position, result: BaseballHitResult.Foul));
            return true;
        }

        /// <summary>穴の上を通るとき、遅ければ落ちる。速すぎると縁に引かれて曲がり、通り過ぎる（ゴルフのカップと同じ形）</summary>
        private bool TryHoles(float dt, List<BaseballEvent> events)
        {
            for (var i = 0; i < _field.Holes.Count; i++)
            {
                var hole = _field.Holes[i];
                var toCenter = hole.Center - Position;
                if (toCenter.Length > hole.Radius)
                {
                    if (_overHoles.Remove(i))
                    {
                        events.Add(new BaseballEvent(BaseballEventKind.LipOut, Position, index: i, result: hole.Result));
                    }

                    continue;
                }

                var speed = Velocity.Length;
                if (speed < BaseballBalance.HoleCaptureSpeed)
                {
                    Position = hole.Center;
                    Finish(hole.Result, i);
                    events.Add(new BaseballEvent(BaseballEventKind.HoleIn, hole.Center, speed, index: i, result: hole.Result));
                    return true;
                }

                _overHoles.Add(i);
                var direction = Velocity / speed;
                var lateral = toCenter - direction * Vec2.Dot(toCenter, direction);
                // 曲がるだけで速さは変えない（縁で加速しないように）
                Velocity = (Velocity + lateral.Normalized * (BaseballBalance.HoleLipPull * dt)).Normalized * speed;
            }

            return false;
        }

        private bool TryStop(List<BaseballEvent> events)
        {
            if (Velocity.Length >= BaseballBalance.StopSpeed)
            {
                return false;
            }

            Settle(events);
            return true;
        }

        /// <summary>その場で止めて、止まった場所の札で決める（ホームの後ろ・ファウルの溝ならファウル）</summary>
        private void Settle(List<BaseballEvent> events)
        {
            Velocity = Vec2.Zero;
            Height = 0f;
            VerticalSpeed = 0f;
            if (!_field.IsFair(Position))
            {
                Finish(BaseballHitResult.Foul, -1);
                events.Add(new BaseballEvent(BaseballEventKind.Foul, Position, result: BaseballHitResult.Foul));
                return;
            }

            var result = _field.StopResult(Position);
            Finish(result, -1);
            events.Add(new BaseballEvent(BaseballEventKind.Stop, Position, result: result));
        }

        private void Finish(BaseballHitResult result, int hole)
        {
            Result = result;
            HoleIndex = hole;
            IsActive = false;
        }
    }
}
