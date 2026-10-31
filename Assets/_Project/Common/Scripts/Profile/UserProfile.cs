using UnityEngine;

namespace PenmonScramble.Common.Profile
{
    /// <summary>
    /// 端末に保存するユーザー名。協力プレイで頭の上に出す。設定からいつでも変えられる。
    /// キー名は一度配ったら変えない（変えると名前が消える）
    /// </summary>
    public static class UserProfile
    {
        public const int MaxNameLength = 6;

        private const string NameKey = "UserProfile.Name";

        public static bool HasName => !string.IsNullOrEmpty(Name);

        public static string Name => PlayerPrefs.GetString(NameKey, string.Empty);

        /// <summary>
        /// 入力値を保存できる形に整える。
        /// &lt; &gt; はリッチテキスト（&lt;color&gt;）を壊すため取り除く。
        /// </summary>
        public static string Sanitize(string raw)
        {
            if (raw == null) return string.Empty;
            return raw.Replace("<", string.Empty).Replace(">", string.Empty).Trim();
        }

        public static bool IsValid(string name)
        {
            string sanitized = Sanitize(name);
            return sanitized.Length > 0 && sanitized.Length <= MaxNameLength;
        }

        public static bool TrySave(string name)
        {
            if (!IsValid(name)) return false;

            PlayerPrefs.SetString(NameKey, Sanitize(name));
            // ブラウザ版は Save しないとタブを閉じたときに消えることがあるため即書き込む
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>動作確認用（初回の名前入力をもう一度見る）。通常のゲーム中からは呼ばない</summary>
        public static void ResetForDebug()
        {
            PlayerPrefs.DeleteKey(NameKey);
            PlayerPrefs.Save();
        }
    }
}
