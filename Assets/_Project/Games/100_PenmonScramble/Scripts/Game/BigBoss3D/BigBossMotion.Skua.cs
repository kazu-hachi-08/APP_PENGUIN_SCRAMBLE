using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// オオトウゾクカモメの動き: いつも宙で羽ばたく。急降下の予告では高く上がって翼を振り上げ、突進中は翼をたたんで床すれすれを飛び、
    /// 足を前へ突き出す。羽の雨の予告では胸を張って激しく羽ばたき、出た瞬間に翼を打ち下ろす
    /// </summary>
    public sealed partial class BigBossMotion
    {
        private static readonly MotionStyle SkuaStyle = new MotionStyle(1.6f, 0.25f, 1.4f, 0f);

        // 羽ばたき: 振れ幅（度）・休みの持ち上げ・先が遅れてしなる量（周の割合で約 1/6）
        private const float SkuaFlap = 32f;
        private const float SkuaWingRest = 8f;
        private const float SkuaTipLag = 1f;
        private const float SkuaTipFollow = 0.7f;
        private const float SkuaBob = 0.1f;
        private const float SkuaMoveLean = 10f;

        // 急降下の予告: 上がって胸を起こし、翼を高く振り上げて止める
        private const float SkuaChargeRise = 0.45f;
        private const float SkuaChargeRear = 22f;
        private const float SkuaChargeWingUp = 45f;
        private const float SkuaChargeHeadDown = 14f;

        // 羽の雨の予告: さらに高く上がって、大きく速く羽ばたく
        private const float SkuaRainRise = 0.65f;
        private const float SkuaRainRear = 30f;
        private const float SkuaRainFlap = 16f;
        private const float SkuaRainHeadUp = 15f;
        private const float SkuaRainSlam = 50f;

        // 突進（急降下）: 床すれすれまで下がり（浮いている高さの割合）、翼を後ろへたたみ、足を前へ
        private const float SkuaDashDrop = 0.42f;
        private const float SkuaDashPitch = 15f;
        private const float SkuaDashWingUp = 18f;
        private const float SkuaDashSweep = 50f;
        private const float SkuaDashTipSweep = 30f;
        private const float SkuaDashFeet = 75f;

        private void PoseSkua(BigBossPose pose, ref FigureOffset f)
        {
            var cycle = CyclePhase(pose);
            var flap = Mathf.Sin(cycle);
            var charge = TelegraphOf(pose, BossAttackKind.Charge);
            var rain = TelegraphOf(pose, BossAttackKind.Rain);
            var dash = pose.Dash;
            var slam = StrikeBell(pose, BossAttackKind.Rain, 0f, 0.2f, 0.8f);
            // 打ち下ろしても翼の先が床に届かないよう、上がった高さから少しずつ下りる
            if (pose.StrikeKind == BossAttackKind.Rain && pose.Strike < 1f)
            {
                rain = Mathf.Max(rain, 1f - Mathf.SmoothStep(0f, 1f, pose.Strike));
            }
            var glide = 1f - dash;

            var amplitude = (SkuaFlap + SkuaRainFlap * rain) * (1f - charge * 0.8f) * glide;
            var wingUp = Mathf.Lerp(SkuaWingRest + amplitude * flap + SkuaChargeWingUp * charge, SkuaDashWingUp, dash) - SkuaRainSlam * slam;
            var tipUp = amplitude * SkuaTipFollow * Mathf.Sin(cycle - SkuaTipLag) * glide + SkuaChargeHeadDown * charge;
            for (var side = -1f; side <= 1f; side += 2f)
            {
                ref var wing = ref Side(BigBossPart.LimbLeft, BigBossPart.LimbRight, side);
                wing.Euler.z += side * wingUp;
                wing.Euler.y += side * SkuaDashSweep * dash;
                ref var tip = ref Side(BigBossPart.TipLeft, BigBossPart.TipRight, side);
                tip.Euler.z += side * tipUp;
                tip.Euler.y += side * SkuaDashTipSweep * dash;
                ref var foot = ref Side(BigBossPart.FootLeft, BigBossPart.FootRight, side);
                foot.Euler.x += 8f * flap * glide - SkuaDashFeet * dash;
            }

            Joint(BigBossPart.Head).Euler.x += SkuaChargeHeadDown * charge - SkuaRainHeadUp * rain - 8f * dash;
            Joint(BigBossPart.Tail).Euler.x += 6f * Mathf.Sin(cycle + 1f) * glide - 10f * dash;

            // 打ち下ろすと体が持ち上がる（羽ばたきの上下と同じ向き）
            f.Lift += SkuaBob * Mathf.Cos(cycle) * glide + SkuaChargeRise * charge + SkuaRainRise * rain - BigBossModelBuilder.SkuaHoverHeight * SkuaDashDrop * dash + 0.25f * slam;
            f.Pitch += SkuaMoveLean * pose.Move * glide - SkuaChargeRear * charge - SkuaRainRear * rain + SkuaDashPitch * dash - 10f * slam;
        }

        /// <summary>その種類の攻撃を構えている度合い（0〜1）</summary>
        private static float TelegraphOf(BigBossPose pose, BossAttackKind kind) => pose.TelegraphKind == kind ? pose.Telegraph : 0f;
    }
}
