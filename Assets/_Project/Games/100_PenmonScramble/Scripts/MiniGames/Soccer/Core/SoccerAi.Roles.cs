using System;
using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// チームの作戦: ボールを取りに行く子（持っている子）を1体決め、残りの子に持ち場を配る。
    /// 持ち場の場所はボールの位置で毎歩動き、誰がどの持ち場かは少し間を空けて決め直す（ころころ入れ替わらないように）
    /// </summary>
    public sealed partial class SoccerAi
    {
        private enum Role
        {
            /// <summary>攻め: ボールより前の、ボールと反対の側（パスの受け手）</summary>
            Front,
            /// <summary>攻め: ボールの少し後ろ（戻しのパスの受け手・取られたときの備え）</summary>
            Support,
            /// <summary>攻め / 守り: ボールの側の横（4人のチーム用）</summary>
            Wing,
            /// <summary>攻め: 自分の陣地の後ろ（4人のチーム用）</summary>
            Back,
            /// <summary>守り: ボールと自分のゴールの間</summary>
            Guard,
            /// <summary>守り: ボールを持っていない一番危ない相手の、ゴール側に付く</summary>
            Cover,
            /// <summary>守り: 少し前に残って、奪ったときのパスを受ける</summary>
            Outlet,
        }

        private static readonly Role[] AttackRoles = { Role.Front, Role.Support, Role.Wing, Role.Back };
        private static readonly Role[] DefendRoles = { Role.Guard, Role.Cover, Role.Outlet, Role.Wing };

        // 持ち場の場所（チームの攻める向きで見た座標。案）
        private const float FrontAhead = 5f;
        private const float FrontMinX = 0f;
        private const float FrontMaxX = 10.5f;
        private const float FrontSide = 3.8f;
        private const float SupportBehind = 4.5f;
        private const float SupportMinX = -9f;
        private const float SupportMaxX = 6f;
        private const float SupportYRatio = 0.4f;
        private const float WingAhead = 2f;
        private const float WingMinX = -6f;
        private const float WingMaxX = 9f;
        private const float WingSide = 4.8f;
        private const float BackBehind = 9f;
        private const float BackMinX = -11f;
        private const float BackMaxX = 0f;
        private const float BackYRatio = 0.25f;
        private const float GuardRatio = 0.3f;
        private const float GuardMinX = -12f;
        private const float CoverGoalSide = 1.3f;
        private const float CoverFallbackBehind = 3f;
        private const float OutletAhead = 3f;
        private const float OutletLimitX = 4f;
        private const float OutletSide = 3.5f;

        // 持ち場をピッチの内側に収める余白
        private const float RoleMargin = 1f;

        private sealed class TeamPlan
        {
            public TeamPlan(int playerCount)
            {
                Targets = new Vec2[playerCount];
                Roles = new Role[playerCount];
            }

            /// <summary>取りに行く子（持っている子）。人の子のこともある（そのときは AI の子は誰も取りに行かない）。いなければ NoPlayer</summary>
            public int Chaser = SoccerBall.NoPlayer;

            public bool Attacking;
            public float RoleTimer;
            public readonly Vec2[] Targets;
            public readonly Role[] Roles;
            public readonly List<int> Fillers = new List<int>();
        }

        private void PlanTeam(SoccerWorld world, int team, Func<int, bool> isHuman)
        {
            var plan = _plans[team];
            plan.RoleTimer -= SoccerRules.StepSeconds;
            var sight = _seen[team];
            plan.Chaser = ChooseChaser(world, sight, team, plan.Chaser, out var ourSeconds);
            plan.Attacking = IsAttacking(world, sight, team, ourSeconds);

            var fillers = new List<int>();
            foreach (var player in world.Players)
            {
                if (player.Team == team && player.Index != plan.Chaser && !isHuman(player.Index))
                {
                    fillers.Add(player.Index);
                }
            }

            if (plan.RoleTimer <= 0f || !SameMembers(fillers, plan.Fillers))
            {
                AssignRoles(world, sight, team, plan, fillers);
                plan.RoleTimer = _levels[team].DecisionSeconds * SoccerBalance.AiRoleHoldScale;
            }

            foreach (var index in plan.Fillers)
            {
                plan.Targets[index] = RoleTarget(world, sight, team, plan.Roles[index]);
            }
        }

        /// <summary>
        /// 味方が持っていればその子。キーパーが持っていれば誰も行かない。そうでなければボールに一番早く着く子
        /// （今の子より AiChaserMargin 以上早い子がいるときだけ替える）
        /// </summary>
        private static int ChooseChaser(SoccerWorld world, SoccerBallSight sight, int team, int current, out float bestSeconds)
        {
            bestSeconds = float.MaxValue;
            var holder = sight.Holder;
            if (holder != SoccerBall.NoPlayer && world.Players[holder].Team == team)
            {
                bestSeconds = 0f;
                return holder;
            }

            if (sight.KeeperTeam != SoccerBall.NoTeam)
            {
                return SoccerBall.NoPlayer;
            }

            var best = SoccerBall.NoPlayer;
            var currentSeconds = float.MaxValue;
            foreach (var player in world.Players)
            {
                if (player.Team != team || player.IsDown)
                {
                    continue;
                }

                var seconds = ReachSeconds(world.Pitch, sight, player);
                if (seconds < bestSeconds)
                {
                    bestSeconds = seconds;
                    best = player.Index;
                }

                if (player.Index == current)
                {
                    currentSeconds = seconds;
                }
            }

            return currentSeconds <= bestSeconds + SoccerBalance.AiChaserMargin ? current : best;
        }

        /// <summary>味方（味方のキーパー）が持っている、または転がるボールに相手より先に着けるなら攻め</summary>
        private static bool IsAttacking(SoccerWorld world, SoccerBallSight ball, int team, float ourSeconds)
        {
            if (ball.KeeperTeam != SoccerBall.NoTeam)
            {
                return ball.KeeperTeam == team;
            }

            if (ball.Holder != SoccerBall.NoPlayer)
            {
                return world.Players[ball.Holder].Team == team;
            }

            var theirSeconds = float.MaxValue;
            foreach (var player in world.Players)
            {
                if (player.Team != team && !player.IsDown)
                {
                    theirSeconds = Math.Min(theirSeconds, ReachSeconds(world.Pitch, ball, player));
                }
            }

            return ourSeconds <= theirSeconds;
        }

        private static float ReachSeconds(SoccerPitchDef pitch, SoccerBallSight ball, SoccerPlayer player)
        {
            return SoccerReach.Seconds(pitch, ball.Position, ball.Velocity, ball.IsHeld, player.Position);
        }

        /// <summary>持ち場の場所への距離の合計が一番小さい配り方にする（子は多くて4体なので、全部の並べ方を試す）</summary>
        private static void AssignRoles(SoccerWorld world, SoccerBallSight sight, int team, TeamPlan plan, List<int> fillers)
        {
            plan.Fillers.Clear();
            plan.Fillers.AddRange(fillers);
            var roles = plan.Attacking ? AttackRoles : DefendRoles;
            var count = Math.Min(fillers.Count, roles.Length);
            var targets = new Vec2[count];
            for (var i = 0; i < count; i++)
            {
                targets[i] = RoleTarget(world, sight, team, roles[i]);
            }

            var order = new int[count];
            var bestOrder = new int[count];
            for (var i = 0; i < count; i++)
            {
                order[i] = i;
            }

            var bestCost = float.MaxValue;
            Permute(order, 0, () =>
            {
                var cost = 0f;
                for (var i = 0; i < count; i++)
                {
                    cost += (world.Players[fillers[i]].Position - targets[order[i]]).LengthSquared;
                }

                if (cost < bestCost)
                {
                    bestCost = cost;
                    Array.Copy(order, bestOrder, count);
                }
            });

            for (var i = 0; i < fillers.Count; i++)
            {
                // 持ち場より子が多いとき（5人以上のチームは無いが念のため）は、最後の持ち場を重ねる
                plan.Roles[fillers[i]] = i < count ? roles[bestOrder[i]] : roles[roles.Length - 1];
            }
        }

        private static void Permute(int[] order, int start, Action visit)
        {
            if (start >= order.Length)
            {
                visit();
                return;
            }

            for (var i = start; i < order.Length; i++)
            {
                (order[start], order[i]) = (order[i], order[start]);
                Permute(order, start + 1, visit);
                (order[start], order[i]) = (order[i], order[start]);
            }
        }

        private static bool SameMembers(List<int> a, List<int> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            for (var i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>持ち場の場所（ボールの位置で動く）</summary>
        private static Vec2 RoleTarget(SoccerWorld world, SoccerBallSight sight, int team, Role role)
        {
            var ball = sight.Position;
            var bx = Forward(team, ball);
            var by = ball.Y;
            var away = by >= 0f ? -1f : 1f;
            Vec2 target;
            switch (role)
            {
                case Role.Front:
                    target = FromLocal(team, Math.Clamp(bx + FrontAhead, FrontMinX, FrontMaxX), away * FrontSide);
                    break;
                case Role.Support:
                    target = FromLocal(team, Math.Clamp(bx - SupportBehind, SupportMinX, SupportMaxX), by * SupportYRatio);
                    break;
                case Role.Wing:
                    target = FromLocal(team, Math.Clamp(bx + WingAhead, WingMinX, WingMaxX), -away * WingSide);
                    break;
                case Role.Back:
                    target = FromLocal(team, Math.Clamp(bx - BackBehind, BackMinX, BackMaxX), by * BackYRatio);
                    break;
                case Role.Guard:
                    target = GuardTarget(world, team, ball);
                    break;
                case Role.Cover:
                    target = CoverTarget(world, sight, team, bx, by);
                    break;
                default:
                    target = FromLocal(team, Math.Clamp(bx + OutletAhead, -OutletLimitX, OutletLimitX), away * OutletSide);
                    break;
            }

            return ClampInside(world.Pitch, target);
        }

        /// <summary>自分のゴールとボールを結ぶ線の、ゴールから3割の所（ゴールの口の前に入りすぎない）</summary>
        private static Vec2 GuardTarget(SoccerWorld world, int team, Vec2 ball)
        {
            var goal = world.Pitch.GoalOf(team).Center;
            var point = goal + (ball - goal) * GuardRatio;
            var x = Math.Max(Forward(team, point), GuardMinX);
            return FromLocal(team, x, point.Y);
        }

        /// <summary>ボールを持っていない相手のうち、自分のゴールに一番近い子の、ゴール側に付く</summary>
        private static Vec2 CoverTarget(SoccerWorld world, SoccerBallSight sight, int team, float bx, float by)
        {
            var goal = world.Pitch.GoalOf(team).Center;
            SoccerPlayer danger = null;
            var dangerDistance = float.MaxValue;
            foreach (var other in world.Players)
            {
                if (other.Team == team || other.Index == sight.Holder)
                {
                    continue;
                }

                var distance = Vec2.Distance(other.Position, goal);
                if (distance < dangerDistance)
                {
                    danger = other;
                    dangerDistance = distance;
                }
            }

            if (danger == null)
            {
                return FromLocal(team, bx - CoverFallbackBehind, -by * SupportYRatio);
            }

            var toGoal = goal - danger.Position;
            return danger.Position + (toGoal.IsNearlyZero ? Vec2.Zero : toGoal.Normalized * CoverGoalSide);
        }

        private static Vec2 ClampInside(SoccerPitchDef pitch, Vec2 point)
        {
            var limitX = pitch.HalfLength - RoleMargin;
            var limitY = pitch.HalfWidth - RoleMargin;
            return new Vec2(Math.Clamp(point.X, -limitX, limitX), Math.Clamp(point.Y, -limitY, limitY));
        }
    }
}
