using UnityEngine;
using static PenmonScramble.Game.ToyMotionMath;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 守る動き（ミニゲームの野球の守備・キャッチャー）: 構える間は腰を落として前かがみになり、ヒレを前で低く開く。
    /// 捕るとヒレをさっと前へ突き出して体を伸ばし、ゆっくり構えに戻る
    /// </summary>
    public sealed partial class PenguinMotion
    {
        public const float CatchSeconds = 0.5f;

        private const float FieldBlendRate = 6f;

        // 構え: 腰を落とす（つぶす）・前へ傾く・ヒレを前へ出して開く・顔は前を見る
        private const float FieldCrouch = 0.14f;
        private const float FieldLean = 18f;
        private const float FieldFlipperForward = 40f;
        private const float FieldFlipperOpen = 30f;
        private const float FieldHeadUp = 12f;

        // 捕る: ここでいちばん伸びる（割合）。ヒレを前へ突き出して閉じ、体を伸ばす
        private const float CatchPeak = 0.2f;
        private const float CatchFlipperForward = 55f;
        private const float CatchFlipperClose = 25f;
        private const float CatchStretch = 0.12f;
        private const float CatchLean = 10f;

        /// <summary>守備の構え。止めると立った姿勢へなめらかに戻る</summary>
        public bool Fielding { get; set; }

        /// <summary>球を捕る</summary>
        public void PlayCatch() => _pose.Catch = 0f;

        private void TickField(float dt)
        {
            _pose.Field = Mathf.MoveTowards(_pose.Field, Fielding && !Down ? 1f : 0f, FieldBlendRate * dt);
            _pose.Catch = Advance(_pose.Catch, dt, CatchSeconds);
        }

        private static void FieldParts(PenguinPose pose, float standing, ref PartPose p)
        {
            var w = pose.Field * standing;
            if (w > 0f)
            {
                p.Squash(FieldCrouch * w);
                p.Body.Euler.x += FieldLean * w;
                p.Head.Euler.x -= FieldHeadUp * w;
                p.SwingFlippers(-FieldFlipperForward * w);
                p.OpenFlippers(FieldFlipperOpen * w);
            }

            if (pose.Catch >= 1f || standing <= 0f)
            {
                return;
            }

            var reach = Bell(pose.Catch, 0f, CatchPeak, 1f) * standing;
            p.SwingFlippers(-CatchFlipperForward * reach);
            p.OpenFlippers(-CatchFlipperClose * reach);
            p.Squash(-CatchStretch * reach);
            p.Body.Euler.x += CatchLean * reach;
        }
    }
}
