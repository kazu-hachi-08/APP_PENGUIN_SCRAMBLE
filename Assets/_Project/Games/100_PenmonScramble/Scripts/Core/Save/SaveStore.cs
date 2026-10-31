using System;
using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>保存先（本番は PlayerPrefs）。テストで差し替えられるようにする</summary>
    public interface ISaveStorage
    {
        /// <summary>無ければ null</summary>
        string Read(string key);

        void Write(string key, string value);
    }

    /// <summary>記録と文字列の変換（本番は JsonUtility）。読めなければ null を返すか例外を投げる</summary>
    public interface ISaveFormat
    {
        string Write(SaveData data);
        SaveData Read(string text);
    }

    /// <summary>
    /// 記録の読み書き。読めない記録は消さずに別のキーへ退避し、新しい記録で始める（CLAUDE.md §3.5）
    /// </summary>
    public sealed class SaveStore
    {
        /// <summary>一度配ったら変えない</summary>
        public const string Key = "PenmonScramble.Save";

        /// <summary>読めなかった記録の退避先。「.1」「.2」…と空いている所に入れ、前に退避したものも消さない</summary>
        public const string BrokenKeyPrefix = "PenmonScramble.Save.Broken.";

        // 退避先を探す上限。これを超えたら最後の枠に上書きする（何度も壊れ続けて無限に増えないように）
        private const int MaxBrokenSlots = 10;

        private readonly ISaveStorage _storage;
        private readonly ISaveFormat _format;

        public SaveStore(ISaveStorage storage, ISaveFormat format)
        {
            _storage = storage;
            _format = format;
        }

        /// <summary>前回の Load で読めない記録を退避したときのキー。無ければ null</summary>
        public string LastBrokenKey { get; private set; }

        /// <summary>記録が無ければ新しく作る。読めなければ退避して新しく作る</summary>
        public SaveData Load()
        {
            LastBrokenKey = null;
            var text = _storage.Read(Key);
            if (string.IsNullOrEmpty(text))
            {
                return SaveData.CreateNew();
            }

            var data = TryRead(text);
            if (data == null)
            {
                LastBrokenKey = Backup(text);
                return SaveData.CreateNew();
            }

            Upgrade(data);
            PlayerRecord.Repair(data);
            return data;
        }

        public void Save(SaveData data)
        {
            _storage.Write(Key, _format.Write(data));
        }

        /// <summary>
        /// 設定の「初期化」（最初の3体を引き直したい人向け）。新しい記録を作って保存し、それを返す。
        /// 読めなかった記録の退避（Broken）には触らない（プレイヤーが消したのは今の記録だけなので）
        /// </summary>
        public SaveData Reset()
        {
            var data = SaveData.CreateNew();
            Save(data);
            return data;
        }

        private SaveData TryRead(string text)
        {
            try
            {
                var data = _format.Read(text);
                // version が無い（0）なら、この記録の形ではない
                return data != null && data.version >= 1 ? data : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 古い版の記録に、後から足した項目の値を入れる。版を上げたらここに足す。
        /// 新しい版で書かれた記録はそのまま使う（知らない項目は読み飛ばされる）
        /// </summary>
        private static void Upgrade(SaveData data)
        {
            data.penguins ??= new List<OwnedPenguin>();
            data.party ??= new List<int>();
            data.clearedStages ??= new List<int>();
            if (data.version < SaveData.CurrentVersion)
            {
                data.version = SaveData.CurrentVersion;
            }
        }

        private string Backup(string text)
        {
            var key = BrokenKeyPrefix + MaxBrokenSlots;
            for (var i = 1; i <= MaxBrokenSlots; i++)
            {
                if (string.IsNullOrEmpty(_storage.Read(BrokenKeyPrefix + i)))
                {
                    key = BrokenKeyPrefix + i;
                    break;
                }
            }

            _storage.Write(key, text);
            return key;
        }
    }
}
