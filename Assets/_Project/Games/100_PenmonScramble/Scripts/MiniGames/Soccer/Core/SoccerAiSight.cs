using System;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>AI が思っているボール（少し前に見たボールが、そのまま進んだと思った姿）</summary>
    internal readonly struct SoccerBallSight
    {
        public readonly Vec2 Position;
        public readonly Vec2 Velocity;
        public readonly int Holder;
        public readonly int KeeperTeam;

        public SoccerBallSight(Vec2 position, Vec2 velocity, int holder, int keeperTeam)
        {
            Position = position;
            Velocity = velocity;
            Holder = holder;
            KeeperTeam = keeperTeam;
        }

        public bool IsHeld => Holder != SoccerBall.NoPlayer || KeeperTeam != SoccerBall.NoTeam;

        public bool IsHeldByPlayer => Holder != SoccerBall.NoPlayer;
    }

    /// <summary>
    /// AI の「気づくまでの遅れ」: 毎歩のボールを少しの間だけ覚えておき、遅れの秒だけ前のボールを、
    /// その速さのまま進んだと思って渡す（蹴られた・奪われた・跳ね返ったのに気づくのが遅れる。弱い AI ほど遅い）
    /// </summary>
    internal sealed class SoccerAiSight
    {
        private readonly SoccerBallSight[] _history;
        private int _latest = -1;
        private int _count;

        public SoccerAiSight()
        {
            _history = new SoccerBallSight[(int)MathF.Ceiling(SoccerBalance.AiMaxReactionSeconds / SoccerRules.StepSeconds) + 1];
        }

        public void Record(SoccerBall ball)
        {
            _latest = (_latest + 1) % _history.Length;
            _history[_latest] = new SoccerBallSight(ball.Position, ball.Velocity, ball.Holder, ball.KeeperTeam);
            _count = Math.Min(_count + 1, _history.Length);
        }

        /// <summary>reactionSeconds 前のボールを、その間そのまま進んだと思った姿（転がる球は摩擦で遅くなる所まで読む）</summary>
        public SoccerBallSight Seen(float reactionSeconds, SoccerPitchDef pitch)
        {
            var back = Math.Min((int)MathF.Round(reactionSeconds / SoccerRules.StepSeconds), _count - 1);
            var old = _history[(_latest - back + _history.Length) % _history.Length];
            if (back <= 0)
            {
                return old;
            }

            var seconds = back * SoccerRules.StepSeconds;
            var speed = old.Velocity.Length;
            var moved = old.IsHeld ? speed * seconds : RollDistance(speed, seconds);
            var position = speed > 0f ? old.Position + old.Velocity / speed * moved : old.Position;
            var limitX = pitch.HalfLength;
            var limitY = pitch.HalfWidth;
            position = new Vec2(Math.Clamp(position.X, -limitX, limitX), Math.Clamp(position.Y, -limitY, limitY));
            return new SoccerBallSight(position, old.Velocity, old.Holder, old.KeeperTeam);
        }

        private static float RollDistance(float speed, float seconds)
        {
            var t = Math.Min(seconds, speed / SoccerBalance.RollDecel);
            return speed * t - 0.5f * SoccerBalance.RollDecel * t * t;
        }
    }
}
