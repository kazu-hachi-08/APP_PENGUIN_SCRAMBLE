using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// 1歩ぶんの位置の写し（ボールと子）。前の歩と今の歩の写しの間を補間して描く
    /// （計算は 1/30 秒ごとなので、そのまま描くと画面のフレームによってはカクつくため）
    /// </summary>
    public readonly struct SoccerFrame
    {
        /// <summary>ボールの中心の位置（Unity の座標。高さを含む）</summary>
        public readonly Vector3 Ball;

        public readonly Vector3[] Players;

        private SoccerFrame(Vector3 ball, Vector3[] players)
        {
            Ball = ball;
            Players = players;
        }

        public static SoccerFrame Capture(SoccerWorld world)
        {
            var ball = world.Ball;
            var players = new Vector3[world.Players.Count];
            for (var i = 0; i < players.Length; i++)
            {
                var p = world.Players[i].Position;
                players[i] = new Vector3(p.X, 0f, p.Y);
            }

            var center = new Vector3(ball.Position.X, ball.Height + SoccerBalance.BallRadius, ball.Position.Y);
            return new SoccerFrame(center, players);
        }
    }
}
