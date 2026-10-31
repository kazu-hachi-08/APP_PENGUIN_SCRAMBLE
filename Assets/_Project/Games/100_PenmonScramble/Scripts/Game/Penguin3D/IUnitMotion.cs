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

    /// <summary>部品1つ分の姿勢（位置・オイラー角・大きさ）。ペンギンと大ボスの動きで共通に使う</summary>
    internal struct Offset
    {
        public Vector3 Position;
        public Vector3 Euler;
        public Vector3 Scale;

        public static Offset From(Transform t) => new Offset { Position = t.localPosition, Euler = t.localEulerAngles, Scale = t.localScale };

        public void ApplyTo(Transform t)
        {
            t.localPosition = Position;
            t.localRotation = Quaternion.Euler(Euler);
            t.localScale = Scale;
        }
    }

    /// <summary>ペンギンと大ボスの動きで同じ計算（山の形・進み具合・向き・床の上の向き）。本文は両方で同じなので一か所にまとめる</summary>
    internal static class ToyMotionMath
    {
        /// <summary>start で 0、peak で 1、end で 0 に戻る山（なめらかに）</summary>
        public static float Bell(float t, float start, float peak, float end)
        {
            if (t <= start || t >= end) return 0f;
            return t < peak ? Mathf.SmoothStep(0f, 1f, (t - start) / (peak - start)) : Mathf.SmoothStep(1f, 0f, (t - peak) / (end - peak));
        }

        public static float Advance(float progress, float dt, float seconds) => Mathf.Min(1f, progress + dt / seconds);

        public static float YawOf(Vector3 direction) => Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        /// <summary>まばたきまでの間隔。ペンギンと大ボスで範囲が違うので、呼び出し側が min・max を渡す</summary>
        public static float NextBlinkInterval(float min, float max) => Random.Range(min, max);

        /// <summary>world の床の上の成分（上下を除いた水平）を、t の向きから見た向きにする</summary>
        public static Vector3 FlatLocal(Transform t, Vector3 world) => t.InverseTransformDirection(new Vector3(world.x, 0f, world.z));

        /// <summary>direction を向く角度を返す。水平の成分がゼロ（真上・真下）なら向きを変えず currentYaw を返す</summary>
        public static float SnapYaw(Transform t, Vector3 direction, float currentYaw)
        {
            var local = t.InverseTransformDirection(direction);
            return local.x * local.x + local.z * local.z > 0f ? YawOf(local) : currentYaw;
        }
    }
}
