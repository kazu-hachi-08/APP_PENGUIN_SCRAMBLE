using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// ボールを取りに行く AI の子: 転がるボールは追いつける所へ走り、相手が持っていれば動く先の、自分のゴール側へ回り込んで
    /// （後ろから追いかけるだけでは持っている子に追いつけないので、前に立ってコースを切る）、届いたらスライディングで奪う。
    /// スライディングを出すか・ダッシュするかは考え直す歩に乱数で決める（強いほど出しやすい）
    /// </summary>
    public sealed partial class SoccerAi
    {
        private SoccerInput Chase(SoccerWorld world, SoccerPlayer player, SoccerAiBrain brain, SoccerAiLevel level, bool decide)
        {
            var ball = _seen[player.Team];
            var target = ball.IsHeldByPlayer ? PressPoint(world, player.Team, ball) : SoccerReach.Intercept(world.Pitch, ball.Position, ball.Velocity, player.Position, out _);
            var to = target - player.Position;
            var distance = to.Length;
            var direction = distance > 0f ? to / distance : player.Facing;
            if (decide)
            {
                brain.SlideArmed = Roll(level.SlideChance);
                if (distance >= SoccerBalance.AiDashDistance && player.DashCooldown <= 0f && Roll(level.DashChance))
                {
                    return new SoccerInput(direction, false, true);
                }
            }

            if (ball.IsHeldByPlayer && TrySlide(player, brain, ball, out var slide))
            {
                return new SoccerInput(slide, true);
            }

            return new SoccerInput(direction, false);
        }

        /// <summary>持っている相手のボールが届く所にあり、すべる向きと今の向きが近ければ、すべり終わる頃のボールへ向けてすべる</summary>
        private static bool TrySlide(SoccerPlayer player, SoccerAiBrain brain, SoccerBallSight ball, out Vec2 direction)
        {
            var aim = ball.Position + ball.Velocity * (SoccerBalance.SlideSeconds * 0.5f) - player.Position;
            direction = aim.IsNearlyZero ? player.Facing : aim.Normalized;
            var ready = brain.SlideArmed
                && player.SlideCooldown <= 0f
                && Vec2.Distance(ball.Position, player.Position) <= SoccerBalance.AiSlideDistance
                && Vec2.Dot(player.Facing, direction) >= SoccerBalance.AiSlideFacingCos;
            if (ready)
            {
                brain.SlideArmed = false;
            }

            return ready;
        }

        /// <summary>持っている相手が少し先に着く所の、自分のゴール側</summary>
        private static Vec2 PressPoint(SoccerWorld world, int team, SoccerBallSight ball)
        {
            var ahead = ball.Position + ball.Velocity * SoccerBalance.AiPressLeadSeconds;
            var toGoal = world.Pitch.GoalOf(team).Center - ahead;
            return toGoal.IsNearlyZero ? ahead : ahead + toGoal.Normalized * SoccerBalance.AiGoalSideOffset;
        }
    }
}
