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

        /// <summary>ピッチ（試合ごとに作る。仕掛けの地面はフェーズ 6 で足す）</summary>
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
    /// スタジアムの定義表（Docs/MINIGAME_SOCCER_PLAN.md フェーズ 6 の表）。今はスタジアム1（海岸。仕掛けなし・CPU ★1）だけ。
    /// 2〜5（仕掛けと強い CPU）はフェーズ 6 で足す
    /// </summary>
    public static class SoccerStadiumTable
    {
        // 海岸の雑魚（本編のワールド1の ★1 の子）
        private static readonly int[] BeachWild = { 1, 2, 3, 6, 7, 11, 12, 13, 16, 17 };

        private static readonly SoccerStadiumDef[] All =
        {
            new SoccerStadiumDef(1, "海岸", 1, 1, "仕掛けなし", BeachWild, SoccerPitchTable.Standard),
        };

        public static IReadOnlyList<SoccerStadiumDef> Stadiums => All;

        /// <summary>番号のスタジアム。まだ作っていなければ null</summary>
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
