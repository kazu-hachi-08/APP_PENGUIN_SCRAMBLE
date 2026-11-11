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
    /// 球場の定義表。奥のワールドほど CPU が強く（★1〜★5）、盤に仕掛けがある（BaseballFieldTable）。
    /// CPU のチームの種類は、本編のそのワールドの雑魚と同じ混ぜ方（★が高い子ほど魔球・必殺打が強い。サッカーと同じ）
    /// </summary>
    public static class BaseballStadiumTable
    {
        /// <summary>CPU のチームの人数（ひとりの相手は編成と同じ3体）</summary>
        public const int CpuLineup = 3;

        // 本編の StageTable の雑魚の混ぜ方と同じ（Weak → Mixed → Strong → Elite → Ace。サッカーと同じ）
        private static readonly int[] BeachWild = { 1, 2, 3, 6, 7, 11, 12, 13, 16, 17 };
        private static readonly int[] IcebergWild = { 1, 2, 3, 6, 7, 11, 12, 13, 16, 17, 4, 8, 9, 14, 18, 19 };
        private static readonly int[] DeepSeaWild = { 4, 8, 9, 14, 18, 19, 3, 7, 11, 16 };
        private static readonly int[] StormWild = { 4, 8, 9, 14, 18, 19, 5, 10, 15, 20 };
        private static readonly int[] VolcanoWild = { 5, 10, 15, 20, 4, 8, 14, 19 };

        private static readonly BaseballStadiumDef[] All =
        {
            new BaseballStadiumDef(1, "海岸", 1, 1, "仕掛けなし", BeachWild, BaseballFieldTable.Beach),
            new BaseballStadiumDef(2, "氷山", 2, 2, "外野が氷（よく転がり、長打が出やすい）", IcebergWild, BaseballFieldTable.Iceberg),
            new BaseballStadiumDef(3, "深海", 3, 3, "水流の帯（転がる打球を横へ流す）", DeepSeaWild, BaseballFieldTable.DeepSea),
            new BaseballStadiumDef(4, "嵐の崖", 4, 4, "左右に動くフライの穴と、浮いた球を流す風", StormWild, BaseballFieldTable.StormCliff),
            new BaseballStadiumDef(5, "火山の入り江", 5, 5, "間欠泉（打球が跳ねる）と近くて高いフェンス", VolcanoWild, BaseballFieldTable.VolcanoCove),
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
