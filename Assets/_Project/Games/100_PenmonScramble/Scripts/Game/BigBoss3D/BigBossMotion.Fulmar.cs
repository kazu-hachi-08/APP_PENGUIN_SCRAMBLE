using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// オオフルマカモメの動き: 長い翼を広げたまま、ゆっくり小さく羽ばたいて低く滑空する（せわしなく羽ばたくカモメと見分ける）。
    /// 突風の予告では胸を起こして翼を前へ大きく振りかぶり、口を開ける。出た瞬間に翼を前へあおぐ。
    /// 竜巻の予告では体を左右にひねりながら激しく羽ばたく。急降下はカモメと同じに、構えで上がって突進中は翼をたたむ。
    /// 低く飛ぶうえに翼が長いので、翼を下へ振る量はカモメより小さくする（先が床に届かないように）
    /// </summary>
    public sealed partial class BigBossMotion
    {
        private static readonly MotionStyle FulmarStyle = new MotionStyle(0.9f, 0.2f, 1.6f, 0f);

        // 滑空: 小さな羽ばたき（度）と、翼の先のしなり
        private const float FulmarFlap = 14f;
        private const float FulmarWingRest = 4f;
        private const float FulmarTipFollow = 0.8f;
        private const float FulmarTipLag = 0.9f;
        private const float FulmarBob = 0.06f;
        private const float FulmarMoveLean = 6f;

        // 突風の予告: 胸を起こし、翼を高く前へ振りかぶる。出た瞬間に前へあおぐ（下へは少しだけ）
        private const float FulmarGustRise = 0.35f;
        private const float FulmarGustRear = 28f;
        private const float FulmarGustWingUp = 55f;
        private const float FulmarGustWingForward = 35f;
        private const float FulmarGustJaw = 28f;
        private const float FulmarGustSlam = 25f;
        private const float FulmarGustSweep = 60f;

        // 竜巻の予告: 大きく速く羽ばたき、体を左右にひねる（傾けると翼の先が床に届くので、ひねるだけ）
        private const float FulmarVortexFlap = 18f;
        private const float FulmarVortexRise = 0.3f;
        private const float FulmarVortexTwist = 14f;
        private const float FulmarVortexTwistSpeed = 7f;

        // 急降下: 構えで上がって胸を起こし、突進中は床すれすれで翼を後ろへたたむ
        private const float FulmarChargeRise = 0.4f;
        private const float FulmarChargeRear = 20f;
        private const float FulmarChargeWingUp = 40f;
        private const float FulmarDashDrop = 0.15f;
        private const float FulmarDashPitch = 6f;
        private const float FulmarDashSweep = 55f;
        private const float FulmarDashFeet = 70f;

        private void PoseFulmar(BigBossPose pose, ref FigureOffset f)
        {
            var cycle = CyclePhase(pose);
            var flap = Mathf.Sin(cycle);
            var gust = TelegraphOf(pose, BossAttackKind.Gust);
            var vortex = TelegraphOf(pose, BossAttackKind.Vortex);
            var charge = TelegraphOf(pose, BossAttackKind.Charge);
            var dash = pose.Dash;
            var slam = StrikeBell(pose, BossAttackKind.Gust, 0f, 0.15f, 0.7f);
            var whirl = StrikeBell(pose, BossAttackKind.Vortex, 0f, 0.2f, 0.8f);
            // あおいでも翼の先が床に届かないよう、構えで上がった高さから少しずつ下りる
            if (pose.StrikeKind == BossAttackKind.Gust && pose.Strike < 1f)
            {
                gust = Mathf.Max(gust, 1f - Mathf.SmoothStep(0f, 1f, pose.Strike));
            }

            var glide = 1f - dash;

            var amplitude = (FulmarFlap + FulmarVortexFlap * (vortex + whirl)) * (1f - gust * 0.85f) * (1f - charge * 0.7f) * (1f - slam) * glide;
            var raise = FulmarGustWingUp * gust + FulmarChargeWingUp * charge - FulmarGustSlam * slam;
            var wingUp = Mathf.Lerp(FulmarWingRest + amplitude * flap + raise, SkuaDashWingUp, dash);
            var tipUp = amplitude * FulmarTipFollow * Mathf.Sin(cycle - FulmarTipLag) * glide;
            for (var side = -1f; side <= 1f; side += 2f)
            {
                ref var wing = ref Side(BigBossPart.LimbLeft, BigBossPart.LimbRight, side);
                wing.Euler.z += side * wingUp;
                // 前へ振りかぶる（y を負へ）・突進中は後ろへたたむ
                wing.Euler.y += side * (FulmarDashSweep * dash - FulmarGustWingForward * gust - FulmarGustSweep * slam);
                ref var tip = ref Side(BigBossPart.TipLeft, BigBossPart.TipRight, side);
                tip.Euler.z += side * tipUp;
                tip.Euler.y += side * SkuaDashTipSweep * dash;
                ref var foot = ref Side(BigBossPart.FootLeft, BigBossPart.FootRight, side);
                foot.Euler.x += 25f * glide - FulmarDashFeet * dash;
            }

            Joint(BigBossPart.Head).Euler.x += 10f * slam - 12f * gust - 6f * dash;
            Joint(BigBossPart.Jaw).Euler.x += FulmarGustJaw * Mathf.Max(gust, slam);
            Joint(BigBossPart.Tail).Euler.x += 4f * Mathf.Sin(cycle + 1f) * glide - 8f * dash;
            Joint(BigBossPart.Body).Euler.y += FulmarVortexTwist * Mathf.Sin(pose.Time * FulmarVortexTwistSpeed) * vortex;

            f.Lift += FulmarBob * Mathf.Cos(cycle) * glide + FulmarGustRise * gust + FulmarVortexRise * vortex + FulmarChargeRise * charge
                - BigBossModelBuilder.FulmarHoverHeight * FulmarDashDrop * dash;
            f.Pitch += FulmarMoveLean * pose.Move * glide - FulmarGustRear * gust - FulmarChargeRear * charge + FulmarDashPitch * dash + 18f * slam;
        }
    }
}
