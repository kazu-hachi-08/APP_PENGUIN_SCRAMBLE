using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>効果音の波形（ペンモンの BattleSounds.Synth と同じ作り。予告の「警報」だけ足した）</summary>
    public static partial class GameSounds
    {
        private const int SampleRate = 44100;

        // 矩形波は音量が大きく聞こえるので、全体を少し抑える
        private const float SquareGain = 0.35f;

        /// <summary>短いピコッという矩形波</summary>
        private static AudioClip Tone(string name, float duration, float frequency)
        {
            return CreateClip(name, duration, t => Square(frequency, t) * (1f - t / duration));
        }

        /// <summary>周波数を滑らせる矩形波。上がると「やった」、下がると「たおれる」</summary>
        private static AudioClip Sweep(string name, float duration, float fromFrequency, float toFrequency)
        {
            var phase = 0f;
            return CreateClip(name, duration, t =>
            {
                var frequency = Mathf.Lerp(fromFrequency, toFrequency, t / duration);
                // 周波数が変わっても波形が途切れないよう、位相を積み上げて鳴らす
                phase += frequency / SampleRate;
                return (phase % 1f < 0.5f ? SquareGain : -SquareGain) * (1f - 0.5f * t / duration);
            });
        }

        /// <summary>だんだん低くなるノイズ。技を出すときの「シュッ」</summary>
        private static AudioClip Whoosh(string name)
        {
            const float duration = 0.12f;
            var random = new System.Random(1);
            var filtered = 0f;
            return CreateClip(name, duration, t =>
            {
                var cutoff = Mathf.Lerp(0.6f, 0.05f, t / duration);
                filtered = Mathf.Lerp(filtered, (float)random.NextDouble() * 2f - 1f, cutoff);
                return filtered * Mathf.Sin(Mathf.PI * t / duration);
            });
        }

        /// <summary>低いドンという音にノイズを混ぜた打撃音。長く低いほど強く聞こえる</summary>
        private static AudioClip Impact(string name, float duration, float frequency)
        {
            var decay = 4f / duration;
            var random = new System.Random(2);
            return CreateClip(name, duration, t =>
                (Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.8f + ((float)random.NextDouble() * 2f - 1f) * 0.5f) * Mathf.Exp(-t * decay));
        }

        /// <summary>高い・低いを2回くり返す警報。大ボスの攻撃が来ると分かるようにする</summary>
        private static AudioClip Alarm(string name)
        {
            const float duration = 0.36f;
            const float half = duration / 4f;
            return CreateClip(name, duration, t => Square(Mathf.FloorToInt(t / half) % 2 == 0 ? 880f : 660f, t));
        }

        /// <summary>音程を順に鳴らす短いジングル。最後の音だけ伸ばして、終わった感じを出す</summary>
        private static AudioClip Jingle(string name, float[] frequencies)
        {
            const float noteDuration = 0.12f;
            var duration = noteDuration * frequencies.Length + noteDuration;
            return CreateClip(name, duration, t =>
            {
                var note = Mathf.Min(frequencies.Length - 1, Mathf.FloorToInt(t / noteDuration));
                var local = t - note * noteDuration;
                var length = note == frequencies.Length - 1 ? noteDuration * 2f : noteDuration;
                return Square(frequencies[note], t) * Mathf.Max(0f, 1f - local / length);
            });
        }

        private static float Square(float frequency, float t)
        {
            return (t * frequency % 1f < 0.5f ? 1f : -1f) * SquareGain;
        }

        private static AudioClip CreateClip(string name, float duration, System.Func<float, float> sampler)
        {
            var sampleCount = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                data[i] = sampler(i / (float)SampleRate);
            }

            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
