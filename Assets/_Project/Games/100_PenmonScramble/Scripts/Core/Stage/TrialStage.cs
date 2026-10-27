namespace PenmonScramble.Core
{
    /// <summary>
    /// フェーズ1のお試しエリア: 仮のペンギン1体と、近接で殴ってくる敵の群れ。
    /// フェーズ3で種類・技の定義表、フェーズ4でステージ定義に置き換える
    /// </summary>
    public static class TrialStage
    {
        public const int EnemyCount = 8;

        // 敵はエリアの右側に出す。最初からプレイヤーに重ならないようにするため
        private const float EnemySpawnMinXRatio = 0.1f;
        private const float EnemySpawnMarginRatio = 0.1f;
        private const float PlayerSpawnXRatio = -0.7f;

        public static readonly SkillDef PlayerPunch = new SkillDef
        {
            Shape = SkillShape.Melee,
            Power = 10f,
            Cooldown = 0.25f,
            Range = 1.0f,
            HalfAngleDegrees = 60f,
            Knockback = 6f,
        };

        public static readonly SkillDef PlayerShot = new SkillDef
        {
            Shape = SkillShape.Projectile,
            Power = 14f,
            Cooldown = 0.8f,
            Range = 8f,
            Knockback = 9f,
            ProjectileSpeed = 12f,
            ProjectileRadius = 0.25f,
            Pierce = true,
        };

        public static readonly SkillDef EnemyPunch = new SkillDef
        {
            Shape = SkillShape.Melee,
            Power = 6f,
            Cooldown = 1.5f,
            Range = 0.7f,
            HalfAngleDegrees = 45f,
            Knockback = 4f,
        };

        public static UnitStats PlayerStats() => new UnitStats
        {
            Radius = 0.45f,
            MoveSpeed = 4.5f,
            MaxHp = 120,
            Attack = 1f,
            Skills = new[] { PlayerPunch, PlayerShot },
        };

        public static UnitStats EnemyStats() => new UnitStats
        {
            Radius = 0.45f,
            MoveSpeed = 2f,
            MaxHp = 30,
            Attack = 1f,
            Skills = new[] { EnemyPunch },
        };

        /// <summary>プレイヤーの Unit を返す。敵の位置は seed で決まる</summary>
        public static Unit Create(int seed, out StageWorld world)
        {
            var half = new Vec2(Balance.AreaWidth * 0.5f, Balance.AreaHeight * 0.5f);
            world = new StageWorld(-half, half, seed);
            var player = world.AddUnit(Team.Player, PlayerStats(), new Vec2(half.X * PlayerSpawnXRatio, 0f));
            AddEnemies(world, seed, half);
            return player;
        }

        private static void AddEnemies(StageWorld world, int seed, Vec2 half)
        {
            var random = new System.Random(seed);
            var minX = half.X * EnemySpawnMinXRatio;
            var maxX = half.X * (1f - EnemySpawnMarginRatio);
            var maxY = half.Y * (1f - EnemySpawnMarginRatio);
            for (var i = 0; i < EnemyCount; i++)
            {
                var x = minX + (float)random.NextDouble() * (maxX - minX);
                var y = -maxY + (float)random.NextDouble() * maxY * 2f;
                world.AddUnit(Team.Enemy, EnemyStats(), new Vec2(x, y));
            }
        }
    }
}
