using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// 飛んでいる必殺シュート1本の様子。飛んでいる間は、フィールドの子は誰も持てない（止められるのはキーパーだけ）。
    /// 壁・網・キーパーに当たる・ゴール・SoccerBalance.SpecialMaxSeconds たつ（水は跳ね終わる）と、ふつうのボールに戻る。
    /// 書き換えるのは SoccerWorld だけ
    /// </summary>
    public sealed class SoccerSpecialShot
    {
        internal SoccerSpecialShot(int shooter, int team, Element element, int stars)
        {
            Shooter = shooter;
            Team = team;
            Element = element;
            Stars = stars;
        }

        public int Shooter { get; }

        public int Team { get; }

        public Element Element { get; }

        public int Stars { get; }

        /// <summary>蹴ってからの秒</summary>
        public float Seconds { get; internal set; }

        /// <summary>雷: 消えている間（見た目はボールを隠す）</summary>
        public bool IsHidden => Flight.HiddenSeconds > 0f;

        /// <summary>雷: 飛ぶ向き（消えている間に止まっても、現れた後はこの向きへ飛ぶ）と、現れた後の速さ</summary>
        internal Vec2 Direction { get; set; }

        internal float AppearSpeed { get; set; }

        /// <summary>風: 向きを変える速さ（ラジアン毎秒。左回りが +）</summary>
        internal float TurnRate { get; set; }

        /// <summary>飛び方の数え（雷の消えている残り・水の跳ねる残り）。ボールの速さ・高さは SoccerBall が持つ</summary>
        internal SoccerSpecialFlight Flight;

        /// <summary>ボールに掛かる重さ（水だけ重い）</summary>
        internal float Gravity => Element == Element.Water ? SoccerBalance.Gravity * SoccerBalance.WaterGravityScale : SoccerBalance.Gravity;
    }

    /// <summary>
    /// 必殺シュートの飛び方の1歩（位置は動かさない。速さと高さだけ）。本当のボールを進めるときと、キーパーが行き先を読むとき
    /// （曲がる・跳ねる・消える球を、まっすぐな球と同じ読み方では読めないため）に同じ計算を使う
    /// </summary>
    internal struct SoccerSpecialFlight
    {
        public Vec2 Velocity;
        public float Height;
        public float VerticalSpeed;
        public float HiddenSeconds;
        public int HopsLeft;
        public float HopSpeed;

        /// <summary>この歩で地面に着いた（水）・跳ね終わった（水）・現れた（雷）</summary>
        public bool Landed;
        public float LandSpeed;
        public bool HopsDone;
        public bool Appeared;

        public void Advance(SoccerSpecialShot shot, float dt)
        {
            Landed = false;
            HopsDone = false;
            Appeared = false;
            switch (shot.Element)
            {
                case Element.Water:
                    Hop(shot, dt);
                    break;
                case Element.Wind:
                    Velocity = Velocity.Rotated(shot.TurnRate * dt);
                    break;
                case Element.Thunder:
                    if (HiddenSeconds > 0f)
                    {
                        HiddenSeconds -= dt;
                        if (HiddenSeconds <= 0f)
                        {
                            HiddenSeconds = 0f;
                            Velocity = shot.Direction * shot.AppearSpeed;
                            Appeared = true;
                        }
                    }

                    break;
            }
        }

        /// <summary>水: 重い放物線で落ち、着いたら残りの回数だけ跳ね上がる（山ごとに低く短く）</summary>
        private void Hop(SoccerSpecialShot shot, float dt)
        {
            if (HopsLeft < 0)
            {
                return;
            }

            VerticalSpeed -= shot.Gravity * dt;
            Height += VerticalSpeed * dt;
            if (Height > 0f)
            {
                return;
            }

            Landed = true;
            LandSpeed = -VerticalSpeed;
            Height = 0f;
            if (HopsLeft == 0)
            {
                VerticalSpeed = 0f;
                HopsLeft = -1;
                HopsDone = true;
                return;
            }

            HopsLeft--;
            VerticalSpeed = HopSpeed;
            HopSpeed *= SoccerBalance.WaterHopRatio;
        }
    }
}
