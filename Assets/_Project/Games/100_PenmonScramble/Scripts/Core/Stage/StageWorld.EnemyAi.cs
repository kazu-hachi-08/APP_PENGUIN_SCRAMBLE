namespace PenmonScramble.Core
{
    /// <summary>
    /// 敵の動き: 一番近いプレイヤーへ近づき、届く距離に入ったら構えてから技1で殴る。ボスは技2も使う。大ボスは StageWorld.BigBoss。
    /// 雑魚は攻撃の権利（StageWorld.AttackRights）が無ければ、少し離れて回り込むだけ
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

            if (unit.WindUp != null)
            {
                UpdateWindUp(unit, unit.WindUp, dt);
                return;
            }

            var target = FindNearestOpponent(unit);
            if (target == null)
            {
                return;
            }

            var skill = unit.Skills[EnemySkillSlot];
            if (UsesAttackRight(unit) && !unit.HasAttackRight)
            {
                CircleWhileWaiting(unit, target, skill, dt);
                return;
            }

            var toTarget = target.Position - unit.Position;
            var attackDistance = target.Radius + skill.Range * Balance.EnemyAttackRangeRatio;
            if (toTarget.LengthSquared > attackDistance * attackDistance)
            {
                MoveBy(unit, toTarget.Normalized, dt);
                return;
            }

            unit.Facing = DirectionOr(toTarget, unit.Facing);
            // 技2を持っている（ボス）なら、待ち時間が明けたときは派手な技2を先に出す
            var slot = unit.Skills.Length > 1 && unit.Cooldowns[1] <= 0f ? 1 : EnemySkillSlot;
            if (unit.Cooldowns[slot] <= 0f)
            {
                unit.WindUp = new WindUpState(slot, Balance.WindUpSeconds(unit.IsBoss, slot));
            }
        }

        /// <summary>構えている間は動かず向きも変えない。時間が来たら、相手がよけていても構えたときの向きへ出す</summary>
        private void UpdateWindUp(Unit unit, WindUpState windUp, float dt)
        {
            windUp.RemainingSeconds -= dt;
            if (windUp.RemainingSeconds > 0f)
            {
                return;
            }

            unit.WindUp = null;
            TryUseSkill(unit, windUp.Slot, faceNearestWhenIdle: false);
        }

        /// <summary>
        /// 構えの途中で強く飛ばされた・凍ったら、構えをやめる（殴って止められるようにする）。
        /// 待ち時間は使ったことにして、起き上がってすぐ構え直さないようにする
        /// </summary>
        private static void InterruptWindUp(Unit unit)
        {
            if (unit.WindUp == null)
            {
                return;
            }

            var slot = unit.WindUp.Slot;
            unit.Cooldowns[slot] = unit.Skills[slot].Cooldown * unit.CooldownScale;
            unit.WindUp = null;
        }
    }
}
