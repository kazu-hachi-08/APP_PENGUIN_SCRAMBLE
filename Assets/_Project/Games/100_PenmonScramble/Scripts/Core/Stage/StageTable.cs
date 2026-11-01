namespace PenmonScramble.Core
{
    /// <summary>
    /// ステージの定義表。5ワールド×3ステージ。エリアの数・各エリアの敵の出方を変えるときはここを直す。
    /// 番号（No）は記録と通信に使うので、並べ替えたり途中に足したりしない
    /// </summary>
    public static class StageTable
    {
        /// <summary>1ワールドのステージの数。ワールドの3ステージ目は大ボス</summary>
        public const int StagesPerWorld = 3;

        // 雑魚の種類。先のワールドほど ★2・★3 を混ぜて、見た目にも手ごわくする（強さ自体はワールドの倍率で上がる）
        private static readonly int[] Weak = { 1, 2, 3, 6, 7, 11, 12, 13, 16, 17 };
        private static readonly int[] Mixed = { 1, 2, 3, 6, 7, 11, 12, 13, 16, 17, 4, 8, 9, 14, 18, 19 };
        private static readonly int[] Strong = { 4, 8, 9, 14, 18, 19, 3, 7, 11, 16 };
        private static readonly int[] Elite = { 4, 8, 9, 14, 18, 19, 5, 10, 15, 20 };
        // ワールド4・5は ★3 を半分まで混ぜ、最後は ★3 だけの波も出す
        private static readonly int[] Ace = { 5, 10, 15, 20, 4, 8, 14, 19 };
        private static readonly int[] Legend = { 5, 10, 15, 20 };

        // ---- ワールド1 海岸 ----

        /// <summary>5エリア＋ボスエリア（ボスはキングス）</summary>
        public static readonly StageDef Stage1 = new StageDef(1, "海岸 1",
            Area(Wave(4, Weak), Wave(5, Weak)),
            Area(Wave(5, Weak), Wave(5, Weak)),
            Area(Wave(5, Weak), Wave(6, Weak), Wave(4, Weak)),
            Area(Wave(6, Mixed), Wave(6, Mixed)),
            Area(Wave(6, Mixed), Wave(6, Mixed), Wave(6, Mixed)),
            Area(Boss(3, Weak, 4), Wave(4, Weak), Wave(4, Weak)));

        private static readonly StageDef Stage2 = new StageDef(2, "海岸 2",
            Area(Wave(5, Weak), Wave(5, Weak)),
            Area(Wave(5, Weak), Wave(6, Mixed)),
            Area(Wave(6, Mixed), Wave(6, Mixed), Wave(4, Mixed)),
            Area(Wave(6, Mixed), Wave(6, Mixed)),
            Area(Wave(6, Mixed), Wave(7, Mixed), Wave(6, Mixed)),
            Area(Boss(3, Mixed, 8), Wave(4, Mixed), Wave(5, Mixed)));

        /// <summary>最後に大ボス（オオトウゾクカモメ）。大ボスの攻撃をよけやすいよう、ボスエリアの雑魚は少なめ</summary>
        private static readonly StageDef Stage3 = new StageDef(3, "海岸 3",
            Area(Wave(5, Mixed), Wave(6, Mixed)),
            Area(Wave(6, Mixed), Wave(6, Mixed)),
            Area(Wave(6, Mixed), Wave(6, Mixed), Wave(5, Mixed)),
            Area(Wave(7, Mixed), Wave(6, Mixed)),
            Area(Wave(6, Mixed), Wave(7, Mixed), Wave(6, Mixed)),
            Area(Boss(2, Weak, BigBossTable.SkuaNo), Wave(3, Weak), Wave(3, Weak)));

        // ---- ワールド2 氷山 ----

        private static readonly StageDef Stage4 = new StageDef(4, "氷山 1",
            Area(Wave(5, Mixed), Wave(6, Mixed)),
            Area(Wave(6, Mixed), Wave(6, Mixed)),
            Area(Wave(6, Mixed), Wave(6, Strong), Wave(5, Mixed)),
            Area(Wave(6, Strong), Wave(6, Strong)),
            Area(Wave(6, Strong), Wave(7, Strong), Wave(6, Strong)),
            Area(Boss(3, Mixed, 14), Wave(4, Mixed), Wave(5, Mixed)));

        private static readonly StageDef Stage5 = new StageDef(5, "氷山 2",
            Area(Wave(6, Mixed), Wave(6, Strong)),
            Area(Wave(6, Strong), Wave(6, Strong)),
            Area(Wave(6, Strong), Wave(7, Strong), Wave(5, Strong)),
            Area(Wave(7, Strong), Wave(7, Strong)),
            Area(Wave(7, Strong), Wave(7, Strong), Wave(6, Strong)),
            Area(Boss(3, Strong, 19), Wave(5, Strong), Wave(5, Strong)));

        /// <summary>最後に大ボス（ヒョウアザラシ）</summary>
        private static readonly StageDef Stage6 = new StageDef(6, "氷山 3",
            Area(Wave(6, Strong), Wave(6, Strong)),
            Area(Wave(6, Strong), Wave(7, Strong)),
            Area(Wave(7, Strong), Wave(7, Strong), Wave(5, Strong)),
            Area(Wave(7, Strong), Wave(7, Strong)),
            Area(Wave(7, Strong), Wave(7, Strong), Wave(7, Strong)),
            Area(Boss(2, Mixed, BigBossTable.LeopardSealNo), Wave(3, Mixed), Wave(4, Mixed)));

        // ---- ワールド3 深海 ----

        private static readonly StageDef Stage7 = new StageDef(7, "深海 1",
            Area(Wave(6, Strong), Wave(6, Strong)),
            Area(Wave(6, Strong), Wave(7, Strong)),
            Area(Wave(7, Strong), Wave(6, Elite), Wave(5, Strong)),
            Area(Wave(7, Elite), Wave(7, Elite)),
            Area(Wave(7, Elite), Wave(7, Elite), Wave(6, Elite)),
            Area(Boss(3, Strong, 10), Wave(5, Strong), Wave(5, Strong)));

        private static readonly StageDef Stage8 = new StageDef(8, "深海 2",
            Area(Wave(6, Strong), Wave(7, Elite)),
            Area(Wave(7, Elite), Wave(7, Elite)),
            Area(Wave(7, Elite), Wave(7, Elite), Wave(6, Elite)),
            Area(Wave(8, Elite), Wave(7, Elite)),
            Area(Wave(8, Elite), Wave(8, Elite), Wave(7, Elite)),
            Area(Boss(3, Elite, 20), Wave(5, Elite), Wave(5, Elite)));

        /// <summary>最後に大ボス（シャチ）</summary>
        private static readonly StageDef Stage9 = new StageDef(9, "深海 3",
            Area(Wave(7, Elite), Wave(7, Elite)),
            Area(Wave(7, Elite), Wave(8, Elite)),
            Area(Wave(8, Elite), Wave(8, Elite), Wave(6, Elite)),
            Area(Wave(8, Elite), Wave(8, Elite)),
            Area(Wave(8, Elite), Wave(8, Elite), Wave(8, Elite)),
            Area(Boss(2, Strong, BigBossTable.OrcaNo), Wave(4, Strong), Wave(4, Strong)));

        // ---- ワールド4 嵐の崖 ----
        // ワールド4・5の1・2ステージ目のボスは巨大ペンモン。闘技場で、属性の大技も使ってくる

        private static readonly StageDef Stage10 = new StageDef(10, "嵐の崖 1",
            Area(Wave(7, Elite), Wave(7, Elite)),
            Area(Wave(7, Elite), Wave(7, Ace)),
            Area(Wave(8, Elite), Wave(7, Ace), Wave(6, Elite)),
            Area(Wave(8, Ace), Wave(7, Ace)),
            Area(Wave(8, Ace), Wave(8, Ace), Wave(7, Ace)),
            AreaDef.Arena(Giant(2, Elite, 15), Wave(3, Elite), Wave(3, Elite)));

        private static readonly StageDef Stage11 = new StageDef(11, "嵐の崖 2",
            Area(Wave(7, Elite), Wave(7, Ace)),
            Area(Wave(7, Ace), Wave(8, Ace)),
            Area(Wave(8, Ace), Wave(8, Ace), Wave(6, Ace)),
            Area(Wave(8, Ace), Wave(8, Ace)),
            Area(Wave(8, Ace), Wave(8, Ace), Wave(8, Ace)),
            AreaDef.Arena(Giant(2, Ace, 18), Wave(3, Ace), Wave(3, Ace)));

        /// <summary>最後に大ボス（オオフルマカモメ）。突風で柵まで押されるので、雑魚はさらに少なめ</summary>
        private static readonly StageDef Stage12 = new StageDef(12, "嵐の崖 3",
            Area(Wave(7, Ace), Wave(8, Ace)),
            Area(Wave(8, Ace), Wave(8, Ace)),
            Area(Wave(8, Ace), Wave(8, Ace), Wave(7, Ace)),
            Area(Wave(8, Ace), Wave(8, Ace)),
            Area(Wave(8, Ace), Wave(8, Ace), Wave(8, Ace)),
            Area(Boss(2, Elite, BigBossTable.FulmarNo), Wave(3, Elite), Wave(3, Elite)));

        // ---- ワールド5 火山の入り江 ----

        private static readonly StageDef Stage13 = new StageDef(13, "火山の入り江 1",
            Area(Wave(7, Ace), Wave(8, Ace)),
            Area(Wave(8, Ace), Wave(8, Ace)),
            Area(Wave(8, Ace), Wave(8, Legend), Wave(7, Ace)),
            Area(Wave(8, Ace), Wave(8, Ace)),
            Area(Wave(8, Ace), Wave(8, Legend), Wave(8, Ace)),
            AreaDef.Arena(Giant(2, Ace, 5), Wave(3, Ace), Wave(3, Ace)));

        private static readonly StageDef Stage14 = new StageDef(14, "火山の入り江 2",
            Area(Wave(8, Ace), Wave(8, Ace)),
            Area(Wave(8, Ace), Wave(8, Legend)),
            Area(Wave(8, Legend), Wave(8, Ace), Wave(7, Legend)),
            Area(Wave(8, Legend), Wave(8, Ace)),
            Area(Wave(8, Legend), Wave(8, Legend), Wave(8, Ace)),
            AreaDef.Arena(Giant(2, Legend, 20), Wave(3, Ace), Wave(3, Ace)));

        /// <summary>最後に大ボス（サメ）。ゲームの最後のステージ</summary>
        private static readonly StageDef Stage15 = new StageDef(15, "火山の入り江 3",
            Area(Wave(8, Ace), Wave(8, Legend)),
            Area(Wave(8, Legend), Wave(8, Ace)),
            Area(Wave(8, Legend), Wave(8, Legend), Wave(7, Ace)),
            Area(Wave(8, Legend), Wave(8, Legend)),
            Area(Wave(8, Legend), Wave(8, Legend), Wave(8, Legend)),
            Area(Boss(2, Ace, BigBossTable.SharkNo), Wave(3, Ace), Wave(4, Ace)));

        /// <summary>番号の順に並べる（No は添字+1）</summary>
        private static readonly StageDef[] All =
        {
            Stage1, Stage2, Stage3, Stage4, Stage5, Stage6, Stage7, Stage8, Stage9,
            Stage10, Stage11, Stage12, Stage13, Stage14, Stage15,
        };

        public static int Count => All.Length;

        /// <summary>番号（1〜）から引く。範囲外は null（通信で壊れた値が届いたときに落ちないように）</summary>
        public static StageDef Get(int no)
        {
            return no >= 1 && no <= All.Length ? All[no - 1] : null;
        }

        /// <summary>そのステージが何番目のワールドか（1〜）</summary>
        public static int WorldOf(int stageNo) => (stageNo - 1) / StagesPerWorld + 1;

        private static AreaDef Area(params WaveDef[] waves) => new AreaDef(waves);
        private static WaveDef Wave(int count, int[] pool) => new WaveDef(count, pool);
        private static WaveDef Boss(int count, int[] pool, int bossNo) => new WaveDef(count, pool, bossNo);
        private static WaveDef Giant(int count, int[] pool, int speciesNo) => new WaveDef(count, pool, new[] { new BossSpawn(speciesNo, BossRank.Giant) });
    }
}
