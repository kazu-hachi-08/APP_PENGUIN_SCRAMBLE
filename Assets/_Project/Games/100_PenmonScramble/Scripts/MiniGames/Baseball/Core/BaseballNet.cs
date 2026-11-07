using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// 試合の区切り（1球の流れ）。オンラインではホストの区切りをゲストへ送るので、一度配ったら番号を変えない
    /// </summary>
    public enum BaseballStage : byte
    {
        /// <summary>構え（投げる人が球種を選ぶ・CPU は少し待つ）</summary>
        Ready,
        /// <summary>振りかぶり</summary>
        WindUp,
        /// <summary>魔球・必殺打のカットイン（盤は止まる）</summary>
        CutIn,
        /// <summary>球が動いている</summary>
        Live,
        /// <summary>1球の結果を見せている</summary>
        Result,
        /// <summary>3アウトの「チェンジ」を見せている</summary>
        Change,
        /// <summary>ゲームセット</summary>
        Over,
    }

    /// <summary>オンラインの間隔と待つ上限</summary>
    public static class BaseballNetRules
    {
        /// <summary>ホストがスナップショットを送る間隔（秒。毎秒15回）。区切りが変わったときと、球が動いている間はもっと早く送る</summary>
        public const float SnapshotSeconds = 1f / 15f;

        /// <summary>球が動いている間の、ホストのスナップショットとゲストの入力の間隔（1歩ごと。相手の入力を早く届けて、計算し直しを小さくする）</summary>
        public const float LiveSendSeconds = BaseballRules.StepSeconds;

        /// <summary>ゲストが球の動いていない間に入力を送る間隔（必殺打の ON・投げる子の選び）</summary>
        public const float IdleSendSeconds = 0.1f;

        /// <summary>ゲストがシーンに入ってから「準備できた（編成）」を送り直す間隔（秒）。最初のスナップショットが届くまで</summary>
        public const float ReadyResendSeconds = 0.5f;

        /// <summary>ホストが全員の準備を待つ上限（秒）。過ぎたら、届いていない人は仮の子で始める</summary>
        public const float ReadyTimeoutSeconds = 6f;

        /// <summary>ゲストがホストの最初のスナップショットを待つ上限（秒）</summary>
        public const float GuestGiveUpSeconds = 20f;

        /// <summary>
        /// ホストが、自分の計算で1球の結果が出てから、遅れている人の入力を待つ上限（秒）。
        /// 過ぎたら届いた所までで決める（タブが裏に回って止まった人を、ずっと待たないように）
        /// </summary>
        public const float CommitWaitSeconds = 2.5f;

        /// <summary>人（ゲスト）が投げるとき、ホストが1球の持ち時間より長く待つ分（秒。投げた知らせが遅れて届いても、持ち時間切れにしないように）</summary>
        public const float PitchClockGraceSeconds = 1.5f;

        /// <summary>試合を通して送る打順のいちばん多い人数（壊れた値で配列を作りすぎないように）</summary>
        public const int MaxLineup = 8;
    }

    /// <summary>
    /// ホストからゲストへ送る、試合のスナップショット（組み方・試合の様子・区切り・今の球の投げ方と2人の入力の記録・打点）。
    /// ゲストは試合を計算せず、区切りが変わったら演出を出し、球が動いている間は投げ方と記録から自分の端末で1球を計算する
    /// </summary>
    public sealed class BaseballSnapshot
    {
        public int Stadium;
        public MiniGameOnlineMode Mode;
        public int CpuStars;

        /// <summary>席ごとのチーム（抜けた席も残す）と、抜けたか</summary>
        public int[] SeatTeam = System.Array.Empty<int>();
        public bool[] SeatLeft = System.Array.Empty<bool>();

        /// <summary>チームごとの打順の種類と、その子の持ち主の席（CPU は −1）</summary>
        public int[][] Lineup = { System.Array.Empty<int>(), System.Array.Empty<int>() };
        public int[][] Owner = { System.Array.Empty<int>(), System.Array.Empty<int>() };

        public BaseballGameState State = new BaseballGameState();

        public BaseballStage Stage;

        /// <summary>これまでに投げた球の数（今の球・最後の球の番号）</summary>
        public int ThrowCount;

        /// <summary>この打席に投げる人の席（CPU は −1）</summary>
        public int PitcherSeat = -1;

        /// <summary>今の球（ThrowCount 番）の投げ方。まだ投げていない打席の始めなどは false</summary>
        public bool HasThrow;
        public BaseballThrow Throw;
        public BaseballRoleLog PitcherLog = new BaseballRoleLog();
        public BaseballRoleLog BatterLog = new BaseballRoleLog();

        /// <summary>今の球の結果をホストが決めた（記録はこれで最後）。DoneStep は結果が出た歩の数</summary>
        public bool Committed;
        public int DoneStep;

        /// <summary>決めた結果（Committed のときだけ。ゲストの計算と少しでも違っても、試合の様子はホストに合わせるため）</summary>
        public BaseballPitchOutcome Outcome = BaseballPitchOutcome.None;
        public BaseballPlateResult Plate;
        public int Runs;

        /// <summary>席ごとの打点（MVP に使う）</summary>
        public int[] SeatRbi = System.Array.Empty<int>();

        public int SeatCount => SeatTeam.Length;
    }

    /// <summary>
    /// ゲストからホストへ送る入力（自分の番の物だけ）。毎回ぜんぶを送る（届いた中でいちばん新しい物だけを使えばよいように）。
    /// 投げる人: 投げる子・球種・魔球の ON と中身の乱数（投げた知らせ）・曲げる入力の記録。打つ人: 必殺打の ON・振る / バントの記録
    /// </summary>
    public sealed class BaseballNetInput
    {
        /// <summary>この入力の球の番号（構えの間は次に投げる球の番号）</summary>
        public int ThrowId;

        /// <summary>投げる子に選んだ打順（選んでいなければ −1）</summary>
        public int PitcherSlot = -1;

        /// <summary>打つ人の必殺打の ON</summary>
        public bool SpecialOn;

        /// <summary>投げた（球種・魔球が決まった）</summary>
        public bool Thrown;
        public BaseballPitchType Type;
        public bool MagicOn;
        public float FreezeSeconds;
        public float WobblePhase;

        /// <summary>自分の番の入力の記録（投げる人は曲げる入力、打つ人は振る・バント・打球を曲げる入力）</summary>
        public BaseballRoleLog Log = new BaseballRoleLog();
    }
}
