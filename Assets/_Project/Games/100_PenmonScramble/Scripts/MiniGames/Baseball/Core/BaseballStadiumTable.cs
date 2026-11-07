using System;
using System.Collections.Generic;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>球場1つ（MiniGameTable の野球のコースの1つ。番号と名前・景色はそちらと同じ）</summary>
    public sealed class BaseballStadiumDef
    {
        private readonly Func<BaseballFieldDef> _field;

        public BaseballStadiumDef(int no, string name, int world, int cpuStars, string gimmick, IReadOnlyList<int> wildSpecies, Func<BaseballFieldDef> field)
        {
            No = no;
            Name = name;
            World = world;
            CpuStars = cpuStars;
            Gimmick = gimmick;
            WildSpecies = wildSpecies;
            _field = field;
        }

        /// <summary>記録（MiniGameRecord.courseNo）に残るので、一度配ったら変えない（MiniGameTable のコースの番号と同じ）</summary>
        public int No { get; }

        public string Name { get; }

        /// <summary>景色（まわりの地面の絵・フェンスの色）のもとのワールド</summary>
        public int World { get; }

        /// <summary>CPU のチームの強さ（★1〜★5。BaseballBalance.AiSkill）</summary>
        public int CpuStars { get; }

        /// <summary>盤と仕掛けの一言（球場選びに出す）</summary>
        public string Gimmick { get; }

        /// <summary>CPU のチームに出る野生のペンモンの種類（この中から試合ごとに3体を選ぶ）</summary>
        public IReadOnlyList<int> WildSpecies { get; }

        /// <summary>盤（試合ごとに作る）</summary>
        public BaseballFieldDef CreateField() => _field();

        /// <summary>CPU のチームの3体を、種から選ぶ（足りる間は同じ種類が重ならないように）</summary>
        public int[] PickWild(int count, int seed)
        {
            var random = new Random(seed);
            var pool = new List<int>(WildSpecies);
            var picked = new int[count];
            for (var i = 0; i < count; i++)
            {
                if (pool.Count == 0)
                {
                    pool.AddRange(WildSpecies);
                }

                var index = random.Next(pool.Count);
                picked[i] = pool[index];
                pool.RemoveAt(index);
            }

            return picked;
        }
    }

    /// <summary>
    /// 球場の定義表（Docs/MINIGAME_BASEBALL_PLAN.md フェーズ 6 の表）。いまは球場1「海岸」だけ（残りはフェーズ 6）。
    /// CPU のチームの種類は、本編のそのワールドの雑魚と同じ混ぜ方（★が高い子ほど魔球・必殺打が強い。サッカーと同じ）
    /// </summary>
    public static class BaseballStadiumTable
    {
        /// <summary>CPU のチームの人数（ひとりの相手は編成と同じ3体）</summary>
        public const int CpuLineup = 3;

        // 本編の StageTable のワールド 1 の雑魚の混ぜ方と同じ
        private static readonly int[] BeachWild = { 1, 2, 3, 6, 7, 11, 12, 13, 16, 17 };

        private static readonly BaseballStadiumDef[] All =
        {
            new BaseballStadiumDef(1, "海岸", 1, 1, "ふつうの盤", BeachWild, BaseballFieldTable.Beach),
        };

        public static IReadOnlyList<BaseballStadiumDef> Stadiums => All;

        /// <summary>番号の球場。無ければ null（まだ作っていない球場）</summary>
        public static BaseballStadiumDef Get(int no)
        {
            foreach (var stadium in All)
            {
                if (stadium.No == no)
                {
                    return stadium;
                }
            }

            return null;
        }
    }
}
