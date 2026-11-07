using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;

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
            SweepBall(holder.Position, holder.Position + holder.Facing * SoccerBalance.DribbleDistance);
            // 跳ねる速さは持たせず、離したときに子の歩く勢いで転がり出すように
            Ball.Velocity = holder.Velocity;
        }

        /// <summary>
        /// 持っている子が押した瞬間からためて、離したら蹴る（持つ前から押しっぱなしのボタンではためない。
        /// スライディングで奪ったあと、押したままのボタンを離した瞬間に勝手に蹴らないように）
        /// </summary>
        private void UpdateShot(SoccerPlayer player, SoccerInput input, bool pressed, List<SoccerEvent> events)
        {
            if (player.IsCharging)
            {
                if (input.Shoot)
                {
                    player.ChargeSeconds += Dt;
                }
                else
                {
                    Kick(player, input.Move, events);
                }

                return;
            }

            if (pressed)
            {
                player.IsCharging = true;
                player.ChargeSeconds = Dt;
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
            Release(player, direction, speed, lofted ? SoccerBalance.LoftUpSpeed : 0f);
            events.Add(new SoccerEvent(SoccerEventKind.Kick, player.Index, player.Team, Ball.Position, charge, lofted));
        }

        /// <summary>持っているボールを direction へ speed で蹴り出す（シュート・パス共通）。蹴った子は少しの間また持てない</summary>
        private void Release(SoccerPlayer player, Vec2 direction, float speed, float verticalSpeed)
        {
            player.Facing = direction;
            player.ResetCharge();
            Ball.Holder = SoccerBall.NoPlayer;
            SweepBall(player.Position, player.Position + direction * SoccerBalance.DribbleDistance);
            Ball.Velocity = direction * speed;
            Ball.VerticalSpeed = verticalSpeed;
            Ball.LastToucher = player.Index;
            Ball.LockedPlayer = player.Index;
            Ball.LockSeconds = SoccerBalance.RepickLockSeconds;
            Ball.IdleSeconds = 0f;
            _lastPasser = SoccerBall.NoPlayer;
            _trajectoryChanged = true;
        }

        /// <summary>
        /// 離れたボールが通った線（from → 今）に届く子のうち、ボールが先に通りかかった子が持つ
        /// （速いパスが歩と歩の間で子をすり抜けないように、この歩で通った線で測る）。頭より高い球・蹴った直後の本人・転んでいる子・点が入った後は持てない。
        /// すべっている子が離れたボールを拾ったら、すべりは成功で終わる。飛んでいる必殺シュートは誰も持てない
        /// </summary>
        private void TryPickup(Vec2 from, List<SoccerEvent> events)
        {
            if (Ball.LockSeconds > 0f)
            {
                Ball.LockSeconds -= Dt;
                if (Ball.LockSeconds <= 0f)
                {
                    Ball.LockedPlayer = SoccerBall.NoPlayer;
                }
            }

            if (Ball.NoPickupSeconds > 0f)
            {
                Ball.NoPickupSeconds -= Dt;
                return;
            }

            if (Ball.IsHeld || IsGoalScored || Special != null || Ball.Height > SoccerBalance.PickupMaxHeight)
            {
                return;
            }

            var picker = NearestPicker(from, Ball.Position);
            if (picker == null)
            {
                return;
            }

            if (picker.State == SoccerPlayerState.Slide)
            {
                picker.Enter(SoccerPlayerState.Normal);
            }

            Ball.Holder = picker.Index;
            Ball.LastToucher = picker.Index;
            Ball.Height = 0f;
            Ball.VerticalSpeed = 0f;
            Ball.IdleSeconds = 0f;
            GaugeOnPossess(picker);
            events.Add(new SoccerEvent(SoccerEventKind.Possess, picker.Index, picker.Team, Ball.Position));
        }

        /// <summary>ボールが通った線（from → to）に届く子のうち、ボールが先に通りかかった子（同じなら近い子）。いなければ null</summary>
        private SoccerPlayer NearestPicker(Vec2 from, Vec2 to)
        {
            SoccerPlayer best = null;
            var bestAlong = float.MaxValue;
            var bestDistance = float.MaxValue;
            foreach (var player in _players)
            {
                if (player.Index == Ball.LockedPlayer || player.IsDown)
                {
                    continue;
                }

                var closest = MiniGameGeometry.ClosestOnSegment(from, to, player.Position);
                var distance = Vec2.Distance(closest, player.Position);
                var along = Vec2.Distance(from, closest);
                var earlier = along < bestAlong - Epsilon || (along <= bestAlong + Epsilon && distance < bestDistance);
                if (distance <= SoccerBalance.PickupReach && earlier)
                {
                    best = player;
                    bestAlong = along;
                    bestDistance = distance;
                }
            }

            return best;
        }
    }
}
