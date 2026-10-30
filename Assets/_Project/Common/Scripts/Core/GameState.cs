namespace PenmonScramble.Common.Core
{
    /// <summary>
    /// ゲーム共通の実行状態
    /// </summary>
    public enum GameState
    {
        /// <summary>初期化・カウントダウン前</summary>
        Ready,

        /// <summary>カウントダウン演出中</summary>
        Countdown,

        /// <summary>ゲームプレイ中</summary>
        Playing,

        /// <summary>一時停止中</summary>
        Paused,

        /// <summary>得点・イベント演出中</summary>
        Event,

        /// <summary>ゲーム終了</summary>
        GameOver,

        /// <summary>リザルト画面表示中</summary>
        Result
    }

    /// <summary>
    /// ゲーム状態変更イベントのデリゲート
    /// </summary>
    public delegate void GameStateChangedHandler(GameState previousState, GameState newState);
}
