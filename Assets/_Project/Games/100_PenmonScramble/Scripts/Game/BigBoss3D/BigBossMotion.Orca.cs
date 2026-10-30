using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// シャチの動き: 尾の付け根と尾びれを上下に振って泳ぐ。波の予告では尾を床に残して体を大きく起こし、出た瞬間に打ち付けてつぶれる。
    /// 突進の予告では身を引いて尾を速く振り、突進中は口を開けたまま突っ込む
    /// </summary>
    public sealed partial class BigBossMotion
    {
        private static readonly MotionStyle OrcaStyle = new MotionStyle(0.5f, 0.35f, 1.5f, 1.8f);

        // 尾の振り（度）。下へ振ると床にめり込むので、上へ寄せて振る
        private const float OrcaHipsBias = 6f;
        private const float OrcaHipsKick = 12f;
        private const float OrcaTailBias = 8f;
        private const float OrcaTailKick = 20f;
        private const float OrcaTailLag = 1.1f;
        private const float OrcaBob = 0.04f;
        private const float OrcaNod = 2.5f;
        private const float OrcaPaddle = 6f;
        private const float OrcaJawRest = 3f;
        // うなずき・突進の構えで頭を下げるときの軸（あごの下の床に近い点）。胴の中心で回すと、あごの下が床にめり込むため
        private static readonly Vector3 OrcaNosePivot = new Vector3(0f, 0.1f, 0.9f);

        // 波の予告: 尾の付け根（体の後ろ寄りの床）を軸に体を起こす
        private static readonly Vector3 OrcaRearPivot = new Vector3(0f, 0.2f, -0.5f);
        private const float OrcaWaveRear = 38f;
        private const float OrcaWaveLift = 0.15f;
        private const float OrcaWaveJaw = 30f;
        private const float OrcaWaveFlippers = 35f;
        private const float OrcaWaveTremble = 3f;
        // 波が出た瞬間: 振り下ろして床に打ち付け、つぶれる。振り下ろしにかける割合（StrikeSeconds のうち）
        private const float OrcaSlamShare = 0.22f;
        private const float OrcaSlamSquash = 0.18f;

        private const float OrcaChargePullBack = 0.25f;
        private const float OrcaChargeDip = 6f;
        private const float OrcaChargeJaw = 20f;
        private const float OrcaDashJaw = 35f;
        private const float OrcaDashSweep = 30f;
        private const float OrcaDashRide = 0.06f;
        private const float OrcaBite = 25f;

        private void PoseOrca(BigBossPose pose, ref FigureOffset f)
        {
            var cycle = pose.Phase * Mathf.PI * 2f;
            var wave = TelegraphOf(pose, BossAttackKind.Wave);
            var charge = TelegraphOf(pose, BossAttackKind.Charge);
            var dash = pose.Dash;
            var kick = Mathf.Lerp(0.6f, 1f, pose.Move) + 0.4f * dash + 0.3f * charge;

            var hips = (OrcaHipsBias + OrcaHipsKick * Mathf.Sin(cycle)) * kick * (1f - wave);
            var tail = (OrcaTailBias + OrcaTailKick * Mathf.Sin(cycle - OrcaTailLag)) * kick * (1f - wave * 0.5f);
            f.Lift += OrcaBob * (0.5f + 0.5f * Mathf.Sin(cycle + Mathf.PI)) * kick + OrcaWaveLift * wave + OrcaDashRide * dash;
            f.Forward -= OrcaChargePullBack * charge;

            // 波: 体を起こす分だけ尾の付け根を逆へ回して、尾を床に残す
            var rear = OrcaWaveRear * wave + SlamPitch(pose);
            if (rear != 0f)
            {
                f.Pivot = OrcaRearPivot;
                f.Pitch -= rear;
                hips += rear;
            }
            else
            {
                // 起こしていないときだけ、あごの下を軸にうなずく（軸が2つあると、体を起こした後ろの軸で頭が床へ沈むため）
                f.Pivot = OrcaNosePivot;
                f.Pitch += OrcaNod * Mathf.Sin(cycle + Mathf.PI * 0.5f) + OrcaChargeDip * charge;
            }

            f.Roll += OrcaWaveTremble * Mathf.Sin(pose.Time * 25f) * wave;
            f.Squash += OrcaSlamSquash * (pose.StrikeKind == BossAttackKind.Wave ? Bell(pose.Strike, OrcaSlamShare, OrcaSlamShare * 1.4f, 0.6f) : 0f);
            Joint(BigBossPart.Hips).Euler.x += hips;
            Joint(BigBossPart.Tail).Euler.x += tail;

            for (var side = -1f; side <= 1f; side += 2f)
            {
                ref var flipper = ref Side(BigBossPart.LimbLeft, BigBossPart.LimbRight, side);
                flipper.Euler.z += side * (OrcaWaveFlippers * wave - OrcaPaddle * Mathf.Sin(cycle));
                flipper.Euler.y += side * OrcaDashSweep * dash;
            }

            var bite = pose.StrikeKind == BossAttackKind.Charge ? Bell(pose.Strike, 0f, 0.1f, 0.35f) : 0f;
            Joint(BigBossPart.Jaw).Euler.x += OrcaJawRest + Mathf.Max(OrcaWaveJaw * wave + OrcaChargeJaw * charge, OrcaDashJaw * dash) + OrcaBite * bite;
        }

        /// <summary>波が出た瞬間の体の起こし具合（度）。起こした所から一気に振り下ろす（床に着いたら PoseOrca でつぶれる）</summary>
        private static float SlamPitch(BigBossPose pose)
        {
            if (pose.StrikeKind != BossAttackKind.Wave || pose.Strike >= OrcaSlamShare)
            {
                return 0f;
            }

            return Mathf.Lerp(OrcaWaveRear, 0f, Mathf.SmoothStep(0f, 1f, pose.Strike / OrcaSlamShare));
        }
    }
}
