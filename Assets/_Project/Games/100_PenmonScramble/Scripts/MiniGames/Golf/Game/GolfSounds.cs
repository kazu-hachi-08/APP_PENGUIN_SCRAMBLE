using System.Collections.Generic;
using PenmonScramble.Common.Audio;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    public enum GolfSound
    {
        /// <summary>クラブで打った（強いほど高く大きい）</summary>
        Shot,
        /// <summary>壁に当たった（強いほど大きい）</summary>
        Wall,
        /// <summary>球どうしが当たった</summary>
        BallHit,
        /// <summary>カップの縁で曲がって入らなかった</summary>
        LipOut,
        /// <summary>カップに入った（カラン＋ジングル）</summary>
        CupIn,
        /// <summary>池・場外に落ちた</summary>
        Splash,
        /// <summary>ジャンプ台で跳んだ</summary>
        Jump,
        /// <summary>打つ子を替えた・ゲージを動かし始めた</summary>
        Select,
        /// <summary>特殊ショットを ON にした・打った（キラッと上がる音）</summary>
        Special,
        /// <summary>雷の特殊ショットで加速した（ビリッ）</summary>
        Boost,
        /// <summary>水切りで跳ねた（ピチャッ）</summary>
        Skip,
    }

    /// <summary>
    /// ゴルフの効果音。本編と同じくコードで作るチップチューン（ToySynth）。音量の設定は AudioManager で掛かる
    /// </summary>
    public static class GolfSounds
    {
        // 強さ（0〜1）で変える幅: 打つ音の高さと大きさ、壁の音の大きさ
        private const float ShotPitchMin = 0.8f;
        private const float ShotPitchMax = 1.3f;
        private const float ShotVolumeMin = 0.35f;
        private const float ShotVolumeMax = 0.75f;
        private const float WallVolumeMin = 0.15f;
        private const float WallVolumeMax = 0.55f;
        // 壁に向かう速さがこれで「いちばん強い」とする（m/s）
        private const float WallFullSpeed = 8f;

        private static readonly Dictionary<GolfSound, AudioClip> Clips = new Dictionary<GolfSound, AudioClip>();

        private static readonly Dictionary<GolfSound, float> Volumes = new Dictionary<GolfSound, float>
        {
            { GolfSound.BallHit, 0.45f },
            { GolfSound.LipOut, 0.4f },
            { GolfSound.CupIn, 0.6f },
            { GolfSound.Splash, 0.55f },
            { GolfSound.Jump, 0.4f },
            { GolfSound.Select, 0.3f },
            { GolfSound.Special, 0.45f },
            { GolfSound.Boost, 0.5f },
            { GolfSound.Skip, 0.35f },
        };

        /// <summary>打つ音。power はゲージの強さ（0〜1）</summary>
        public static void PlayShot(float power)
        {
            var t = Mathf.Clamp01(power);
            Play(GolfSound.Shot, Mathf.Lerp(ShotVolumeMin, ShotVolumeMax, t), Mathf.Lerp(ShotPitchMin, ShotPitchMax, t));
        }

        /// <summary>壁の音。speed は壁に向かう速さ（m/s）</summary>
        public static void PlayWall(float speed)
        {
            Play(GolfSound.Wall, Mathf.Lerp(WallVolumeMin, WallVolumeMax, Mathf.Clamp01(speed / WallFullSpeed)), 1f);
        }

        public static void Play(GolfSound sound) => Play(sound, Volumes.TryGetValue(sound, out var volume) ? volume : 0.5f, 1f);

        private static void Play(GolfSound sound, float volume, float pitch)
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

        private static AudioClip Create(GolfSound sound)
        {
            switch (sound)
            {
                // 木のクラブの「コン」: 短く高めの打撃
                case GolfSound.Shot: return ToySynth.Impact("Golf_Shot", 0.09f, 560f);
                // 柵の「トン」: 打つ音より低く短い
                case GolfSound.Wall: return ToySynth.Impact("Golf_Wall", 0.07f, 260f);
                // 球どうしの「カチッ」
                case GolfSound.BallHit: return ToySynth.Tone("Golf_BallHit", 0.035f, 1760f);
                // 縁をなめて外れる「ヒュン」: 下がっていく音
                case GolfSound.LipOut: return ToySynth.Sweep("Golf_LipOut", 0.18f, 1100f, 420f);
                case GolfSound.CupIn: return CupIn();
                case GolfSound.Splash: return Splash();
                case GolfSound.Jump: return ToySynth.Sweep("Golf_Jump", 0.14f, 400f, 1200f);
                case GolfSound.Special: return ToySynth.Sweep("Golf_Special", 0.22f, 660f, 1760f);
                case GolfSound.Boost: return Boost();
                case GolfSound.Skip: return ToySynth.Sweep("Golf_Skip", 0.08f, 1400f, 700f);
                default: return ToySynth.Tone("Golf_Select", 0.05f, 1319f);
            }
        }

        /// <summary>カップの中で「カラン」と2回鳴ってから、上がっていく短いジングル</summary>
        private static AudioClip CupIn()
        {
            const float rattle = 0.16f;
            float[] notes = { 784f, 988f, 1175f, 1568f };
            const float noteSeconds = 0.09f;
            var duration = rattle + noteSeconds * (notes.Length + 1);
            return ToySynth.CreateClip("Golf_CupIn", duration, t =>
            {
                if (t < rattle)
                {
                    // 2回の短い金属音（高い矩形波がすぐ減る）
                    var local = t % (rattle * 0.5f);
                    return ToySynth.Square(2349f, t) * Mathf.Exp(-local * 60f);
                }

                var s = t - rattle;
                var note = Mathf.Min(notes.Length - 1, Mathf.FloorToInt(s / noteSeconds));
                var into = s - note * noteSeconds;
                var length = note == notes.Length - 1 ? noteSeconds * 2f : noteSeconds;
                return ToySynth.Square(notes[note], t) * Mathf.Max(0f, 1f - into / length);
            });
        }

        /// <summary>「ビリッ」: 速く上下する矩形波に、ざらついたノイズを重ねる</summary>
        private static AudioClip Boost()
        {
            const float duration = 0.28f;
            var random = new System.Random(7);
            return ToySynth.CreateClip("Golf_Boost", duration, t =>
            {
                var buzz = ToySynth.Square(Mathf.Lerp(300f, 1500f, t / duration) + Mathf.Sin(t * 180f) * 120f, t);
                var noise = (float)random.NextDouble() * 2f - 1f;
                return (buzz * 0.6f + noise * 0.3f) * (1f - t / duration);
            });
        }

        /// <summary>「ポチャン」: 高い音が落ちる「ポ」と、こもったノイズの「シャー」</summary>
        private static AudioClip Splash()
        {
            const float duration = 0.4f;
            var random = new System.Random(3);
            var filtered = 0f;
            return ToySynth.CreateClip("Golf_Splash", duration, t =>
            {
                var plop = t < 0.06f ? Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(900f, 300f, t / 0.06f) * t) * (1f - t / 0.06f) * 0.6f : 0f;
                filtered = Mathf.Lerp(filtered, (float)random.NextDouble() * 2f - 1f, 0.25f);
                return plop + filtered * Mathf.Exp(-t * 9f) * 0.7f;
            });
        }
    }
}
