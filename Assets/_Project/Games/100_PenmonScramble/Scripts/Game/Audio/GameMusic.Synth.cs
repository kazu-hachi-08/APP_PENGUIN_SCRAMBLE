using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>楽譜をチップチューンの音にする（ペンモンの GameMusic.Synth と同じ作り）</summary>
    public static partial class GameMusic
    {
        // チップチューンなら低めのサンプリングで十分。メモリと生成時間を半分にできる
        private const int SampleRate = 22050;
        private const int StepsPerBeat = 2;

        // 3つの声部を足しても 1 を超えないよう、それぞれの音量を抑える
        private const float MelodyGain = 0.16f;
        private const float BassGain = 0.28f;
        private const float DrumGain = 0.22f;

        // 音の頭と終わりを少しだけなだらかにして、プツッという音を消す
        private const float AttackSeconds = 0.005f;
        private const float ReleaseSeconds = 0.015f;

        private const string Sustain = "-";
        private const string Rest = ".";

        private static AudioClip Render(Song song)
        {
            var stepSeconds = 60f / song.Bpm / StepsPerBeat;
            var melody = Tokens(song.Melody);
            var steps = melody.Length;
            var data = new float[Mathf.CeilToInt(steps * stepSeconds * SampleRate)];

            AddVoice(data, melody, stepSeconds, MelodyGain, PulseWave);
            AddVoice(data, Tokens(song.Bass), stepSeconds, BassGain, TriangleWave);
            AddDrums(data, song.Drums.Replace(" ", ""), stepSeconds, steps);

            var clip = AudioClip.Create(song.Name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static string[] Tokens(string score)
        {
            // 改行コードが CRLF でも LF でも同じに読めるよう、空白類はまとめて区切りにする
            return score.Split(new[] { ' ', '\t', '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>楽譜の1声部を鳴らして足し込む。「-」は前の音を伸ばし、「.」は休み</summary>
        private static void AddVoice(float[] data, string[] tokens, float stepSeconds, float gain, System.Func<float, float> wave)
        {
            for (var step = 0; step < tokens.Length; step++)
            {
                if (tokens[step] == Sustain || tokens[step] == Rest)
                {
                    continue;
                }

                var length = 1;
                while (step + length < tokens.Length && tokens[step + length] == Sustain)
                {
                    length++;
                }

                AddNote(data, step * stepSeconds, length * stepSeconds, NoteFrequency(tokens[step]), gain, wave);
            }
        }

        private static void AddNote(float[] data, float start, float duration, float frequency, float gain, System.Func<float, float> wave)
        {
            var first = Mathf.FloorToInt(start * SampleRate);
            var count = Mathf.Min(Mathf.CeilToInt(duration * SampleRate), data.Length - first);
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                var envelope = Mathf.Min(1f, t / AttackSeconds, (duration - t) / ReleaseSeconds)
                    // 鳴らした直後を少し強くして、音の粒をはっきりさせる
                    * Mathf.Lerp(1f, 0.7f, Mathf.Min(1f, t / 0.15f));
                data[first + i] += wave(t * frequency % 1f) * envelope * gain;
            }
        }

        /// <summary>8分音符ごとに k（バスドラム）/ s（スネア）/ h（ハイハット）/ .（休み）を1文字ずつ。足りなければ頭から繰り返す</summary>
        private static void AddDrums(float[] data, string pattern, float stepSeconds, int steps)
        {
            var random = new System.Random(0);
            for (var step = 0; step < steps; step++)
            {
                var start = step * stepSeconds;
                switch (pattern[step % pattern.Length])
                {
                    case 'k': AddDrum(data, start, 0.12f, t => Mathf.Sin(2f * Mathf.PI * (120f * t - 200f * t * t))); break;
                    case 's': AddDrum(data, start, 0.1f, t => (float)random.NextDouble() * 2f - 1f); break;
                    case 'h': AddDrum(data, start, 0.03f, t => ((float)random.NextDouble() * 2f - 1f) * 0.4f); break;
                }
            }
        }

        private static void AddDrum(float[] data, float start, float duration, System.Func<float, float> sampler)
        {
            var first = Mathf.FloorToInt(start * SampleRate);
            var count = Mathf.Min(Mathf.CeilToInt(duration * SampleRate), data.Length - first);
            for (var i = 0; i < count; i++)
            {
                var t = i / (float)SampleRate;
                data[first + i] += sampler(t) * (1f - t / duration) * DrumGain;
            }
        }

        /// <summary>25% の矩形波。50% より細くて、ファミコンらしいメロディーの音になる</summary>
        private static float PulseWave(float phase)
        {
            return phase < 0.25f ? 1f : -1f;
        }

        /// <summary>三角波。丸い音なのでベースにしても耳に痛くない</summary>
        private static float TriangleWave(float phase)
        {
            return phase < 0.5f ? phase * 4f - 1f : 3f - phase * 4f;
        }

        private const string NoteNames = "C D EF G A B";

        /// <summary>「A4」「G#3」のような音名を周波数にする（A4 = 440Hz）</summary>
        private static float NoteFrequency(string note)
        {
            var semitone = NoteNames.IndexOf(note[0]);
            var sharp = note[1] == '#';
            if (sharp)
            {
                semitone++;
            }

            var octave = note[sharp ? 2 : 1] - '0';
            var midi = (octave + 1) * 12 + semitone;
            return 440f * Mathf.Pow(2f, (midi - 69) / 12f);
        }
    }
}
