namespace PenmonScramble.Core
{
    /// <summary>
    /// ダウンと復活。3体とも倒れたプレイヤーはその場に残り、仲間が近くに合計3秒いると起き上がる
    /// </summary>
    public sealed partial class StageWorld
    {
        private void UpdateRevives(float dt)
        {
            foreach (var unit in _units)
            {
                if (!unit.IsDown || !HasRescuerNear(unit))
                {
                    continue;
                }

                unit.ReviveSeconds += dt;
                if (unit.ReviveSeconds >= Balance.ReviveSeconds)
                {
                    Revive(unit, Balance.ReviveHpRatio);
                }
            }
        }

        private bool HasRescuerNear(Unit downed)
        {
            foreach (var other in _units)
            {
                if (other.Team == Team.Player && other.IsAlive && IsWithin(other, downed.Position, Balance.ReviveRadius))
                {
                    return true;
                }
            }

            return false;
        }

        /// <param name="hpRatio">起き上がる先頭の子の HP の割合</param>
        private static void Revive(Unit unit, float hpRatio)
        {
            unit.ReviveFirstMember(hpRatio);
            unit.ReviveSeconds = 0f;
            unit.KnockbackVelocity = Vec2.Zero;
            unit.InvincibleSeconds = Balance.ReviveInvincibleSeconds;
        }
    }
}
