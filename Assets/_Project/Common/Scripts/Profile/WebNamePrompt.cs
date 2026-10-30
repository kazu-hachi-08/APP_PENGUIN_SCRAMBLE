namespace PenmonScramble.Common.Profile
{
    /// <summary>
    /// ブラウザ版の名前入力。Unity の InputField はブラウザ版だと日本語IMEで変換できないため、
    /// ブラウザ標準の入力ダイアログ（window.prompt）で入力させる。JS 側は Common/Plugins/WebGL/NameInput.jslib。
    /// </summary>
    public static class WebNamePrompt
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern string PenmonScramble_PromptText(string message, string defaultText);

        public static bool IsAvailable => true;

        /// <summary>キャンセルされたら null</summary>
        public static string Prompt(string message, string defaultText) => PenmonScramble_PromptText(message, defaultText);
#else
        public static bool IsAvailable => false;

        public static string Prompt(string message, string defaultText) => null;
#endif
    }
}
