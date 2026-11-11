using UnityEngine;
using static PenmonScramble.Game.ToyMotionMath;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 球を投げる動き（ミニゲームの野球）: 振りかぶる間は右のヒレを後ろへ高く引き、左足を上げて体をのけぞらせてひねる。
    /// 投げるとヒレを前へ振り下ろし、体を前へ倒して足を着く（投げ終わりはゆっくり戻る）
    /// </summary>
    public sealed partial class PenguinMotion
    {
        public const float ThrowSeconds = 0.45f;

        // 振りかぶりが目標へ追いつく速さ（毎秒）
        private const float ThrowChargeBlendRate = 6f;

        // 振りかぶり: 右のヒレを後ろへ・上へ、左足を上げる、体はのけぞって右へひねる
        private const float WindArmBack = 70f;
        private const float WindArmRaise = 80f;
        private const float WindFootLift = 0.12f;
        private const float WindLean = 10f;
        private const float WindTwist = 30f;
        private const float WindOtherFlipperOpen = 35f;

        // 投げる: ここまでで腕が前へ振り下りきる（割合）。その後ゆっくり戻る
        private const float ThrowSwingEnd = 0.3f;
        private const float ThrowArmForward = 95f;
        private const float ThrowLean = 22f;
        private const float ThrowTwist = 25f;
        private const float ThrowStretch = 0.08f;

        /// <summary>振りかぶりの深さ（0〜1）。0 で構えを解く</summary>
        public float ThrowCharge { get; set; }

        /// <summary>投げる（振りかぶっていた構えから前へ振り下ろす）</summary>
        public void PlayThrow()
        {
            _pose.ThrowFrom = _pose.ThrowCharge;
            _pose.ThrowCharge = 0f;
            ThrowCharge = 0f;
            _pose.Throw = 0f;
        }

        private void TickThrow(float dt)
        {
            _pose.ThrowCharge = Mathf.MoveTowards(_pose.ThrowCharge, Mathf.Clamp01(ThrowCharge), ThrowChargeBlendRate * dt);
            _pose.Throw = Advance(_pose.Throw, dt, ThrowSeconds);
        }

        private static void ThrowParts(PenguinPose pose, float standing, ref PartPose p)
        {
            WindParts(pose.ThrowCharge * standing, ref p);
            ReleaseParts(pose, standing, ref p);
        }

        private static void WindParts(float c, ref PartPose p)
        {
            if (c <= 0f)
            {
                return;
            }

            p.FlipperRight.Euler += new Vector3(WindArmBack * c, 0f, WindArmRaise * c);
            p.FlipperLeft.Euler.z -= WindOtherFlipperOpen * c;
            p.FootLeft.Position += Vector3.up * (WindFootLift * c);
            p.Body.Euler += new Vector3(-WindLean * c, WindTwist * c, 0f);
        }

        /// <summary>引いていた所から前へ速く振り下ろし、体を前へ倒してひねりを戻す</summary>
        private static void ReleaseParts(PenguinPose pose, float standing, ref PartPose p)
        {
            if (pose.Throw >= 1f || standing <= 0f)
            {
                return;
            }

            var swing = pose.Throw < ThrowSwingEnd
                ? Mathf.SmoothStep(0f, 1f, pose.Throw / ThrowSwingEnd)
                : 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ThrowSwingEnd, 1f, pose.Throw));
            var back = pose.Throw < ThrowSwingEnd ? (1f - swing) * pose.ThrowFrom : 0f;
            swing *= standing;
            back *= standing;
            p.FlipperRight.Euler += new Vector3(-ThrowArmForward * swing + WindArmBack * back, 0f, WindArmRaise * (back + swing * 0.3f));
            p.FootLeft.Position += Vector3.up * (WindFootLift * back);
            p.Body.Euler += new Vector3(ThrowLean * swing - WindLean * back, -ThrowTwist * swing + WindTwist * back, 0f);
            p.Squash(-ThrowStretch * swing);
        }
    }
}
