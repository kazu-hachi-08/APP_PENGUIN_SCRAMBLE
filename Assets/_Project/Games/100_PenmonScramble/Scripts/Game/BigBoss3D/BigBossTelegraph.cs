using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// スナップショットの赤い範囲（WarningState）から、大ボスがどの攻撃を構えているかを読み取る。
    /// どの攻撃かはスナップショットに無い（オンラインの形を変えないため）ので、範囲の形から見分ける
    /// </summary>
    public static class BigBossTelegraph
    {
        // 突進の帯は大ボスの足元から出る。通信で位置が丸められても見分けられる幅（マス）
        private const float ChargeStartTolerance = 0.3f;

        /// <param name="bossPosition">大ボスの床の上の位置</param>
        /// <param name="submerged">もぐっている（飛び出しの予告）</param>
        /// <returns>構えている攻撃。範囲が出ていなければ null</returns>
        public static BossAttackKind? Read(IReadOnlyList<WarningState> warnings, Vector2 bossPosition, bool submerged)
        {
            if (warnings.Count == 0)
            {
                return null;
            }

            if (submerged)
            {
                return BossAttackKind.Ambush;
            }

            var first = warnings[0];
            if (warnings.Count > 1 || IsCircle(first))
            {
                return BossAttackKind.Rain;
            }

            // 帯: 足元から出ていれば突進（横切る突進も同じ）、エリアの端から端までなら波
            return Vector2.Distance(first.From.ToVector2(), bossPosition) <= ChargeStartTolerance ? BossAttackKind.Charge : BossAttackKind.Wave;
        }

        /// <summary>飛び出す場所（飛び出しの予告の赤い円の中心）。円が無ければ false</summary>
        public static bool TryGetAmbushSpot(IReadOnlyList<WarningState> warnings, out Vector2 spot, out float progress)
        {
            foreach (var warning in warnings)
            {
                if (IsCircle(warning))
                {
                    spot = warning.From.ToVector2();
                    progress = warning.Progress;
                    return true;
                }
            }

            spot = Vector2.zero;
            progress = 0f;
            return false;
        }

        private static bool IsCircle(WarningState warning) => (warning.To - warning.From).LengthSquared <= 1e-6f;
    }
}
