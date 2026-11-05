using System;
using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 端末に保存する記録。JSON にそのまま書くので、項目名と型は一度配ったら変えない（CLAUDE.md §3.5）。
    /// 項目を足すときは CurrentVersion を上げ、SaveStore.Upgrade に「古い記録に無かったときの値」を書く。
    /// v1 → v2 だけは、ユーザーの承認を得て古い記録を消して初めからにした（SaveStore.OldestReadableVersion）
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 3;

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

        /// <summary>ボスラッシュのクリア済み（v2〜）。ステージ番号 × 10 ＋ 難易度</summary>
        public List<int> clearedRush = new List<int>();

        /// <summary>
        /// ミニゲームの記録（v3〜）。ミニゲームとコースごとに1行。次のミニゲームは項目を足さずに、この表に行を足す
        /// （MiniGames.Core の MiniGameRecords が読み書きする）
        /// </summary>
        public List<MiniGameRecord> miniGames = new List<MiniGameRecord>();

        public static SaveData CreateNew()
        {
            return new SaveData { version = CurrentVersion };
        }
    }

    /// <summary>持っている1体。同じ種類でも引くたびに別の個体になる</summary>
    [Serializable]
    public sealed class OwnedPenguin
    {
        public int id;
        public int speciesNo;

        /// <summary>引いたときのパワー。強化しても変わらない（上限はこれを元に決める）</summary>
        public int basePower;

        /// <summary>強化で足した分</summary>
        public int boost;

        /// <summary>上限解放の回数（0〜Balance.MaxLimitBreak）</summary>
        public int limitBreak;

        public OwnedPenguin()
        {
        }

        public OwnedPenguin(int id, int speciesNo, int basePower)
        {
            this.id = id;
            this.speciesNo = speciesNo;
            this.basePower = basePower;
        }

        /// <summary>ステージで使う強さ。JsonUtility はプロパティを書かないので、記録には残らない</summary>
        public int Power => basePower + boost;
    }
}
