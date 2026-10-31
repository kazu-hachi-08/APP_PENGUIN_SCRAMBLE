namespace PenmonScramble.Core
{
    /// <summary>合体技の種類。2つの属性の組み合わせで決まる</summary>
    public enum ComboKind : byte
    {
        /// <summary>同じ属性</summary>
        Explosion,

        /// <summary>氷＋水</summary>
        Freeze,

        /// <summary>氷＋風</summary>
        Blizzard,

        /// <summary>氷＋雷</summary>
        IceBurst,

        /// <summary>水＋風</summary>
        Whirlpool,

        /// <summary>水＋雷</summary>
        Shock,

        /// <summary>風＋雷</summary>
        ThunderCloud,
    }

    /// <summary>
    /// 合体技の数値。必殺技と同じく、技の形（落下・設置・弾）を組み合わせて作る
    /// </summary>
    public static class ComboMoves
    {
        /// <summary>氷晶爆発: 周りへ飛び散る破片の数</summary>
        public const int IceShardCount = 8;

        /// <summary>感電: 電気が伝わる敵の数（最初の敵を含む）と、伝わる距離（マス）</summary>
        public const int ShockMaxTargets = 5;
        public const float ShockRange = 3.5f;

        /// <summary>感電: 1体ずつずらして落とす間隔（秒）。順に伝わって見えるようにする</summary>
        public const float ShockStagger = 0.08f;

        // 落下の形を「待ち0秒」で使い、その場ですぐ当てる
        public static readonly SkillDef Explosion = new SkillDef
        {
            Name = "大ばくはつ", Shape = SkillShape.Drop, Power = 30f, AreaRadius = 2.5f, Knockback = 16f, IsCombo = true,
        };

        public static readonly SkillDef Freeze = new SkillDef
        {
            Name = "凍結", Shape = SkillShape.Drop, Power = 10f, AreaRadius = 2.0f, FreezeSeconds = 2f, IsCombo = true,
        };

        public static readonly SkillDef Blizzard = new SkillDef
        {
            // 足を遅くする時間は当たる間隔より長くして、中にいる間はずっと遅いままにする
            Name = "吹雪", Shape = SkillShape.Zone, Power = 3f, AreaRadius = 2.2f, Duration = 3f, Interval = 0.3f,
            SlowSeconds = 0.5f, IsCombo = true,
        };

        public static readonly SkillDef IceBurst = new SkillDef
        {
            Name = "氷晶爆発", Shape = SkillShape.Drop, Power = 20f, AreaRadius = 1.5f, Knockback = 10f, IsCombo = true,
        };

        public static readonly SkillDef IceShard = new SkillDef
        {
            Name = "氷晶爆発", Shape = SkillShape.Projectile, Power = 8f, Range = 4f,
            ProjectileSpeed = 12f, ProjectileRadius = 0.25f, Knockback = 7f, IsCombo = true,
        };

        public static readonly SkillDef Whirlpool = new SkillDef
        {
            Name = "うず潮", Shape = SkillShape.Zone, Power = 3f, AreaRadius = 2.5f, Duration = 3f, Interval = 0.3f,
            Pull = 3f, IsCombo = true,
        };

        public static readonly SkillDef Shock = new SkillDef
        {
            Name = "感電", Shape = SkillShape.Drop, Power = 15f, AreaRadius = 0.6f, Knockback = 3f, IsCombo = true,
        };

        public static readonly SkillDef ThunderCloud = new SkillDef
        {
            // 形は使わない（雷雲は AreaEffectKind.Cloud で動かす）
            Name = "雷雲", Shape = SkillShape.Zone, Power = 12f, AreaRadius = 1.0f, Duration = 4f, Interval = 0.6f,
            Knockback = 5f, ChaseSpeed = 3f, IsCombo = true,
        };

        /// <summary>a・b は順不同</summary>
        public static ComboKind KindOf(Element a, Element b)
        {
            if (a == b)
            {
                return ComboKind.Explosion;
            }

            var low = a < b ? a : b;
            var high = a < b ? b : a;
            switch (low)
            {
                case Element.Ice:
                    return high == Element.Water ? ComboKind.Freeze : high == Element.Wind ? ComboKind.Blizzard : ComboKind.IceBurst;
                case Element.Water:
                    return high == Element.Wind ? ComboKind.Whirlpool : ComboKind.Shock;
                default:
                    return ComboKind.ThunderCloud;
            }
        }

        /// <summary>「合体！○○」に出す名前</summary>
        public static string NameOf(ComboKind kind) => MainSkillOf(kind).Name;

        private static SkillDef MainSkillOf(ComboKind kind)
        {
            switch (kind)
            {
                case ComboKind.Freeze: return Freeze;
                case ComboKind.Blizzard: return Blizzard;
                case ComboKind.IceBurst: return IceBurst;
                case ComboKind.Whirlpool: return Whirlpool;
                case ComboKind.Shock: return Shock;
                case ComboKind.ThunderCloud: return ThunderCloud;
                default: return Explosion;
            }
        }
    }
}
