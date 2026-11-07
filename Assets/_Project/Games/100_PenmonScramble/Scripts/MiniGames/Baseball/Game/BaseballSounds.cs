using System.Collections.Generic;
using PenmonScramble.Common.Audio;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    public enum BaseballSound
    {
        /// <summary>投げた（シュッ）</summary>
        Throw,
        /// <summary>バットを振った（ブン）</summary>
        Swing,
        /// <summary>バットに当たった（強いほど高く響くカキーン、弱いとコツッ）</summary>
        Bat,
        /// <summary>バントで当てた（コン）</summary>
        Bunt,
        /// <summary>ミットに収まった（バシッ）</summary>
        Mitt,
        /// <summary>浮いた打球が盤に着いた（ポン）</summary>
        Land,
        /// <summary>打球がフェンスに当たった（ドン）</summary>
        Fence,
        /// <summary>打球が穴の縁で曲がって通り過ぎた（カラン）</summary>
        Lip,
        /// <summary>打球が穴に落ちた（ポトン＋ころころ）</summary>
        Hole,
        /// <summary>ストライクの判定（ピッ）</summary>
        Strike,
        /// <summary>ボールの判定（ポッ）</summary>
        Ball,
        /// <summary>ヒット（上がる短い音）</summary>
        Hit,
        /// <summary>アウト（下がる2つの音）</summary>
        Out,
        /// <summary>三振（下がっていく音）</summary>
        Strikeout,
        /// <summary>四球（軽い3つの音）</summary>
        Walk,
        /// <summary>ホームランのファンファーレ</summary>
        HomeRun,
        /// <summary>観客の歓声（ワーッ）</summary>
        Cheer,
        /// <summary>観客のどよめき（おお〜。フェンス直撃・縁で外れた）</summary>
        Ooh,
    }

    /// <summary>
    /// 野球の効果音。本編・ゴルフ・サッカーと同じくコードで作るチップチューン（ToySynth）。観客の声はサッカーと共通（MiniGameCrowdSounds）。
    /// 音量の設定は AudioManager で掛かる
    /// </summary>
    public static class BaseballSounds
    {
        // 当たりの強さ（0〜1）で変える幅: 強いほど高く大きく響く
        private const float BatPitchWeak = 0.75f;
        private const float BatPitchStrong = 1.15f;
        private const float BatVolumeMin = 0.3f;
        private const float BatVolumeMax = 0.85f;
        private const float FenceVolumeMin = 0.15f;
        private const float FenceVolumeMax = 0.6f;
        // フェンスに向かう速さがこれで「いちばん強い」とする（マス/秒）
        private const float FenceFullSpeed = 10f;

        private static readonly Dictionary<BaseballSound, AudioClip> Clips = new Dictionary<BaseballSound, AudioClip>();

        private static readonly Dictionary<BaseballSound, float> Volumes = new Dictionary<BaseballSound, float>
        {
            { BaseballSound.Throw, 0.3f },
            { BaseballSound.Swing, 0.3f },
            { BaseballSound.Bunt, 0.45f },
            { BaseballSound.Mitt, 0.55f },
            { BaseballSound.Land, 0.25f },
            { BaseballSound.Lip, 0.4f },
            { BaseballSound.Hole, 0.5f },
            { BaseballSound.Strike, 0.35f },
            { BaseballSound.Ball, 0.3f },
            { BaseballSound.Hit, 0.45f },
            { BaseballSound.Out, 0.4f },
            { BaseballSound.Strikeout, 0.45f },
            { BaseballSound.Walk, 0.4f },
            { BaseballSound.HomeRun, 0.6f },
            // 観客の声はファンファーレ・打球の音の後ろで鳴る大きさに
            { BaseballSound.Cheer, 0.35f },
            { BaseballSound.Ooh, 0.3f },
        };

        /// <summary>バットに当たった音。power は当たりの強さ（0〜1）</summary>
        public static void PlayBat(float power)
        {
            var t = Mathf.Clamp01(power);
            Play(BaseballSound.Bat, Mathf.Lerp(BatVolumeMin, BatVolumeMax, t), Mathf.Lerp(BatPitchWeak, BatPitchStrong, t));
        }

        /// <summary>フェンスの音。speed はフェンスへ向かう速さ</summary>
        public static void PlayFence(float speed)
        {
            Play(BaseballSound.Fence, Mathf.Lerp(FenceVolumeMin, FenceVolumeMax, Mathf.Clamp01(speed / FenceFullSpeed)), 1f);
        }

        public static void Play(BaseballSound sound) => Play(sound, Volumes.TryGetValue(sound, out var volume) ? volume : 0.5f, 1f);

        private static void Play(BaseballSound sound, float volume, float pitch)
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

        private static AudioClip Create(BaseballSound sound)
        {
            switch (sound)
            {
                case BaseballSound.Throw: return ToySynth.Whoosh("Baseball_Throw");
                case BaseballSound.Swing: return Swing();
                case BaseballSound.Bat: return Bat();
                // バントの「コン」: 短く乾いた木の音
                case BaseballSound.Bunt: return ToySynth.Impact("Baseball_Bunt", 0.06f, 520f);
                case BaseballSound.Mitt: return Mitt();
                case BaseballSound.Land: return ToySynth.Impact("Baseball_Land", 0.06f, 190f);
                // フェンスの「ドン」: 低い打撃
                case BaseballSound.Fence: return ToySynth.Impact("Baseball_Fence", 0.12f, 120f);
                case BaseballSound.Lip: return Lip();
                case BaseballSound.Hole: return Hole();
                case BaseballSound.Strike: return Beep("Baseball_Strike", 1320f, 0.12f);
                case BaseballSound.Ball: return Beep("Baseball_Ball", 520f, 0.1f);
                case BaseballSound.Hit: return ToySynth.Jingle("Baseball_Hit", new[] { 659f, 784f, 1047f });
                case BaseballSound.Out: return ToySynth.Jingle("Baseball_Out", new[] { 523f, 392f });
                case BaseballSound.Strikeout: return ToySynth.Sweep("Baseball_Strikeout", 0.45f, 700f, 200f);
                case BaseballSound.Walk: return ToySynth.Jingle("Baseball_Walk", new[] { 784f, 784f, 988f });
                case BaseballSound.Cheer: return MiniGameCrowdSounds.Cheer();
                case BaseballSound.Ooh: return MiniGameCrowdSounds.Ooh();
                default: return ToySynth.Jingle("Baseball_HomeRun", new[] { 523f, 659f, 784f, 1047f, 784f, 1047f, 1319f });
            }
        }

        /// <summary>振る「ブン」: 低めのノイズが一気に通り過ぎる</summary>
        private static AudioClip Swing()
        {
            const float duration = 0.16f;
            var random = new System.Random(41);
            var filtered = 0f;
            return ToySynth.CreateClip("Baseball_Swing", duration, t =>
            {
                filtered = Mathf.Lerp(filtered, (float)random.NextDouble() * 2f - 1f, 0.15f);
                var envelope = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);
                return filtered * 1.6f * envelope;
            });
        }

        /// <summary>
        /// バットの「カキーン」: 高い金属の響き2つと、当たった瞬間の乾いたノイズ。弱い当たりは高さを下げて鳴らす（PlayBat）ので「コツッ」に近くなる
        /// </summary>
        private static AudioClip Bat()
        {
            const float duration = 0.35f;
            var random = new System.Random(43);
            return ToySynth.CreateClip("Baseball_Bat", duration, t =>
            {
                var ring = Mathf.Sin(2f * Mathf.PI * 1760f * t) * 0.45f + Mathf.Sin(2f * Mathf.PI * 2640f * t) * 0.25f;
                var crack = ((float)random.NextDouble() * 2f - 1f) * Mathf.Exp(-t * 120f) * 0.7f;
                var knock = Mathf.Sin(2f * Mathf.PI * 300f * t) * Mathf.Exp(-t * 40f) * 0.5f;
                return ring * Mathf.Exp(-t * 11f) + crack + knock;
            });
        }

        /// <summary>ミットの「バシッ」: こもった低い音と短いノイズ</summary>
        private static AudioClip Mitt()
        {
            const float duration = 0.12f;
            var random = new System.Random(47);
            var filtered = 0f;
            return ToySynth.CreateClip("Baseball_Mitt", duration, t =>
            {
                filtered = Mathf.Lerp(filtered, (float)random.NextDouble() * 2f - 1f, 0.45f);
                var thump = Mathf.Sin(2f * Mathf.PI * 150f * t) * Mathf.Exp(-t * 30f);
                return (thump * 0.6f + filtered * 0.8f) * Mathf.Exp(-t * 25f);
            });
        }

        /// <summary>縁の「カラン」: 高い音が2回、小さく</summary>
        private static AudioClip Lip()
        {
            const float step = 0.07f;
            const float duration = step * 2.5f;
            return ToySynth.CreateClip("Baseball_Lip", duration, t =>
            {
                var local = t % step;
                var pitch = t < step ? 1568f : 1319f;
                return Mathf.Sin(2f * Mathf.PI * pitch * local) * Mathf.Exp(-local * 35f) * 0.6f;
            });
        }

        /// <summary>穴の「ポトン」: 下がっていく丸い音のあとに、底で転がる小さな音が続く</summary>
        private static AudioClip Hole()
        {
            const float duration = 0.45f;
            var phase = 0f;
            return ToySynth.CreateClip("Baseball_Hole", duration, t =>
            {
                var drop = Mathf.Lerp(900f, 300f, Mathf.Clamp01(t / 0.1f));
                phase += 2f * Mathf.PI * drop / ToySynth.SampleRate;
                var plop = Mathf.Sin(phase) * Mathf.Exp(-t * 20f);
                var rattle = t > 0.12f ? Mathf.Sin(2f * Mathf.PI * 1100f * t) * Mathf.Abs(Mathf.Sin(2f * Mathf.PI * 18f * t)) * Mathf.Exp(-(t - 0.12f) * 9f) * 0.25f : 0f;
                return plop * 0.8f + rattle;
            });
        }

        private static AudioClip Beep(string name, float frequency, float duration)
        {
            return ToySynth.CreateClip(name, duration, t => ToySynth.Square(frequency, t) * 0.4f * Mathf.Min(1f, t / 0.005f) * Mathf.Exp(-t * 18f));
        }
    }
}
