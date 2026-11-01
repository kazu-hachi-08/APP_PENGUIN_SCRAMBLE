namespace PenmonScramble.Core
{
    /// <summary>
    /// 突風（オオフルマカモメ）に押し流されている間の状態。吹っ飛びと違って歩くことも技を出すこともできる（逆らって歩けば押される距離が縮む）。
    /// 押されたまま柵に当たると、そこで1回だけ痛い
    /// </summary>
    internal sealed class GustState
    {
        public GustState(int ownerId, Vec2 velocity, float seconds, int wallDamage, SkillDef skill)
        {
            OwnerId = ownerId;
            Velocity = velocity;
            Remaining = seconds;
            WallDamage = wallDamage;
            Skill = skill;
        }

        public int OwnerId { get; }

        /// <summary>押す向きと速さ（マス/秒）</summary>
        public Vec2 Velocity { get; }
        public float Remaining { get; set; }

        /// <summary>柵に当たったときのダメージ。押し始めたときに決めておく（ボスが倒れても残りの押しは続くため）</summary>
        public int WallDamage { get; }

        /// <summary>柵に当たったときの跳ね返り（Knockback）と、当たり方（合体技・必殺技のゲージ）に使う</summary>
        public SkillDef Skill { get; }
    }
}
