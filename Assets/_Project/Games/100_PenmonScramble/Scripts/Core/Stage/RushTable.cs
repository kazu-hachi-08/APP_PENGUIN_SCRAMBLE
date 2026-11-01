using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>
    /// ボスラッシュ（「みんなで遊ぶ」専用）の定義表。5ステージで、どれも闘技場の3連戦:
    /// 1戦目＝巨大ペンモン2体組 → 2戦目＝大ボス → 3戦目＝そのステージの看板の大ボスの「本気」。
    /// 番号（101〜）は記録（clearedRush）と通信に使うので、並べ替えたり途中に足したりしない
    /// </summary>
    public static class RushTable
    {
        public const int FirstNo = 101;

        /// <summary>1ステージの戦いの数</summary>
        public const int RoundsPerStage = 3;

        // 取り巻きの雑魚の種類。強さはワールドの倍率で上がるので、種類は見た目の手ごわさを先のステージほど上げる
        private static readonly int[] Shore = { 1, 2, 3, 6, 7, 11, 12, 13, 16, 17 };
        private static readonly int[] Ice = { 1, 3, 4, 6, 8, 9, 11, 14, 16, 19 };
        private static readonly int[] Deep = { 4, 8, 9, 14, 18, 19, 3, 7, 11, 16 };
        private static readonly int[] Storm = { 4, 8, 9, 14, 18, 19, 5, 10, 15, 20 };
        private static readonly int[] Volcano = { 5, 10, 15, 20, 4, 8, 14, 19 };

        // 取り巻きは、大技をよける邪魔をしすぎないよう、ボスと一緒に2体・後から少しだけ
        private const int EscortsWithBoss = 2;
        private const int EscortsAfter = 2;

        /// <summary>海鳥の闘技場（ワールド1の色）: 氷の巨大ペンモン → ヒョウアザラシ → 本気のオオトウゾクカモメ</summary>
        private static readonly StageDef Rush1 = Rush(101, "海鳥の闘技場", 1, Shore, 4, 5, BigBossTable.LeopardSealNo, BigBossTable.SkuaNo);

        /// <summary>氷の闘技場（ワールド2の色）: 水の巨大ペンモン → オオトウゾクカモメ → 本気のヒョウアザラシ</summary>
        private static readonly StageDef Rush2 = Rush(102, "氷の闘技場", 2, Ice, 8, 10, BigBossTable.SkuaNo, BigBossTable.LeopardSealNo);

        /// <summary>深海の闘技場（ワールド3の色）: 風の巨大ペンモン → オオフルマカモメ → 本気のシャチ</summary>
        private static readonly StageDef Rush3 = Rush(103, "深海の闘技場", 3, Deep, 14, 15, BigBossTable.FulmarNo, BigBossTable.OrcaNo);

        /// <summary>嵐の闘技場（ワールド4の色）: 雷の巨大ペンモン → シャチ → 本気のオオフルマカモメ</summary>
        private static readonly StageDef Rush4 = Rush(104, "嵐の闘技場", 4, Storm, 19, 20, BigBossTable.OrcaNo, BigBossTable.FulmarNo);

        /// <summary>火山の闘技場（ワールド5の色）: ★3 の巨大ペンモン（水の波と雷の帯）→ オオフルマカモメ → 本気のサメ</summary>
        private static readonly StageDef Rush5 = Rush(105, "火山の闘技場", 5, Volcano, 10, 20, BigBossTable.FulmarNo, BigBossTable.SharkNo);

        /// <summary>番号の順に並べる（No は添字 + FirstNo）</summary>
        private static readonly StageDef[] All = { Rush1, Rush2, Rush3, Rush4, Rush5 };

        public static int Count => All.Length;

        public static int LastNo => FirstNo + All.Length - 1;

        /// <summary>番号（101〜）から引く。範囲外は null（通信で壊れた値が届いたときに落ちないように）</summary>
        public static StageDef Get(int no)
        {
            return no >= FirstNo && no <= LastNo ? All[no - FirstNo] : null;
        }

        public static bool IsRush(int no) => Get(no) != null;

        /// <summary>何戦目（1〜）に出てくるボス。ロビーのカードに3戦の顔を並べるのに使う</summary>
        public static IReadOnlyList<BossSpawn> BossesOf(StageDef stage, int round)
        {
            var index = round - 1;
            if (stage == null || index < 0 || index >= stage.Areas.Count || stage.Areas[index].Waves.Count == 0)
            {
                return new BossSpawn[0];
            }

            return stage.Areas[index].Waves[0].Bosses;
        }

        private static StageDef Rush(int no, string name, int world, int[] escorts, int giantA, int giantB, int bigBoss, int seriousBoss)
        {
            return new StageDef(no, name, world,
                Round(escorts, new BossSpawn(giantA, BossRank.Giant), new BossSpawn(giantB, BossRank.Giant)),
                Round(escorts, new BossSpawn(bigBoss, BossRank.Normal)),
                Round(escorts, new BossSpawn(seriousBoss, BossRank.Serious)));
        }

        private static AreaDef Round(int[] escorts, params BossSpawn[] bosses)
        {
            return AreaDef.Arena(new WaveDef(EscortsWithBoss, escorts, bosses), new WaveDef(EscortsAfter, escorts));
        }
    }
}
