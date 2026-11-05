using System.Collections.Generic;
using PenmonScramble.Common.Audio;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    public enum SoccerSound
    {
        /// <summary>蹴った（強いほど低く重く大きい）</summary>
        Kick,
        /// <summary>柵に当たった（強いほど大きい）</summary>
        Wall,
        /// <summary>柱・バーに当たった（カーン）</summary>
        Post,
        /// <summary>ゴールの網に入った・当たった（ザサッ）</summary>
        Net,
        /// <summary>浮いた球が地面に着いた（ポン）</summary>
        Land,
        /// <summary>ボールを持った（トッ）</summary>
        Trap,
        /// <summary>キーパーが飛びついた（シュッ）</summary>
        Dive,
        /// <summary>キーパーが取った（手袋のバフッ）</summary>
        Catch,
        /// <summary>キーパーがはじいた（ボン）</summary>
        Parry,
        /// <summary>キーパーが投げた</summary>
        Throw,
        /// <summary>笛（キックオフ）</summary>
        Whistle,
        /// <summary>点が入った（上がっていくファンファーレ）</summary>
        Goal,
    }

    /// <summary>
    /// サッカーの効果音。本編・ゴルフと同じくコードで作るチップチューン（ToySynth）。音量の設定は AudioManager で掛かる
    /// </summary>
    public static class SoccerSounds
    {
        // 強さ（0〜1）で変える幅: 蹴る音は強いほど低く大きく（ドスッ）、弱いと高く小さく（ポン）
        private const float KickPitchWeak = 1.35f;
        private const float KickPitchStrong = 0.8f;
        private const float KickVolumeMin = 0.3f;
        private const float KickVolumeMax = 0.8f;
        private const float WallVolumeMin = 0.12f;
        private const float WallVolumeMax = 0.55f;
        private const float NetVolumeMin = 0.2f;
        private const float NetVolumeMax = 0.6f;
        // 当たる速さがこれで「いちばん強い」とする（m/s）
        private const float FullSpeed = 14f;

        private static readonly Dictionary<SoccerSound, AudioClip> Clips = new Dictionary<SoccerSound, AudioClip>();

        private static readonly Dictionary<SoccerSound, float> Volumes = new Dictionary<SoccerSound, float>
        {
            { SoccerSound.Post, 0.5f },
            { SoccerSound.Land, 0.25f },
            { SoccerSound.Trap, 0.18f },
            { SoccerSound.Dive, 0.35f },
            { SoccerSound.Catch, 0.55f },
            { SoccerSound.Parry, 0.5f },
            { SoccerSound.Throw, 0.25f },
            { SoccerSound.Whistle, 0.45f },
            { SoccerSound.Goal, 0.6f },
        };

        /// <summary>蹴る音。strength はため（0〜1）</summary>
        public static void PlayKick(float strength)
        {
            var t = Mathf.Clamp01(strength);
            Play(SoccerSound.Kick, Mathf.Lerp(KickVolumeMin, KickVolumeMax, t), Mathf.Lerp(KickPitchWeak, KickPitchStrong, t));
        }

        /// <summary>柵・網の音。speed は当たる速さ（m/s）</summary>
        public static void PlayHit(SoccerSound sound, float speed)
        {
            var t = Mathf.Clamp01(speed / FullSpeed);
            var volume = sound == SoccerSound.Net ? Mathf.Lerp(NetVolumeMin, NetVolumeMax, t) : Mathf.Lerp(WallVolumeMin, WallVolumeMax, t);
            Play(sound, volume, 1f);
        }

        public static void Play(SoccerSound sound) => Play(sound, Volumes.TryGetValue(sound, out var volume) ? volume : 0.5f, 1f);

        private static void Play(SoccerSound sound, float volume, float pitch)
        {
            if (!Clips.TryGetValue(sound, out var clip) || clip == null)
            {
                clip = Create(sound);
                Clips[sound] = clip;
            }

            var audio = AudioManager.Instance;
            if (audio != null)
            {
                audio.PlaySeClip(clip, volume, pitch);
            }
        }

        private static AudioClip Create(SoccerSound sound)
        {
            switch (sound)
            {
                // ボールを蹴る「ボスッ」: 低めの打撃に、短いノイズを重ねる
                case SoccerSound.Kick: return Kick();
                // 柵の「トン」
                case SoccerSound.Wall: return ToySynth.Impact("Soccer_Wall", 0.07f, 240f);
                case SoccerSound.Post: return Post();
                case SoccerSound.Net: return Net();
                case SoccerSound.Land: return ToySynth.Impact("Soccer_Land", 0.06f, 170f);
                case SoccerSound.Trap: return ToySynth.Impact("Soccer_Trap", 0.04f, 320f);
                case SoccerSound.Dive: return ToySynth.Whoosh("Soccer_Dive");
                case SoccerSound.Catch: return Catch();
                case SoccerSound.Parry: return ToySynth.Impact("Soccer_Parry", 0.09f, 200f);
                case SoccerSound.Throw: return ToySynth.Sweep("Soccer_Throw", 0.1f, 300f, 600f);
                case SoccerSound.Whistle: return Whistle();
                default: return ToySynth.Jingle("Soccer_Goal", new[] { 523f, 659f, 784f, 1047f, 1319f });
            }
        }

        private static AudioClip Kick()
        {
            const float duration = 0.12f;
            var random = new System.Random(5);
            return ToySynth.CreateClip("Soccer_Kick", duration, t =>
            {
                var thump = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(220f, 90f, t / duration) * t) * Mathf.Exp(-t * 30f);
                var slap = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 90f) * 0.5f;
                return thump * 0.9f + slap;
            });
        }

        /// <summary>柱の「カーン」: 高い音が2つ重なって長めに響く（金属らしく）</summary>
        private static AudioClip Post()
        {
            const float duration = 0.45f;
            return ToySynth.CreateClip("Soccer_Post", duration, t =>
            {
                var ring = Mathf.Sin(2f * Mathf.PI * 1480f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 2210f * t) * 0.35f;
                return ring * Mathf.Exp(-t * 9f);
            });
        }

        /// <summary>網の「ザサッ」: こもったノイズがふわっと立って消える</summary>
        private static AudioClip Net()
        {
            const float duration = 0.3f;
            var random = new System.Random(9);
            var filtered = 0f;
            return ToySynth.CreateClip("Soccer_Net", duration, t =>
            {
                filtered = Mathf.Lerp(filtered, (float)random.NextDouble() * 2f - 1f, 0.35f);
                var envelope = Mathf.Min(1f, t / 0.02f) * Mathf.Exp(-t * 11f);
                return filtered * envelope;
            });
        }

        /// <summary>手袋で取る「バフッ」: 低い音とこもったノイズ</summary>
        private static AudioClip Catch()
        {
            const float duration = 0.14f;
            var random = new System.Random(13);
            var filtered = 0f;
            return ToySynth.CreateClip("Soccer_Catch", duration, t =>
            {
                filtered = Mathf.Lerp(filtered, (float)random.NextDouble() * 2f - 1f, 0.2f);
                var thump = Mathf.Sin(2f * Mathf.PI * 130f * t) * Mathf.Exp(-t * 25f);
                return (thump * 0.7f + filtered * 0.6f) * Mathf.Exp(-t * 18f);
            });
        }

        /// <summary>笛の「ピーッ」: 高い音を細かく震わせて、短く2回</summary>
        private static AudioClip Whistle()
        {
            const float blow = 0.16f;
            const float gap = 0.05f;
            const float duration = blow * 2f + gap;
            return ToySynth.CreateClip("Soccer_Whistle", duration, t =>
            {
                var local = t < blow ? t : t - blow - gap;
                if (local < 0f)
                {
                    return 0f;
                }

                var trill = 2350f + Mathf.Sin(t * 2f * Mathf.PI * 32f) * 140f;
                var envelope = Mathf.Min(1f, local / 0.01f) * Mathf.Min(1f, (blow - local) / 0.02f);
                return ToySynth.Square(trill, t) * 0.5f * envelope;
            });
        }
    }
}
