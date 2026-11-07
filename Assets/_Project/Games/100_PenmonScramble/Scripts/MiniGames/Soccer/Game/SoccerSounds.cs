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
        /// <summary>スライディングで芝をすべる（ザザッ）</summary>
        Slide,
        /// <summary>スライディングでボールを奪った（ドスッ）</summary>
        Tackle,
        /// <summary>ダッシュで駆け出す（タタッ）</summary>
        Dash,
        /// <summary>残り10秒からの1秒ごと（ピッ）</summary>
        Countdown,
        /// <summary>時間切れの笛（ピッ、ピッ、ピーー）</summary>
        TimeUp,
        /// <summary>必殺シュートを蹴った（ギュイーン）</summary>
        SpecialShot,
        /// <summary>氷の必殺シュートで凍った（キラキラ）</summary>
        Freeze,
        /// <summary>PK戦で外れた（ヒューン）</summary>
        Miss,
        /// <summary>バンパー・柱で弾かれた（ボヨン）</summary>
        Bumper,
        /// <summary>間欠泉が噴いてボールが跳ね上がった（シュワーッ）</summary>
        Geyser,
        /// <summary>点が入ったときの観客の歓声（ワーッ）</summary>
        Cheer,
        /// <summary>おしい・止められたときの観客のどよめき（おお〜）</summary>
        Ooh,
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
        // 残りの秒がこれ以下のカウントダウンは高く鳴らす（最後の3秒を耳でも分かるように）
        private const int FinalCountdownSeconds = 3;
        private const float FinalCountdownPitch = 1.5f;

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
            { SoccerSound.Slide, 0.3f },
            { SoccerSound.Tackle, 0.55f },
            { SoccerSound.Dash, 0.2f },
            { SoccerSound.Countdown, 0.35f },
            { SoccerSound.TimeUp, 0.5f },
            { SoccerSound.SpecialShot, 0.55f },
            { SoccerSound.Freeze, 0.4f },
            { SoccerSound.Miss, 0.35f },
            { SoccerSound.Bumper, 0.5f },
            { SoccerSound.Geyser, 0.45f },
            // 観客の声は、ゴールのファンファーレ・キーパーの音の後ろで鳴る大きさに
            { SoccerSound.Cheer, 0.35f },
            { SoccerSound.Ooh, 0.3f },
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

        /// <summary>残り10秒からの「ピッ」。secondsLeft は画面に出ている残りの秒</summary>
        public static void PlayCountdown(int secondsLeft)
        {
            var pitch = secondsLeft <= FinalCountdownSeconds ? FinalCountdownPitch : 1f;
            Play(SoccerSound.Countdown, Volumes[SoccerSound.Countdown], pitch);
        }

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
                case SoccerSound.Slide: return Slide();
                // 奪った「ドスッ」: 低い打撃（転ぶ重さ）
                case SoccerSound.Tackle: return ToySynth.Impact("Soccer_Tackle", 0.12f, 110f);
                // 駆け出す「タタッ」: 短い打撃を2つ
                case SoccerSound.Dash: return Dash();
                case SoccerSound.Countdown: return ToySynth.CreateClip("Soccer_Countdown", 0.09f, t => ToySynth.Square(1320f, t) * 0.4f * Mathf.Exp(-t * 25f));
                case SoccerSound.TimeUp: return TimeUp();
                // 必殺シュートの「ギュイーン」: 低いところから一気に上がる
                case SoccerSound.SpecialShot: return ToySynth.Sweep("Soccer_Special", 0.35f, 220f, 1400f);
                case SoccerSound.Freeze: return Freeze();
                case SoccerSound.Miss: return ToySynth.Sweep("Soccer_Miss", 0.4f, 900f, 300f);
                case SoccerSound.Bumper: return Bumper();
                case SoccerSound.Geyser: return Geyser();
                case SoccerSound.Cheer: return Cheer();
                case SoccerSound.Ooh: return Ooh();
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

        /// <summary>バンパーの「ボヨン」: 低い所から跳ね上がって揺れる音（板の「トン」と聞き分けられるように）</summary>
        private static AudioClip Bumper()
        {
            const float duration = 0.25f;
            var phase = 0f;
            return ToySynth.CreateClip("Soccer_Bumper", duration, t =>
            {
                var pitch = Mathf.Lerp(180f, 620f, Mathf.Min(1f, t / 0.06f)) * (1f + 0.08f * Mathf.Sin(2f * Mathf.PI * 30f * t));
                phase += 2f * Mathf.PI * pitch / ToySynth.SampleRate;
                return Mathf.Sign(Mathf.Sin(phase)) * 0.45f * Mathf.Exp(-t * 12f);
            });
        }

        /// <summary>間欠泉の「シュワーッ」: 明るいノイズが一気に立って、少し長く抜けていく</summary>
        private static AudioClip Geyser()
        {
            const float duration = 0.6f;
            var random = new System.Random(21);
            var previous = 0f;
            return ToySynth.CreateClip("Soccer_Geyser", duration, t =>
            {
                var noise = (float)random.NextDouble() * 2f - 1f;
                var hiss = Mathf.Lerp(noise - previous, noise, 0.4f);
                previous = noise;
                return hiss * 0.45f * Mathf.Min(1f, t / 0.03f) * Mathf.Exp(-t * 4.5f);
            });
        }

        /// <summary>
        /// 観客の歓声「ワーッ」: 中くらいの高さだけ残したノイズ（大勢のざわめき）に、ばらばらの高さの声を薄く重ね、
        /// 波打たせながら一気に立ち上げて、ゴールの間（2 秒）のうちに引いていく
        /// </summary>
        private static AudioClip Cheer()
        {
            const float duration = 1.8f;
            const float hold = 0.5f;
            var random = new System.Random(31);
            var voices = new[] { 330f, 415f, 494f, 587f, 698f };
            var phases = new float[voices.Length];
            var low = 0f;
            var lower = 0f;
            return ToySynth.CreateClip("Soccer_Cheer", duration, t =>
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
        private static AudioClip Ooh()
        {
            const float duration = 1f;
            var random = new System.Random(37);
            var voices = new[] { 196f, 220f, 247f, 262f };
            var phases = new float[voices.Length];
            var murmur = 0f;
            return ToySynth.CreateClip("Soccer_Ooh", duration, t =>
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

        /// <summary>芝をすべる「ザザッ」: 高めのノイズが擦れるように続いて消える</summary>
        private static AudioClip Slide()
        {
            const float duration = 0.3f;
            var random = new System.Random(17);
            var previous = 0f;
            return ToySynth.CreateClip("Soccer_Slide", duration, t =>
            {
                // 前の値との差を取って高い音だけ残す（芝の擦れる音）
                var noise = (float)random.NextDouble() * 2f - 1f;
                var high = noise - previous;
                previous = noise;
                var scrape = 0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 38f * t);
                return high * 0.5f * scrape * Mathf.Min(1f, t / 0.02f) * Mathf.Exp(-t * 7f);
            });
        }

        private static AudioClip Dash()
        {
            const float step = 0.07f;
            const float duration = step * 2f;
            return ToySynth.CreateClip("Soccer_Dash", duration, t =>
            {
                var local = t % step;
                return Mathf.Sin(2f * Mathf.PI * 260f * local) * Mathf.Exp(-local * 60f);
            });
        }

        /// <summary>時間切れの笛「ピッ、ピッ、ピーー」: 短く2回と長く1回</summary>
        private static AudioClip TimeUp()
        {
            var blows = new[] { (0f, 0.14f), (0.22f, 0.14f), (0.44f, 0.6f) };
            const float duration = 1.04f;
            return ToySynth.CreateClip("Soccer_TimeUp", duration, t =>
            {
                foreach (var (start, length) in blows)
                {
                    var local = t - start;
                    if (local >= 0f && local < length)
                    {
                        var trill = 2350f + Mathf.Sin(t * 2f * Mathf.PI * 32f) * 140f;
                        var envelope = Mathf.Min(1f, local / 0.01f) * Mathf.Min(1f, (length - local) / 0.03f);
                        return ToySynth.Square(trill, t) * 0.5f * envelope;
                    }
                }

                return 0f;
            });
        }

        /// <summary>凍る「キラキラ」: 高い音を細かく上へ重ねて消える</summary>
        private static AudioClip Freeze()
        {
            const float duration = 0.45f;
            var notes = new[] { 1760f, 2217f, 2637f, 3520f };
            return ToySynth.CreateClip("Soccer_Freeze", duration, t =>
            {
                var sum = 0f;
                for (var i = 0; i < notes.Length; i++)
                {
                    var local = t - i * 0.05f;
                    if (local >= 0f)
                    {
                        sum += Mathf.Sin(2f * Mathf.PI * notes[i] * local) * Mathf.Exp(-local * 12f);
                    }
                }

                return sum * 0.25f;
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
