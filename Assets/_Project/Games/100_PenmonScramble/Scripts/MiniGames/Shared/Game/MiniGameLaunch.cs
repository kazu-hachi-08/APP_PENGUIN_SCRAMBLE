using PenmonScramble.Common.Scene;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Core;
using UnityEngine;

namespace PenmonScramble.MiniGames.Game
{
    /// <summary>
    /// ミニゲームのシーンへの行き来。遊ぶミニゲームとコースはシーンをまたいで残す
    /// （ミニゲームのシーンが読み、本編へ戻ったミニゲーム画面が同じコースを選んだ状態で開く）
    /// </summary>
    public static class MiniGameLaunch
    {
        /// <summary>最後に遊び始めたミニゲーム。まだ無ければ null</summary>
        public static MiniGameDef Game { get; private set; }

        public static int CourseNo { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Game = null;
            CourseNo = 0;
        }

        /// <summary>ひとりで遊ぶ: ミニゲームのシーンへ移る</summary>
        public static void StartSolo(MiniGameDef game, int courseNo)
        {
            Game = game;
            CourseNo = courseNo;
            Load(game.SceneName);
        }

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
        }

        /// <summary>本編のシーンを読み直して、ミニゲーム画面から始める（ロビーの上に開く）</summary>
        public static void ReturnToHub()
        {
            LobbyReturn.Request(LobbyScreen.MiniGames);
            Load(SceneNames.Title);
        }

        /// <summary>本編のシーンを読み直して、ロビーから始める（スタート画面は飛ばす）</summary>
        public static void ReturnToLobby()
        {
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
