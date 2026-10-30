using UnityEngine;

namespace PenmonScramble.Common.Scene
{
    /// <summary>
    /// Windows版のウィンドウ形をシーンの向きに合わせる。
    /// PCでは Screen.orientation が効かず、横長ウィンドウのままだと縦画面用のUIが上下にはみ出すため、
    /// ウィンドウそのものを 9:16 / 16:9 に切り替える（WebScreen のPC版）。
    /// </summary>
    public static class StandaloneScreen
    {
        // タイトルバーとタスクバーの分だけ余らせないと、縦長ウィンドウの下端が画面外に出る
        private const float MaxScreenRatio = 0.85f;

        public static void SetLandscape() => SetAspect(WebScreen.LongSide, WebScreen.ShortSide);

#if UNITY_STANDALONE && !UNITY_EDITOR
        private static void SetAspect(int aspectWidth, int aspectHeight)
        {
            // ウィンドウモードでもデスクトップ解像度が返るので、モニターに収まる最大サイズを求められる
            var display = Screen.currentResolution;
            float maxWidth = display.width * MaxScreenRatio;
            float maxHeight = display.height * MaxScreenRatio;

            float scale = Mathf.Min(maxWidth / aspectWidth, maxHeight / aspectHeight);
            int width = Mathf.RoundToInt(aspectWidth * scale);
            int height = Mathf.RoundToInt(aspectHeight * scale);

            // 全画面だと縦長にできないため、常にウィンドウモードで形を合わせる
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
        }
#else
        private static void SetAspect(int aspectWidth, int aspectHeight) { }
#endif
    }
}
