using System.Collections.Generic;

namespace PenmonScramble.MiniGames.Core
{
    /// <summary>
    /// ミニゲームの定義表。1つのミニゲームにつき1行。ここに1行足せば、ミニゲーム画面にカードが並ぶ
    /// （足すときの手順は Docs/MINIGAME_GOLF_PLAN.md の「新しいミニゲームを足すときの手順」）
    /// </summary>
    public static class MiniGameTable
    {
        /// <summary>記録に残るので一度配ったら変えない</summary>
        public const string GolfId = "golf";

        /// <summary>記録に残るので一度配ったら変えない</summary>
        public const string SoccerId = "soccer";

        /// <summary>記録に残るので一度配ったら変えない</summary>
        public const string BaseballId = "baseball";

        public static readonly IReadOnlyList<MiniGameDef> All = new[]
        {
            new MiniGameDef(
                GolfId,
                "ペンモンゴルフ",
                "編成の3体で打って、少ない打数でカップへ",
                "GolfScene",
                maxPlayers: 4,
                lowerIsBetter: true,
                scoreUnit: "打",
                courses: new[]
                {
                    // 景色はストーリーの5ワールド。そのワールドに入れたら開く
                    new MiniGameCourseDef(1, "海岸", 1),
                    new MiniGameCourseDef(2, "氷山", 2),
                    new MiniGameCourseDef(3, "深海", 3),
                    new MiniGameCourseDef(4, "嵐の崖", 4),
                    new MiniGameCourseDef(5, "火山の入り江", 5),
                }),
            new MiniGameDef(
                SoccerId,
                "ペンモンサッカー",
                "編成の3体でチームを組んで、3分間の試合",
                "SoccerScene",
                maxPlayers: 4,
                // ベストは1試合の得失点差の最高（大きいほど良い。見せ方は SoccerCourseGuide で「+3」「±0」「−1」）
                lowerIsBetter: false,
                scoreUnit: "点差",
                courses: new[]
                {
                    // スタジアムの景色もストーリーの5ワールド。番号は一度配ったら変えない
                    new MiniGameCourseDef(1, "海岸", 1),
                    new MiniGameCourseDef(2, "氷山", 2),
                    new MiniGameCourseDef(3, "深海", 3),
                    new MiniGameCourseDef(4, "嵐の崖", 4),
                    new MiniGameCourseDef(5, "火山の入り江", 5),
                },
                courseWord: "スタジアム"),
            new MiniGameDef(
                BaseballId,
                "ペンモンベースボール",
                "魔球を曲げて、タイミングで打ち返す3イニングの試合",
                "BaseballScene",
                maxPlayers: 4,
                // ベストは1試合の得失点差の最高（大きいほど良い。見せ方は BaseballCourseGuide で「+3」「±0」「−1」）
                lowerIsBetter: false,
                scoreUnit: "点差",
                courses: new[]
                {
                    // 球場の景色もストーリーの5ワールド。番号は一度配ったら変えない
                    new MiniGameCourseDef(1, "海岸", 1),
                    new MiniGameCourseDef(2, "氷山", 2),
                    new MiniGameCourseDef(3, "深海", 3),
                    new MiniGameCourseDef(4, "嵐の崖", 4),
                    new MiniGameCourseDef(5, "火山の入り江", 5),
                },
                courseWord: "球場"),
        };

        /// <summary>Id のミニゲーム。無ければ null（新しい版で増えたミニゲームの記録など）</summary>
        public static MiniGameDef Get(string id)
        {
            foreach (var game in All)
            {
                if (game.Id == id)
                {
                    return game;
                }
            }

            return null;
        }
    }
}
