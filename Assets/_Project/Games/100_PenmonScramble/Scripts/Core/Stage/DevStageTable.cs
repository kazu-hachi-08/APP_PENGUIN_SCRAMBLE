namespace PenmonScramble.Core
{
    /// <summary>
    /// 開発用のステージ。エディタの Tools/PenmonScramble/Dev からだけ遊べ、記録（クリア・コイン）には何も書かない。
    /// ボスラッシュ（101〜）の土台（闘技場の連戦・巨大ペンモン・本気・難易度）を、ストーリーとは別に確かめるためのもの
    /// </summary>
    public static class DevStageTable
    {
        public const int ArenaGauntletNo = 100;
        public const int NewBossesNo = 99;

        private static readonly int[] Escorts = { 1, 2, 3, 6, 7, 11, 12, 13, 16, 17 };

        /// <summary>
        /// 闘技場3連戦: 巨大ペンモン2体組（風と雷。新しい大技の形の2つ）→ 大ボス → 本気の大ボス。
        /// 最初から闘技場に立って始まり、勝つたびに通り道が開いて次の闘技場へ進む
        /// </summary>
        public static readonly StageDef ArenaGauntlet = new StageDef(ArenaGauntletNo, "闘技場3連戦", 2,
            AreaDef.Arena(Bosses(2, new BossSpawn(15, BossRank.Giant), new BossSpawn(20, BossRank.Giant)), new WaveDef(2, Escorts)),
            AreaDef.Arena(Bosses(2, new BossSpawn(BigBossTable.LeopardSealNo, BossRank.Normal)), new WaveDef(2, Escorts)),
            AreaDef.Arena(Bosses(2, new BossSpawn(BigBossTable.OrcaNo, BossRank.Serious)), new WaveDef(2, Escorts)));

        /// <summary>
        /// 新しい大ボス: オオフルマカモメ → サメ → 本気のオオフルマカモメ → 本気のサメ。
        /// 4戦続けて、ふだんの攻撃と、本気で吠えた後に増える攻撃（竜巻が増える・背びれが2本）をまとめて確かめる
        /// </summary>
        public static readonly StageDef NewBosses = new StageDef(NewBossesNo, "新しい大ボス", 3,
            AreaDef.Arena(Bosses(1, new BossSpawn(BigBossTable.FulmarNo, BossRank.Normal)), new WaveDef(2, Escorts)),
            AreaDef.Arena(Bosses(1, new BossSpawn(BigBossTable.SharkNo, BossRank.Normal)), new WaveDef(2, Escorts)),
            AreaDef.Arena(Bosses(1, new BossSpawn(BigBossTable.FulmarNo, BossRank.Serious)), new WaveDef(2, Escorts)),
            AreaDef.Arena(Bosses(1, new BossSpawn(BigBossTable.SharkNo, BossRank.Serious)), new WaveDef(2, Escorts)));

        /// <summary>開発用のステージなら返す。違えば null</summary>
        public static StageDef Get(int no)
        {
            switch (no)
            {
                case ArenaGauntletNo: return ArenaGauntlet;
                case NewBossesNo: return NewBosses;
                default: return null;
            }
        }

        private static WaveDef Bosses(int escorts, params BossSpawn[] bosses) => new WaveDef(escorts, Escorts, bosses);
    }

    /// <summary>番号からステージを引く入口。ストーリー（1〜）・開発用（99・100）・ボスラッシュ（101〜）</summary>
    public static class Stages
    {
        public static StageDef Get(int no) => StageTable.Get(no) ?? RushTable.Get(no) ?? DevStageTable.Get(no);

        /// <summary>記録に書かないステージ（開発用）か</summary>
        public static bool IsDev(int no) => DevStageTable.Get(no) != null;
    }
}
