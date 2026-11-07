using System;
using System.Collections.Generic;

namespace PenmonScramble.MiniGames.Core
{
    /// <summary>
    /// チームで戦うミニゲームの待ちの窓でホストが選んだもの（コース・対戦 / 協力・席ごとの赤・青）。ゲストへ送って同じ板を見せる
    /// （サッカーは scramble.soccer.choice、野球は scramble.baseball.choice）。
    /// 並びは配った版どうしで合わせるので変えない（足すときは後ろへ。古い版は後ろを読まない）
    /// </summary>
    public sealed class MiniGameRoomChoice
    {
        public const int MaxSeats = MiniGameTeams.MaxSeats;

        // コース・遊び方・人数・席のチーム（ビット。立っていれば青）
        private const int EncodedLength = 4;

        private readonly int[] _teams = new int[MaxSeats];

        public MiniGameRoomChoice(int course, MiniGameOnlineMode mode, int seatCount, IReadOnlyList<int> teams = null)
        {
            Course = course;
            Mode = mode;
            SeatCount = Math.Clamp(seatCount, 1, MaxSeats);
            for (var seat = 0; seat < MaxSeats; seat++)
            {
                _teams[seat] = teams != null && seat < teams.Count ? teams[seat] : MiniGameTeams.DefaultTeam(seat);
            }
        }

        /// <summary>コース（サッカーのスタジアム・野球の球場）の番号</summary>
        public int Course { get; }

        public MiniGameOnlineMode Mode { get; }

        /// <summary>部屋にいる人数（ホストを含む）。席は P1（ホスト）から入った順</summary>
        public int SeatCount { get; }

        /// <summary>席ごとのチーム（対戦で使う。人数より後ろの席も、入ってきたときのために覚えておく）</summary>
        public IReadOnlyList<int> Teams => _teams;

        public int TeamOf(int seat) => seat >= 0 && seat < MaxSeats ? _teams[seat] : MiniGameTeams.DefaultTeam(seat);

        public bool CanStart => MiniGameTeams.CanStart(Mode, _teams, SeatCount);

        public byte[] Encode()
        {
            var bits = 0;
            for (var seat = 0; seat < MaxSeats; seat++)
            {
                if (_teams[seat] == MiniGameTeams.Blue)
                {
                    bits |= 1 << seat;
                }
            }

            return new[] { (byte)Course, (byte)Mode, (byte)SeatCount, (byte)bits };
        }

        /// <summary>短い・知らない遊び方のときは null（相手が新しい版なら、後ろに足された物は読まない）</summary>
        public static MiniGameRoomChoice Decode(byte[] data)
        {
            if (data == null || data.Length < EncodedLength || data[1] > (byte)MiniGameOnlineMode.Coop)
            {
                return null;
            }

            var teams = new int[MaxSeats];
            for (var seat = 0; seat < MaxSeats; seat++)
            {
                teams[seat] = (data[3] & (1 << seat)) != 0 ? MiniGameTeams.Blue : MiniGameTeams.Red;
            }

            return new MiniGameRoomChoice(data[0], (MiniGameOnlineMode)data[1], data[2], teams);
        }
    }
}
