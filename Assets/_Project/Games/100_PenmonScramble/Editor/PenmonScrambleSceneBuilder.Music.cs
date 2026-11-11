using PenmonScramble.Game;
using UnityEditor;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 素材の曲（Music/）を MusicClips につなぐ。曲は規約でリポジトリに入れられないので、無ければ空のままにして仮の曲で鳴らす。
    /// どの曲を使うか・入手先は Docs/PENMON_SCRAMBLE_SPEC.md
    /// </summary>
    public static partial class PenmonScrambleSceneBuilder
    {
        public const string MusicFolder = "Assets/_Project/Games/100_PenmonScramble/Music/";

        // 素材ごとに録音の大きさが違うので、曲ごとに効果音が埋もれない大きさへ合わせる
        private static readonly (string Property, string File, string VolumeProperty, float Volume)[] MusicSlots =
        {
            ("_title", "Bgm_Title.mp3", "_titleVolume", 0.3f),
            ("_stage", "Bgm_Stage.mp3", "_stageVolume", 0.3f),
            ("_boss", "Bgm_Boss.mp3", "_bossVolume", 0.3f),
            ("_golf", "Bgm_Golf.mp3", "_golfVolume", 0.3f),
            ("_soccer", "Bgm_Soccer.mp3", "_soccerVolume", 0.3f),
            ("_baseball", "Bgm_Baseball.mp3", "_baseballVolume", 0.3f),
        };

        /// <summary>ミニゲームのシーン（GolfSceneBuilder・SoccerSceneBuilder・BaseballSceneBuilder）も同じ曲の入れ物を使う</summary>
        internal static MusicClips CreateMusicClips(GameObject owner)
        {
            var clips = owner.AddComponent<MusicClips>();
            var so = new SerializedObject(clips);
            foreach (var slot in MusicSlots)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicFolder + slot.File);
                if (clip == null)
                {
                    Debug.Log($"[PenmonScrambleSceneBuilder] {MusicFolder}{slot.File} が無いので、この曲はコードで作る仮の曲になります");
                }

                so.FindProperty(slot.Property).objectReferenceValue = clip;
                so.FindProperty(slot.VolumeProperty).floatValue = slot.Volume;
            }

            so.ApplyModifiedProperties();
            return clips;
        }
    }
}
