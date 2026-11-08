using System;
using System.Collections.Generic;
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
    /// みんなで遊ぶときは、部屋（OnlineSession）を試合ごと次のシーンへ引き継ぐ
    /// </summary>
    public static class MiniGameLaunch
    {
        /// <summary>最後に遊び始めたミニゲーム。まだ無ければ null</summary>
        public static MiniGameDef Game { get; private set; }

        public static int CourseNo { get; private set; }

        /// <summary>みんなで遊んでいる（部屋の待ちの窓から始めた）。ひとりで遊ぶ・戻ると false</summary>
        public static bool IsOnline { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Game = null;
            CourseNo = 0;
            IsOnline = false;
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
        public static void StartOnline(MiniGameDef game, int courseNo, OnlineSession session)
        {
            session.CarryMatchToNextScene();
            Game = game;
            CourseNo = courseNo;
            IsOnline = true;
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

        /// <summary>
        /// みんなで: 部屋を試合ごと引き継げていなければ（部屋が切れた・試合が始まっていない）警告を出して true。
        /// 呼び出し側は _leaving を立ててからロビーへ戻る
        /// </summary>
        public static bool IsRoomLost(OnlineSession session, string runnerName)
        {
            if (session.IsInRoom && session.IsMatchStarted)
            {
                return false;
            }

            Debug.LogWarning($"[{runnerName}] 部屋を引き継げなかったのでロビーへ戻ります");
            return true;
        }

        /// <summary>
        /// みんなで: 3つのミニゲームで同じ部屋の配線。人が抜けても試合を止めず（ContinueWhenMemberLeaves）、
        /// 切断・接続切れは leave（ロビーへ戻る）で受ける。解除は UnbindRoom に同じデリゲートを渡すこと
        /// </summary>
        public static void BindRoom(OnlineSession session, Action leave)
        {
            session.ContinueWhenMemberLeaves = true;
            session.OnPeerDisconnected += leave;
            session.OnConnectionLost += leave;
        }

        /// <summary>みんなで: BindRoom の配線を外す（OnDestroy）</summary>
        public static void UnbindRoom(OnlineSession session, Action leave)
        {
            session.OnPeerDisconnected -= leave;
            session.OnConnectionLost -= leave;
        }

        /// <summary>
        /// みんなで（ホストのみ）: 結果で「もう一度・選び直し」へ進むとき、部屋を全員の待ちの窓へ戻してから本編の待ちの窓へ移る。
        /// 先に OnlineSession.ReturnToRoom を呼んでゲストにも知らせる必要がある
        /// </summary>
        public static void LeaveToRoom(OnlineSession session)
        {
            session.ReturnToRoom();
            ReturnToRoom();
        }

        /// <summary>この端末の編成のペンモンの種類（並び順のまま）。ひとり・みんなで共通</summary>
        public static List<int> PartySpecies()
        {
            var species = new List<int>();
            foreach (var penguin in GameSave.Record.PartyPenguins())
            {
                species.Add(penguin.speciesNo);
            }

            return species;
        }

        /// <summary>本編のシーンを読み直して、ミニゲーム画面から始める（ロビーの上に開く）。部屋にいれば抜ける</summary>
        public static void ReturnToHub() => ReturnToTitle(LobbyScreen.MiniGames);

        /// <summary>本編のシーンを読み直して、ロビーから始める（スタート画面は飛ばす）。部屋にいれば抜ける</summary>
        public static void ReturnToLobby() => ReturnToTitle(LobbyScreen.Lobby);

        /// <summary>
        /// みんなでのみ: 同じ顔ぶれのまま部屋の待ちの窓へ戻る（本編のシーンを読み直すと、待ちの窓が部屋を引き継いで開く）。
        /// ホストは先に OnlineSession.ReturnToRoom を呼んでゲストにも知らせる。ゲストは OnReturnToRoom を受けて呼ぶ
        /// </summary>
        public static void ReturnToRoom() => ReturnToTitle(LobbyScreen.Lobby);

        /// <summary>本編のシーンを読み直して、指定のロビー画面から始める（3つの戻り先で共通の手順）</summary>
        private static void ReturnToTitle(LobbyScreen screen)
        {
            IsOnline = false;
            LobbyReturn.Request(screen);
            Load(SceneNames.Title);
        }

        private static void Load(string sceneName)
        {
            // SceneLoader は無ければその場で作られる（フェードつきで移る）。読み込み中の2度押しは SceneLoader が無視する
            SceneLoader.Instance.LoadScene(sceneName);
        }
    }
}
