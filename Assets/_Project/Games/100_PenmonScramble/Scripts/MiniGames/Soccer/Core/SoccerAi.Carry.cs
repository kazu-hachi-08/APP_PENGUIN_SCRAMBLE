using System;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// ボールを持っている AI の子: シュートが通る所ならためて蹴る、囲まれた・前の味方が空いていればパス、そうでなければ相手をよけてゴールへ運ぶ。
    /// シュートとパスを決めるのは考え直す歩だけ（弱い AI ほど決めるのが遅い）。運ぶ向きは毎歩決める
    /// </summary>
    public sealed partial class SoccerAi
    {
        private SoccerInput Carry(SoccerWorld world, SoccerPlayer player, SoccerAiBrain brain, SoccerAiLevel level, bool decide)
        {
            if (brain.Charging)
            {
                // ためている間もその向きへ歩き、ためきったら離して蹴る（離した歩のスティックの向きへ飛ぶ）
                var keepCharging = brain.ChargeLeft > 0f;
                brain.Charging = keepCharging;
                return new SoccerInput(brain.Aim, keepCharging);
            }

            if (decide)
            {
                if (_usesSpecial[player.Team] && world.IsSpecialReady(player.Team) && CanShoot(world, player, level, out var specialAim, out _))
                {
                    // 必殺シュートは狙いを付けなくても口の内側へ飛ぶので、ゴールの方へ倒すだけ
                    return new SoccerInput((specialAim - player.Position).Normalized, false, false, true);
                }

                if (TryShoot(world, player, brain, level))
                {
                    return new SoccerInput(brain.Aim, true);
                }

                if (TryPass(world, player, level, out var passDirection))
                {
                    return new SoccerInput(passDirection, false, true);
                }
            }

            return new SoccerInput(DribbleDirection(world, player), false);
        }

        /// <summary>シュートが通る所なら、キーパーのいない側の柱の内側を狙ってため始める</summary>
        private bool TryShoot(SoccerWorld world, SoccerPlayer player, SoccerAiBrain brain, SoccerAiLevel level)
        {
            if (!CanShoot(world, player, level, out var aimPoint, out var distance))
            {
                return false;
            }

            var error = Triangle() * level.AimErrorDegrees * MathF.PI / 180f;
            brain.Aim = (aimPoint - player.Position).Normalized.Rotated(error);
            var charge = Math.Min(SoccerBalance.AiChargeMin + distance * SoccerBalance.AiChargePerMeter, SoccerBalance.AiChargeMax);
            brain.ChargeLeft = charge * SoccerBalance.ShotChargeSeconds;
            brain.Charging = true;
            return true;
        }

        /// <summary>
        /// シュートを蹴る所か: 届く距離でコースが空いている、またはゴールのすぐ前・キーパーが迫っている。
        /// ゴールの線の近くで角度が無い所では蹴らない。aimPoint はキーパーのいない側の柱の内側
        /// </summary>
        private bool CanShoot(SoccerWorld world, SoccerPlayer player, SoccerAiLevel level, out Vec2 aimPoint, out float distance)
        {
            var goal = world.Pitch.GoalOf(SoccerRules.Opponent(player.Team));
            var ball = world.Ball.Position;
            distance = Vec2.Distance(ball, goal.Center);
            aimPoint = goal.Center;
            var depth = -goal.DepthPastLine(ball);
            if (depth < SoccerBalance.AiNoAngleDepth && Math.Abs(ball.Y) >= goal.HalfWidth)
            {
                return false;
            }

            aimPoint = ShotTarget(world, goal);
            var close = distance <= SoccerBalance.AiForceShootDistance || KeeperDistance(world, goal.Team, ball) <= SoccerBalance.AiKeeperPanicDistance;
            var open = distance <= level.ShootRange && LaneClear(world, player.Team, ball, aimPoint, SoccerBalance.AiShotLaneClearance);
            return close || open;
        }

        /// <summary>キーパーが立っていない側の柱の少し内側（真ん中にいれば、どちらかを乱数で）</summary>
        private Vec2 ShotTarget(SoccerWorld world, SoccerGoal goal)
        {
            var keeperY = world.Keepers.Count > goal.Team ? world.Keepers[goal.Team].Position.Y : 0f;
            var side = Math.Abs(keeperY) < SoccerBalance.BallRadius ? (Roll(0.5f) ? 1f : -1f) : -Math.Sign(keeperY);
            return new Vec2(goal.LineX, side * (goal.HalfWidth - SoccerBalance.AiShotPostInset));
        }

        private static float KeeperDistance(SoccerWorld world, int keeperTeam, Vec2 ball)
        {
            return world.Keepers.Count > keeperTeam ? Vec2.Distance(world.Keepers[keeperTeam].Position, ball) : float.MaxValue;
        }

        /// <summary>
        /// 囲まれていればコースの空いた味方へ、囲まれていなくても前の空いた味方がいれば（賢さの割合で）パスする。
        /// 向きはその味方へ向ける（SoccerWorld はスティックの向きの扇の中の一番近い味方へ出す）
        /// </summary>
        private bool TryPass(SoccerWorld world, SoccerPlayer player, SoccerAiLevel level, out Vec2 direction)
        {
            direction = Vec2.Zero;
            var mate = BestPassTarget(world, player, out var score);
            if (mate == null)
            {
                return false;
            }

            var pressured = NearestOpponentDistance(world, player.Team, player.Position, out _) <= SoccerBalance.AiPressureRadius;
            var worth = score >= SoccerBalance.AiPassWorthScore && Roll(level.PassSense);
            if (!pressured && !worth)
            {
                return false;
            }

            direction = (mate.Position - player.Position).Normalized;
            return true;
        }

        /// <summary>コースが空いている味方のうち、前へ進む距離と受ける子のまわりの空きが一番良い子</summary>
        private static SoccerPlayer BestPassTarget(SoccerWorld world, SoccerPlayer passer, out float bestScore)
        {
            SoccerPlayer best = null;
            bestScore = float.MinValue;
            foreach (var mate in world.Players)
            {
                if (mate.Team != passer.Team || mate.Index == passer.Index || mate.IsDown)
                {
                    continue;
                }

                var distance = Vec2.Distance(mate.Position, passer.Position);
                if (distance < SoccerBalance.AiPassMinDistance || distance > SoccerBalance.AiPassMaxDistance)
                {
                    continue;
                }

                if (!LaneClear(world, passer.Team, passer.Position, mate.Position, SoccerBalance.AiPassLaneClearance))
                {
                    continue;
                }

                var open = Math.Min(NearestOpponentDistance(world, passer.Team, mate.Position, out _), SoccerBalance.AiPassOpenCap);
                var score = Forward(passer.Team, mate.Position) - Forward(passer.Team, passer.Position) + open * SoccerBalance.AiPassOpenWeight;
                if (score > bestScore)
                {
                    best = mate;
                    bestScore = score;
                }
            }

            return best;
        }

        /// <summary>相手のゴールへ向かいつつ、前にいる相手を横へよけ、縦の壁に寄りすぎたら内へ戻る</summary>
        private static Vec2 DribbleDirection(SoccerWorld world, SoccerPlayer player)
        {
            var goal = world.Pitch.GoalOf(SoccerRules.Opponent(player.Team));
            var desired = (goal.Center - player.Position).Normalized;
            var steer = desired;
            foreach (var other in world.Players)
            {
                if (other.Team == player.Team || other.IsDown)
                {
                    continue;
                }

                var to = other.Position - player.Position;
                var distance = to.Length;
                if (distance >= SoccerBalance.AiDribbleAvoidRadius || Vec2.Dot(to, desired) <= 0f)
                {
                    continue;
                }

                // 相手のいない側へ、近いほど強く曲がる
                var side = desired.X * to.Y - desired.Y * to.X >= 0f ? -1f : 1f;
                var weight = (SoccerBalance.AiDribbleAvoidRadius - distance) / SoccerBalance.AiDribbleAvoidRadius * SoccerBalance.AiDribbleAvoidWeight;
                steer += new Vec2(-desired.Y, desired.X) * (side * weight);
            }

            var edge = world.Pitch.HalfWidth - Math.Abs(player.Position.Y);
            if (edge < SoccerBalance.AiWallAvoidDistance)
            {
                steer += new Vec2(0f, -Math.Sign(player.Position.Y) * SoccerBalance.AiWallAvoidWeight);
            }

            return steer.IsNearlyZero ? desired : steer.Normalized;
        }
    }
}
