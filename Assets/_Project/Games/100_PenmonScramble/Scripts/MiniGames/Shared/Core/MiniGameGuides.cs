using System.Collections.Generic;

namespace PenmonScramble.MiniGames.Core
{
    /// <summary>
    /// ミニゲーム画面のコース選びに出す、そのミニゲームならではの中身（ゴルフならホールとパー・報酬）。
    /// 共通側はミニゲームの中身を知らないので、各ミニゲームがこれを作って MiniGameGuides に登録する
    /// </summary>
    public interface IMiniGameCourseGuide
    {
        /// <summary>もう遊べるコースか（作っている途中のコースは false。コース選びで「準備中」になる）</summary>
        bool IsReady(int courseNo);

        /// <summary>コースの中身（1行目「3ホール  パー 2・3・4（合計 9）」・2行目に仕掛けの一言。MiniGameGuides.TwoLines で作る）</summary>
        string Layout(int courseNo);

        /// <summary>ベストの見せ方（「8打（−1）」など）</summary>
        string FormatBest(int courseNo, int best);

        /// <summary>報酬の一言。firstPlay なら初回ボーナスも書く</summary>
        string Reward(int courseNo, bool firstPlay);
    }

    /// <summary>
    /// ミニゲームの Id ごとのコース選びの中身。各ミニゲームの Game 側が起動時に登録する
    /// （共通側 → 各ミニゲームの向きに参照しないため）。登録が無ければ、コース選びは名前とベストだけになる
    /// </summary>
    public static class MiniGameGuides
    {
        private static readonly Dictionary<string, IMiniGameCourseGuide> Guides = new Dictionary<string, IMiniGameCourseGuide>();

        public static void Register(string gameId, IMiniGameCourseGuide guide) => Guides[gameId] = guide;

        /// <summary>登録が無ければ null</summary>
        public static IMiniGameCourseGuide Get(string gameId) => Guides.TryGetValue(gameId, out var guide) ? guide : null;

        /// <summary>
        /// コースの中身の2行（1行目＝強さ・ホールとパー、2行目＝仕掛けの一言）。
        /// 1行に並べると枠の幅を越えて折り返し、上下の行に重なったので、はじめから2行に分けて2行ぶんの枠に入れる
        /// </summary>
        public static string TwoLines(string head, string gimmick) => string.IsNullOrEmpty(gimmick) ? head : $"{head}\n{gimmick}";
    }
}
