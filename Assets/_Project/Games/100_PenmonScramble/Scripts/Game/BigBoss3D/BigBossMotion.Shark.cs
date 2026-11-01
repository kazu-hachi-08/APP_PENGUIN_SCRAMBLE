using PenmonScramble.Core;
using UnityEngine;
using static PenmonScramble.Game.ToyMotionMath;

namespace PenmonScramble.Game
{
    /// <summary>
    /// サメの動き: 尾の付け根と尾びれを左右に振って泳ぐ（頭は逆へ少し振って前を見続ける）。
    /// 突進の予告では浮き上がって鼻先を上げ、大口を開ける。突進中は口を開けたまま突っ込み、出た瞬間にかみつく。
    /// 背びれ・食いつきでもぐっている間は、尾を速く振って床の下を進む（沈むのは BigBossMotion.Figure.cs）。飛び出すときは大口を開ける
    /// </summary>
    public sealed partial class BigBossMotion
    {
        private static readonly MotionStyle SharkStyle = new MotionStyle(0.6f, 0.4f, 1.6f, 2.2f);

        // 泳ぎ: 尾の振り（度）。後ろの関節ほど遅れて大きく振れる
        private const float SharkHipsSwing = 12f;
        private const float SharkTailSwing = 24f;
        private const float SharkTailLag = 1.1f;
        private const float SharkHeadCounter = 4f;
        private const float SharkBob = 0.04f;
        private const float SharkPaddle = 5f;
        private const float SharkJawRest = 3f;

        // 突進の予告: 浮き上がり、鼻先を上げて大口を開ける。軸は尾の付け根の下（お腹の後ろが床にめり込まないように）
        private static readonly Vector3 SharkRearPivot = new Vector3(0f, 0.2f, -0.6f);
        private const float SharkChargeLift = 0.45f;
        private const float SharkChargeRear = 14f;
        private const float SharkChargeJaw = 38f;
        private const float SharkChargeTremble = 2.5f;
        private const float SharkDashLift = 0.25f;
        private const float SharkDashJaw = 42f;
        private const float SharkBite = 30f;

        // もぐっている間（背びれ・食いつき）: 尾をもっと速く大きく振る
        private const float SharkUnderSwing = 1.6f;
        private const float SharkEmergeJaw = 50f;

        private void PoseShark(BigBossPose pose, ref FigureOffset f)
        {
            var cycle = CyclePhase(pose);
            var charge = TelegraphOf(pose, BossAttackKind.Charge);
            var dash = pose.Dash;
            var under = Mathf.Max(pose.Submerge, TelegraphOf(pose, BossAttackKind.Fin));
            var emerge = Bell(pose.Emerge, 0f, 0.3f, 0.85f);
            var bite = StrikeBell(pose, BossAttackKind.Charge, 0f, 0.1f, 0.35f);

            var swing = Mathf.Lerp(0.6f, 1f, pose.Move) + 0.4f * dash + 0.5f * charge + (SharkUnderSwing - 1f) * under;
            Joint(BigBossPart.Hips).Euler.y += SharkHipsSwing * swing * Mathf.Sin(cycle);
            Joint(BigBossPart.Tail).Euler.y += SharkTailSwing * swing * Mathf.Sin(cycle - SharkTailLag);
            Joint(BigBossPart.Body).Euler.y -= SharkHeadCounter * swing * Mathf.Sin(cycle);
            for (var side = -1f; side <= 1f; side += 2f)
            {
                ref var flipper = ref Side(BigBossPart.LimbLeft, BigBossPart.LimbRight, side);
                flipper.Euler.z += side * SharkPaddle * Mathf.Sin(cycle + side);
            }

            // 突進中に浮いている高さは、構えの高さから少し下げて保つ（床すれすれに突っ込んで見える）
            var lift = Mathf.Max(SharkChargeLift * charge, SharkDashLift * dash);
            f.Lift += SharkBob * (0.5f + 0.5f * Mathf.Sin(cycle * 2f)) * (1f - under) + lift;
            f.Pivot = SharkRearPivot;
            f.Pitch -= SharkChargeRear * charge;
            f.Roll += SharkChargeTremble * Mathf.Sin(pose.Time * 25f) * charge;

            var jaw = SharkJawRest + Mathf.Max(SharkChargeJaw * charge, SharkDashJaw * dash) + SharkEmergeJaw * emerge + SharkBite * bite;
            Joint(BigBossPart.Jaw).Euler.x += jaw;
        }
    }
}
