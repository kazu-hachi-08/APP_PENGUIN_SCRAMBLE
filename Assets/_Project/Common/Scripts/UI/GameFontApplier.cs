using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityScene = UnityEngine.SceneManagement.Scene;

namespace PenmonScramble.Common.UI
{
    /// <summary>
    /// Unity標準フォントの Text を、同梱の丸ゴシック（M PLUS Rounded 1c）に差し替える。
    /// 標準フォント(LegacyRuntime)は日本語を持たず、スマホ版はOSの細いフォントで、ブラウザ版は何も出ないため、全プラットフォームで同じ字にそろえる。
    /// 各シーンのTextを書き換えるとSceneBuilderで作り直したときに戻るので、実行時に差し替える。
    /// </summary>
    public static class GameFontApplier
    {
        private const string FontPath = "Fonts/MPLUSRounded1c-Medium";
        private const string BuiltinFontName = "LegacyRuntime";

        private static Font _font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            _font = Resources.Load<Font>(FontPath);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            Apply();
        }

        private static void OnSceneLoaded(UnityScene scene, LoadSceneMode mode)
        {
            Apply();
        }

        private static void Apply()
        {
            if (_font == null)
            {
                return;
            }

            // 閉じているダイアログも後で開くので、非表示のものも含める
            var texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include);
            foreach (var text in texts)
            {
                if (text.font != null && text.font.name == BuiltinFontName)
                {
                    text.font = _font;
                    // このフォントは行の高さが文字の大きさの約1.45倍と高く、枠の高さぎりぎりの Text は
                    // Truncate だと1行も入らず丸ごと消える。はみ出しても出すようにする。
                    // 自動で縮める Text は高さの制限で縮み具合を決めるので、そのままにする
                    if (!text.resizeTextForBestFit)
                    {
                        text.verticalOverflow = VerticalWrapMode.Overflow;
                    }
                }
            }
        }
    }
}
