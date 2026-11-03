using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.MiniGames.Core
{
    /// <summary>1回遊んだ結果を記録したときの、変わったこと（結果の画面で「初めて」「ベスト更新」を出す）</summary>
    public readonly struct MiniGamePlayResult
    {
        public MiniGamePlayResult(bool firstPlay, bool newBest)
        {
            FirstPlay = firstPlay;
            NewBest = newBest;
        }

        public bool FirstPlay { get; }
        public bool NewBest { get; }
    }

    /// <summary>
    /// ミニゲームの記録（SaveData.miniGames）の読み書き。どのミニゲームも同じ表に、Id とコースの番号で行を持つ。
    /// 知らない Id の行（新しい版で増えたミニゲーム）には触らない。保存はしない（呼んだ側が結果の後に保存する）
    /// </summary>
    public sealed class MiniGameRecords
    {
        private readonly SaveData _data;

        public MiniGameRecords(SaveData data)
        {
            _data = data;
        }

        /// <summary>そのコースの行。まだ遊んでいなければ null</summary>
        public MiniGameRecord Find(string gameId, int courseNo)
        {
            return Rows.Find(r => r.gameId == gameId && r.courseNo == courseNo);
        }

        public int Plays(string gameId, int courseNo) => Find(gameId, courseNo)?.plays ?? 0;

        /// <summary>ベスト。まだ回っていなければ false</summary>
        public bool TryGetBest(string gameId, int courseNo, out int best)
        {
            var row = Find(gameId, courseNo);
            best = row?.best ?? 0;
            return row != null && row.plays > 0;
        }

        /// <summary>1回遊んだ結果を書く: 回った回数を足し、ベストなら書き換え、special（ホールインワンなど）を足す</summary>
        public MiniGamePlayResult RecordPlay(MiniGameDef game, int courseNo, int score, int special)
        {
            var row = Find(game.Id, courseNo);
            if (row == null)
            {
                row = new MiniGameRecord { gameId = game.Id, courseNo = courseNo };
                Rows.Add(row);
            }

            var firstPlay = row.plays == 0;
            var newBest = firstPlay || game.IsBetter(score, row.best);
            row.plays++;
            if (newBest)
            {
                row.best = score;
            }

            row.special += System.Math.Max(0, special);
            return new MiniGamePlayResult(firstPlay, newBest);
        }

        // 古い記録は SaveStore.Upgrade で空の表になるが、手で作った SaveData でも落ちないように
        private List<MiniGameRecord> Rows => _data.miniGames ??= new List<MiniGameRecord>();
    }
}
