using System;
using UnityEngine;

namespace PenmonScramble.Common.Audio
{
    /// <summary>
    /// BGM識別子。番号はインスペクタに保存されるので、値を消しても残りの番号は詰めない
    /// </summary>
    public enum BgmId
    {
        None = 0,
        TitleBgm = 1,
        GameBgm01 = 2,
        GameBgm02 = 3,
        GameBgm03 = 5,
        GameBgm04 = 6,
        GameBgm05 = 7
    }

    /// <summary>
    /// SE識別子。ゲーム固有の音はここに足さず、ゲーム側で AudioManager.PlaySeClip に直接渡す。
    /// SE も BGM と同じく、値を消しても番号を詰めない（インスペクタに保存されるため）
    /// </summary>
    public enum SeId
    {
        None = 0,
        ButtonClick = 1,
        GameOver = 6,
        GameClear = 7
    }

    [Serializable]
    public struct BgmData
    {
        public BgmId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }

    [Serializable]
    public struct SeData
    {
        public SeId id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume;
    }
}
