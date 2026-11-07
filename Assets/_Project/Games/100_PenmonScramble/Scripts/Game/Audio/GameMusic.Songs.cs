namespace PenmonScramble.Game
{
    /// <summary>
    /// 曲の楽譜。1つの記号が8分音符1つ分で、1行が1小節（8つ）。
    /// 音名（C4, G#3 など）で鳴らし、「-」は前の音を伸ばす、「.」は休み。
    /// メロディーとベースは同じ長さにする（曲の長さはメロディーで決まり、そこでループする）
    /// </summary>
    public static partial class GameMusic
    {
        private sealed class Song
        {
            public string Name;
            public float Bpm;
            public string Melody;
            public string Bass;
            public string Drums;
        }

        private static Song SongOf(MusicTrack track)
        {
            switch (track)
            {
                case MusicTrack.Stage: return StageSong;
                case MusicTrack.Boss: return BossSong;
                case MusicTrack.Golf: return GolfSong;
                case MusicTrack.Soccer: return SoccerSong;
                case MusicTrack.Baseball: return BaseballSong;
                default: return TitleSong;
            }
        }

        // タイトル: おもちゃのペンギンが行進する感じの、明るく跳ねる曲（C - Am - F - G）
        private static readonly Song TitleSong = new Song
        {
            Name = "Bgm_Title",
            Bpm = 132f,
            Melody = @"
                C5 - G4 - E4 G4 C5 -
                A4 - E4 - A4 C5 E5 -
                F5 - C5 - A4 - C5 -
                D5 - B4 - G4 - . .
                C5 - G4 - E4 G4 C5 E5
                A4 - C5 - E5 - A5 -
                F5 - E5 - D5 - C5 -
                B4 - D5 - C5 - - -",
            Bass = @"
                C3 - G3 - C3 - G3 -
                A2 - E3 - A2 - E3 -
                F2 - C3 - F2 - C3 -
                G2 - D3 - G2 - D3 -
                C3 - G3 - C3 - G3 -
                A2 - E3 - A2 - E3 -
                F2 - C3 - F2 - C3 -
                G2 - D3 - G2 - G2 -",
            Drums = "k.h.s.h.",
        };

        // ステージ: 大勢をなぎ倒していく、速くて前のめりな曲（Am - Dm - F - G - C - E - Am - E）
        private static readonly Song StageSong = new Song
        {
            Name = "Bgm_Stage",
            Bpm = 150f,
            Melody = @"
                A4 . A4 C5 E5 - D5 C5
                D5 - C5 - A4 - F4 -
                F4 . F4 A4 C5 - B4 A4
                G4 - - - B4 - D5 -
                E5 . E5 D5 C5 - B4 A4
                B4 - G#4 - E4 - B4 -
                A4 - C5 - E5 - A5 -
                G#5 - E5 - B4 - G#4 -",
            Bass = @"
                A2 A3 A2 A3 A2 A3 A2 A3
                D2 D3 D2 D3 D2 D3 D2 D3
                F2 F3 F2 F3 F2 F3 F2 F3
                G2 G3 G2 G3 G2 G3 G2 G3
                C3 C4 C3 C4 C3 C4 C3 C4
                E2 E3 E2 E3 E2 E3 E2 E3
                A2 A3 A2 A3 A2 A3 A2 A3
                E2 E3 E2 E3 G#2 G#3 B2 B3",
            Drums = "k.h.s.h.k.k.s.h.",
        };

        // ボス: 天敵が出てきた、追い立てられる感じの曲（Dm - A - Dm - Bb - F - Gm - Dm - A）
        private static readonly Song BossSong = new Song
        {
            Name = "Bgm_Boss",
            Bpm = 160f,
            Melody = @"
                D5 - D5 - F5 - E5 D5
                C#5 - - - A4 - - -
                D5 - D5 - F5 - G5 A5
                A#5 - A5 - G5 - F5 E5
                F5 - E5 - D5 - C5 -
                A#4 - A4 - G4 - A4 -
                D5 - F5 - A5 - D6 -
                C#6 - A5 - E5 - C#5 -",
            Bass = @"
                D2 D3 D2 D3 D2 D3 D2 D3
                A1 A2 A1 A2 A1 A2 A1 A2
                D2 D3 D2 D3 D2 D3 D2 D3
                A#1 A#2 A#1 A#2 A#1 A#2 A#1 A#2
                F2 F3 F2 F3 F2 F3 F2 F3
                G1 G2 G1 G2 G1 G2 G1 G2
                D2 D3 D2 D3 D2 D3 D2 D3
                A1 A2 A1 A2 A1 A2 A1 A2",
            Drums = "k.skk.s.",
        };

        // ゴルフ: 芝の上でのんびり狙う、口笛っぽい軽い曲（F - Dm - Bb - C）。本編より遅くして気分を切り替える
        private static readonly Song GolfSong = new Song
        {
            Name = "Bgm_Golf",
            Bpm = 104f,
            Melody = @"
                A4 - C5 - F5 - - -
                D5 - C5 - A4 - - -
                A#4 - D5 - F5 - D5 -
                C5 - - - . . . .
                A4 - C5 - F5 - A5 -
                G5 - F5 - D5 - - -
                A#4 - A4 - G4 - E4 -
                F4 - - - . . . .",
            Bass = @"
                F2 - C3 - F2 - C3 -
                D2 - A2 - D2 - A2 -
                A#1 - F2 - A#1 - F2 -
                C2 - G2 - C2 - G2 -
                F2 - C3 - F2 - C3 -
                D2 - A2 - D2 - A2 -
                A#1 - F2 - C2 - G2 -
                F2 - C3 - F2 - - -",
            Drums = "k.h.s.h.",
        };

        // サッカー: 3分間走り回る、サンバっぽく跳ねる元気な曲（G - Em - C - D / G - B7 - C - D）。ゴルフより速く、本編のステージより明るく
        private static readonly Song SoccerSong = new Song
        {
            Name = "Bgm_Soccer",
            Bpm = 144f,
            Melody = @"
                D5 - B4 D5 G5 - D5 B4
                E5 - G5 - E5 D5 B4 -
                C5 E5 G5 - E5 - C5 E5
                D5 - F#5 - A5 - - -
                B5 - A5 G5 D5 - G5 -
                F#5 - D#5 - B4 - D#5 F#5
                E5 - G5 - C6 - B5 A5
                A5 - F#5 - G5 - . .",
            Bass = @"
                G2 . G2 D3 . G2 D3 .
                E2 . E2 B2 . E2 B2 .
                C3 . C3 G3 . C3 G3 .
                D3 . D3 A2 . D3 A2 .
                G2 . G2 D3 . G2 D3 .
                B1 . B1 F#2 . B1 F#2 .
                C3 . C3 G2 . C3 G2 .
                D3 . D3 A2 G2 - - .",
            Drums = "k.hsk.hsk.h.khs.",
        };

        // 野球: スタンドの応援のように、手拍子に乗って跳ねる明るい曲（F - B♭ - C - F / Dm - Gm - C - F）。
        // 1球ごとに間があるゲームなので、サッカーより少しゆったり・ゴルフより元気に
        private static readonly Song BaseballSong = new Song
        {
            Name = "Bgm_Baseball",
            Bpm = 138f,
            Melody = @"
                C5 - F5 - A5 - F5 -
                D5 - F5 - A#5 - - .
                C5 - E5 - G5 - E5 C5
                F5 - - - . . C5 C5
                D5 - F5 - A5 - D5 -
                G5 - F5 - D5 - A#4 -
                C5 - E5 - G5 - A#5 -
                A5 - - - F5 - . .",
            Bass = @"
                F2 . C3 . F2 . C3 .
                A#2 . F3 . A#2 . F3 .
                C3 . G3 . C3 . G3 .
                F2 . C3 . F2 A2 C3 .
                D3 . A2 . D3 . A2 .
                G2 . D3 . G2 . D3 .
                C3 . G2 . C3 . E3 .
                F2 . C3 . F2 - - .",
            Drums = "k.s.k.shk.s.kss.",
        };
    }
}
