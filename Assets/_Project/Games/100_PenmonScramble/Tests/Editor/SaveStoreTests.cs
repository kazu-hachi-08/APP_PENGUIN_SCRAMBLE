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
            Assert.That(loaded.penguins[0].power, Is.EqualTo(150));
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

        [Test]
        public void Load_RepairsParty_DropsMissingAndFillsStrongest()
        {
            _storage.Write(SaveStore.Key, "{\"version\":1,\"penguins\":[{\"id\":1,\"speciesNo\":1,\"power\":80},{\"id\":2,\"speciesNo\":2,\"power\":120},{\"id\":3,\"speciesNo\":3,\"power\":100},{\"id\":4,\"speciesNo\":4,\"power\":90}],\"party\":[9,1,1]}");

            var data = _store.Load();

            Assert.That(data.party, Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(data.nextPenguinId, Is.EqualTo(5));
        }

        /// <summary>新しい版で増えた種類の子は、古い版で読んでも消さない（編成にだけ入れない）</summary>
        [Test]
        public void Load_UnknownSpecies_IsKeptButNotInParty()
        {
            _storage.Write(SaveStore.Key, "{\"version\":1,\"penguins\":[{\"id\":1,\"speciesNo\":99,\"power\":80}],\"party\":[1]}");

            var data = _store.Load();

            Assert.That(data.penguins.Count, Is.EqualTo(1));
            Assert.That(data.party, Is.Empty);
        }
    }
}
