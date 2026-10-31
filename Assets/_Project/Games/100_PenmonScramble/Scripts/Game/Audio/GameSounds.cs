using System.Collections.Generic;
using PenmonScramble.Common.Audio;
using UnityEngine;

namespace PenmonScramble.Game
{
    public enum GameSound
    {
        Skill,
        Hit,
        /// <summary>プレイヤーが攻撃を受けた</summary>
        Hurt,
        Defeat,
        Special,
        Combo,
        /// <summary>大ボスの攻撃の予告（赤い範囲）が出た</summary>
        Warning,
        BossDefeat,
        Clear,
        Fail,
        /// <summary>ガチャで★が1つ出た</summary>
        Star,
        /// <summary>メニューのボタンを押した</summary>
        Click,
        /// <summary>タイトルで「タップしてスタート」を押した</summary>
        TitleStart,
        /// <summary>ガチャのレバーが 1/4 回った（カチッ）</summary>
        GachaLever,
        /// <summary>ガチャのカプセルが落ちた・揺れた（コトン）</summary>
        GachaDrop,
        /// <summary>ガチャのカプセルが割れて、中の子が出てきた</summary>
        GachaOpen,
        /// <summary>ガチャで★3 の光の柱が立った</summary>
        GachaRare,
    }

    /// <summary>
    /// 仮の効果音。素材がまだ無いのでコードで作る（ペンモンの BattleSounds から移植）。
    /// 共通基盤の SeId はゲーム固有の音を持たないので、クリップを作って AudioManager に直接渡す（音量設定はそちらで掛かる）。
    /// 音の作り方は GameSounds.Synth.cs
    /// </summary>
    public static partial class GameSounds
    {
        private static readonly Dictionary<GameSound, AudioClip> Clips = new Dictionary<GameSound, AudioClip>();

        // 乱戦で同時にたくさん鳴るので、技・当たりは控えめにする
        private static readonly Dictionary<GameSound, float> Volumes = new Dictionary<GameSound, float>
        {
            { GameSound.Skill, 0.25f },
            { GameSound.Hit, 0.45f },
            { GameSound.Hurt, 0.6f },
            { GameSound.Defeat, 0.4f },
            { GameSound.Special, 0.6f },
            { GameSound.Combo, 0.6f },
            { GameSound.Warning, 0.45f },
            { GameSound.BossDefeat, 0.7f },
            { GameSound.Clear, 0.55f },
            { GameSound.Fail, 0.55f },
            { GameSound.Star, 0.45f },
            { GameSound.Click, 0.35f },
            { GameSound.TitleStart, 0.5f },
            { GameSound.GachaLever, 0.5f },
            { GameSound.GachaDrop, 0.55f },
            { GameSound.GachaOpen, 0.5f },
            { GameSound.GachaRare, 0.55f },
        };

        public static void Play(GameSound sound)
        {
            if (!Clips.TryGetValue(sound, out var clip) || clip == null)
            {
                clip = Create(sound);
                Clips[sound] = clip;
            }

            var audio = AudioManager.Instance;
            if (audio != null)
            {
                audio.PlaySeClip(clip, Volumes[sound]);
            }
        }

        private static AudioClip Create(GameSound sound)
        {
            switch (sound)
            {
                case GameSound.Skill: return Whoosh("Se_Skill");
                case GameSound.Hit: return Impact("Se_Hit", 0.1f, 180f);
                case GameSound.Hurt: return Impact("Se_Hurt", 0.2f, 90f);
                case GameSound.Defeat: return Sweep("Se_Defeat", 0.18f, 900f, 1800f);
                case GameSound.Special: return Sweep("Se_Special", 0.5f, 300f, 1600f);
                case GameSound.Combo: return Jingle("Se_Combo", new[] { 784f, 988f, 1175f, 1568f });
                case GameSound.Warning: return Alarm("Se_Warning");
                case GameSound.BossDefeat: return Impact("Se_BossDefeat", 0.6f, 55f);
                case GameSound.Clear: return Jingle("Se_Clear", new[] { 523f, 659f, 784f, 1047f, 784f, 1047f });
                case GameSound.Star: return Tone("Se_Star", 0.08f, 1568f);
                case GameSound.Click: return Tone("Se_Click", 0.05f, 1047f);
                case GameSound.TitleStart: return Jingle("Se_TitleStart", new[] { 784f, 1175f, 1568f });
                case GameSound.GachaLever: return Impact("Se_GachaLever", 0.05f, 600f);
                case GameSound.GachaDrop: return Impact("Se_GachaDrop", 0.12f, 260f);
                case GameSound.GachaOpen: return Sweep("Se_GachaOpen", 0.25f, 500f, 1400f);
                case GameSound.GachaRare: return Jingle("Se_GachaRare", new[] { 1047f, 1319f, 1568f, 2093f });
                default: return Jingle("Se_Fail", new[] { 392f, 330f, 262f, 196f });
            }
        }
    }
}
