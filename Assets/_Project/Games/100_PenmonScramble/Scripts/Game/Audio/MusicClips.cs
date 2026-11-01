using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 素材の曲（OpenTracks の BGM）をシーンから GameMusic に渡す入れ物。割り当ては SceneBuilder がやる。
    /// 曲の素材は規約でリポジトリに入れられないので、無い環境では空のまま。そのときは GameMusic がコードで作る仮の曲を鳴らす
    /// </summary>
    public sealed class MusicClips : MonoBehaviour
    {
        [SerializeField] private AudioClip _title;
        [SerializeField] private AudioClip _stage;
        [SerializeField] private AudioClip _boss;
        // 素材ごとに音の大きさが違うので、曲ごとに合わせる。値は SceneBuilder の MusicSlots で決める
        [SerializeField, Range(0f, 1f)] private float _titleVolume;
        [SerializeField, Range(0f, 1f)] private float _stageVolume;
        [SerializeField, Range(0f, 1f)] private float _bossVolume;

        /// <summary>素材が無ければ false（仮の曲にする）</summary>
        public bool TryGet(MusicTrack track, out AudioClip clip, out float volume)
        {
            switch (track)
            {
                case MusicTrack.Stage:
                    clip = _stage;
                    volume = _stageVolume;
                    break;
                case MusicTrack.Boss:
                    clip = _boss;
                    volume = _bossVolume;
                    break;
                default:
                    clip = _title;
                    volume = _titleVolume;
                    break;
            }

            return clip != null;
        }
    }
}
