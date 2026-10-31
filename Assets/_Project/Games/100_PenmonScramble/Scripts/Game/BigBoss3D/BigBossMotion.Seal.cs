using PenmonScramble.Core;
using UnityEngine;
using static PenmonScramble.Game.ToyMotionMath;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ヒョウアザラシの動き: 胴・腰・後ろヒレを順に遅らせて左右へくねらせ、前ヒレでこいで這う（頭はくねりと逆へ振って前を見続ける）。
    /// 体当たりの予告では胸を起こして口を大きく開け、歯をがちがち鳴らす。突進中は口を開けたまま腹ですべる。
    /// 飛び出しは、もぐる（BigBossMotion.Figure.cs）→ 口を開けて床から飛び出す
    /// </summary>
    public sealed partial class BigBossMotion
    {
        private static readonly MotionStyle SealStyle = new MotionStyle(0.35f, 0.55f, 2f, 2.5f);

        // くねり（度）: 休み・這う・突進。後ろの関節ほど遅れて大きく振れる
        private const float SealWiggleRest = 5f;
        private const float SealWiggleMove = 15f;
        private const float SealWiggleDash = 8f;
        private const float SealSegmentLag = 1.2f;
        private const float SealHump = 0.05f;
        private const float SealRock = 4f;
        private const float SealPaddle = 35f;
        private const float SealBreath = 0.025f;
        private const float SealJawRest = 4f;

        // 胸を起こすときの軸（体全体の土台から見た、胸の後ろの床に近い点）。胸の前で回すと胸の後ろが床にめり込むため
        private static readonly Vector3 SealRearPivot = new Vector3(0f, 0.1f, -0.35f);

        // 体当たりの予告: 胸を起こし（腰は床に残す）、口を開けて歯を鳴らす
        private const float SealChargeRear = 16f;
        private const float SealChargeHeadUp = 12f;
        private const float SealChargeJaw = 38f;
        private const float SealChatter = 6f;
        private const float SealChargePullBack = 0.15f;

        private const float SealDashJaw = 42f;
        private const float SealDashTuck = 25f;
        private const float SealEmergeJaw = 45f;
        private const float SealBite = 30f;

        private void PoseSeal(BigBossPose pose, ref FigureOffset f)
        {
            var cycle = CyclePhase(pose);
            var charge = TelegraphOf(pose, BossAttackKind.Charge);
            var ambush = TelegraphOf(pose, BossAttackKind.Ambush);
            var dash = pose.Dash;
            var emerge = Bell(pose.Emerge, 0f, 0.3f, 0.85f);
            var bite = StrikeBell(pose, BossAttackKind.Charge, 0f, 0.1f, 0.35f);

            var wiggle = Mathf.Lerp(SealWiggleRest, SealWiggleMove, pose.Move) + SealWiggleDash * dash;
            Joint(BigBossPart.Body).Euler.y += wiggle * 0.5f * Mathf.Sin(cycle);
            Joint(BigBossPart.Head).Euler.y -= wiggle * 0.7f * Mathf.Sin(cycle);
            Joint(BigBossPart.Hips).Euler.y += wiggle * 1.1f * Mathf.Sin(cycle - SealSegmentLag);
            Joint(BigBossPart.Tail).Euler.y += wiggle * 1.4f * Mathf.Sin(cycle - SealSegmentLag * 2f);

            // 前ヒレで床を押すたびに胸が少し持ち上がる
            var push = 0.5f + 0.5f * Mathf.Sin(cycle * 2f);
            var crawl = pose.Move * (1f - dash);
            f.Lift += SealHump * crawl * push;
            // 胸を起こす分だけ腰を逆へ回して、腰と後ろヒレを床に残す
            var rear = SealRock * crawl * push + SealChargeRear * charge;
            f.Pivot = SealRearPivot;
            f.Pitch -= rear;
            Joint(BigBossPart.Hips).Euler.x += rear + 15f * emerge;
            var breath = 1f + SealBreath * Mathf.Sin(pose.Time * 2.2f);
            Joint(BigBossPart.Body).Scale = Vector3.Scale(Joint(BigBossPart.Body).Scale, new Vector3(breath, breath, 1f));

            for (var side = -1f; side <= 1f; side += 2f)
            {
                ref var flipper = ref Side(BigBossPart.LimbLeft, BigBossPart.LimbRight, side);
                // 後ろへ寝かせたヒレを持ち上げては床へ戻す（下へ振ると床にめり込む）
                flipper.Euler.x += SealPaddle * crawl * (0.5f + 0.5f * Mathf.Sin(cycle + side * Mathf.PI * 0.5f));
                flipper.Euler.y += side * (SealDashTuck * dash - 20f * (charge + emerge));
            }

            Joint(BigBossPart.Head).Euler.x += -SealChargeHeadUp * charge + 15f * ambush - 6f * dash - 10f * emerge;
            var chatter = SealChatter * Mathf.Sin(pose.Time * 30f) * charge;
            var jaw = SealJawRest + Mathf.Max(SealChargeJaw * charge + chatter, SealDashJaw * dash) + SealEmergeJaw * emerge + SealBite * bite;
            Joint(BigBossPart.Jaw).Euler.x += jaw * (1f - ambush);
            f.Forward -= SealChargePullBack * charge;
        }
    }
}
