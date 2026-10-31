namespace PenmonScramble.Core
{
    /// <summary>
    /// 敵の動き: 一番近いプレイヤーへ近づいて、技1で殴るだけ。ボスは技2も使う。大ボスは StageWorld.BigBoss
    /// </summary>
    public sealed partial class StageWorld
    {
        private const int EnemySkillSlot = 0;

        private void UpdateEnemy(Unit unit, float dt)
        {
            if (unit.Brain != null)
            {
                UpdateBigBoss(unit, unit.Brain, dt);
                return;
            }

            // 技を持たない敵は立っているだけ（ペンモン画面の技の実演の的。SkillPractice）
            if (unit.Skills.Length == 0)
            {
                return;
            }

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
            // 技2を持っている（ボス）なら、待ち時間が明けたときは派手な技2を先に出す
            var slot = unit.Skills.Length > 1 && unit.Cooldowns[1] <= 0f ? 1 : EnemySkillSlot;
            TryUseSkill(unit, slot, faceNearestWhenIdle: false);
        }
    }
}
