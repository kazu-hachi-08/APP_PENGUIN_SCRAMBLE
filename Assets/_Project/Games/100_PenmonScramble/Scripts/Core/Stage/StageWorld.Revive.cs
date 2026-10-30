namespace PenmonScramble.Core
{
    /// <summary>
    /// ダウンと復活（§4.5）。3体とも倒れたプレイヤーはその場に残り、仲間が近くに合計3秒いると起き上がる
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
                    Revive(unit);
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

        private static void Revive(Unit unit)
        {
            unit.ReviveFirstMember(Balance.ReviveHpRatio);
            unit.ReviveSeconds = 0f;
            unit.KnockbackVelocity = Vec2.Zero;
            unit.InvincibleSeconds = Balance.ReviveInvincibleSeconds;
        }
    }
}
