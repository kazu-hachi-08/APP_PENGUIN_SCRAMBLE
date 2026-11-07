using System;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// 球とバットの調整値（長さはマス・時間は秒・角度は度）。ホストが計算して全員に配るので、Unity の物理は使わず、ここの数で決める。
    /// 「（案）」の値は試遊と試合の確認で決め直す
    /// </summary>
    public static partial class BaseballBalance
    {
        /// <summary>球の半径（バット・穴・フェンスとの当たりに使う）</summary>
        public const float BallRadius = 0.12f;

        /// <summary>球を細かく分けて進める1回の長さ（穴の半径より十分短く。速い打球が穴をすり抜けないように）</summary>
        public const float BallSubstepLength = 0.08f;

        // ---- 投げる ----

        /// <summary>
        /// 道のりのこの割合までは曲げられる（案）。最後の区間はまっすぐ来る
        /// （打つ人が読める区間を残すため。オンラインの遅れもこの区間で吸収する）
        /// </summary>
        public const float BendableFraction = 0.7f;

        /// <summary>投げた球が左右へ行ける幅（ホームの真ん中から。盤の溝の幅）</summary>
        public const float PitchMaxOffset = 1.6f;

        /// <summary>球種ごとの速さと曲げやすさ（案）</summary>
        public static BaseballPitchSpec Spec(BaseballPitchType type)
        {
            return type switch
            {
                BaseballPitchType.Straight => new BaseballPitchSpec(0.7f, 4f, 1.2f),
                BaseballPitchType.Curve => new BaseballPitchSpec(1.0f, 9f, 2.6f),
                BaseballPitchType.Changeup => new BaseballPitchSpec(1.4f, 5f, 1.6f),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };
        }

        // ---- バット ----

        /// <summary>振ってから振り終わるまで（角度は一定の速さで回る。読みやすいように）</summary>
        public const float SwingSeconds = 0.16f;

        /// <summary>
        /// 構えの角度と振り終わりの角度（度。0 ＝ ホームの上を横切る向き。正が投手の方へ回った向き）。
        /// 早く振ると大きい角度で当たって引っ張り（左）、遅いと負の角度で当たって流し（右）になる
        /// </summary>
        public const float SwingStartDegrees = -75f;
        public const float SwingEndDegrees = 100f;

        /// <summary>バントで寝かせる角度（ホームの上を横切る向き）</summary>
        public const float BuntDegrees = 0f;

        /// <summary>手元（握り）からバットが当たり始める所・先までの長さ（軸から）</summary>
        public const float BatInner = 0.35f;
        public const float BatLength = 1.9f;

        /// <summary>芯の位置（軸から）と、芯として強く打てる幅（片側）</summary>
        public const float SweetSpot = 1.15f;
        public const float SweetHalfWidth = 0.2f;

        /// <summary>芯の中でも真ん中から縁へ行くと少し弱くなる割合（ジャスト＆芯の中にも差をつけ、ホームランを真ん中だけにする）</summary>
        public const float SweetEdgeLoss = 0.15f;

        /// <summary>根元・先で当たったときの強さ（ボテボテ）</summary>
        public const float MinSweetPower = 0.3f;

        /// <summary>この角度（度）までに当たればジャスト（タイミングがぴったり）</summary>
        public const float JustDegrees = 15f;

        /// <summary>ジャストの中でも端へ行くと少し弱くなる割合</summary>
        public const float JustEdgeLoss = 0.1f;

        /// <summary>この角度（度）で当たると強さは MinTimingPower まで下がる（振り遅れ・早すぎ）</summary>
        public const float TimingFalloffDegrees = 55f;
        public const float MinTimingPower = 0.6f;

        /// <summary>当たった角度の何倍の向きへ飛ぶか（早すぎ・遅すぎがファウルになるように 1 より大きく）</summary>
        public const float DirectionScale = 1.3f;

        /// <summary>転がる打球の速さ（強さ 1 のとき。案）</summary>
        public const float MaxHitSpeed = 15f;

        /// <summary>強さがこれ以上（ジャスト＆芯）なら浮く</summary>
        public const float LoftMinPower = 0.9f;

        /// <summary>浮いた打球の横の速さと上向きの速さ（強さ 1 のとき。案。ほぼ真ん中だけがフェンスを越える）</summary>
        public const float LoftHorizontalSpeed = 15f;
        public const float LoftVerticalSpeed = 7f;

        /// <summary>バントの打球の速さ（芯で当たったとき。手元・先では少し弱い）と、当たった所で向きが変わる大きさ（度。案）</summary>
        public const float BuntSpeed = 4.2f;
        public const float BuntMinSpeedScale = 0.7f;
        public const float BuntSteerDegrees = 30f;

        // ---- 打球 ----

        /// <summary>重さ（浮いた球の落ち方）</summary>
        public const float Gravity = 12f;

        /// <summary>転がる球が毎秒失う速さ（案。強い当たりは外野まで、弱い当たりは内野で止まる）</summary>
        public const float RollFriction = 7f;

        /// <summary>
        /// 盤のゆるいすり鉢の引き（毎秒。すり鉢の底へ向かう）。動いている球にだけ効かせる
        /// （止まった球が勝手に動き出さないように）。フェンスで跳ね返った球が外野の奥でいつまでも転がらないようにする
        /// </summary>
        public const float BowlPull = 0.6f;

        /// <summary>この速さより遅くなったら止まった扱い</summary>
        public const float StopSpeed = 0.25f;

        /// <summary>浮いた球が地面に着いたときに残る横の速さと、跳ね返る上向きの速さの割合（これより遅ければ跳ねずに転がる）</summary>
        public const float LandKeep = 0.6f;
        public const float LandBounce = 0.35f;
        public const float MinBounceSpeed = 1.5f;

        /// <summary>フェンスに当たった球に残る速さ（はね返り）</summary>
        public const float FenceBounce = 0.45f;

        /// <summary>穴の上を通る球がこれより遅ければ落ちる（速すぎると縁で曲がって通り過ぎる。ゴルフのカップと同じ形）</summary>
        public const float HoleCaptureSpeed = 5f;

        /// <summary>穴の縁が速い球を穴の真ん中へ引いて曲げる強さ（毎秒。速さは変えない）</summary>
        public const float HoleLipPull = 20f;

        /// <summary>ホームからこの距離までは、ファウルラインの外にいてもまだ決めない（打った直後の球はホームのすぐ横にいるため）</summary>
        public const float FoulGraceDistance = 0.9f;

        /// <summary>打ってからこの秒たっても決まらなければ、その場で止まった扱い（終わらないことが無いように）</summary>
        public const float MaxBallSeconds = 10f;

        // ---- CPU（★ごとの強さは試合の確認 Tools/PenmonScramble/Baseball/CPUの試合の確認 で決めた） ----

        public const int AiMinStars = 1;
        public const int AiMaxStars = 5;

        /// <summary>試しの打席の CPU（★3）</summary>
        public const int AiTestStars = 3;

        public static BaseballAiSkill AiTest => AiSkill(AiTestStars);

        /// <summary>
        /// ★ごとのうまさ（振る時刻のぶれ秒・ゾーンの外を見送る割合・狙いのぶれマス・凍った魔球を待ち直せる割合）。
        /// ぶれが強さの一番の差（★1 はジャストに当たることが少なく、★5 はほとんど芯で捉える）
        /// </summary>
        private const float AiTiming1 = 0.10f;
        private const float AiTiming2 = 0.08f;
        private const float AiTiming3 = 0.06f;
        private const float AiTiming4 = 0.045f;
        private const float AiTiming5 = 0.03f;

        private static readonly BaseballAiSkill[] AiSkills =
        {
            new BaseballAiSkill(AiTiming1, 0.45f, 0.22f, 0.15f),
            new BaseballAiSkill(AiTiming2, 0.55f, 0.17f, 0.25f),
            new BaseballAiSkill(AiTiming3, 0.65f, 0.13f, 0.35f),
            new BaseballAiSkill(AiTiming4, 0.75f, 0.10f, 0.45f),
            new BaseballAiSkill(AiTiming5, 0.85f, 0.07f, 0.55f),
        };

        public static BaseballAiSkill AiSkill(int stars) => AiSkills[Math.Clamp(stars, AiMinStars, AiMaxStars) - AiMinStars];

        /// <summary>曲げ方の型を選ぶ割合（まっすぐ・後半だけ曲げる。残りを外へ・内へで半分ずつ）</summary>
        public const float AiStraightChance = 0.1f;
        public const float AiLateChance = 0.2f;

        /// <summary>曲げる球のねらい: ゾーンの幅のこの割合の所（端の少し内）。わざと外す球はこの割合で、ゾーンの外へこれだけ</summary>
        public const float AiEdgeAim = 0.75f;
        public const float AiWasteChance = 0.3f;
        public const float AiWasteMargin = 0.35f;

        /// <summary>2ストライクに追い込んだら、わざと外す球をこの割合に増やす</summary>
        public const float AiWasteChanceAhead = 0.4f;

        /// <summary>打つ CPU の、球の通る所の見え方のぶれ（振る時刻のぶれ 1 秒あたりのマス。うまい CPU ほど見極めがよい）</summary>
        public const float AiReadError = 3f;

        /// <summary>追い込まれるまで（2ストライク未満）の打つ CPU が、ゾーンの中の球を見送る割合</summary>
        public const float AiTakeStrikeChance = 0.3f;

        /// <summary>追い込まれた打つ CPU は、ゾーンの外でもこれだけの所までの球はカットしにいく</summary>
        public const float AiProtectMargin = 0.15f;

        /// <summary>送りバントの場面で、その打席をバントにする割合と、バントする点差（勝っていても負けていても）</summary>
        public const float AiBuntChance = 0.3f;
        public const int AiBuntMaxLead = 2;

        /// <summary>後半だけ曲げる型は、曲げられる区間の残りがこの割合になってから曲げ始める</summary>
        public const float AiLateFraction = 0.5f;

        /// <summary>打つ CPU のぶれは、マウンドから横へ曲げられた 1 マスごとにこの割合だけ大きくなる</summary>
        public const float AiBendDifficulty = 0.6f;

        /// <summary>球種ごとの打ちにくさ（打つ CPU のぶれに掛ける。速い球と、緩急のチェンジアップを少し打ちにくく）</summary>
        public static float AiTimingDifficulty(BaseballPitchType type)
        {
            return type switch
            {
                BaseballPitchType.Straight => 1.25f,
                BaseballPitchType.Changeup => 1.15f,
                _ => 1f,
            };
        }
    }

    public enum BaseballPitchType
    {
        Straight,
        Curve,
        Changeup,
    }

    /// <summary>球種の速さと曲げやすさ</summary>
    public readonly struct BaseballPitchSpec
    {
        public BaseballPitchSpec(float plateSeconds, float bendAccel, float maxBendSpeed)
        {
            PlateSeconds = plateSeconds;
            BendAccel = bendAccel;
            MaxBendSpeed = maxBendSpeed;
        }

        /// <summary>投げてからホームの線を越えるまでの秒</summary>
        public float PlateSeconds { get; }

        /// <summary>曲げる入力いっぱいのときの横の速さの増え方（毎秒）</summary>
        public float BendAccel { get; }

        /// <summary>横の速さの上限</summary>
        public float MaxBendSpeed { get; }
    }
}
