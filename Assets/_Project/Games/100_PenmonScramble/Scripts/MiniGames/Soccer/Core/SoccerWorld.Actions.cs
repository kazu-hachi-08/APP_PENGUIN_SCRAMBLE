using System;
using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// ボタンで出す技。持っているときはシュート・パス、持っていないときはスライディング（シュートのボタン）・ダッシュ（パスのボタン）。
    /// ボタンは「押しているか」で届くので、押した瞬間は前の歩と比べて決める。転ぶ・すべるの時間もここで進める
    /// </summary>
    public sealed partial class SoccerWorld
    {
        private static readonly float PassConeCos = MathF.Cos(SoccerBalance.PassConeDegrees * MathF.PI / 180f);

        // パスの先読みを何回くり返すか（受ける子が動いた先へ転がす。2回で十分そろう）
        private const int PassLeadIterations = 2;

        // パスの先読みの的をピッチの内側に収める余白（壁ぎわへ蹴り込まないように）
        private const float PassTargetMargin = 1f;

        private void HandleButtons(SoccerPlayer player, SoccerInput input, List<SoccerEvent> events)
        {
            var shootPressed = input.Shoot && !player.WasShooting;
            var passPressed = input.Pass && !player.WasPassing;
            var specialPressed = input.Special && !player.WasSpecial;
            player.WasShooting = input.Shoot;
            player.WasPassing = input.Pass;
            player.WasSpecial = input.Special;

            if (Ball.Holder == player.Index)
            {
                if (specialPressed && IsSpecialReady(player.Team) && !IsGoalScored)
                {
                    StartSpecial(player, input.Move, events);
                    return;
                }

                UpdateShot(player, input, shootPressed, events);
                if (passPressed && Ball.Holder == player.Index && !player.IsCharging)
                {
                    Pass(player, input.Move, events);
                }

                return;
            }

            player.ResetCharge();
            if (!player.CanAct)
            {
                return;
            }

            if (shootPressed && player.SlideCooldown <= 0f)
            {
                StartSlide(player, input.Move, events);
            }
            else if (passPressed && player.DashCooldown <= 0f)
            {
                player.DashSeconds = SoccerBalance.DashSeconds;
                player.DashCooldown = SoccerBalance.DashCooldownSeconds;
                events.Add(new SoccerEvent(SoccerEventKind.Dash, player.Index, player.Team, player.Position));
            }
        }

        /// <summary>転ぶ・すべる・待ちの時間を進め、終わったら次の状態へ</summary>
        private void TickPlayer(SoccerPlayer player, List<SoccerEvent> events)
        {
            player.StateSeconds += Dt;
            player.SlideCooldown = Math.Max(0f, player.SlideCooldown - Dt);
            player.DashCooldown = Math.Max(0f, player.DashCooldown - Dt);
            player.DashSeconds = Math.Max(0f, player.DashSeconds - Dt);
            switch (player.State)
            {
                case SoccerPlayerState.Slide when player.StateSeconds >= SoccerBalance.SlideSeconds:
                    player.Enter(SoccerPlayerState.Recover);
                    events.Add(new SoccerEvent(SoccerEventKind.SlideMiss, player.Index, player.Team, player.Position));
                    break;
                case SoccerPlayerState.Down when player.StateSeconds >= SoccerBalance.TackledDownSeconds:
                case SoccerPlayerState.Recover when player.StateSeconds >= SoccerBalance.SlideMissSeconds:
                case SoccerPlayerState.Frozen when player.StateSeconds >= player.FrozenSeconds:
                    player.Enter(SoccerPlayerState.Normal);
                    break;
            }
        }

        // ---- パス ----

        /// <summary>
        /// 向いている向き（スティック）の ±PassConeDegrees にいる味方のうち一番近い子の足元へ、着いたときに少し勢いが残る速さで転がす。
        /// 受ける子が走っていれば、その先へ転がす。いなければ向いている向きへ弱く蹴り出す
        /// </summary>
        private void Pass(SoccerPlayer player, Vec2 stick, List<SoccerEvent> events)
        {
            var aim = stick.IsNearlyZero ? player.Facing : stick.Normalized;
            var receiver = PassReceiver(player, aim);
            var from = player.Position + aim * SoccerBalance.DribbleDistance;
            var direction = aim;
            var speed = SoccerBalance.PassFallbackSpeed;
            if (receiver != null)
            {
                var target = LeadTarget(from, receiver);
                var toTarget = target - from;
                direction = toTarget.IsNearlyZero ? aim : toTarget.Normalized;
                speed = PassSpeed(toTarget.Length);
            }

            Release(player, direction, speed, 0f);
            _lastPasser = player.Index;
            var other = receiver?.Index ?? SoccerBall.NoPlayer;
            events.Add(new SoccerEvent(SoccerEventKind.Pass, player.Index, player.Team, Ball.Position, speed, false, other));
        }

        private SoccerPlayer PassReceiver(SoccerPlayer passer, Vec2 aim)
        {
            SoccerPlayer best = null;
            var bestDistance = float.MaxValue;
            foreach (var mate in _players)
            {
                if (mate.Team != passer.Team || mate.Index == passer.Index || mate.IsDown)
                {
                    continue;
                }

                var to = mate.Position - passer.Position;
                var distance = to.Length;
                if (distance < SoccerBalance.PassMinDistance || Vec2.Dot(to / distance, aim) < PassConeCos)
                {
                    continue;
                }

                if (distance < bestDistance)
                {
                    best = mate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <summary>受ける子が今の速さのまま走ったときに、ボールと出会う所（ピッチの内側に収める）</summary>
        private Vec2 LeadTarget(Vec2 from, SoccerPlayer receiver)
        {
            var target = receiver.Position;
            for (var i = 0; i < PassLeadIterations; i++)
            {
                var distance = Vec2.Distance(from, target);
                var speed = PassSpeed(distance);
                var seconds = distance / ((speed + SoccerBalance.PassArriveSpeed) * 0.5f);
                target = receiver.Position + receiver.Velocity * seconds;
            }

            var limitX = Pitch.HalfLength - PassTargetMargin;
            var limitY = Pitch.HalfWidth - PassTargetMargin;
            return new Vec2(Math.Clamp(target.X, -limitX, limitX), Math.Clamp(target.Y, -limitY, limitY));
        }

        /// <summary>distance 先で PassArriveSpeed が残る速さ（一定の摩擦で減る）</summary>
        public static float PassSpeed(float distance)
        {
            var arrive = SoccerBalance.PassArriveSpeed;
            var speed = MathF.Sqrt(arrive * arrive + 2f * SoccerBalance.RollDecel * distance);
            return Math.Clamp(speed, SoccerBalance.PassMinSpeed, SoccerBalance.PassMaxSpeed);
        }

        // ---- スライディング ----

        /// <summary>スティックの向き（倒していなければ向いている向き）へすべる。すべる間は向きを変えられない</summary>
        private void StartSlide(SoccerPlayer player, Vec2 stick, List<SoccerEvent> events)
        {
            player.Facing = stick.IsNearlyZero ? player.Facing : stick.Normalized;
            player.Enter(SoccerPlayerState.Slide);
            player.SlideCooldown = SoccerBalance.SlideCooldownSeconds;
            player.DashSeconds = 0f;
            player.Velocity = player.Facing * SoccerBalance.SlideSpeed;
            events.Add(new SoccerEvent(SoccerEventKind.Slide, player.Index, player.Team, player.Position));
        }

        /// <summary>
        /// すべっている子が、持っている相手のボール（か体）に届いたら奪う。奪われた子は転ぶ。
        /// 奪った子のすべりは成功で終わる（起き上がりの待ちは無い）
        /// </summary>
        private void StealBySlides(List<SoccerEvent> events)
        {
            if (!Ball.IsHeldByPlayer)
            {
                return;
            }

            foreach (var slider in _players)
            {
                var holder = _players[Ball.Holder];
                if (slider.State != SoccerPlayerState.Slide || holder.Team == slider.Team || !Reaches(slider, holder))
                {
                    continue;
                }

                holder.ResetCharge();
                holder.Enter(SoccerPlayerState.Down);
                holder.DashSeconds = 0f;
                slider.Enter(SoccerPlayerState.Normal);
                Ball.Holder = slider.Index;
                Ball.LastToucher = slider.Index;
                Ball.IdleSeconds = 0f;
                _lastPasser = SoccerBall.NoPlayer;
                AddGauge(slider.Team, SoccerBalance.GaugeTackle);
                events.Add(new SoccerEvent(SoccerEventKind.Tackle, slider.Index, slider.Team, Ball.Position, 0f, false, holder.Index));
                return;
            }
        }

        private bool Reaches(SoccerPlayer slider, SoccerPlayer holder)
        {
            var bodyReach = SoccerBalance.PlayerRadius * 2f + SoccerBalance.BallRadius;
            return Vec2.Distance(slider.Position, Ball.Position) <= SoccerBalance.SlideStealReach
                || Vec2.Distance(slider.Position, holder.Position) <= bodyReach;
        }
    }
}
