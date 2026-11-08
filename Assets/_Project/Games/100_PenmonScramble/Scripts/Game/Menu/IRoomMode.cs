namespace PenmonScramble.Game
{
    /// <summary>
    /// 部屋の待ちの窓で選べる「遊ぶもの」1つ（ボスラッシュ・ゴルフ・…）。待ちの窓の右の板（その遊びの選択）を受け持つ。
    /// 本編はミニゲームを知らないので、ミニゲームは自分の板をこの形で作り、RoomModeSelector に並べてもらう
    /// （SceneBuilder がつなぐ。Docs/PENMON_SCRAMBLE_SPEC.md §13.1「新しいミニゲームを足すときの手順」の 4）
    /// </summary>
    public interface IRoomMode
    {
        /// <summary>通信でホストとゲストが同じものを指すための名前（記録には残らない。ミニゲームは MiniGameTable の Id）</summary>
        string Id { get; }

        /// <summary>札に出す名前</summary>
        string Label { get; }

        /// <summary>
        /// 選ばれた・外れた（部屋の外にいる間は false）。選ばれている間だけ自分の板を出し、
        /// ホストなら ModeSelectPanel.SetStartAllowed で開始できるかを知らせる
        /// </summary>
        void SetSelected(bool selected);

        /// <summary>ミニゲーム画面の「みんなで」などから、選んでおくもの（ゴルフならコースの番号）を頼まれた</summary>
        void Preselect(int option);

        /// <summary>
        /// 試合が始まった（全員の端末で呼ぶ）。別のシーンへ移って遊ぶなら、移り始めて true。
        /// false なら本編のシーンの中で遊ぶ（StageRunner がステージを始める）
        /// </summary>
        bool StartMatch(int seat, int playerCount);
    }
}
