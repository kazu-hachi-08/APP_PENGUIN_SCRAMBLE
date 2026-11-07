namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// サッカーの試合のルールの数（チームの数・人数・刻み）。ボールと動きの調整値は SoccerBalance
    /// </summary>
    public static class SoccerRules
    {
        /// <summary>
        /// 1歩の時間。本編と同じ 1/30 秒の固定の刻み（描画のフレームに関係なく、同じ入力なら同じ結果になるように）。
        /// 速いボールが壁をすり抜けないよう、ボールだけはこの中をさらに細かく分けて進める（SoccerBalance.BallSubstepLength）
        /// </summary>
        public const float StepSeconds = 1f / 30f;

        public const int TeamCount = 2;

        /// <summary>1チームのフィールドの子の数（キーパーは別）</summary>
        public const int PlayersPerTeam = 3;

        /// <summary>1試合の長さ（秒。案。ゴールの演出・カットインの間は減らない）</summary>
        public const float MatchSeconds = 180f;

        /// <summary>キックオフの並びで「キックオフ！」を見せる間（この間はボールに触れない）</summary>
        public const float KickoffWaitSeconds = 1.2f;

        /// <summary>点が入ってから並び直すまで（「ゴール！」の演出。時間は止まる）</summary>
        public const float GoalPauseSeconds = 2f;

        /// <summary>時間切れの笛から、結果か PK戦へ移るまで</summary>
        public const float TimeUpSeconds = 2f;

        /// <summary>残りがこの秒になったら、1秒ごとに数える</summary>
        public const int CountdownFrom = 10;

        /// <summary>PK戦の1チームの本数（同点ならサドンデス）と、サドンデスを続けるいちばん多い組の数（終わらないことが無いように）</summary>
        public const int PenaltyRounds = 3;
        public const int PenaltyMaxRounds = 30;

        /// <summary>赤（左のゴールを守り、右へ攻める）</summary>
        public const int Red = 0;

        /// <summary>青（右のゴールを守り、左へ攻める）</summary>
        public const int Blue = 1;

        public static int Opponent(int team) => 1 - team;
    }
}
