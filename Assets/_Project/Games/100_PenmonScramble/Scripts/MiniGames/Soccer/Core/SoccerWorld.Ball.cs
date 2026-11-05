using System;
using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>離れたボールの動き（転がり・浮いた球の放物線・壁の跳ね返り）と、ゴールの判定</summary>
    public sealed partial class SoccerWorld
    {
        private void StepFreeBall(List<SoccerEvent> events)
        {
            if (Ball.IsAirborne)
            {
                Fly(events);
            }
            else
            {
                Roll();
            }

            MoveBall(events);
        }

        /// <summary>速さに比べない一定の摩擦で遅くする（止まる位置を読みやすく）</summary>
        private void Roll()
        {
            var speed = Ball.Velocity.Length;
            var decel = SoccerBalance.RollDecel * SoccerBalance.RollDecelScale(Pitch.GroundAt(Ball.Position)) * Dt;
            var next = speed - decel;
            Ball.Velocity = next <= SoccerBalance.StopSpeed ? Vec2.Zero : Ball.Velocity * (next / speed);
        }

        /// <summary>簡単な放物線。地面に着いたら弱く跳ね、弱すぎる跳ね返りは転がりに変える。浮いている間は摩擦なし</summary>
        private void Fly(List<SoccerEvent> events)
        {
            Ball.VerticalSpeed -= SoccerBalance.Gravity * Dt;
            Ball.Height += Ball.VerticalSpeed * Dt;
            if (Ball.Height > 0f)
            {
                return;
            }

            var falling = -Ball.VerticalSpeed;
            var bounce = falling * SoccerBalance.GroundBounce;
            Ball.Height = 0f;
            Ball.VerticalSpeed = bounce >= SoccerBalance.MinBounceSpeed ? bounce : 0f;
            events.Add(new SoccerEvent(SoccerEventKind.Land, SoccerBall.NoPlayer, -1, Ball.Position, falling));
        }

        /// <summary>速い球が壁をすり抜けないよう、1歩を細かく分けて進め、そのたびに壁と当てる</summary>
        private void MoveBall(List<SoccerEvent> events)
        {
            var travel = Ball.Velocity.Length * Dt;
            var steps = Math.Max(1, (int)MathF.Ceiling(travel / SoccerBalance.BallSubstepLength));
            var dt = Dt / steps;
            for (var i = 0; i < steps; i++)
            {
                Ball.Position += Ball.Velocity * dt;
                BounceBallOffWalls(events);
            }
        }

        /// <summary>
        /// 壁に食い込んでいれば押し出し、壁に向かう速さを跳ね返す。ゴールの口はバーより低い球なら通す。
        /// 持っているボールにも使う（壁ぎわでボールが壁に埋まらないように。持っている間は速さを持たないので跳ねない）
        /// </summary>
        private void BounceBallOffWalls(List<SoccerEvent> events)
        {
            var position = Ball.Position;
            var velocity = Ball.Velocity;
            foreach (var wall in Pitch.Walls)
            {
                if (wall.Kind == SoccerWallKind.GoalMouth && Pitch.GoalOf(wall.Team).PassesUnderBar(Ball.Height))
                {
                    continue;
                }

                if (!TryPushOut(wall, SoccerBalance.BallRadius, ref position, out var normal))
                {
                    continue;
                }

                var into = -Vec2.Dot(velocity, normal);
                if (into <= 0f)
                {
                    continue;
                }

                var isNet = wall.Kind == SoccerWallKind.Net;
                var bounce = isNet ? SoccerBalance.NetBounce : SoccerBalance.WallBounce;
                velocity += normal * (into * (1f + bounce));
                if (into >= SoccerBalance.HitEventMinSpeed)
                {
                    var kind = isNet ? SoccerEventKind.NetHit : SoccerEventKind.WallHit;
                    events?.Add(new SoccerEvent(kind, SoccerBall.NoPlayer, wall.Team, position, into));
                }
            }

            Ball.Position = position;
            Ball.Velocity = velocity;
        }

        /// <summary>
        /// ボールの中心がゴールの線を越えたら点（丸ごと越えるまで待たない。持ったまま運び込んだとき、子は口で止まるので
        /// ボールが丸ごとは越えられないため）。浮いた球は、バーより高ければ口の壁で跳ね返されるので、越えた時点でバーの下を通っている。
        /// 持ったまま運び込んだら、運んでいた子からボールを離して網へ転がす
        /// </summary>
        private void CheckGoal(List<SoccerEvent> events)
        {
            if (IsGoalScored)
            {
                return;
            }

            foreach (var goal in Pitch.Goals)
            {
                if (goal.DepthPastLine(Ball.Position) <= 0f || Math.Abs(Ball.Position.Y) >= goal.HalfWidth)
                {
                    continue;
                }

                ScoreGoal(goal, events);
                return;
            }
        }

        private void ScoreGoal(SoccerGoal goal, List<SoccerEvent> events)
        {
            var scorer = SoccerRules.Opponent(goal.Team);
            _score[scorer]++;
            IsGoalScored = true;
            if (Ball.IsHeld)
            {
                var holder = _players[Ball.Holder];
                holder.ResetCharge();
                Ball.Velocity = holder.Velocity;
                Ball.Holder = SoccerBall.NoPlayer;
            }

            events.Add(new SoccerEvent(SoccerEventKind.Goal, Ball.LastToucher, scorer, Ball.Position));
        }

        /// <summary>
        /// 誰も触らないまま長くたったら、一番近い子の方へ自分で少し転がす（壁ぎわで止まって誰も取りに行かない詰まりを防ぐ）
        /// </summary>
        private void UpdateIdle(List<SoccerEvent> events)
        {
            if (Ball.IsHeld || IsGoalScored)
            {
                Ball.IdleSeconds = 0f;
                return;
            }

            Ball.IdleSeconds += Dt;
            if (Ball.IdleSeconds < SoccerBalance.IdleNudgeSeconds || _players.Length == 0)
            {
                return;
            }

            var nearest = Nearest(Ball.Position, SoccerBall.NoPlayer);
            Ball.Velocity = (nearest.Position - Ball.Position).Normalized * SoccerBalance.IdleNudgeSpeed;
            Ball.IdleSeconds = 0f;
            events.Add(new SoccerEvent(SoccerEventKind.Nudge, nearest.Index, nearest.Team, Ball.Position));
        }

        /// <summary>position に一番近い子（except の子は除く）。いなければ null</summary>
        private SoccerPlayer Nearest(Vec2 position, int except)
        {
            SoccerPlayer best = null;
            var bestDistance = float.MaxValue;
            foreach (var player in _players)
            {
                var distance = (player.Position - position).LengthSquared;
                if (player.Index != except && distance < bestDistance)
                {
                    best = player;
                    bestDistance = distance;
                }
            }

            return best;
        }
    }
}
