using UnityEditor;
using UnityEngine;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// 素材の曲の取り込み設定を決める。曲はリポジトリに入れない（.meta も無い）ので、どの PC で取り込んでも同じ設定になるようコードで持つ。
    /// </summary>
    public sealed class MusicImportSettings : AssetPostprocessor
    {
        // 128kbps ほど。BGM は効果音と重なるので、これより上げても違いが分かりにくく、ブラウザ版の容量だけ増える
        private const float VorbisQuality = 0.5f;

        // 設定を変えたら上げる。上げると Unity が曲を取り込み直す（このクラスより先に取り込まれた曲にも設定が掛かる）
        public override uint GetVersion() => 1;

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(PenmonScrambleSceneBuilder.MusicFolder))
            {
                return;
            }

            var importer = (AudioImporter)assetImporter;
            // 数分の曲を丸ごと展開するとスマホのメモリを食うので、少しずつ読みながら流す
            importer.defaultSampleSettings = new AudioImporterSampleSettings
            {
                loadType = AudioClipLoadType.Streaming,
                compressionFormat = AudioCompressionFormat.Vorbis,
                quality = VorbisQuality,
                sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
                preloadAudioData = true,
            };
            importer.loadInBackground = false;
        }
    }
}
