namespace PenmonScramble.Core
{
    /// <summary>
    /// 属性ごとの必殺技（§4.3）。新しい処理を増やさないよう、技の形（落下・弾・周囲）を組み合わせて作る
    /// </summary>
    public static class SpecialMoves
    {
        /// <summary>氷: 画面の敵がこの秒数凍って、溶けるときに砕ける</summary>
        public const float IceFreezeSeconds = 1.5f;

        /// <summary>水: 大波を作る弾の、縦の間隔（マス）。弾の直径より狭くして、すき間を抜けられないようにする</summary>
        public const float WaterWaveSpacing = 1.0f;

        /// <summary>雷: 敵がいない所にも落とす本数。画面中に落ちて見えるようにするため</summary>
        public const int ThunderExtraBolts = 4;

        /// <summary>雷: 1本ずつずらして落とす間隔（秒）</summary>
        public const float ThunderBoltStagger = 0.12f;

        public static readonly SkillDef IceShatter = new SkillDef
        {
            Name = "ダイヤモンドダスト", Shape = SkillShape.Drop, Power = 40f, AreaRadius = 1.0f,
            Delay = IceFreezeSeconds, Knockback = 10f, IsSpecial = true,
        };

        public static readonly SkillDef WaterWave = new SkillDef
        {
            // 飛ぶ距離はエリアの端で消えるので、どの広さでも端から端まで届く長さにしておく
            Name = "大つなみ", Shape = SkillShape.Projectile, Power = 35f, Range = 100f,
            ProjectileSpeed = 10f, ProjectileRadius = 0.7f, Knockback = 14f, Pierce = true, IsSpecial = true,
        };

        public static readonly SkillDef WindTornado = new SkillDef
        {
            Name = "大竜巻", Shape = SkillShape.Nova, Power = 35f, Range = 5f, Knockback = 20f, IsSpecial = true,
        };

        public static readonly SkillDef ThunderStorm = new SkillDef
        {
            Name = "いかずちの雨", Shape = SkillShape.Drop, Power = 45f, AreaRadius = 1.2f,
            Delay = 0.2f, Knockback = 8f, IsSpecial = true,
        };

        public static SkillDef For(Element element)
        {
            switch (element)
            {
                case Element.Water: return WaterWave;
                case Element.Wind: return WindTornado;
                case Element.Thunder: return ThunderStorm;
                default: return IceShatter;
            }
        }
    }
}
