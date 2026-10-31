using System;

namespace PenmonScramble.Core
{
    /// <summary>
    /// ペンモン画面の棒（体力・攻撃・速さ）の長さ。20種類の中でどのくらいかを 0〜1 で返す。
    /// いちばん低い種類でも棒が見えるよう、最小を MinRatio にする
    /// </summary>
    public static class SpeciesProfile
    {
        public const float MinRatio = 0.2f;

        public static float HpRatio(SpeciesDef species) => Ratio(species, s => s.HpRate);

        public static float AttackRatio(SpeciesDef species) => Ratio(species, s => s.AttackRate);

        public static float SpeedRatio(SpeciesDef species) => Ratio(species, s => s.MoveSpeed);

        private static float Ratio(SpeciesDef species, Func<SpeciesDef, float> value)
        {
            var min = float.MaxValue;
            var max = float.MinValue;
            foreach (var other in SpeciesTable.Species)
            {
                min = MathF.Min(min, value(other));
                max = MathF.Max(max, value(other));
            }

            if (max - min <= 0f)
            {
                return 1f;
            }

            var t = Math.Clamp((value(species) - min) / (max - min), 0f, 1f);
            return MinRatio + (1f - MinRatio) * t;
        }
    }
}
