using System;

namespace PenmonScramble.Core
{
    /// <summary>
    /// ミニゲームの1コースぶんの記録（SaveData.miniGames の1行。v3〜）。
    /// どのミニゲームも同じ形で持つ（次のミニゲームを足すときに SaveData の項目を増やさずに済むように）。
    /// JSON にそのまま書くので、項目名と型は一度配ったら変えない（CLAUDE.md §3.5）。
    /// 本編の Core はミニゲームの中身を知らないので、ここは入れ物だけ。読み書きは MiniGames.Core の MiniGameRecords
    /// </summary>
    [Serializable]
    public sealed class MiniGameRecord
    {
        /// <summary>ミニゲームの Id（"golf" など。MiniGameTable）。知らない Id の行も消さずに残す</summary>
        public string gameId;

        /// <summary>コース（遊ぶ単位）の番号。一度配ったら変えない</summary>
        public int courseNo;

        /// <summary>回った回数（初回ボーナスの判定に使う）</summary>
        public int plays;

        /// <summary>ベスト。小さいほど良いか大きいほど良いかはミニゲームの定義表で決める。まだ回っていない（plays が 0）なら 0</summary>
        public int best;

        /// <summary>ミニゲームごとの数（ゴルフはホールインワンの数）</summary>
        public int special;
    }
}
