using PenmonScramble.Common.Online;
using PenmonScramble.Common.Scene;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Core;
using UnityEngine;

namespace PenmonScramble.MiniGames.Game
{
    /// <summary>
    /// ミニゲームのシーンへの行き来。遊ぶミニゲームとコースはシーンをまたいで残す
    /// （ミニゲームのシーンが読み、本編へ戻ったミニゲーム画面が同じコースを選んだ状態で開く）。
    /// みんなで遊ぶときは、部屋（OnlineSession）を試合ごと次のシーンへ引き継ぎ、席番号と人数もここで渡す
    /// </summary>
    public static class MiniGameLaunch
    {
        /// <summary>最後に遊び始めたミニゲーム。まだ無ければ null</summary>
        public static MiniGameDef Game { get; private set; }

        public static int CourseNo { get; private set; }

        /// <summary>みんなで遊んでいる（部屋の待ちの窓から始めた）。ひとりで遊ぶ・戻ると false</summary>
        public static bool IsOnline { get; private set; }

        /// <summary>みんなでのみ: 自分の席番号（ホスト=0）</summary>
        public static int Seat { get; private set; }

        /// <summary>みんなでのみ: 始めたときの人数</summary>
        public static int PlayerCount { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Game = null;
            CourseNo = 0;
            IsOnline = false;
            Seat = 0;
            PlayerCount = 0;
        }

        /// <summary>ひとりで遊ぶ: ミニゲームのシーンへ移る</summary>
        public static void StartSolo(MiniGameDef game, int courseNo)
        {
            Game = game;
            CourseNo = courseNo;
            IsOnline = false;
            Load(game.SceneName);
        }

        /// <summary>
        /// みんなで遊ぶ（部屋の待ちの窓で試合が始まった。全員の端末で呼ぶ）: 部屋を試合ごと引き継いで、ミニゲームのシーンへ移る。
        /// ゲストの courseNo は待ちの窓で届いたもの（届いていなければ 0。正しいコースはシーンに入ってからホストが送る）
        /// </summary>
        public static void StartOnline(MiniGameDef game, int courseNo, OnlineSession session, int seat, int playerCount)
        {
            session.CarryMatchToNextScene();
            Game = game;
            CourseNo = courseNo;
            IsOnline = true;
            Seat = seat;
            PlayerCount = playerCount;
            Load(game.SceneName);
        }

        /// <summary>みんなでのみ: ホストから正しいコースが届いた（コース選びへ戻ったときに同じコースを選んでおくため）</summary>
        public static void SetCourse(int courseNo) => CourseNo = courseNo;

        /// <summary>
        /// エディタでミニゲームのシーンから再生したときは、ミニゲーム画面を通っていないので、そのミニゲームの最初のコースにする
        /// </summary>
        public static void EnsureSelected(string gameId)
        {
            if (Game != null && Game.Id == gameId)
            {
                return;
            }

            Game = MiniGameTable.Get(gameId);
            CourseNo = Game != null && Game.Courses.Count > 0 ? Game.Courses[0].No : 0;
            IsOnline = false;
        }

        /// <summary>本編のシーンを読み直して、ミニゲーム画面から始める（ロビーの上に開く）。部屋にいれば抜ける</summary>
        public static void ReturnToHub()
        {
            IsOnline = false;
            LobbyReturn.Request(LobbyScreen.MiniGames);
            Load(SceneNames.Title);
        }

        /// <summary>本編のシーンを読み直して、ロビーから始める（スタート画面は飛ばす）。部屋にいれば抜ける</summary>
        public static void ReturnToLobby()
        {
            IsOnline = false;
            LobbyReturn.Request(LobbyScreen.Lobby);
            Load(SceneNames.Title);
        }

        /// <summary>
        /// みんなでのみ: 同じ顔ぶれのまま部屋の待ちの窓へ戻る（本編のシーンを読み直すと、待ちの窓が部屋を引き継いで開く）。
        /// ホストは先に OnlineSession.ReturnToRoom を呼んでゲストにも知らせる。ゲストは OnReturnToRoom を受けて呼ぶ
        /// </summary>
        public static void ReturnToRoom()
        {
            IsOnline = false;
            LobbyReturn.Request(LobbyScreen.Lobby);
            Load(SceneNames.Title);
        }

        private static void Load(string sceneName)
        {
            // SceneLoader は無ければその場で作られる（フェードつきで移る）。読み込み中の2度押しは SceneLoader が無視する
            SceneLoader.Instance.LoadScene(sceneName);
        }
    }
}
