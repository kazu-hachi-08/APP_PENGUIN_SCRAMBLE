using System;
using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 端末に保存する記録（§10）。JSON にそのまま書くので、項目名と型は一度配ったら変えない（CLAUDE.md §3.5）。
    /// 項目を足すときは CurrentVersion を上げ、SaveStore.Upgrade に「古い記録に無かったときの値」を書く
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        // JsonUtility で読み書きするため、プロパティではなく public のフィールドにする。
        // version は初期値を入れない（version の無い JSON を、この記録だと思い込まないように）
        public int version;
        public int coins;
        public int nextPenguinId = 1;
        public int freePullsUsed;
        public List<OwnedPenguin> penguins = new List<OwnedPenguin>();

        /// <summary>編成の3体（個体の id）。並びが交代の順</summary>
        public List<int> party = new List<int>();

        public List<int> clearedStages = new List<int>();

        public static SaveData CreateNew()
        {
            return new SaveData { version = CurrentVersion };
        }
    }

    /// <summary>持っている1体。同じ種類でも引くたびに別の個体になる（§7）</summary>
    [Serializable]
    public sealed class OwnedPenguin
    {
        public int id;
        public int speciesNo;
        public int power;

        public OwnedPenguin()
        {
        }

        public OwnedPenguin(int id, int speciesNo, int power)
        {
            this.id = id;
            this.speciesNo = speciesNo;
            this.power = power;
        }
    }
}
