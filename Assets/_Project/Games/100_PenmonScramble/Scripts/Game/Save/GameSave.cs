using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 端末の記録。PlayerPrefs に JSON で保存する。シーンを読み直しても同じものを使うよう static に持つ
    /// </summary>
    public static class GameSave
    {
        private static readonly SaveStore Store = new SaveStore(new PlayerPrefsStorage(), new JsonFormat());
        private static PlayerRecord _record;

        public static PlayerRecord Record
        {
            get
            {
                if (_record == null)
                {
                    _record = new PlayerRecord(Store.Load());
                    if (Store.LastBrokenKey != null)
                    {
                        Debug.LogWarning($"[GameSave] 記録が読めなかったので {Store.LastBrokenKey} に退避して新しく始めます");
                    }
                }

                return _record;
            }
        }

        public static void Save()
        {
            Store.Save(Record.Data);
        }

        /// <summary>記録を消して初めからにする（名前と音量は共通基盤の別のキーなので残る）</summary>
        public static void Reset()
        {
            _record = new PlayerRecord(Store.Reset());
        }

        private sealed class PlayerPrefsStorage : ISaveStorage
        {
            public string Read(string key)
            {
                return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
            }

            public void Write(string key, string value)
            {
                PlayerPrefs.SetString(key, value);
                // ブラウザ版は Save しないとタブを閉じたときに消えることがあるため即書き込む
                PlayerPrefs.Save();
            }
        }

        private sealed class JsonFormat : ISaveFormat
        {
            public string Write(SaveData data) => JsonUtility.ToJson(data);

            public SaveData Read(string text) => JsonUtility.FromJson<SaveData>(text);
        }
    }
}
