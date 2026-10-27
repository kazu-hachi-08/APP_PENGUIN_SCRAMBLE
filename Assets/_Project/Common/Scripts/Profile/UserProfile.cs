using UnityEngine;

namespace PenmonScramble.Common.Profile
{
    /// <summary>
    /// 端末に保存するユーザー名。一度決めたら変更できない仕様なので、保存は TrySave の1か所だけにしている。
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

        /// <summary>動作確認用。通常のゲーム中からは呼ばない（名前は変更不可の仕様）</summary>
        public static void ResetForDebug()
        {
            PlayerPrefs.DeleteKey(NameKey);
            PlayerPrefs.Save();
        }
    }
}
