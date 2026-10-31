using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 結果の舞台での見た目。Core は止まっていてもう状態が届かないので、ここで向きと動きを決める
    /// </summary>
    public partial class UnitView
    {
        // 結果の舞台では、点滅・突進の残像・構え・氷・名前の「MENU」は止める（喜んでいる最中に点滅し続けないように）
        private const UnitFlags ResultClearedFlags = UnitFlags.Invincible | UnitFlags.Dashing | UnitFlags.WindingUp | UnitFlags.Frozen | UnitFlags.InMenu;

        /// <summary>クリアした: カメラの方を向いて跳ねて喜ぶ。ダウンしていた子も起き上がる（煙がぽんと出る）</summary>
        public void Celebrate(Vector3 facing)
        {
            if (IsDown)
            {
                Poof();
            }

            _flags &= ~(ResultClearedFlags | UnitFlags.Down);
            StopForResult(facing);
            _motion.Cheering = true;
        }

        /// <summary>やられた: 倒れたまま（ダウンのじたばた）で、点滅などだけ止める</summary>
        public void HoldForResult()
        {
            _flags &= ~ResultClearedFlags;
            _motion.Velocity = Vector3.zero;
            _motion.Sliding = false;
            _motion.WindingUp = false;
            ApplyStatus();
        }

        private void StopForResult(Vector3 facing)
        {
            _motion.Velocity = Vector3.zero;
            _motion.Facing = facing;
            _motion.Sliding = false;
            _motion.Down = false;
            _motion.WindingUp = false;
            ApplyStatus();
        }
    }
}
