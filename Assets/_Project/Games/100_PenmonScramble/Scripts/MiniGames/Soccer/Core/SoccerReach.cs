using System;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// 子がボールに着くまでの見込み（AI の取りに行く子の選び方と、人の動かす子の自動の切り替えで同じ物差しを使う）。
    /// 転がるボールは一定の摩擦で遅くなるとして読み、壁で跳ね返るのは読まない（壁の内側に収めるだけ）
    /// </summary>
    public static class SoccerReach
    {
        /// <summary>
        /// 持たれているボールは今の距離を走る秒、転がるボールは追いつける一番早い秒
        /// </summary>
        public static float Seconds(SoccerPitchDef pitch, Vec2 ballPosition, Vec2 ballVelocity, bool held, Vec2 player)
        {
            if (held)
            {
                return Vec2.Distance(player, ballPosition) / SoccerBalance.RunSpeed;
            }

            Intercept(pitch, ballPosition, ballVelocity, player, out var seconds);
            return seconds;
        }

        /// <summary>
        /// 転がるボールに一番早く追いつける所（読む先のいちばん先でも追いつけなければ、ボールが止まる所）。
        /// 壁ぎわで止まったボールにも届くよう、内側に収める余白はボールの半径だけ
        /// </summary>
        public static Vec2 Intercept(SoccerPitchDef pitch, Vec2 ballPosition, Vec2 ballVelocity, Vec2 player, out float seconds)
        {
            var speed = ballVelocity.Length;
            var direction = speed > 0f ? ballVelocity / speed : Vec2.Zero;
            var stopSeconds = speed / SoccerBalance.RollDecel;
            for (var t = 0f; t <= SoccerBalance.InterceptHorizon; t += SoccerBalance.InterceptStep)
            {
                var point = BallAt(pitch, ballPosition, direction, speed, Math.Min(t, stopSeconds));
                if (Vec2.Distance(player, point) <= SoccerBalance.RunSpeed * t + SoccerBalance.PickupReach)
                {
                    seconds = t;
                    return point;
                }
            }

            var rest = BallAt(pitch, ballPosition, direction, speed, stopSeconds);
            seconds = Math.Max(SoccerBalance.InterceptHorizon, Vec2.Distance(player, rest) / SoccerBalance.RunSpeed);
            return rest;
        }

        private static Vec2 BallAt(SoccerPitchDef pitch, Vec2 from, Vec2 direction, float speed, float t)
        {
            var point = from + direction * (speed * t - 0.5f * SoccerBalance.RollDecel * t * t);
            var limitX = pitch.HalfLength - SoccerBalance.BallRadius;
            var limitY = pitch.HalfWidth - SoccerBalance.BallRadius;
            return new Vec2(Math.Clamp(point.X, -limitX, limitX), Math.Clamp(point.Y, -limitY, limitY));
        }
    }
}
