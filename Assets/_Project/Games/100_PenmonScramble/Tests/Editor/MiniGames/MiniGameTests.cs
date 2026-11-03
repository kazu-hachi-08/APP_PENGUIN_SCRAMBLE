using System.Collections.Generic;
using NUnit.Framework;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Tests
{
    public class MiniGameTests
    {
        private static MiniGameDef Golf => MiniGameTable.Get(MiniGameTable.GolfId);

        /// <summary>Id とコースの番号は記録に残るので、一度配ったら変えない</summary>
        [Test]
        public void Table_GolfIdAndCoursesAreFixed()
        {
            Assert.That(MiniGameTable.GolfId, Is.EqualTo("golf"));
            Assert.That(Golf, Is.Not.Null);
            Assert.That(Golf.Courses.Count, Is.EqualTo(5));
            for (var i = 0; i < Golf.Courses.Count; i++)
            {
                Assert.That(Golf.Courses[i].No, Is.EqualTo(i + 1));
                Assert.That(Golf.Courses[i].World, Is.EqualTo(i + 1));
            }
        }

        [Test]
        public void Table_IdsAndCourseNumbersAreUnique()
        {
            var ids = new HashSet<string>();
            foreach (var game in MiniGameTable.All)
            {
                Assert.That(ids.Add(game.Id), Is.True, game.Id);
                Assert.That(game.SceneName, Is.Not.Empty);
                var numbers = new HashSet<int>();
                foreach (var course in game.Courses)
                {
                    Assert.That(numbers.Add(course.No), Is.True, $"{game.Id} {course.No}");
                }
            }
        }

        [Test]
        public void Table_UnknownIdIsNull()
        {
            Assert.That(MiniGameTable.Get("unknown"), Is.Null);
        }

        [Test]
        public void Unlock_FirstCourseIsOpenFromTheStart()
        {
            var record = new PlayerRecord(SaveData.CreateNew());

            Assert.That(MiniGameUnlock.IsUnlocked(Golf.Course(1), record), Is.True);
            Assert.That(MiniGameUnlock.IsUnlocked(Golf.Course(2), record), Is.False);
        }

        /// <summary>前のワールドの最後のステージ（大ボス）をクリアして次のワールドに入れたら、その景色のコースが開く</summary>
        [Test]
        public void Unlock_OpensWhenEnteringTheWorld()
        {
            var data = SaveData.CreateNew();
            data.clearedStages.AddRange(new[] { 1, 2 });
            var record = new PlayerRecord(data);
            Assert.That(MiniGameUnlock.IsUnlocked(Golf.Course(2), record), Is.False);

            data.clearedStages.Add(StageTable.StagesPerWorld);

            Assert.That(MiniGameUnlock.IsUnlocked(Golf.Course(2), record), Is.True);
            Assert.That(MiniGameUnlock.IsUnlocked(Golf.Course(3), record), Is.False);
        }

        /// <summary>オンラインはホストの記録（入れるワールドの数）で決める</summary>
        [TestCase(1, 1)]
        [TestCase(3, 3)]
        [TestCase(5, 5)]
        public void Unlock_ByWorldsUnlocked(int worlds, int openCourses)
        {
            var open = 0;
            foreach (var course in Golf.Courses)
            {
                open += MiniGameUnlock.IsUnlocked(course, worlds) ? 1 : 0;
            }

            Assert.That(open, Is.EqualTo(openCourses));
        }

        [Test]
        public void Records_NoPlayYet_HasNoBest()
        {
            var records = new MiniGameRecords(SaveData.CreateNew());

            Assert.That(records.TryGetBest(Golf.Id, 1, out _), Is.False);
            Assert.That(records.Plays(Golf.Id, 1), Is.Zero);
        }

        /// <summary>ゴルフは打数が小さいほど良い。悪い記録ではベストを書き換えない</summary>
        [Test]
        public void Records_KeepsLowestForGolf()
        {
            var data = SaveData.CreateNew();
            var records = new MiniGameRecords(data);

            var first = records.RecordPlay(Golf, 1, 12, special: 0);
            var worse = records.RecordPlay(Golf, 1, 14, special: 1);
            var better = records.RecordPlay(Golf, 1, 9, special: 0);

            Assert.That(first.FirstPlay, Is.True);
            Assert.That(first.NewBest, Is.True);
            Assert.That(worse.FirstPlay, Is.False);
            Assert.That(worse.NewBest, Is.False);
            Assert.That(better.NewBest, Is.True);
            Assert.That(records.TryGetBest(Golf.Id, 1, out var best), Is.True);
            Assert.That(best, Is.EqualTo(9));
            Assert.That(records.Plays(Golf.Id, 1), Is.EqualTo(3));
            Assert.That(records.Find(Golf.Id, 1).special, Is.EqualTo(1));
            Assert.That(data.miniGames.Count, Is.EqualTo(1));
        }

        /// <summary>点数制のミニゲーム（大きいほど良い）でも同じ表で済む</summary>
        [Test]
        public void Records_KeepsHighestWhenHigherIsBetter()
        {
            var game = new MiniGameDef("points", "点数", string.Empty, "PointsScene", 4, lowerIsBetter: false, "点", new[] { new MiniGameCourseDef(1, "1", 1) });
            var records = new MiniGameRecords(SaveData.CreateNew());

            records.RecordPlay(game, 1, 50, 0);
            records.RecordPlay(game, 1, 30, 0);

            records.TryGetBest(game.Id, 1, out var best);
            Assert.That(best, Is.EqualTo(50));
        }

        /// <summary>コースごと・ミニゲームごとに別の行。知らない Id の行（新しい版で増えたミニゲーム）には触らない</summary>
        [Test]
        public void Records_SeparateRowsAndKeepUnknownGames()
        {
            var data = SaveData.CreateNew();
            var unknown = new MiniGameRecord { gameId = "future", courseNo = 1, plays = 2, best = 7 };
            data.miniGames.Add(unknown);
            var records = new MiniGameRecords(data);

            records.RecordPlay(Golf, 1, 10, 0);
            records.RecordPlay(Golf, 2, 11, 0);

            Assert.That(data.miniGames.Count, Is.EqualTo(3));
            Assert.That(data.miniGames[0], Is.SameAs(unknown));
            Assert.That(unknown.plays, Is.EqualTo(2));
            Assert.That(unknown.best, Is.EqualTo(7));
        }
    }
}
