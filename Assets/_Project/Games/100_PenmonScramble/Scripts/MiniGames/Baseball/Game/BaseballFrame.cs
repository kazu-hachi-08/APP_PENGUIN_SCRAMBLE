using PenmonScramble.Baseball.Core;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 1歩ぶんの写し（球の位置とバットの角度）。前の歩と今の歩の写しの間を補間して描く
    /// （計算は 1/30 秒ごとなので、そのまま描くと画面のフレームによってはカクつくため）
    /// </summary>
    public readonly struct BaseballFrame
    {
        /// <summary>球の中心の位置（Unity の座標。高さを含む）</summary>
        public readonly Vector3 Ball;

        /// <summary>バットの角度（度。BaseballBat.Degrees）</summary>
        public readonly float BatDegrees;

        private BaseballFrame(Vector3 ball, float batDegrees)
        {
            Ball = ball;
            BatDegrees = batDegrees;
        }

        public static BaseballFrame Capture(BaseballBoard board)
        {
            var p = board.BallPosition;
            var center = new Vector3(p.X, board.BallHeight + BaseballBalance.BallRadius, p.Y);
            return new BaseballFrame(center, board.Bat.Degrees);
        }

        public static BaseballFrame Lerp(BaseballFrame a, BaseballFrame b, float t)
        {
            return new BaseballFrame(Vector3.Lerp(a.Ball, b.Ball, t), Mathf.Lerp(a.BatDegrees, b.BatDegrees, t));
        }
    }
}
