using System;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// シュートの向きを相手のゴールの口へ少し寄せる（スティックで細かく狙わなくても枠に飛びやすく）。
    /// もう口へ向いていれば変えない。ゴールと反対の方へ蹴るときも変えない（パスのつもりの蹴り出しを曲げない）
    /// </summary>
    public static class SoccerAim
    {
        private const float DegToRad = MathF.PI / 180f;

        public static Vec2 Assist(Vec2 from, Vec2 direction, SoccerGoal goal)
        {
            return Assist(from, direction, goal, SoccerBalance.AimAssistDegrees * DegToRad);
        }

        /// <param name="maxRadians">寄せる最大の角度</param>
        public static Vec2 Assist(Vec2 from, Vec2 direction, SoccerGoal goal, float maxRadians)
        {
            var dir = direction.Normalized;
            var toGoal = goal.Center - from;
            if (dir.IsNearlyZero || Vec2.Dot(dir, toGoal) <= 0f)
            {
                return dir;
            }

            // ボールが柱に当たらずに通る口の両端
            var inner = goal.HalfWidth - SoccerBalance.BallRadius;
            var toLow = Angle(new Vec2(goal.LineX, -inner) - from);
            var toHigh = Angle(new Vec2(goal.LineX, inner) - from);
            var heading = Angle(dir);
            var lowDiff = Wrap(toLow - heading);
            var highDiff = Wrap(toHigh - heading);
            if (Math.Sign(lowDiff) != Math.Sign(highDiff))
            {
                return dir;
            }

            var diff = Math.Abs(lowDiff) < Math.Abs(highDiff) ? lowDiff : highDiff;
            var turn = Math.Sign(diff) * Math.Min(Math.Abs(diff), maxRadians);
            return dir.Rotated(turn);
        }

        private static float Angle(Vec2 v) => MathF.Atan2(v.Y, v.X);

        /// <summary>−π〜π に収める</summary>
        private static float Wrap(float radians)
        {
            while (radians > MathF.PI)
            {
                radians -= 2f * MathF.PI;
            }

            while (radians < -MathF.PI)
            {
                radians += 2f * MathF.PI;
            }

            return radians;
        }
    }
}
