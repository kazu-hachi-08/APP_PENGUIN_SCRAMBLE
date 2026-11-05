using System.Collections.Generic;
using NUnit.Framework;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Tests
{
    public class SaveStoreTests
    {
        private sealed class MemoryStorage : ISaveStorage
        {
            public readonly Dictionary<string, string> Values = new Dictionary<string, string>();

            public string Read(string key) => Values.TryGetValue(key, out var value) ? value : null;

            public void Write(string key, string value) => Values[key] = value;
        }

        // 本番（GameSave）と同じ JsonUtility で読み書きする
        private sealed class JsonFormat : ISaveFormat
        {
            public string Write(SaveData data) => JsonUtility.ToJson(data);

            public SaveData Read(string text) => JsonUtility.FromJson<SaveData>(text);
        }

        private MemoryStorage _storage;
        private SaveStore _store;

        [SetUp]
        public void SetUp()
        {
            _storage = new MemoryStorage();
            _store = new SaveStore(_storage, new JsonFormat());
        }

        [Test]
        public void Load_NoRecord_StartsNewWithVersion()
        {
            var data = _store.Load();

            Assert.That(data.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(data.coins, Is.Zero);
            Assert.That(data.penguins, Is.Empty);
            Assert.That(_store.LastBrokenKey, Is.Null);
        }

        [Test]
        public void SaveThenLoad_KeepsEverything()
        {
            var data = SaveData.CreateNew();
            data.coins = 250;
            data.freePullsUsed = 3;
            data.penguins.Add(new OwnedPenguin(1, 5, 150));
            data.penguins.Add(new OwnedPenguin(2, 6, 90));
            data.penguins.Add(new OwnedPenguin(3, 7, 100));
            data.party.AddRange(new[] { 3, 1, 2 });
            data.clearedStages.Add(1);
            data.nextPenguinId = 4;
            _store.Save(data);

            var loaded = _store.Load();

            Assert.That(loaded.coins, Is.EqualTo(250));
            Assert.That(loaded.freePullsUsed, Is.EqualTo(3));
            Assert.That(loaded.penguins.Count, Is.EqualTo(3));
            Assert.That(loaded.penguins[0].speciesNo, Is.EqualTo(5));
            Assert.That(loaded.penguins[0].Power, Is.EqualTo(150));
            Assert.That(loaded.party, Is.EqualTo(new[] { 3, 1, 2 }));
            Assert.That(loaded.clearedStages, Is.EqualTo(new[] { 1 }));
            Assert.That(loaded.nextPenguinId, Is.EqualTo(4));
        }

        /// <summary>保存のキーは一度配ったら変えない（変えると、アップデートで記録が消えて見える）</summary>
        [Test]
        public void Key_IsFixed()
        {
            Assert.That(SaveStore.Key, Is.EqualTo("PenmonScramble.Save"));
        }

        [TestCase("not json")]
        [TestCase("{\"coins\":10}")]
        public void Load_Unreadable_BacksUpAndStartsNew(string broken)
        {
            _storage.Write(SaveStore.Key, broken);

            var data = _store.Load();

            Assert.That(data.coins, Is.Zero);
            Assert.That(_store.LastBrokenKey, Is.EqualTo(SaveStore.BrokenKeyPrefix + 1));
            Assert.That(_storage.Read(SaveStore.BrokenKeyPrefix + 1), Is.EqualTo(broken));
        }

        [Test]
        public void Load_UnreadableTwice_KeepsBothBackups()
        {
            _storage.Write(SaveStore.Key, "first");
            _store.Load();
            _storage.Write(SaveStore.Key, "second");
            _store.Load();

            Assert.That(_storage.Read(SaveStore.BrokenKeyPrefix + 1), Is.EqualTo("first"));
            Assert.That(_storage.Read(SaveStore.BrokenKeyPrefix + 2), Is.EqualTo("second"));
        }

        /// <summary>v1 の記録は今回だけ読まずに消して初めからにする（ユーザー承認済み）。すぐ上書きし、退避はしない</summary>
        [Test]
        public void Load_Version1_IsDiscardedAndOverwritten()
        {
            _storage.Write(SaveStore.Key, "{\"version\":1,\"coins\":500,\"freePullsUsed\":3,\"penguins\":[{\"id\":1,\"speciesNo\":1,\"power\":80}],\"party\":[1],\"clearedStages\":[1]}");

            var data = _store.Load();

            Assert.That(_store.DiscardedOldRecord, Is.True);
            Assert.That(_store.LastBrokenKey, Is.Null);
            Assert.That(data.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(data.coins, Is.Zero);
            Assert.That(data.penguins, Is.Empty);
            Assert.That(new PlayerRecord(data).FreePullsLeft, Is.EqualTo(Balance.FreePulls));
            Assert.That(_storage.Read(SaveStore.BrokenKeyPrefix + 1), Is.Null);

            // 2回目からは v2 の記録として読む（もう知らせない）
            _store.Load();
            Assert.That(_store.DiscardedOldRecord, Is.False);
        }

        [Test]
        public void SaveThenLoad_KeepsGrowth()
        {
            var data = SaveData.CreateNew();
            data.penguins.Add(new OwnedPenguin(1, 5, 150) { boost = 40, limitBreak = 2 });
            data.clearedRush.Add(1012);
            _store.Save(data);

            var loaded = _store.Load();

            Assert.That(loaded.penguins[0].basePower, Is.EqualTo(150));
            Assert.That(loaded.penguins[0].boost, Is.EqualTo(40));
            Assert.That(loaded.penguins[0].limitBreak, Is.EqualTo(2));
            Assert.That(loaded.penguins[0].Power, Is.EqualTo(190));
            Assert.That(loaded.clearedRush, Is.EqualTo(new[] { 1012 }));
        }

        /// <summary>
        /// v2（ミニゲームの前）の記録はそのまま読めて、ミニゲームの記録だけ空で始まる。
        /// JSON は v2 で JsonUtility が実際に書いていた形のまま（項目の名前と並び）
        /// </summary>
        [Test]
        public void Load_Version2_KeepsEverythingAndAddsEmptyMiniGames()
        {
            const string v2 = "{\"version\":2,\"coins\":1234,\"nextPenguinId\":4,\"freePullsUsed\":3," +
                "\"penguins\":[{\"id\":1,\"speciesNo\":5,\"basePower\":150,\"boost\":20,\"limitBreak\":1},{\"id\":2,\"speciesNo\":6,\"basePower\":90,\"boost\":0,\"limitBreak\":0},{\"id\":3,\"speciesNo\":7,\"basePower\":100,\"boost\":0,\"limitBreak\":0}]," +
                "\"party\":[3,1,2],\"clearedStages\":[1,2,3,4],\"clearedRush\":[1012]}";
            _storage.Write(SaveStore.Key, v2);

            var data = _store.Load();

            Assert.That(_store.LastBrokenKey, Is.Null);
            Assert.That(_store.DiscardedOldRecord, Is.False);
            Assert.That(data.version, Is.EqualTo(3));
            Assert.That(data.coins, Is.EqualTo(1234));
            Assert.That(data.freePullsUsed, Is.EqualTo(3));
            Assert.That(data.penguins.Count, Is.EqualTo(3));
            Assert.That(data.penguins[0].Power, Is.EqualTo(170));
            Assert.That(data.penguins[0].limitBreak, Is.EqualTo(1));
            Assert.That(data.party, Is.EqualTo(new[] { 3, 1, 2 }));
            Assert.That(data.clearedStages, Is.EqualTo(new[] { 1, 2, 3, 4 }));
            Assert.That(data.clearedRush, Is.EqualTo(new[] { 1012 }));
            Assert.That(data.nextPenguinId, Is.EqualTo(4));
            Assert.That(data.miniGames, Is.Not.Null.And.Empty);
        }

        /// <summary>ミニゲームの記録は書いて読み直しても残る。知らないミニゲームの行も消さない</summary>
        [Test]
        public void SaveThenLoad_KeepsMiniGames()
        {
            var data = SaveData.CreateNew();
            data.miniGames.Add(new MiniGameRecord { gameId = "golf", courseNo = 2, plays = 3, best = 11, special = 1 });
            data.miniGames.Add(new MiniGameRecord { gameId = "future", courseNo = 1, plays = 1, best = 500 });
            _store.Save(data);

            var loaded = _store.Load();

            Assert.That(loaded.miniGames.Count, Is.EqualTo(2));
            var golf = loaded.miniGames[0];
            Assert.That(golf.gameId, Is.EqualTo("golf"));
            Assert.That(golf.courseNo, Is.EqualTo(2));
            Assert.That(golf.plays, Is.EqualTo(3));
            Assert.That(golf.best, Is.EqualTo(11));
            Assert.That(golf.special, Is.EqualTo(1));
            Assert.That(loaded.miniGames[1].gameId, Is.EqualTo("future"));
            Assert.That(loaded.miniGames[1].best, Is.EqualTo(500));
        }

        /// <summary>記録の項目名は一度配ったら変えない（JSON の名前がそのまま保存先の中身になる）</summary>
        [Test]
        public void MiniGameRecord_FieldNamesAreFixed()
        {
            var data = SaveData.CreateNew();
            data.miniGames.Add(new MiniGameRecord { gameId = "golf", courseNo = 1, plays = 1, best = 9, special = 0 });

            var json = JsonUtility.ToJson(data);

            StringAssert.Contains("\"miniGames\":[{\"gameId\":\"golf\",\"courseNo\":1,\"plays\":1,\"best\":9,\"special\":0}]", json);
        }

        [Test]
        public void Load_RepairsParty_DropsMissingAndFillsStrongest()
        {
            _storage.Write(SaveStore.Key, "{\"version\":2,\"penguins\":[{\"id\":1,\"speciesNo\":1,\"basePower\":80},{\"id\":2,\"speciesNo\":2,\"basePower\":120},{\"id\":3,\"speciesNo\":3,\"basePower\":100},{\"id\":4,\"speciesNo\":4,\"basePower\":90}],\"party\":[9,1,1]}");

            var data = _store.Load();

            Assert.That(data.party, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(data.nextPenguinId, Is.EqualTo(5));
        }

        /// <summary>新しい版で増えた種類の子は、古い版で読んでも消さない（編成にだけ入れない）</summary>
        [Test]
        public void Load_UnknownSpecies_IsKeptButNotInParty()
        {
            _storage.Write(SaveStore.Key, "{\"version\":2,\"penguins\":[{\"id\":1,\"speciesNo\":99,\"basePower\":80}],\"party\":[1]}");

            var data = _store.Load();

            Assert.That(data.penguins.Count, Is.EqualTo(1));
            Assert.That(data.party, Is.Empty);
        }

        /// <summary>初期化したら、次に読んだときも初めから（無料ガチャが3回に戻る）。前に退避した記録は残す</summary>
        [Test]
        public void Reset_StartsOverAndKeepsBrokenBackups()
        {
            _storage.Write(SaveStore.Key, "{\"version\":2,\"coins\":500,\"freePullsUsed\":3,\"penguins\":[{\"id\":1,\"speciesNo\":1,\"basePower\":80}],\"party\":[1],\"clearedStages\":[1]}");
            _storage.Write(SaveStore.BrokenKeyPrefix + 1, "old");

            var reset = _store.Reset();
            var data = _store.Load();

            Assert.That(reset.version, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(data.coins, Is.Zero);
            Assert.That(data.penguins, Is.Empty);
            Assert.That(data.party, Is.Empty);
            Assert.That(data.clearedStages, Is.Empty);
            Assert.That(new PlayerRecord(data).FreePullsLeft, Is.EqualTo(Balance.FreePulls));
            Assert.That(_storage.Read(SaveStore.BrokenKeyPrefix + 1), Is.EqualTo("old"));
        }
    }
}
