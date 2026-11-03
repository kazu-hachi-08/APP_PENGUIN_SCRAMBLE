using PenmonScramble.Core;

namespace PenmonScramble.MiniGames.Core
{
    /// <summary>
    /// ミニゲームの報酬を記録に書く（コインを足して、記録の表に1回分を書く）。
    /// 報酬の計算は各ミニゲームの Core が持ち、ここは書くだけ。保存はしない（呼んだ側が直後に保存する）
    /// </summary>
    public static class MiniGameReward
    {
        /// <param name="score">ベストと比べる点（ゴルフは打数）</param>
        /// <param name="special">ミニゲームごとの数（ゴルフはホールインワンの数）</param>
        /// <param name="coins">もらえるコイン（各ミニゲームの Core で計算した合計）</param>
        public static MiniGamePlayResult Apply(SaveData data, MiniGameDef game, int courseNo, int score, int special, int coins)
        {
            data.coins += System.Math.Max(0, coins);
            return new MiniGameRecords(data).RecordPlay(game, courseNo, score, special);
        }
    }
}
