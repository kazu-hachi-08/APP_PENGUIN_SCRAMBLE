using System;
using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// 盤の時計（仕掛けの様子を決める秒）。投げた球の番号（BaseballThrow.Id）から投げた瞬間の時計を決め、そこから歩ごとに進める。
    /// 番号だけで決まるので、オンラインでもどの端末も同じ1球になる（1球ごとに計算し直しても同じ）。
    /// 番号ごとにずらすのは、毎球同じ時刻に同じ様子にならないように（風の向き・動く穴の位置が球ごとに変わる）
    /// </summary>
    public static class BaseballClock
    {
        /// <summary>1球ごとに時計をずらす秒（仕掛けの1回りの長さと割り切れない値にして、様子が球ごとに散らばるように）</summary>
        public const float SecondsPerThrow = 2.37f;

        public static float AtThrow(int throwId) => Math.Max(0, throwId) * SecondsPerThrow;
    }

    /// <summary>
    /// 左右に動くアウトの穴（嵐の崖）。ホームからの距離は変えずに、方角を Amplitude 度だけ行き来する（Period 秒で1往復）
    /// </summary>
    public sealed class BaseballHoleSway
    {
        public BaseballHoleSway(float amplitude, float period, float phase)
        {
            Amplitude = amplitude;
            Period = period;
            Phase = phase;
        }

        /// <summary>真ん中から左右へ動く角度（度）</summary>
        public float Amplitude { get; }

        public float Period { get; }

        /// <summary>1往復の中のどこから始めるか（0〜1。穴ごとにずらして、そろって動かないように）</summary>
        public float Phase { get; }

        /// <summary>この時計での、真ん中からずれる角度（度）</summary>
        public float OffsetAt(float clock)
        {
            return Period > 0f ? Amplitude * MathF.Sin(2f * MathF.PI * (clock / Period + Phase)) : 0f;
        }
    }

    /// <summary>ホームからの距離と方角で区切った帯（扇形の盤の弧に沿った帯。氷の外野・水流の帯）</summary>
    public readonly struct BaseballArcBand
    {
        public BaseballArcBand(float innerRadius, float outerRadius, float halfDegrees)
        {
            InnerRadius = innerRadius;
            OuterRadius = outerRadius;
            HalfDegrees = halfDegrees;
        }

        public float InnerRadius { get; }
        public float OuterRadius { get; }

        /// <summary>センターから左右に何度まで</summary>
        public float HalfDegrees { get; }

        public bool Contains(Vec2 point)
        {
            var distance = point.Length;
            return distance >= InnerRadius && distance <= OuterRadius && point.Y > 0f
                && MathF.Abs(BaseballFieldDef.DegreesOf(point)) <= HalfDegrees;
        }
    }

    /// <summary>
    /// 水流の帯（深海）。転がる打球を弧に沿って横へ押し流す。押す向きは時計だけで決まり、Cycle の1回りごとに左右が入れ替わる。
    /// 弧に沿う速さが MaxSpeed より遅い間だけ押す（帯の中でいつまでも速くならないように）
    /// </summary>
    public sealed class BaseballCurrent
    {
        public BaseballCurrent(BaseballArcBand band, float accel, float maxSpeed, MiniGameCycle cycle)
        {
            Band = band;
            Accel = accel;
            MaxSpeed = maxSpeed;
            Cycle = cycle;
        }

        public BaseballArcBand Band { get; }
        public float Accel { get; }
        public float MaxSpeed { get; }

        /// <summary>Period 秒ごとに向きが替わる（Active は Period と同じにして、止まらずに流れ続ける）</summary>
        public MiniGameCycle Cycle { get; }

        /// <summary>この時計で流れる向き（1 ＝ 右回り＝一塁側へ・−1 ＝ 左回り＝三塁側へ）</summary>
        public float SideAt(float clock)
        {
            if (Cycle.Period <= 0f)
            {
                return 1f;
            }

            var round = (long)Math.Floor((clock + Cycle.Offset) / Cycle.Period);
            return round % 2 == 0 ? 1f : -1f;
        }

        /// <summary>点での流れの向き（弧に沿う長さ 1 の向き）</summary>
        public Vec2 DirectionAt(Vec2 point, float clock)
        {
            var radial = point.Normalized;
            // 右回り（方角が増える向き）は、外向きの向きを右へ 90° 回した向き
            var clockwise = new Vec2(radial.Y, -radial.X);
            return clockwise * SideAt(clock);
        }
    }

    /// <summary>
    /// 風（嵐の崖）。浮いている打球だけを左右へ流す（転がる球は流さない）。Cycle の「動いている」間だけ吹き、1回りごとに向きが替わる
    /// </summary>
    public sealed class BaseballWind
    {
        public BaseballWind(float accel, MiniGameCycle cycle)
        {
            Accel = accel;
            Cycle = cycle;
        }

        /// <summary>浮いた球を毎秒これだけ横へ速くする</summary>
        public float Accel { get; }

        public MiniGameCycle Cycle { get; }

        /// <summary>この時計で吹く向き（1 ＝ 右・−1 ＝ 左・0 ＝ 止んでいる）</summary>
        public float SideAt(float clock)
        {
            if (!Cycle.IsActive(clock))
            {
                return 0f;
            }

            var round = (long)Math.Floor((clock + Cycle.Offset) / Cycle.Period);
            return round % 2 == 0 ? 1f : -1f;
        }

        /// <summary>次の1回りに吹く向き（止んでいる間に、次にどちらから吹くかを旗で見せる）</summary>
        public float NextSideAt(float clock)
        {
            var round = (long)Math.Floor((clock + Cycle.Offset) / Cycle.Period);
            if (!Cycle.IsActive(clock))
            {
                round++;
            }

            return round % 2 == 0 ? 1f : -1f;
        }
    }

    /// <summary>
    /// 間欠泉（火山の入り江）。Cycle の「動いている」間に噴き、口の上を転がる打球を跳ね上げる（浮いている間は穴に落ちない）。
    /// 1回噴く間に跳ね上げるのは1度だけ（落ちてきた球をまた跳ね上げ続けないように）
    /// </summary>
    public sealed class BaseballGeyser
    {
        public BaseballGeyser(Vec2 center, float radius, MiniGameCycle cycle)
        {
            Center = center;
            Radius = radius;
            Cycle = cycle;
        }

        public Vec2 Center { get; }
        public float Radius { get; }
        public MiniGameCycle Cycle { get; }

        public bool IsErupting(float clock) => Cycle.IsActive(clock);

        /// <summary>何回目の噴き出しか（1回に1度だけ跳ね上げるのに使う）</summary>
        public long Eruption(float clock)
        {
            return Cycle.Period > 0f ? (long)Math.Floor((clock + Cycle.Offset) / Cycle.Period) : 0L;
        }

        public bool Contains(Vec2 point) => Vec2.Distance(point, Center) <= Radius;
    }

    /// <summary>球場の盤の仕掛け（Docs/MINIGAME_BASEBALL_PLAN.md フェーズ 6）。動く穴は BaseballHole.Sway に持つ</summary>
    public sealed class BaseballGimmicks
    {
        public static readonly BaseballGimmicks None = new BaseballGimmicks();

        public BaseballGimmicks(
            IReadOnlyList<BaseballArcBand> ice = null,
            IReadOnlyList<BaseballCurrent> currents = null,
            BaseballWind wind = null,
            IReadOnlyList<BaseballGeyser> geysers = null)
        {
            Ice = ice ?? Array.Empty<BaseballArcBand>();
            Currents = currents ?? Array.Empty<BaseballCurrent>();
            Wind = wind;
            Geysers = geysers ?? Array.Empty<BaseballGeyser>();
        }

        /// <summary>氷の床（よく転がる）</summary>
        public IReadOnlyList<BaseballArcBand> Ice { get; }

        public IReadOnlyList<BaseballCurrent> Currents { get; }

        /// <summary>風（無ければ null）</summary>
        public BaseballWind Wind { get; }

        public IReadOnlyList<BaseballGeyser> Geysers { get; }

        public bool IsOnIce(Vec2 point)
        {
            foreach (var band in Ice)
            {
                if (band.Contains(point))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
