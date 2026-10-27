namespace PenmonScramble.Core
{
    /// <summary>技の形（§4.2）。フェーズ1は近接・弾の2つだけ</summary>
    public enum SkillShape
    {
        Melee,
        Projectile,
    }

    /// <summary>
    /// 技1つ分の数値。形ごとに使わない項目は無視する
    /// </summary>
    public sealed class SkillDef
    {
        public SkillShape Shape { get; set; }

        /// <summary>威力。ダメージ ＝ 威力 × 使い手の攻撃</summary>
        public float Power { get; set; }

        /// <summary>待ち時間（秒）</summary>
        public float Cooldown { get; set; }

        /// <summary>近接: 自分の中心から相手の縁までの届く距離 / 弾: 飛ぶ距離</summary>
        public float Range { get; set; }

        /// <summary>近接: 前方の扇形の半分の角度（度）</summary>
        public float HalfAngleDegrees { get; set; }

        /// <summary>吹っ飛ばす初速（マス/秒）</summary>
        public float Knockback { get; set; }

        public float ProjectileSpeed { get; set; }
        public float ProjectileRadius { get; set; }

        /// <summary>弾が当たっても消えずに進むか</summary>
        public bool Pierce { get; set; }
    }
}
