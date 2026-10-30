using PenmonScramble.Core;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 人の代わりに戦うプレイヤーの入力（スクショの台本・重さの計測で使う）。
    /// 近い敵へ歩いて技1を出し続け、ときどき技2、ゲージがたまれば必殺技を出す
    /// </summary>
    public static class StageBot
    {
        // この距離まで近づいたら止まって技を出す（マス）
        private const float AttackDistance = 2f;
        // 技2はときどき押す（ずっと押すと技1が出ない）
        private const float Skill2Period = 1.5f;
        private const float Skill2Hold = 0.2f;

        public static PlayerInput InputFor(StageWorld world, Unit player, float time)
        {
            var move = Vec2.Zero;
            if (TryNearestEnemy(world, player.Position, out var enemy))
            {
                var toEnemy = enemy - player.Position;
                if (toEnemy.Length > AttackDistance)
                {
                    move = toEnemy.Normalized;
                }
            }

            var skill2 = time % Skill2Period < Skill2Hold;
            return new PlayerInput(move, skill1: !skill2, skill2: skill2, special: true);
        }

        private static bool TryNearestEnemy(StageWorld world, Vec2 from, out Vec2 nearest)
        {
            nearest = default;
            var best = float.MaxValue;
            foreach (var unit in world.Units)
            {
                var distance = (unit.Position - from).LengthSquared;
                if (unit.Team == Team.Enemy && distance < best)
                {
                    best = distance;
                    nearest = unit.Position;
                }
            }

            return best < float.MaxValue;
        }
    }
}
