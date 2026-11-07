using System;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Baseball.Core
{
    public enum BaseballBatState
    {
        /// <summary>構えている（まだ振っていない）</summary>
        Ready,
        /// <summary>振っている途中（当たる）</summary>
        Swinging,
        /// <summary>振り終わった（もう当たらない）</summary>
        Swung,
        /// <summary>バントの構え（寝かせたバットに当たる）</summary>
        Bunting,
    }

    /// <summary>
    /// バット。軸（打つ子の立つ所）を中心に、盤の上で水平に回る（おもちゃの盤のバットと同じ）。
    /// 角度は度で、0 がホームの上を横切る向き、正が投手の方へ回った向き。振ると一定の速さで構えから振り終わりまで回る。
    /// 当たったときの角度で打球の向きが（早い＝引っ張り・遅い＝流し）、軸からの距離で強さが（芯が強い）決まる
    /// </summary>
    public sealed class BaseballBat
    {
        private float _elapsed;

        public BaseballBatState State { get; private set; }

        public float Degrees { get; private set; } = BaseballBalance.SwingStartDegrees;

        /// <summary>当たり得る状態か（振っている途中・バントの構え）</summary>
        public bool IsLive => State == BaseballBatState.Swinging || State == BaseballBatState.Bunting;

        /// <summary>打ちにいったか（振った・バントの構えのまま球が来た）。当たらなければ空振り</summary>
        public bool HasAttempted => State != BaseballBatState.Ready;

        public void Reset()
        {
            State = BaseballBatState.Ready;
            Degrees = BaseballBalance.SwingStartDegrees;
            _elapsed = 0f;
        }

        /// <summary>
        /// 振る（1球に1回。バントの構えからも振れる＝バスター）。振り始めたら true。
        /// elapsed は、もう振っていた秒（負なら次の Step の途中から回り始める。押した時刻を歩の中まで合わせるため）
        /// </summary>
        public bool Swing(float elapsed = 0f)
        {
            if (State != BaseballBatState.Ready && State != BaseballBatState.Bunting)
            {
                return false;
            }

            State = BaseballBatState.Swinging;
            _elapsed = elapsed;
            Degrees = BaseballBalance.SwingStartDegrees;
            return true;
        }

        /// <summary>バントの構え（押している間）。構え始めたら true。離したら構えに戻す（見送り）</summary>
        public bool SetBunt(bool held)
        {
            if (held && State == BaseballBatState.Ready)
            {
                State = BaseballBatState.Bunting;
                Degrees = BaseballBalance.BuntDegrees;
                return true;
            }

            if (!held && State == BaseballBatState.Bunting)
            {
                State = BaseballBatState.Ready;
                Degrees = BaseballBalance.SwingStartDegrees;
            }

            return false;
        }

        /// <summary>当たったら振り終わりにする（打ち返した後でバットが球をもう一度叩かないように）</summary>
        public void Finish()
        {
            State = BaseballBatState.Swung;
        }

        public void Step(float dt)
        {
            if (State != BaseballBatState.Swinging)
            {
                return;
            }

            _elapsed += dt;
            var t = Math.Clamp(_elapsed / BaseballBalance.SwingSeconds, 0f, 1f);
            Degrees = BaseballBalance.SwingStartDegrees + (BaseballBalance.SwingEndDegrees - BaseballBalance.SwingStartDegrees) * t;
            if (t >= 1f)
            {
                State = BaseballBatState.Swung;
            }
        }

        /// <summary>
        /// 振り始めてから、バットがホームの上を横切る向き（0°＝ジャストの真ん中）になるまでの秒。
        /// 球がバットの線に届くのがこの秒後になるよう振れば、ジャストで当たる（CPU の振る時刻の目安）
        /// </summary>
        public static float SecondsToSquare =>
            BaseballBalance.SwingSeconds * (0f - BaseballBalance.SwingStartDegrees) / (BaseballBalance.SwingEndDegrees - BaseballBalance.SwingStartDegrees);

        /// <summary>角度のバットの向き（軸から先へ）</summary>
        public static Vec2 Direction(float degrees)
        {
            var radians = degrees * MathF.PI / 180f;
            return new Vec2(MathF.Cos(radians), MathF.Sin(radians));
        }

        /// <summary>
        /// この細かい1歩でバットが球に当たったか。バットの線に対して球が前（回る向きの先）から触れる所まで来たら当たり
        /// （振ればバットが球へ、バントなら球がバットへ来る。どちらも同じ形で見る）。along は軸から当たった所までの距離
        /// </summary>
        public static bool TryContact(Vec2 pivot, float prevDegrees, float degrees, Vec2 prevBall, Vec2 ball, out float along)
        {
            along = 0f;
            var r = BaseballBalance.BallRadius;
            var before = MiniGameGeometry.Cross(Direction(prevDegrees), prevBall - pivot);
            var after = MiniGameGeometry.Cross(Direction(degrees), ball - pivot);
            if (before <= r || after > r)
            {
                return false;
            }

            along = Vec2.Dot(Direction(degrees), ball - pivot);
            return along >= BaseballBalance.BatInner && along <= BaseballBalance.BatLength + r;
        }

        /// <summary>振って当たったときの打球（当たった角度と、軸からの距離で決まる）</summary>
        public static BaseballHit SwingHit(float degrees, float along)
        {
            var offset = MathF.Abs(along - BaseballBalance.SweetSpot);
            var angle = MathF.Abs(degrees);
            var sweet = offset <= BaseballBalance.SweetHalfWidth;
            var just = angle <= BaseballBalance.JustDegrees;
            var power = SweetPower(along) * TimingPower(angle);
            var direction = new Vec2(0f, 1f).Rotated(degrees * BaseballBalance.DirectionScale * MathF.PI / 180f);
            if (sweet && just && power >= BaseballBalance.LoftMinPower)
            {
                return new BaseballHit(direction, BaseballBalance.LoftHorizontalSpeed * power, BaseballBalance.LoftVerticalSpeed * power, power, true, false);
            }

            return new BaseballHit(direction, BaseballBalance.MaxHitSpeed * power, 0f, power, false, false);
        }

        /// <summary>バントで当てたときの打球（弱く転がる。手元・先で当てると少し弱く、当てた所で左右へ向きが変わる）</summary>
        public static BaseballHit BuntHit(float along)
        {
            var reach = along >= BaseballBalance.SweetSpot
                ? BaseballBalance.BatLength - BaseballBalance.SweetSpot
                : BaseballBalance.SweetSpot - BaseballBalance.BatInner;
            var t = Math.Clamp((along - BaseballBalance.SweetSpot) / reach, -1f, 1f);
            var speed = BaseballBalance.BuntSpeed * (1f - (1f - BaseballBalance.BuntMinSpeedScale) * MathF.Abs(t));
            // 先（一塁側）で当てると右へ、手元で当てると左へ転がる（左回りが正なので、右へは負に回す）
            var direction = new Vec2(0f, 1f).Rotated(-t * BaseballBalance.BuntSteerDegrees * MathF.PI / 180f);
            return new BaseballHit(direction, speed, 0f, speed / BaseballBalance.MaxHitSpeed, false, true);
        }

        /// <summary>軸からの距離での強さ。芯の中はほぼ 1（真ん中が一番）、外は根元・先へ向かって MinSweetPower まで下がる</summary>
        internal static float SweetPower(float along)
        {
            var offset = MathF.Abs(along - BaseballBalance.SweetSpot);
            var edge = 1f - BaseballBalance.SweetEdgeLoss;
            if (offset <= BaseballBalance.SweetHalfWidth)
            {
                return 1f - BaseballBalance.SweetEdgeLoss * offset / BaseballBalance.SweetHalfWidth;
            }

            var end = along >= BaseballBalance.SweetSpot
                ? BaseballBalance.BatLength - BaseballBalance.SweetSpot
                : BaseballBalance.SweetSpot - BaseballBalance.BatInner;
            var t = Math.Clamp((offset - BaseballBalance.SweetHalfWidth) / Math.Max(end - BaseballBalance.SweetHalfWidth, 0.01f), 0f, 1f);
            return edge + (BaseballBalance.MinSweetPower - edge) * t;
        }

        /// <summary>当たった角度での強さ。ジャストの中はほぼ 1、外は早すぎ・遅すぎほど MinTimingPower まで下がる</summary>
        internal static float TimingPower(float absDegrees)
        {
            var edge = 1f - BaseballBalance.JustEdgeLoss;
            if (absDegrees <= BaseballBalance.JustDegrees)
            {
                return 1f - BaseballBalance.JustEdgeLoss * absDegrees / BaseballBalance.JustDegrees;
            }

            var t = Math.Clamp((absDegrees - BaseballBalance.JustDegrees) / (BaseballBalance.TimingFalloffDegrees - BaseballBalance.JustDegrees), 0f, 1f);
            return edge + (BaseballBalance.MinTimingPower - edge) * t;
        }
    }

    /// <summary>バットに当たった打球の出だし</summary>
    public readonly struct BaseballHit
    {
        public BaseballHit(Vec2 direction, float speed, float verticalSpeed, float power, bool lofted, bool bunt)
        {
            Direction = direction;
            Speed = speed;
            VerticalSpeed = verticalSpeed;
            Power = power;
            Lofted = lofted;
            Bunt = bunt;
        }

        /// <summary>打球の向き（長さ 1）</summary>
        public Vec2 Direction { get; }

        /// <summary>横の速さ</summary>
        public float Speed { get; }

        /// <summary>上向きの速さ（浮いた球だけ）</summary>
        public float VerticalSpeed { get; }

        /// <summary>強さ 0〜1（音と演出に使う）</summary>
        public float Power { get; }

        public bool Lofted { get; }
        public bool Bunt { get; }
    }
}
