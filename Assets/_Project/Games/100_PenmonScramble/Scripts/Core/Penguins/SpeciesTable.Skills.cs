namespace PenmonScramble.Core
{
    /// <summary>
    /// 技の形ごとの作り方。定義表を1行で読めるよう、形ごとに要る数値だけを引数にしている
    /// </summary>
    public static partial class SpeciesTable
    {
        private static SkillDef Melee(string name, float power, float cooldown, float range, float halfAngle, float knockback) => new SkillDef
        {
            Name = name, Shape = SkillShape.Melee, Power = power, Cooldown = cooldown,
            Range = range, HalfAngleDegrees = halfAngle, Knockback = knockback,
        };

        /// <param name="extraWidth">体より広く当たる分（大きな波に乗る・回りながら、など）</param>
        private static SkillDef Dash(string name, float power, float cooldown, float distance, float speed, float knockback, float extraWidth = 0f) => new SkillDef
        {
            Name = name, Shape = SkillShape.Dash, Power = power, Cooldown = cooldown,
            Range = distance, DashSpeed = speed, Knockback = knockback, AreaRadius = extraWidth,
        };

        private static SkillDef Shot(string name, float power, float cooldown, float range, float speed, float radius, float knockback, bool pierce) => new SkillDef
        {
            Name = name, Shape = SkillShape.Projectile, Power = power, Cooldown = cooldown,
            Range = range, ProjectileSpeed = speed, ProjectileRadius = radius, Knockback = knockback, Pierce = pierce,
        };

        private static SkillDef Spread(string name, float power, float cooldown, float range, float speed, float radius, float knockback, int count, float spreadDegrees) => new SkillDef
        {
            Name = name, Shape = SkillShape.Spread, Power = power, Cooldown = cooldown,
            Range = range, ProjectileSpeed = speed, ProjectileRadius = radius, Knockback = knockback,
            Count = count, SpreadDegrees = spreadDegrees,
        };

        private static SkillDef Nova(string name, float power, float cooldown, float radius, float knockback) => new SkillDef
        {
            Name = name, Shape = SkillShape.Nova, Power = power, Cooldown = cooldown,
            Range = radius, Knockback = knockback,
        };

        /// <param name="pull">0より大きいと、外へ飛ばす代わりに中心へ吸い込む</param>
        private static SkillDef Zone(string name, float power, float cooldown, float distance, float radius, float duration, float interval, float knockback, float pull) => new SkillDef
        {
            Name = name, Shape = SkillShape.Zone, Power = power, Cooldown = cooldown,
            Range = distance, AreaRadius = radius, Duration = duration, Interval = interval, Knockback = knockback, Pull = pull,
        };

        private static SkillDef Drop(string name, float power, float cooldown, float range, float radius, float delay, int count, float knockback) => new SkillDef
        {
            Name = name, Shape = SkillShape.Drop, Power = power, Cooldown = cooldown,
            Range = range, AreaRadius = radius, Delay = delay, Count = count, Knockback = knockback,
        };
    }
}
