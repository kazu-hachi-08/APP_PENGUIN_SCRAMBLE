namespace PenmonScramble.Core
{
    /// <summary>
    /// 巨大ペンモンの闘技場の大技。ふつうのボスと同じに技1・技2で戦いながら、間をあけて大ボスと同じ赤い予告の大技を出す。
    /// 2体組では、2体の大技が重なると逃げ場が無くなるので、1体ずつ交互に出す
    /// </summary>
    public sealed partial class StageWorld
    {
        // 最後に大技を出した巨大ペンモンの Id。もう1体がいれば、次はそちらの番にする
        private int _lastGiantAttackerId;

        /// <summary>大技の予告を出している・出し始めたら true（その更新はふつうの動きをしない）</summary>
        private bool UpdateGiantBigAttack(Unit giant, BigBossBrain brain, float dt)
        {
            if (brain.Current != null)
            {
                UpdateBossTelegraph(giant, brain, dt);
                return true;
            }

            brain.RestSeconds -= dt;
            // ふつうの技を構えている途中では出さない（構えの予告と赤い予告が重なって読めなくなるため）
            if (brain.RestSeconds > 0f || giant.WindUp != null || !IsGiantTurn(giant))
            {
                return false;
            }

            var target = FindNearestOpponent(giant);
            if (target == null)
            {
                return false;
            }

            _lastGiantAttackerId = giant.Id;
            StartBossAttack(giant, brain, target);
            return true;
        }

        /// <summary>ほかの巨大ペンモンが大技の予告中でなく、前に出したのが自分でない（または、ほかに元気な巨大ペンモンがいない）</summary>
        private bool IsGiantTurn(Unit giant)
        {
            var hasOther = false;
            foreach (var other in _units)
            {
                if (other == giant || !other.IsAlive || !other.IsGiant)
                {
                    continue;
                }

                if (other.Brain.Current != null)
                {
                    return false;
                }

                hasOther = true;
            }

            return !hasOther || _lastGiantAttackerId != giant.Id;
        }
    }
}
