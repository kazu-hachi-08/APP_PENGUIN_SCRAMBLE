using System.Collections.Generic;

namespace PenmonScramble.MiniGames.Core
{
    /// <summary>
    /// ミニゲーム1つの定義（MiniGameTable の1行）。ミニゲーム画面のカード・コース選び・記録はここから作る
    /// </summary>
    public sealed class MiniGameDef
    {
        public MiniGameDef(string id, string name, string description, string sceneName, int maxPlayers, bool lowerIsBetter, string scoreUnit, IReadOnlyList<MiniGameCourseDef> courses)
        {
            Id = id;
            Name = name;
            Description = description;
            SceneName = sceneName;
            MaxPlayers = maxPlayers;
            LowerIsBetter = lowerIsBetter;
            ScoreUnit = scoreUnit;
            Courses = courses;
        }

        /// <summary>記録（MiniGameRecord.gameId）に残るので、一度配ったら変えない</summary>
        public string Id { get; }

        public string Name { get; }

        /// <summary>カードに出す一言の説明</summary>
        public string Description { get; }

        /// <summary>ミニゲームごとに別のシーン（本編のシーンを重くしないように）。ビルドの一覧に入れておく</summary>
        public string SceneName { get; }

        /// <summary>みんなで遊ぶときの最大の人数（ひとりでも遊べる）</summary>
        public int MaxPlayers { get; }

        /// <summary>ベストは小さいほど良いか（ゴルフの打数は小さいほど良い。点数制なら false）</summary>
        public bool LowerIsBetter { get; }

        /// <summary>ベストの単位（「打」など）</summary>
        public string ScoreUnit { get; }

        /// <summary>遊ぶ単位（コース）の一覧。並びが番号の順</summary>
        public IReadOnlyList<MiniGameCourseDef> Courses { get; }

        /// <summary>番号のコース。無ければ null</summary>
        public MiniGameCourseDef Course(int courseNo)
        {
            foreach (var course in Courses)
            {
                if (course.No == courseNo)
                {
                    return course;
                }
            }

            return null;
        }

        /// <summary>a が b より良い記録か</summary>
        public bool IsBetter(int a, int b) => LowerIsBetter ? a < b : a > b;
    }

    /// <summary>コース（遊ぶ単位）1つ。景色はストーリーのワールドと同じで、そのワールドに入れたら開く</summary>
    public sealed class MiniGameCourseDef
    {
        public MiniGameCourseDef(int no, string name, int world)
        {
            No = no;
            Name = name;
            World = world;
        }

        /// <summary>記録（MiniGameRecord.courseNo）に残るので、一度配ったら変えない</summary>
        public int No { get; }

        public string Name { get; }

        /// <summary>景色のもとになったストーリーのワールド。このワールドに入れたら開く（MiniGameUnlock）</summary>
        public int World { get; }
    }
}
