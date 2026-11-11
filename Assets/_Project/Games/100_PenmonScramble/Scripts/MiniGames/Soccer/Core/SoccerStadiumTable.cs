using System;
using System.Collections.Generic;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>スタジアム1つ（MiniGameTable のサッカーのコースの1つ。番号と名前・景色はそちらと同じ）</summary>
    public sealed class SoccerStadiumDef
    {
        public SoccerStadiumDef(int no, string name, int world, int cpuStars, string gimmick, IReadOnlyList<int> wildSpecies, Func<SoccerPitchDef> pitch)
        {
            No = no;
            Name = name;
            World = world;
            CpuStars = cpuStars;
            Gimmick = gimmick;
            WildSpecies = wildSpecies;
            _pitch = pitch;
        }

        private readonly Func<SoccerPitchDef> _pitch;

        /// <summary>記録（MiniGameRecord.courseNo）に残るので、一度配ったら変えない（MiniGameTable のコースの番号と同じ）</summary>
        public int No { get; }

        public string Name { get; }

        /// <summary>景色（柵の色・外の地面の絵）のもとのワールド</summary>
        public int World { get; }

        /// <summary>CPU のチームの強さ（★1〜★5。SoccerBalance.AiLevel）</summary>
        public int CpuStars { get; }

        /// <summary>ピッチの仕掛けの一言（コース選びに出す）</summary>
        public string Gimmick { get; }

        /// <summary>CPU のチームに出る野生のペンモンの種類（この中から試合ごとに3体を選ぶ）</summary>
        public IReadOnlyList<int> WildSpecies { get; }

        /// <summary>ピッチ（試合ごとに作る。仕掛けもここに入る）</summary>
        public SoccerPitchDef CreatePitch() => _pitch();

        /// <summary>CPU のチームの3体を、種から選ぶ（同じ種類が続けて出ないよう、足りる間は重ならないように）</summary>
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
    /// スタジアムの定義表。奥のワールドほど CPU が強く（★1〜★5）、
    /// ピッチに仕掛けがある（SoccerPitchTable）。CPU のチームの種類は、本編のそのワールドの雑魚と同じ混ぜ方
    /// （★が高い子ほど必殺シュートが強い）
    /// </summary>
    public static class SoccerStadiumTable
    {
        // 本編の StageTable の雑魚の混ぜ方と同じ（Weak → Mixed → Strong → Elite → Ace）
        private static readonly int[] BeachWild = { 1, 2, 3, 6, 7, 11, 12, 13, 16, 17 };
        private static readonly int[] IcebergWild = { 1, 2, 3, 6, 7, 11, 12, 13, 16, 17, 4, 8, 9, 14, 18, 19 };
        private static readonly int[] DeepSeaWild = { 4, 8, 9, 14, 18, 19, 3, 7, 11, 16 };
        private static readonly int[] StormWild = { 4, 8, 9, 14, 18, 19, 5, 10, 15, 20 };
        private static readonly int[] VolcanoWild = { 5, 10, 15, 20, 4, 8, 14, 19 };

        private static readonly SoccerStadiumDef[] All =
        {
            new SoccerStadiumDef(1, "海岸", 1, 1, "仕掛けなし", BeachWild, SoccerPitchTable.Beach),
            new SoccerStadiumDef(2, "氷山", 2, 2, "氷の床（ボールも子もよく滑る）", IcebergWild, SoccerPitchTable.Iceberg),
            new SoccerStadiumDef(3, "深海", 3, 3, "水流の帯（転がるボールを押し流す）", DeepSeaWild, SoccerPitchTable.DeepSea),
            new SoccerStadiumDef(4, "嵐の崖", 4, 4, "風の帯と、強く跳ね返すバンパー", StormWild, SoccerPitchTable.StormCliff),
            new SoccerStadiumDef(5, "火山の入り江", 5, 5, "間欠泉（ボールを上へ跳ね上げる）", VolcanoWild, SoccerPitchTable.VolcanoCove),
        };

        public static IReadOnlyList<SoccerStadiumDef> Stadiums => All;

        /// <summary>番号のスタジアム。無ければ null</summary>
        public static SoccerStadiumDef Get(int no)
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
