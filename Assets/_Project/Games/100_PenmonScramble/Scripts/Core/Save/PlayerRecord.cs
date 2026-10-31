using System;
using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 記録（SaveData）への操作: 編成・売る・ステージの報酬と解放。ガチャは PlayerRecord.Gacha.cs。
    /// 保存はしない（呼んだ側が、ガチャの後・結果の後・売った後に保存する）
    /// </summary>
    public sealed partial class PlayerRecord
    {
        public PlayerRecord(SaveData data)
        {
            Data = data;
        }

        public SaveData Data { get; }
        public int Coins => Data.coins;
        public IReadOnlyList<OwnedPenguin> Penguins => Data.penguins;
        public IReadOnlyList<int> Party => Data.party;

        public OwnedPenguin Find(int id)
        {
            return Data.penguins.Find(p => p.id == id);
        }

        public bool IsInParty(int id) => Data.party.Contains(id);

        /// <summary>編成の slot 番目を id の子にする。もう編成にいる子なら、2つの場所を入れ替える</summary>
        public bool SetPartySlot(int slot, int id)
        {
            if (slot < 0 || slot >= Data.party.Count || !IsUsable(Find(id)))
            {
                return false;
            }

            var current = Data.party.IndexOf(id);
            if (current >= 0)
            {
                Data.party[current] = Data.party[slot];
            }

            Data.party[slot] = id;
            return true;
        }

        /// <summary>売ってコインに換える。編成の子は売れない</summary>
        public bool Sell(int id)
        {
            var penguin = Find(id);
            if (penguin == null || IsInParty(id))
            {
                return false;
            }

            Data.penguins.Remove(penguin);
            Data.coins += SellPrice(penguin);
            return true;
        }

        public static int SellPrice(OwnedPenguin penguin)
        {
            var species = SpeciesTable.Get(penguin.speciesNo);
            return Balance.SellPrice(species?.Stars ?? 1);
        }

        /// <summary>ステージの1番目か、1つ前をクリアしていれば遊べる</summary>
        public bool IsUnlocked(int stageNo)
        {
            return stageNo == 1 || Data.clearedStages.Contains(stageNo - 1);
        }

        public bool IsCleared(int stageNo) => Data.clearedStages.Contains(stageNo);

        /// <summary>解放したワールドの数。ワールドの1ステージ目が遊べれば、そのワールドは解放済み</summary>
        public int WorldsUnlocked
        {
            get
            {
                var worlds = 1;
                while (IsUnlocked(worlds * StageTable.StagesPerWorld + 1))
                {
                    worlds++;
                }

                return worlds;
            }
        }

        /// <summary>ステージの結果を記録し、もらったコインを返す（協力ではホストの結果を各自の端末で記録する）</summary>
        public int RecordStage(int stageNo, bool cleared, int enemiesDefeated)
        {
            var coins = cleared ? Balance.ClearCoins : Math.Max(0, enemiesDefeated) * Balance.FailCoinsPerEnemy;
            if (cleared && StageTable.Get(stageNo) != null && !IsCleared(stageNo))
            {
                Data.clearedStages.Add(stageNo);
                coins += Balance.FirstClearBonusCoins;
            }

            Data.coins += coins;
            return coins;
        }

        /// <summary>編成の3体（並びが交代の順）。足りなければ少ない数のまま返す</summary>
        public List<OwnedPenguin> PartyPenguins()
        {
            var penguins = new List<OwnedPenguin>();
            foreach (var id in Data.party)
            {
                var penguin = Find(id);
                if (IsUsable(penguin))
                {
                    penguins.Add(penguin);
                }
            }

            return penguins;
        }

        /// <summary>編成の3体を、ステージで使う値にする</summary>
        public UnitStats[] PartyStats()
        {
            return PartyPenguins().ConvertAll(StatsOf).ToArray();
        }

        public static UnitStats StatsOf(OwnedPenguin penguin)
        {
            return SpeciesTable.PlayerStats(SpeciesTable.Get(penguin.speciesNo), penguin.power);
        }

        /// <summary>
        /// 読み込んだ記録の食い違いを直す: いない子・重なった子を編成から外し、空いた所を強い順に埋める。
        /// 知らない種類（新しい版で増えた子など）は消さずに残し、編成にだけ入れない
        /// </summary>
        public static void Repair(SaveData data)
        {
            var record = new PlayerRecord(data);
            var party = new List<int>();
            foreach (var id in data.party)
            {
                if (!party.Contains(id) && IsUsable(record.Find(id)) && party.Count < Balance.PartySize)
                {
                    party.Add(id);
                }
            }

            data.party = party;
            record.FillParty();
            foreach (var penguin in data.penguins)
            {
                data.nextPenguinId = Math.Max(data.nextPenguinId, penguin.id + 1);
            }
        }

        /// <summary>編成が3体に足りなければ、入っていない子をパワーの高い順に足す</summary>
        private void FillParty()
        {
            var candidates = Data.penguins.FindAll(p => IsUsable(p) && !IsInParty(p.id));
            candidates.Sort((a, b) => b.power.CompareTo(a.power));
            foreach (var penguin in candidates)
            {
                if (Data.party.Count >= Balance.PartySize)
                {
                    break;
                }

                Data.party.Add(penguin.id);
            }
        }

        private static bool IsUsable(OwnedPenguin penguin)
        {
            return penguin != null && SpeciesTable.Get(penguin.speciesNo) != null;
        }
    }
}
