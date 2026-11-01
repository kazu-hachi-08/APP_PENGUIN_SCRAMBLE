using System.Collections.Generic;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// スナップショットの赤い範囲（WarningState）から、大ボスがどの攻撃を構えているかを読み取る。
    /// 範囲はどの攻撃の予告かを持っているので、それをそのまま使う
    /// </summary>
    public static class BigBossTelegraph
    {
        /// <returns>構えている攻撃。範囲が出ていなければ null</returns>
        public static BossAttackKind? Read(IReadOnlyList<WarningState> warnings)
        {
            return warnings.Count > 0 ? warnings[0].Kind : (BossAttackKind?)null;
        }

        /// <summary>飛び出す場所（飛び出しの予告の赤い円の中心）。円が無ければ false</summary>
        public static bool TryGetAmbushSpot(IReadOnlyList<WarningState> warnings, out Vector2 spot, out float progress)
        {
            foreach (var warning in warnings)
            {
                if (warning.Kind == BossAttackKind.Ambush)
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
    }
}
