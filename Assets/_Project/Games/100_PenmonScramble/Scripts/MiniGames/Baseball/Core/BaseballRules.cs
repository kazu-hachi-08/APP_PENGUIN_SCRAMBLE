namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// 野球のルールの数（刻み・カウント・1球の持ち時間・イニング・点差で早く終わる条件）。球とバットの調整値は BaseballBalance、盤の形は BaseballFieldDef
    /// </summary>
    public static class BaseballRules
    {
        /// <summary>
        /// 1歩の時間。本編・サッカーと同じ 1/30 秒の固定の刻み（描画のフレームに関係なく、同じ入力なら同じ結果になるように）。
        /// 速い球が穴やバットをすり抜けないよう、球はこの中をさらに細かく分けて進める（BaseballBalance.BallSubstepLength）
        /// </summary>
        public const float StepSeconds = 1f / 30f;

        /// <summary>このボールの数で四球</summary>
        public const int BallsForWalk = 4;

        /// <summary>このストライクの数で三振（ファウルではここまで足さない）</summary>
        public const int StrikesForOut = 3;

        /// <summary>このアウトの数で攻守交代</summary>
        public const int OutsPerInning = 3;

        /// <summary>1球の持ち時間（秒。案）。過ぎたら、そのとき選んでいる球種でまっすぐ投げる</summary>
        public const float PitchClockSeconds = 8f;

        /// <summary>試合のイニングの数。終わって同点なら引き分け</summary>
        public const int Innings = 3;

        /// <summary>
        /// このイニングが終わった後（後攻が勝っていればその途中でも）に、点差がこれ以上開いていたら早く終わる（案）。
        /// CPU の強さが段違いのときにだらだら続かないように
        /// </summary>
        public const int MercyInning = 2;
        public const int MercyRuns = 10;

        /// <summary>チームの番号。先攻（表に打つ・赤）と後攻（裏に打つ・青）</summary>
        public const int Away = 0;
        public const int Home = 1;
        public const int TeamCount = 2;

        /// <summary>塁の数（一塁〜三塁）。ホームへ還ったら点</summary>
        public const int BaseCount = 3;
    }
}
