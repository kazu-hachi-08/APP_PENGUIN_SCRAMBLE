using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ユニットの見た目（UnitView）から動き役へ渡すもの。ペンギン（PenguinMotion）と大ボス（BigBossMotion）で共通にして、
    /// UnitView がどちらの体かを気にせずに状態を渡せるようにする
    /// </summary>
    public interface IUnitMotion
    {
        /// <summary>床の上での速度（マス/秒）</summary>
        Vector3 Velocity { get; set; }

        /// <summary>向く方向。ゼロなら進む方向を向く</summary>
        Vector3 Facing { get; set; }

        /// <summary>突進中</summary>
        bool Sliding { get; set; }

        /// <summary>ダウン中</summary>
        bool Down { get; set; }

        /// <summary>凍っている（姿勢をその場で止める）</summary>
        bool Frozen { get; set; }

        bool IsDefeatFinished { get; }

        void SnapFacing(Vector3 direction);

        void PlayAttack(SkillShape shape);

        void PlaySpecial();

        void PlayHit();

        void PlayKnockback(Vector3 velocity);

        void PlayDefeat();
    }

    /// <summary>撃破の動き（ペンギン・大ボスで共通）: 後ろへのけぞりながら回って昇り、後半で小さくなって消える</summary>
    public static class ToyVanish
    {
        private const float Rise = 2f;
        private const float Turns = 2.5f;
        private const float Tilt = 30f;
        private const float ShrinkStart = 0.45f;

        /// <param name="t">撃破が始まってからの割合（0〜1）</param>
        /// <returns>昇った高さ（影の大きさに使う）</returns>
        public static float Apply(float t, ref Vector3 position, ref Quaternion rotation, ref float scale)
        {
            var eased = 1f - (1f - t) * (1f - t);
            var spin = Quaternion.Euler(0f, Turns * 360f * eased, 0f);
            var rise = Rise * eased;
            position = spin * position + Vector3.up * rise;
            rotation = spin * rotation * Quaternion.Euler(-Tilt * Mathf.Min(1f, t * 3f), 0f, 0f);
            scale = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ShrinkStart, 1f, t));
            return rise;
        }
    }
}
