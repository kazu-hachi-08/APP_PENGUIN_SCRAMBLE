using PenmonScramble.Core;

namespace PenmonScramble.Tests
{
    /// <summary>テストで使う小さなエリアと、動かない的</summary>
    internal static class TestWorld
    {
        public const float Tolerance = 0.001f;
        public const float AreaHalf = 10f;

        public static StageWorld Create()
        {
            return new StageWorld(new Vec2(-AreaHalf, -AreaHalf), new Vec2(AreaHalf, AreaHalf), seed: 1);
        }

        public static UnitStats Stats(params SkillDef[] skills) => new UnitStats
        {
            Radius = 0.5f,
            MoveSpeed = 3f,
            MaxHp = 100,
            Attack = 1f,
            Skills = skills,
        };

        public static SkillDef Melee(float power = 10f) => new SkillDef
        {
            Shape = SkillShape.Melee,
            Power = power,
            Cooldown = 1f,
            Range = 1f,
            HalfAngleDegrees = 60f,
        };

        /// <summary>動かない的。届かない技で、テスト中に殴り返してこないようにする</summary>
        public static Unit AddDummyEnemy(StageWorld world, Vec2 position, int maxHp = 100)
        {
            var idle = Melee();
            idle.Range = 0f;
            idle.Cooldown = 1000f;
            var stats = Stats(idle);
            stats.MoveSpeed = 0f;
            stats.MaxHp = maxHp;
            return world.AddUnit(Team.Enemy, stats, position);
        }

        public static void TickFor(StageWorld world, int ticks)
        {
            for (var i = 0; i < ticks; i++)
            {
                world.Tick();
            }
        }

        /// <summary>1回だけ技を出す（押した次の更新で離す）</summary>
        public static void Press(StageWorld world, Unit unit, PlayerInput input)
        {
            world.SetInput(unit.Id, input);
            world.Tick();
            world.SetInput(unit.Id, default);
        }

        public static int Seconds(float seconds) => (int)System.MathF.Ceiling(seconds / Balance.TickSeconds);
    }
}
