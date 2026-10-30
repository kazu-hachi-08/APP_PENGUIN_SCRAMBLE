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
    }
}
