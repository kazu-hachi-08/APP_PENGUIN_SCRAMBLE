using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// 1歩ぶんの位置の写し（ボール・子・キーパー）。前の歩と今の歩の写しの間を補間して描く
    /// （計算は 1/30 秒ごとなので、そのまま描くと画面のフレームによってはカクつくため）
    /// </summary>
    public readonly struct SoccerFrame
    {
        /// <summary>ボールの中心の位置（Unity の座標。高さを含む）</summary>
        public readonly Vector3 Ball;

        public readonly Vector3[] Players;

        public readonly Vector3[] Keepers;

        private SoccerFrame(Vector3 ball, Vector3[] players, Vector3[] keepers)
        {
            Ball = ball;
            Players = players;
            Keepers = keepers;
        }

        public static SoccerFrame Capture(SoccerWorld world)
        {
            var ball = world.Ball;
            var players = new Vector3[world.Players.Count];
            for (var i = 0; i < players.Length; i++)
            {
                players[i] = SoccerSpace.ToWorld(world.Players[i].Position);
            }

            var keepers = new Vector3[world.Keepers.Count];
            for (var i = 0; i < keepers.Length; i++)
            {
                keepers[i] = SoccerSpace.ToWorld(world.Keepers[i].Position);
            }

            var center = SoccerSpace.ToWorld(ball.Position, ball.Height + SoccerBalance.BallRadius);
            return new SoccerFrame(center, players, keepers);
        }

        /// <summary>prev と next の間の t（0〜1）の写し</summary>
        public static Vector3 Lerp(Vector3[] previous, Vector3[] next, int index, float t)
        {
            return Vector3.Lerp(previous[index], next[index], t);
        }
    }
}
