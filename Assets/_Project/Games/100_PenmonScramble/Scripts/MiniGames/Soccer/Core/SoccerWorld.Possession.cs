using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>ボールを持つ（足元に吸い付く）・運ぶ・ためて蹴る</summary>
    public sealed partial class SoccerWorld
    {
        /// <summary>持っているボールは子の向いている向きの足元の少し前に付いてくる</summary>
        private void CarryBall(List<SoccerEvent> events)
        {
            if (!Ball.IsHeldByPlayer)
            {
                return;
            }

            var holder = _players[Ball.Holder];
            Ball.Position = holder.Position + holder.Facing * SoccerBalance.DribbleDistance;
            Ball.Velocity = Vec2.Zero;
            BounceBallOffWalls(events);
            // 跳ねる速さは持たせず、離したときに子の歩く勢いで転がり出すように
            Ball.Velocity = holder.Velocity;
        }

        /// <summary>持っている子がシュートを押している間ためて、離したら蹴る。持っていない子のためは消す</summary>
        private void UpdateShot(SoccerPlayer player, SoccerInput input, List<SoccerEvent> events)
        {
            if (Ball.Holder != player.Index)
            {
                player.ResetCharge();
                return;
            }

            if (input.Shoot)
            {
                player.IsCharging = true;
                player.ChargeSeconds += Dt;
                return;
            }

            if (player.IsCharging)
            {
                Kick(player, input.Move, events);
            }
        }

        /// <summary>
        /// ためた長さで強さを決めて蹴る。満タンなら浮いた球。向きはスティック（倒していなければ向いている向き）で、
        /// 相手のゴールの口へ少し寄せる
        /// </summary>
        private void Kick(SoccerPlayer player, Vec2 stick, List<SoccerEvent> events)
        {
            var charge = player.Charge01;
            var aim = stick.IsNearlyZero ? player.Facing : stick.Normalized;
            var direction = SoccerAim.Assist(player.Position, aim, Pitch.GoalOf(SoccerRules.Opponent(player.Team)));
            var speed = SoccerBalance.MinShotSpeed + (SoccerBalance.MaxShotSpeed - SoccerBalance.MinShotSpeed) * charge;
            var lofted = charge >= SoccerBalance.LoftCharge;

            player.Facing = direction;
            player.ResetCharge();
            Ball.Holder = SoccerBall.NoPlayer;
            Ball.Position = player.Position + direction * SoccerBalance.DribbleDistance;
            Ball.Velocity = direction * speed;
            Ball.VerticalSpeed = lofted ? SoccerBalance.LoftUpSpeed : 0f;
            Ball.LastToucher = player.Index;
            Ball.LockedPlayer = player.Index;
            Ball.LockSeconds = SoccerBalance.RepickLockSeconds;
            Ball.IdleSeconds = 0f;
            _trajectoryChanged = true;
            events.Add(new SoccerEvent(SoccerEventKind.Kick, player.Index, player.Team, Ball.Position, charge, lofted));
        }

        /// <summary>
        /// 離れたボールに一番近い子が、届く所にいれば持つ。頭より高い球・蹴った直後の本人・点が入った後は持てない
        /// </summary>
        private void TryPickup(List<SoccerEvent> events)
        {
            if (Ball.LockSeconds > 0f)
            {
                Ball.LockSeconds -= Dt;
                if (Ball.LockSeconds <= 0f)
                {
                    Ball.LockedPlayer = SoccerBall.NoPlayer;
                }
            }

            if (Ball.IsHeld || IsGoalScored || Ball.Height > SoccerBalance.PickupMaxHeight)
            {
                return;
            }

            var nearest = Nearest(Ball.Position, Ball.LockedPlayer);
            if (nearest == null || Vec2.Distance(nearest.Position, Ball.Position) > SoccerBalance.PickupReach)
            {
                return;
            }

            Ball.Holder = nearest.Index;
            Ball.LastToucher = nearest.Index;
            Ball.Height = 0f;
            Ball.VerticalSpeed = 0f;
            Ball.IdleSeconds = 0f;
            events.Add(new SoccerEvent(SoccerEventKind.Possess, nearest.Index, nearest.Team, Ball.Position));
        }
    }
}
