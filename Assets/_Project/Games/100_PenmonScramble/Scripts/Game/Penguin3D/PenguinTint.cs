using System;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ペンギンの部品の色の変え方（敵の暗さ・凍る・ダウン・被弾の光）。元の色を Darkness だけ暗くしてから、Target へ Amount だけ寄せる。
    /// 素材は色ごとに1つ（ToonMaterials）なので、色を変えるときは素材を差し替える。状態の数だけ素材が増えるが、まとめて描けるまま
    /// </summary>
    public readonly struct PenguinTint : IEquatable<PenguinTint>
    {
        public static readonly PenguinTint None = default;

        public readonly float Darkness;
        public readonly Color Target;
        public readonly float Amount;

        public PenguinTint(float darkness, Color target, float amount)
        {
            Darkness = darkness;
            Target = target;
            Amount = amount;
        }

        public static PenguinTint Dark(float darkness) => new PenguinTint(darkness, Color.black, 0f);

        /// <summary>暗さはそのままで、Target へ寄せる</summary>
        public PenguinTint Toward(Color target, float amount) => new PenguinTint(Darkness, target, amount);

        public Color Apply(Color color)
        {
            var dark = Color.Lerp(color, Color.black, Darkness);
            var tinted = Color.Lerp(dark, Target, Amount);
            tinted.a = color.a;
            return tinted;
        }

        public bool Equals(PenguinTint other) => Darkness == other.Darkness && Amount == other.Amount && Target.Equals(other.Target);

        public override bool Equals(object obj) => obj is PenguinTint other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Darkness, Target, Amount);
    }
}
