namespace PenmonScramble.Core
{
    /// <summary>
    /// 敵の動き: 一番近いプレイヤーへ近づいて、技1で殴るだけ（§4.7）
    /// </summary>
    public sealed partial class StageWorld
    {
        private const int EnemySkillSlot = 0;

        private void UpdateEnemy(Unit unit, float dt)
        {
            var target = FindNearestOpponent(unit);
            if (target == null)
            {
                return;
            }

            var toTarget = target.Position - unit.Position;
            var skill = unit.Skills[EnemySkillSlot];
            var attackDistance = target.Radius + skill.Range * Balance.EnemyAttackRangeRatio;
            if (toTarget.LengthSquared > attackDistance * attackDistance)
            {
                MoveBy(unit, toTarget.Normalized, dt);
                return;
            }

            unit.Facing = toTarget.IsNearlyZero ? unit.Facing : toTarget.Normalized;
            TryUseSkill(unit, EnemySkillSlot, faceNearestWhenIdle: false);
        }
    }
}
