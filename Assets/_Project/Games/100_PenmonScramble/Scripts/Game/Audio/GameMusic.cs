using System.Collections.Generic;
using PenmonScramble.Common.Audio;
using UnityEngine;

namespace PenmonScramble.Game
{
    public enum MusicTrack
    {
        Title,
        Stage,
        /// <summary>ボスエリア（ステージのボス・大ボス）</summary>
        Boss,
        /// <summary>ミニゲームのゴルフ（のんびり狙って打つ）</summary>
        Golf,
        /// <summary>ミニゲームのサッカー（元気に走り回る）</summary>
        Soccer,
        /// <summary>ミニゲームの野球（スタンドの応援のように明るく）</summary>
        Baseball,
    }

    /// <summary>
    /// BGM。素材の曲（MusicClips）があればそれを流し、無ければ楽譜（GameMusic.Songs.cs）からコードで作る仮の曲を流す。
    /// 素材は規約でリポジトリに入れられないので、取り出した直後の環境でも音が鳴るように仮の曲を残している。
    /// 再生は共通基盤の AudioManager に任せる。同じ曲が続くときに頭から流し直さない判定とクロスフェードは、AudioManager がやってくれる。
    /// 仮の曲の作り方は GameMusic.Synth.cs
    /// </summary>
    public static partial class GameMusic
    {
        // 効果音が BGM に埋もれないよう、仮の曲は控えめにする（素材の曲の大きさは MusicClips で曲ごとに決める）
        private const float SynthVolume = 0.45f;
        private const float StopFadeSeconds = 0.3f;

        // 共通基盤の BgmId は「何番目の曲か」だけを表す汎用の名前なので、この作品の曲を順に割り当てる
        private static readonly Dictionary<MusicTrack, BgmId> Ids = new Dictionary<MusicTrack, BgmId>
        {
            { MusicTrack.Title, BgmId.TitleBgm },
            { MusicTrack.Stage, BgmId.GameBgm01 },
            { MusicTrack.Boss, BgmId.GameBgm02 },
            { MusicTrack.Golf, BgmId.GameBgm03 },
            { MusicTrack.Soccer, BgmId.GameBgm04 },
            { MusicTrack.Baseball, BgmId.GameBgm05 },
        };

        private static readonly Dictionary<MusicTrack, AudioClip> SynthClips = new Dictionary<MusicTrack, AudioClip>();

        /// <param name="library">素材の曲。null や素材の無い曲は仮の曲にする</param>
        public static void Play(MusicTrack track, MusicClips library)
        {
            if (library == null || !library.TryGet(track, out var clip, out var volume))
            {
                clip = SynthClipOf(track);
                volume = SynthVolume;
            }

            var audio = AudioManager.Instance;
            if (audio == null)
            {
                return;
            }

            // AudioManager が作り直されても鳴るよう、毎回登録し直す（辞書に入れるだけなので軽い）
            audio.RegisterBgm(Ids[track], clip, volume);
            audio.PlayBgm(Ids[track]);
        }

        /// <summary>仮の曲は作るのに時間がかかるので、一度作ったら使い回す</summary>
        private static AudioClip SynthClipOf(MusicTrack track)
        {
            if (!SynthClips.TryGetValue(track, out var clip) || clip == null)
            {
                clip = Render(SongOf(track));
                SynthClips[track] = clip;
            }

            return clip;
        }

        /// <summary>クリア・失敗のジングルと重ならないよう、結果で止める</summary>
        public static void Stop()
        {
            if (AudioManager.HasInstance)
            {
                AudioManager.Instance.StopBgm(StopFadeSeconds);
            }
        }
    }
}
