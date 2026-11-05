using System;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>ボールがキーパーの線を通るときの見込み（いつ・どこ・どの高さ）</summary>
    public readonly struct SoccerShotPrediction
    {
        /// <summary>キーパーの線に着くまでの秒</summary>
        public readonly float Seconds;

        /// <summary>キーパーの線を通る Y</summary>
        public readonly float Y;

        /// <summary>キーパーの線を通るときのボールの下の端の高さ</summary>
        public readonly float Height;

        public SoccerShotPrediction(float seconds, float y, float height)
        {
            Seconds = seconds;
            Y = y;
            Height = height;
        }
    }

    /// <summary>
    /// キーパーが止められるかの計算（純粋な関数。乱数は呼ぶ側が持つ）。
    /// ボールの速さ・キーパーからの距離・高さで止める割合を決める: 真正面の弱い球はほぼ止め、端への強い球はほぼ入る
    /// </summary>
    public static class SoccerKeeperSave
    {
        /// <summary>
        /// 離れたボールが、ゴールの口へ向かってキーパーの線を通るなら、その見込み。
        /// 向かっていない・線まで届かない・口から外れる・バーより高いときは false（キーパーは飛びつかない）
        /// </summary>
        /// <param name="decel">転がりの摩擦（毎秒の減り方）。浮いている間は摩擦なしで数える</param>
        public static bool TryPredict(SoccerBall ball, float lineX, SoccerGoal goal, float decel, out SoccerShotPrediction prediction)
        {
            prediction = default;
            var velocity = ball.Velocity;
            var toLine = lineX - ball.Position.X;
            if (velocity.X * goal.Outward <= 0f || toLine * goal.Outward <= 0f)
            {
                return false;
            }

            var slope = velocity.Y / velocity.X;
            var yAtGoal = ball.Position.Y + slope * (goal.LineX - ball.Position.X);
            if (Math.Abs(yAtGoal) >= goal.HalfWidth)
            {
                return false;
            }

            var speed = velocity.Length;
            var distance = Math.Abs(toLine) * speed / Math.Abs(velocity.X);
            if (!TryTimeToTravel(ball.IsAirborne, speed, distance, decel, out var seconds))
            {
                return false;
            }

            var height = ball.IsAirborne ? HeightAfter(ball, seconds) : 0f;
            if (!goal.PassesUnderBar(height))
            {
                return false;
            }

            prediction = new SoccerShotPrediction(seconds, ball.Position.Y + slope * toLine, height);
            return true;
        }

        /// <summary>
        /// 止める割合（0〜1）。capability はキーパーが動ける距離、need は届かせるのに要る距離。
        /// 余裕（capability − need）が大きいほど止め、高い球ほど少し止めにくい。いつも少しだけ運の入る余地を残す
        /// </summary>
        public static float Chance(float capability, float need, float height, float barHeight)
        {
            var chance = 0.5f + (capability - need) / SoccerBalance.KeeperSaveSpread;
            var high = barHeight > 0f ? Math.Clamp(height / barHeight, 0f, 1f) : 0f;
            chance *= 1f - SoccerBalance.KeeperHighBallPenalty * high;
            return Math.Clamp(chance, SoccerBalance.KeeperMinSave, SoccerBalance.KeeperMaxSave);
        }

        /// <summary>構えているキーパーが、seconds 秒のうちに飛びついて動ける距離（気づくまでの遅れの分は動けない）</summary>
        public static float DiveCapability(float seconds)
        {
            var moving = Math.Max(0f, seconds - SoccerBalance.KeeperReactSeconds);
            return Math.Min(SoccerBalance.KeeperDiveReach, SoccerBalance.KeeperDiveSpeed * moving);
        }

        /// <summary>立ったまま腕で届く分を引いた、届かせるのに要る距離</summary>
        public static float Need(float keeperY, float ballY)
        {
            return Math.Max(0f, Math.Abs(ballY - keeperY) - SoccerBalance.KeeperBodyReach);
        }

        /// <summary>一定の摩擦で遅くなる転がりが distance 進むまでの秒。途中で止まるなら false</summary>
        private static bool TryTimeToTravel(bool airborne, float speed, float distance, float decel, out float seconds)
        {
            if (airborne || decel <= 0f)
            {
                seconds = distance / speed;
                return true;
            }

            var remaining = speed * speed - 2f * decel * distance;
            if (remaining < 0f)
            {
                seconds = 0f;
                return false;
            }

            seconds = (speed - MathF.Sqrt(remaining)) / decel;
            return true;
        }

        /// <summary>浮いた球の seconds 秒後の高さ（地面に着いた後は 0 とみなす）</summary>
        private static float HeightAfter(SoccerBall ball, float seconds)
        {
            var height = ball.Height + ball.VerticalSpeed * seconds - 0.5f * SoccerBalance.Gravity * seconds * seconds;
            return Math.Max(0f, height);
        }
    }
}
