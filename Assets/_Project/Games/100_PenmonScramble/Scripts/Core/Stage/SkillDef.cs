namespace PenmonScramble.Core
{
    /// <summary>技の形（§4.2）。40個の技を、この7つの処理の組み合わせで作る</summary>
    public enum SkillShape : byte
    {
        Melee,
        Projectile,
        Dash,
        Spread,
        Nova,
        Zone,
        Drop,
    }

    /// <summary>
    /// 技1つ分の数値。形ごとに使わない項目は無視する
    /// </summary>
    public sealed class SkillDef
    {
        public string Name { get; set; } = string.Empty;
        public SkillShape Shape { get; set; }

        /// <summary>威力。ダメージ ＝ 威力 × 使い手の攻撃</summary>
        public float Power { get; set; }

        /// <summary>待ち時間（秒）</summary>
        public float Cooldown { get; set; }

        /// <summary>
        /// 近接: 自分の中心から相手の縁までの届く距離 / 弾・拡散: 飛ぶ距離 / 突進: すべる距離 /
        /// 周囲: 当たる半径 / 設置: 置く場所までの距離 / 落下: 狙う敵を探す距離
        /// </summary>
        public float Range { get; set; }

        /// <summary>近接: 前方の扇形の半分の角度（度）</summary>
        public float HalfAngleDegrees { get; set; }

        /// <summary>吹っ飛ばす初速（マス/秒）</summary>
        public float Knockback { get; set; }

        public float ProjectileSpeed { get; set; }
        public float ProjectileRadius { get; set; }

        /// <summary>弾が当たっても消えずに進むか</summary>
        public bool Pierce { get; set; }

        /// <summary>拡散: 弾の数 / 落下: 落ちる場所の数</summary>
        public int Count { get; set; } = 1;

        /// <summary>拡散: 扇の端から端までの角度（度）</summary>
        public float SpreadDegrees { get; set; }

        /// <summary>設置・落下: 当たる範囲の半径 / 突進: 体より広く当たる分</summary>
        public float AreaRadius { get; set; }

        /// <summary>設置: 残る時間（秒）</summary>
        public float Duration { get; set; }

        /// <summary>設置: 中の敵にダメージを与える間隔（秒）</summary>
        public float Interval { get; set; }

        /// <summary>設置: 中心へ吸い込む速さ（マス/秒）。0なら外へ飛ばす</summary>
        public float Pull { get; set; }

        /// <summary>落下: 狙ってから落ちるまで（秒）。よけられるようにするため</summary>
        public float Delay { get; set; }

        /// <summary>突進: すべる速さ（マス/秒）</summary>
        public float DashSpeed { get; set; }

        /// <summary>必殺技の一部か。必殺技のダメージではゲージがたまらない（続けて出せないようにするため）</summary>
        public bool IsSpecial { get; set; }
    }
}
