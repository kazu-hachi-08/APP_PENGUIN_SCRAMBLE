namespace PenmonScramble.Core
{
    /// <summary>
    /// 巨大ペンモンの闘技場の大技。属性ごとに1つ、大ボスと同じ赤い予告で知らせる（よけ方は大ボスと同じに読めるように）。
    /// 数値を変えるときはここを直す
    /// </summary>
    public static class GiantAttacks
    {
        // 氷: 闘技場にばらばらにつららが降る（羽の雨と同じ形。足元とばらばらの場所）
        private static readonly BossAttackDef Ice = new BossAttackDef(BossAttackKind.Rain, 1.0f, BigBossTable.Circles(22f, 5f, 1.0f, 4));

        // 水: 横一列の波（シャチの波と同じ形）
        private static readonly BossAttackDef Water = new BossAttackDef(BossAttackKind.Wave, 1.1f, BigBossTable.Circles(26f, 8f, 0f, 0));

        // 風: 3か所に竜巻が立ち、しばらく中へ吸い込む。歩けば抜け出せる速さで引き、中にいる間だけ少しずつ削る
        private static readonly BossAttackDef Wind = new BossAttackDef(BossAttackKind.Vortex, 1.0f, new SkillDef
        {
            Shape = SkillShape.Zone, Power = 4f, AreaRadius = 2.2f, Count = 3, Duration = 3f, Interval = 0.5f, Pull = 2.2f,
        });

        // 雷: 一番近いプレイヤーへ向けた稲妻の帯。柵まで届く
        private static readonly BossAttackDef Thunder = new BossAttackDef(BossAttackKind.Beam, 1.0f, new SkillDef
        {
            Shape = SkillShape.Dash, Power = 30f, Knockback = 7f, AreaRadius = 1.1f, Range = 0f,
        });

        public static BossAttackDef For(Element element)
        {
            switch (element)
            {
                case Element.Ice:
                    return Ice;
                case Element.Water:
                    return Water;
                case Element.Wind:
                    return Wind;
                default:
                    return Thunder;
            }
        }
    }
}
