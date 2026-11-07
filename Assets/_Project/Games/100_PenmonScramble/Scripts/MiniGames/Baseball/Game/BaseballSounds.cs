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
        /// <summary>三振（審判の「ストラーイク！」の代わりの、短い音と伸びて上がる音）</summary>
        Strikeout,
        /// <summary>四球（軽い3つの音）</summary>
        Walk,
        /// <summary>ホームランのファンファーレ</summary>
        HomeRun,
        /// <summary>観客の歓声（ワーッ）</summary>
        Cheer,
        /// <summary>観客のどよめき（おお〜。フェンス直撃・縁で外れた）</summary>
        Ooh,
        /// <summary>魔球・必殺打のカットイン（上がっていく音）</summary>
        Special,
        /// <summary>チェンジ（攻守交代の、オルガンのような2声のジングル）</summary>
        Change,
        /// <summary>フリーズボールが凍った（キーン）</summary>
        Freeze,
        /// <summary>消える魔球が消えた・現れた（ヒュン）</summary>
        Vanish,
        /// <summary>水切りヒットが跳ねた（ピチャッ）</summary>
        Skip,
        /// <summary>間欠泉が打球を跳ね上げた（シュワッ）</summary>
        Geyser,
        /// <summary>試合の始め（審判の「プレイボール！」の代わりの、呼びかけとファンファーレ）</summary>
        PlayBall,
        /// <summary>走者がホームへ還った（ピコン。1人ずつ高くなる）</summary>
        Score,
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
            { BaseballSound.Special, 0.55f },
            { BaseballSound.Change, 0.4f },
            { BaseballSound.Freeze, 0.45f },
            { BaseballSound.Vanish, 0.4f },
            { BaseballSound.Skip, 0.35f },
            { BaseballSound.Geyser, 0.4f },
            { BaseballSound.PlayBall, 0.5f },
            { BaseballSound.Score, 0.45f },
        };

        // 同じ打球で還った2人目・3人目…は、これだけ高くして「点が重なった」のが分かるようにする
        private const float ScorePitchStep = 0.12f;
        private const int ScorePitchSteps = 3;

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

        /// <summary>走者がホームへ還った音。index はその打球で何人目か（0 から）</summary>
        public static void PlayScore(int index)
        {
            Play(BaseballSound.Score, Volumes[BaseballSound.Score], 1f + Mathf.Min(index, ScorePitchSteps) * ScorePitchStep);
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
                case BaseballSound.Strikeout: return UmpireCall("Baseball_Strikeout", 0.55f);
                case BaseballSound.Walk: return ToySynth.Jingle("Baseball_Walk", new[] { 784f, 784f, 988f });
                case BaseballSound.Cheer: return MiniGameCrowdSounds.Cheer();
                case BaseballSound.Ooh: return MiniGameCrowdSounds.Ooh();
                case BaseballSound.Special: return ToySynth.Sweep("Baseball_Special", 0.35f, 220f, 1400f);
                case BaseballSound.Change: return Change();
                case BaseballSound.Freeze: return Freeze();
                // 消える・現れる「ヒュン」: 高い所から一気に下がる
                case BaseballSound.Vanish: return ToySynth.Sweep("Baseball_Vanish", 0.15f, 1800f, 500f);
                // 水切りの「ピチャッ」: 短く高めの打撃
                case BaseballSound.Skip: return ToySynth.Impact("Baseball_Skip", 0.05f, 900f);
                case BaseballSound.Geyser: return ToySynth.Whoosh("Baseball_Geyser");
                case BaseballSound.PlayBall: return PlayBall();
                case BaseballSound.Score: return ToySynth.Jingle("Baseball_Score", new[] { 1047f, 1568f });
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

        /// <summary>凍る「キーン」: 高い澄んだ音が2つ重なって、ゆっくり消える</summary>
        private static AudioClip Freeze()
        {
            const float duration = 0.5f;
            return ToySynth.CreateClip("Baseball_Freeze", duration, t =>
            {
                var ring = Mathf.Sin(2f * Mathf.PI * 2093f * t) * 0.4f + Mathf.Sin(2f * Mathf.PI * 3136f * t) * 0.25f;
                return ring * Mathf.Min(1f, t / 0.01f) * Mathf.Exp(-t * 6f);
            });
        }

        /// <summary>
        /// 審判の呼び声の代わり: 短く切った「スト」と、下から持ち上げて伸ばす「ラーイク」。声に聞こえるよう、
        /// 角の丸い三角波に少しの揺れを付け、高さは人の声の上のあたり（言葉を録らずに、審判が叫んだ感じだけ出す）
        /// </summary>
        private static AudioClip UmpireCall(string name, float longLength)
        {
            var sampler = UmpireSampler(longLength, out var duration);
            return ToySynth.CreateClip(name, duration, sampler);
        }

        /// <summary>呼び声の波形（t は鳴らし始めからの秒。前から順に呼ぶ）。PlayBall がファンファーレの前につなぐので、クリップにせず波形のまま返す</summary>
        private static System.Func<float, float> UmpireSampler(float longLength, out float duration)
        {
            const float shortLength = 0.08f;
            const float gap = 0.05f;
            duration = shortLength + gap + longLength;
            var phase = 0f;
            return t =>
            {
                float frequency;
                float envelope;
                if (t < shortLength)
                {
                    frequency = 520f;
                    envelope = Mathf.Min(1f, t / 0.01f) * (1f - t / shortLength * 0.4f);
                }
                else if (t < shortLength + gap)
                {
                    return 0f;
                }
                else
                {
                    var local = t - shortLength - gap;
                    frequency = Mathf.Lerp(440f, 700f, Mathf.Clamp01(local / 0.12f)) * (1f + 0.02f * Mathf.Sin(2f * Mathf.PI * 6f * local));
                    envelope = Mathf.Min(1f, local / 0.02f) * Mathf.Clamp01((longLength - local) / 0.15f);
                }

                phase += frequency / ToySynth.SampleRate;
                var triangle = Mathf.Abs(phase % 1f * 4f - 2f) - 1f;
                var edge = ToySynth.Square(frequency, t) * 0.25f;
                return (triangle * 0.5f + edge) * envelope;
            };
        }

        /// <summary>チェンジ: 上の旋律と下の支えの2声で、球場のオルガンのように明るく区切る（1 秒ほど。帯を見せている間に鳴り終わる）</summary>
        private static AudioClip Change()
        {
            const float note = 0.11f;
            var melody = new[] { 784f, 988f, 1175f, 988f, 1175f, 1568f };
            var bass = new[] { 196f, 196f, 247f, 247f, 294f, 392f };
            var duration = note * (melody.Length + 2);
            return ToySynth.CreateClip("Baseball_Change", duration, t =>
            {
                var index = Mathf.Min(melody.Length - 1, Mathf.FloorToInt(t / note));
                var local = t - index * note;
                // 最後の音だけ伸ばして、区切りの感じを出す
                var length = index == melody.Length - 1 ? note * 3f : note;
                var envelope = Mathf.Max(0f, 1f - local / length);
                var top = ToySynth.Square(melody[index], t) * 0.8f;
                var low = Mathf.Sin(2f * Mathf.PI * bass[index] * t) * 0.35f;
                return (top + low) * envelope;
            });
        }

        /// <summary>
        /// プレイボール: 審判の呼び声の代わり（短い「プレイ」と伸ばす「ボール」）のあとに、上がっていく短いファンファーレ。
        /// 2つのクリップを読み出してつなぐのではなく波形でつなぐ（ブラウザ版ではクリップの中身を読み出せないため）
        /// </summary>
        private static AudioClip PlayBall()
        {
            const float note = 0.11f;
            var fanfare = new[] { 523f, 659f, 784f, 1047f };
            var call = UmpireSampler(0.4f, out var callLength);
            var duration = callLength + note * (fanfare.Length + 1);
            return ToySynth.CreateClip("Baseball_PlayBall", duration, t =>
            {
                if (t < callLength)
                {
                    return call(t);
                }

                var local = t - callLength;
                var index = Mathf.Min(fanfare.Length - 1, Mathf.FloorToInt(local / note));
                var inNote = local - index * note;
                var length = index == fanfare.Length - 1 ? note * 2f : note;
                return ToySynth.Square(fanfare[index], t) * Mathf.Max(0f, 1f - inNote / length);
            });
        }

        private static AudioClip Beep(string name, float frequency, float duration)
        {
            return ToySynth.CreateClip(name, duration, t => ToySynth.Square(frequency, t) * 0.4f * Mathf.Min(1f, t / 0.005f) * Mathf.Exp(-t * 18f));
        }
    }
}
