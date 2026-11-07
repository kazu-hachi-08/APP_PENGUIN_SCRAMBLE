using System.Collections.Generic;
using PenmonScramble.MiniGames.Core;
using UnityEngine;

namespace PenmonScramble.MiniGames.Game
{
    /// <summary>
    /// ホストが待ちの窓で選んだ、チームで戦うミニゲームの遊び方（コース・対戦 / 協力・席ごとの赤・青）。ミニゲームごとに1つ。
    /// シーンをまたいで残す（待ちの窓の板が書き、ミニゲームのシーンのホストがチームを組むときに読む。試合から待ちの窓へ戻ったときも同じ選びで開く）
    /// </summary>
    public sealed class MiniGameRoomSetup
    {
        private static readonly Dictionary<string, MiniGameRoomSetup> ByGame = new Dictionary<string, MiniGameRoomSetup>();

        private readonly int[] _teams = new int[MiniGameRoomChoice.MaxSeats];

        private MiniGameRoomSetup()
        {
            Reset();
        }

        /// <summary>選んだコース（まだ選んでいなければ 0）</summary>
        public int Course { get; set; }

        public MiniGameOnlineMode Mode { get; set; }

        /// <summary>席ごとのチーム（対戦で使う）</summary>
        public int[] Teams => _teams;

        /// <summary>そのミニゲームの選び（MiniGameTable の Id ごと）</summary>
        public static MiniGameRoomSetup For(string gameId)
        {
            if (!ByGame.TryGetValue(gameId, out var setup))
            {
                setup = new MiniGameRoomSetup();
                ByGame[gameId] = setup;
            }

            return setup;
        }

        public void SetTeam(int seat, int team)
        {
            if (seat >= 0 && seat < _teams.Length)
            {
                _teams[seat] = team;
            }
        }

        private void Reset()
        {
            Course = 0;
            // はじめての部屋は、友達どうしで戦う対戦から（ひとりのときは開始できないので、協力へ替える一言を出す）
            Mode = MiniGameOnlineMode.Versus;
            for (var seat = 0; seat < _teams.Length; seat++)
            {
                _teams[seat] = MiniGameTeams.DefaultTeam(seat);
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ByGame.Clear();
        }
    }
}
