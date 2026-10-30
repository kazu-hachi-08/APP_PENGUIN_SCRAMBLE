namespace PenmonScramble.Common.Scene
{
    /// <summary>
    /// ブラウザ版の表示枠をシーンの向きに合わせる。
    /// ブラウザでは Screen.orientation が効かず、枠の形が固定だと縦画面用のUIが横長に潰れるため、
    /// 枠そのものを 9:16 / 16:9 に切り替える。
    /// </summary>
    public static class WebScreen
    {
        // UIの基準解像度(CanvasScaler)と同じ比率にする
        // StandaloneScreen と共有する（PC版も同じ比率で窓を作る）
        internal const int ShortSide = 9;
        internal const int LongSide = 16;

        public static void SetLandscape() => SetAspect(LongSide, ShortSide);

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void PenmonScramble_SetAspect(int width, int height);

        private static void SetAspect(int width, int height) => PenmonScramble_SetAspect(width, height);
#else
        private static void SetAspect(int width, int height) { }
#endif
    }
}
