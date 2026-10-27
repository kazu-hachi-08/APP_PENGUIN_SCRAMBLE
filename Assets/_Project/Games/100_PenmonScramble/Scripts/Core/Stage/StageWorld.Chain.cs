using System;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 吹っ飛びの連鎖（§4.4）。強く飛ばされた敵がほかの敵にぶつかると、ダメージを与えてその敵も飛ばす
    /// </summary>
    public sealed partial class StageWorld
    {
        /// <summary>
        /// 飛ばされた技で倒れた敵も、場から消える前のこの更新の間はぶつかる（倒した勢いで周りを巻き込めるように）
        /// </summary>
        private void ResolveChains()
        {
            foreach (var flyer in _units)
            {
                if (flyer.Team != Team.Enemy)
                {
                    continue;
                }

                if (flyer.KnockbackVelocity.Length < Balance.ChainMinSpeed)
                {
                    // 飛び終わったら、次に飛ばされたときはまた同じ相手にぶつかれるようにする
                    flyer.ChainHitIds.Clear();
                    continue;
                }

                foreach (var target in _units)
                {
                    if (flyer.KnockbackVelocity.Length < Balance.ChainMinSpeed)
                    {
                        break;
                    }

                    if (CanChainInto(flyer, target))
                    {
                        HitByChain(flyer, target);
                    }
                }
            }
        }

        private static bool CanChainInto(Unit flyer, Unit target)
        {
            if (target == flyer || target.Team != Team.Enemy || !target.IsAlive || flyer.ChainHitIds.Contains(target.Id))
            {
                return false;
            }

            var toTarget = target.Position - flyer.Position;
            var reach = flyer.Radius + target.Radius;
            // 押し合いで隣にくっついているだけの敵に、離れる向きへ飛んでいるのに当たらないよう、向かっている相手だけにする
            return toTarget.LengthSquared <= reach * reach && Vec2.Dot(toTarget, flyer.KnockbackVelocity) > 0f;
        }

        /// <summary>ダメージはぶつかった速さに比例する。ぶつかった側は遅くなり、ぶつけられた側が飛ぶ</summary>
        private void HitByChain(Unit flyer, Unit target)
        {
            var speed = flyer.KnockbackVelocity.Length;
            flyer.ChainHitIds.Add(target.Id);
            // ぶつけられた側が、すぐにぶつけ返さないようにする
            target.ChainHitIds.Add(flyer.Id);

            var damage = Math.Max(Balance.MinDamage, (int)MathF.Round(speed * Balance.ChainDamagePerSpeed));
            target.Hp = Math.Max(0, target.Hp - damage);
            var direction = AwayFrom(flyer.Position, target, flyer.KnockbackVelocity.Normalized);
            target.KnockbackVelocity = direction * (speed * Balance.ChainTransferRatio * target.KnockbackScale);
            target.Dash = null;
            flyer.KnockbackVelocity *= Balance.ChainKeepRatio;
            // 連鎖のダメージでは、必殺技のゲージも合体技も起こさない（技を当てた人が分からなくなるため）
            Hit?.Invoke(new HitEvent(flyer.Id, target.Id, damage, target.Position));
        }
    }
}
