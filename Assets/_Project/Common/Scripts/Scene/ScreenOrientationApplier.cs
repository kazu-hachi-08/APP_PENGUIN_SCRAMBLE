using UnityEngine;

namespace PenguinScramble.Common.Scene
{
    /// <summary>
    /// 端末の画面向きを横に固定する（このゲームは横画面固定）。
    /// シーンは1つだけで向きが途中で変わらないので、起動時に1回だけ適用する。
    /// </summary>
    public static class ScreenOrientationApplier
    {
        /// <summary>SceneLoader を経由せずに読み込まれる最初のシーンにも効かせるため、AfterSceneLoad で適用する</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SetLandscape();
        }

        private static void SetLandscape()
        {
            // 左右どちらの横持ちでも遊べるように両方許可する
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;

            // AutoRotationだけだと縦持ちのまま入った端末がセンサー検知まで縦に残るため、
            // まず横に固定して強制的に回転させる
            Screen.orientation = ScreenOrientation.LandscapeLeft;
            EnableLandscapeAutoRotationNextFrame();

            WebScreen.SetLandscape();
            StandaloneScreen.SetLandscape();
        }

        /// <summary>
        /// 同じフレームで orientation を上書きすると固定指定が反映されないことがあるため、
        /// 1フレーム待ってから左右どちらの横持ちにも追従させる
        /// </summary>
        private static async void EnableLandscapeAutoRotationNextFrame()
        {
            await Awaitable.NextFrameAsync();
            Screen.orientation = ScreenOrientation.AutoRotation;
        }
    }
}
