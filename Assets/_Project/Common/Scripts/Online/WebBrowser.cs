using UnityEngine;

namespace PenguinScramble.Common.Online
{
    /// <summary>
    /// 部屋コードのコピーを、ブラウザ版ではブラウザの機能で行う。
    /// GUIUtility.systemCopyBuffer はブラウザ版だとPCのクリップボードに届かず、友達にコードを貼れないため。
    /// </summary>
    public static class WebBrowser
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void PenguinScramble_CopyText(string text);

        public static void CopyText(string text) => PenguinScramble_CopyText(text);
#else
        public static void CopyText(string text) => GUIUtility.systemCopyBuffer = text;
#endif
    }
}
