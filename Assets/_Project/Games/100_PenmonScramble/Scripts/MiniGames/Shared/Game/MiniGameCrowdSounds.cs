using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.MiniGames.Game
{
    /// <summary>
    /// ミニゲームの観客の声（サッカー・野球で共通。ミニゲーム同士をつながないよう、ここに置いて両方から使う）。
    /// 本編・ゴルフと同じくコードで作る（ToySynth）。鳴らすのと音量は各ミニゲームの音の表が受け持つ
    /// </summary>
    public static class MiniGameCrowdSounds
    {
        /// <summary>
        /// 観客の歓声「ワーッ」: 中くらいの高さだけ残したノイズ（大勢のざわめき）に、ばらばらの高さの声を薄く重ね、
        /// 波打たせながら一気に立ち上げて、2 秒ほどで引いていく（サッカーのゴール・野球のホームラン）
        /// </summary>
        public static AudioClip Cheer()
        {
            const float duration = 1.8f;
            const float hold = 0.5f;
            var random = new System.Random(31);
            var voices = new[] { 330f, 415f, 494f, 587f, 698f };
            var phases = new float[voices.Length];
            var low = 0f;
            var lower = 0f;
            return ToySynth.CreateClip("MiniGame_Cheer", duration, t =>
            {
                // 高い所を削った音から、もっと低い所を引いて、中くらいの高さだけ残す
                low = Mathf.Lerp(low, (float)random.NextDouble() * 2f - 1f, 0.3f);
                lower = Mathf.Lerp(lower, low, 0.04f);
                var crowd = low - lower;
                var voice = 0f;
                for (var i = 0; i < voices.Length; i++)
                {
                    var wobble = 1f + 0.03f * Mathf.Sin(2f * Mathf.PI * (4.5f + i * 1.3f) * t);
                    phases[i] += 2f * Mathf.PI * voices[i] * wobble / ToySynth.SampleRate;
                    voice += Mathf.Sin(phases[i]);
                }

                var flutter = 0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * 7.3f * t) * Mathf.Sin(2f * Mathf.PI * 3.1f * t);
                var envelope = Mathf.Min(1f, t / 0.12f) * (t < hold ? 1f : Mathf.Exp(-(t - hold) * 2.2f));
                return (crowd * 1.4f + voice * 0.035f) * flutter * envelope;
            });
        }

        /// <summary>観客のどよめき「おお〜」: 低い声の束が少し上がってから下がり、こもったざわめきと一緒に消える（歓声と聞き分けられるように低く短く）</summary>
        public static AudioClip Ooh()
        {
            const float duration = 1f;
            var random = new System.Random(37);
            var voices = new[] { 196f, 220f, 247f, 262f };
            var phases = new float[voices.Length];
            var murmur = 0f;
            return ToySynth.CreateClip("MiniGame_Ooh", duration, t =>
            {
                var glide = t < 0.25f ? Mathf.Lerp(1f, 1.08f, t / 0.25f) : Mathf.Lerp(1.08f, 0.9f, (t - 0.25f) / (duration - 0.25f));
                var voice = 0f;
                for (var i = 0; i < voices.Length; i++)
                {
                    phases[i] += 2f * Mathf.PI * voices[i] * glide / ToySynth.SampleRate;
                    // 三角波で丸い「お」の声にする
                    voice += Mathf.Abs((phases[i] / (2f * Mathf.PI)) % 1f * 4f - 2f) - 1f;
                }

                murmur = Mathf.Lerp(murmur, (float)random.NextDouble() * 2f - 1f, 0.08f);
                var envelope = Mathf.Min(1f, t / 0.1f) * Mathf.Exp(-t * 2.5f);
                return (voice * 0.12f + murmur * 0.8f) * envelope;
            });
        }
    }
}
