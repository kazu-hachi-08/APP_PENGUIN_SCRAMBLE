using UnityEngine;
using static PenmonScramble.Game.ToyMotionMath;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ボールを蹴る動き（ミニゲームのサッカー）: ためる間は右足を後ろへ引いて体をひねり、ためるほど深く構える（満タンで小刻みに震える）。
    /// 蹴ると足を前へ振り抜き、体をのけぞらせてヒレを開く（強いほど大きく）
    /// </summary>
    public sealed partial class PenguinMotion
    {
        public const float KickSeconds = 0.4f;

        // ためが目標へ追いつく速さ（毎秒）。押した瞬間から構えが見えるくらい速く
        private const float KickChargeBlendRate = 8f;

        // ため: 足を引く長さ・上げる高さ・つま先を下げる角度、体の後ろへの傾き・ひねり、ヒレの開き
        private const float ChargeFootBack = 0.16f;
        private const float ChargeFootLift = 0.05f;
        private const float ChargeFootTilt = 35f;
        private const float ChargeLean = 8f;
        private const float ChargeTwist = 18f;
        private const float ChargeFlipperOpen = 30f;
        // 満タンの震え（体を左右に）
        private const float ChargeShakeRate = 50f;
        private const float ChargeShake = 0.015f;
        private const float ChargeFullFrom = 0.95f;

        // 振り抜き: ここまでで足が前へ出きる（割合）。その後ゆっくり戻る
        private const float KickSwingEnd = 0.25f;
        private const float KickFootForward = 0.22f;
        private const float KickFootLift = 0.14f;
        private const float KickFootTilt = 50f;
        private const float KickLean = 14f;
        private const float KickTwist = 16f;
        private const float KickFlipperOpen = 55f;
        // 弱く蹴ったときの振りの大きさ（強さ 0 のとき。1 で全部）
        private const float KickWeakScale = 0.45f;

        /// <summary>シュートのため（0〜1）。0 で構えを解く</summary>
        public float KickCharge { get; set; }

        /// <summary>蹴る。power は強さ（0〜1）。ためていた構えから振り抜く</summary>
        public void PlayKick(float power)
        {
            _pose.KickFrom = _pose.KickCharge;
            _pose.KickCharge = 0f;
            KickCharge = 0f;
            _pose.KickPower = Mathf.Clamp01(power);
            _pose.Kick = 0f;
        }

        private void TickKick(float dt)
        {
            _pose.KickCharge = Mathf.MoveTowards(_pose.KickCharge, Mathf.Clamp01(KickCharge), KickChargeBlendRate * dt);
            _pose.Kick = Advance(_pose.Kick, dt, KickSeconds);
        }

        private static void KickParts(PenguinPose pose, float standing, ref PartPose p)
        {
            ChargeParts(pose, standing, ref p);
            SwingParts(pose, standing, ref p);
        }

        /// <summary>右足を後ろへ引いて上げ、体を少しのけぞらせて右へひねる（右足で蹴る構え）</summary>
        private static void ChargeParts(PenguinPose pose, float standing, ref PartPose p)
        {
            var c = pose.KickCharge * standing;
            if (c <= 0f)
            {
                return;
            }

            p.FootRight.Position += new Vector3(0f, ChargeFootLift * c, -ChargeFootBack * c);
            p.FootRight.Euler.x += ChargeFootTilt * c;
            p.Body.Euler += new Vector3(-ChargeLean * c, ChargeTwist * c, 0f);
            p.OpenFlippers(ChargeFlipperOpen * c);
            var full = Mathf.InverseLerp(ChargeFullFrom, 1f, pose.KickCharge);
            p.Body.Position.x += Mathf.Sin(pose.Time * ChargeShakeRate) * ChargeShake * full * standing;
        }

        /// <summary>引いた所から前へ速く振り抜き（つま先を上げる）、体はのけぞってひねりを戻し、ヒレを開いて釣り合いをとる</summary>
        private static void SwingParts(PenguinPose pose, float standing, ref PartPose p)
        {
            if (pose.Kick >= 1f || standing <= 0f)
            {
                return;
            }

            var size = Mathf.Lerp(KickWeakScale, 1f, pose.KickPower) * standing;
            var swing = pose.Kick < KickSwingEnd
                ? Mathf.SmoothStep(0f, 1f, pose.Kick / KickSwingEnd)
                : 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(KickSwingEnd, 1f, pose.Kick));
            // 振り始めは引いていた分だけ後ろから出す（構えから途切れずにつなぐ）
            var back = pose.Kick < KickSwingEnd ? (1f - swing) * pose.KickFrom : 0f;
            p.FootRight.Position += new Vector3(0f, KickFootLift * swing * size + ChargeFootLift * back, KickFootForward * swing * size - ChargeFootBack * back);
            p.FootRight.Euler.x += -KickFootTilt * swing * size + ChargeFootTilt * back;
            p.Body.Euler += new Vector3(-KickLean * swing * size, -KickTwist * swing * size + ChargeTwist * back, 0f);
            p.OpenFlippers(KickFlipperOpen * swing * size);
        }
    }
}
