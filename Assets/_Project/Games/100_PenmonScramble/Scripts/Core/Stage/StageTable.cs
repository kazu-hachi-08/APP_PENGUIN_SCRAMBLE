namespace PenmonScramble.Core
{
    /// <summary>
    /// ステージの定義表（§6）。エリアの数・各エリアの敵の出方を変えるときはここを直す。
    /// 9ステージはフェーズ7でそろえる
    /// </summary>
    public static class StageTable
    {
        // 前半の雑魚は ★1 だけ。後半は ★2 も混ぜて、少しずつ手ごわくする
        private static readonly int[] Weak = { 1, 2, 3, 6, 7, 11, 12, 13, 16, 17 };
        private static readonly int[] Mixed = { 1, 2, 3, 6, 7, 11, 12, 13, 16, 17, 4, 8, 9, 14, 18, 19 };

        /// <summary>ワールド1の1つ目。5エリア＋ボスエリア（ボスはキングス）</summary>
        public static readonly StageDef Stage1 = new StageDef("海岸 1",
            new AreaDef(new WaveDef(4, Weak), new WaveDef(5, Weak)),
            new AreaDef(new WaveDef(5, Weak), new WaveDef(5, Weak)),
            new AreaDef(new WaveDef(5, Weak), new WaveDef(6, Weak), new WaveDef(4, Weak)),
            new AreaDef(new WaveDef(6, Mixed), new WaveDef(6, Mixed)),
            new AreaDef(new WaveDef(6, Mixed), new WaveDef(6, Mixed), new WaveDef(6, Mixed)),
            new AreaDef(new WaveDef(3, Weak, bossSpeciesNo: 4), new WaveDef(4, Weak), new WaveDef(4, Weak)));
    }
}
