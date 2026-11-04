namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// 狙っている人の手元の様子（打つ子・向き・ゲージ・振り下ろし中か・特殊の ON と風の向き）。
    /// オンラインでは番の人の端末がホストへ送り、ホストはスナップショットに載せて全員へ配る（ほかの人にも狙いとゲージが見えるように）
    /// </summary>
    public readonly struct GolfAim
    {
        public readonly int Member;
        public readonly float Angle;
        public readonly float Power;
        public readonly bool GaugeRunning;
        public readonly bool Striking;
        public readonly bool Special;

        /// <summary>風の曲げる向き（右 +1 / 左 −1）</summary>
        public readonly int Curve;

        public GolfAim(int member, float angle, float power, bool gaugeRunning, bool striking, bool special, int curve)
        {
            Member = member;
            Angle = angle;
            Power = power;
            GaugeRunning = gaugeRunning;
            Striking = striking;
            Special = special;
            Curve = curve < 0 ? -1 : 1;
        }

        /// <summary>まだ何も届いていない人（ゲージは止まっていて、特殊は OFF）</summary>
        public static GolfAim Idle(int member, float angle) => new GolfAim(member, angle, 0f, false, false, false, 1);

        public GolfAim WithStriking(bool striking) => new GolfAim(Member, Angle, Power, GaugeRunning, striking, Special, Curve);

        /// <summary>この手元で打つときの1打（ゲージを止めた強さで）</summary>
        public GolfShot ToShot() => new GolfShot(Member, Angle, Power, Special, Curve);
    }

    public enum GolfInputKind
    {
        /// <summary>狙っている様子（向き・ゲージ・打つ子・特殊）。番の間、変わったら送る</summary>
        Aim,
        /// <summary>ゲージを止めて振り下ろした（ホストはクラブが当たった瞬間に打つ）</summary>
        Strike,
        /// <summary>雷の加速（BoostTurn だけ向きを変える）</summary>
        Boost,
    }

    /// <summary>オンラインのゲストが自分の番のときだけホストへ送る入力</summary>
    public readonly struct GolfInput
    {
        public readonly GolfInputKind Kind;
        public readonly GolfAim Aim;

        /// <summary>雷の加速で変える向き（ラジアン。正で左回り）</summary>
        public readonly float BoostTurn;

        private GolfInput(GolfInputKind kind, GolfAim aim, float boostTurn)
        {
            Kind = kind;
            Aim = aim;
            BoostTurn = boostTurn;
        }

        public static GolfInput ForAim(GolfAim aim) => new GolfInput(GolfInputKind.Aim, aim, 0f);

        public static GolfInput ForStrike(GolfAim aim) => new GolfInput(GolfInputKind.Strike, aim, 0f);

        public static GolfInput ForBoost(float turn) => new GolfInput(GolfInputKind.Boost, default, turn);
    }
}
