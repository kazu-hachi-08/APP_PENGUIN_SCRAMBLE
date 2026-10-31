using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 攻撃の順番待ち: プレイヤー1人に、同時に構え・技を出してよい雑魚を Balance.AttackersPerPlayer 体までにしぼる。
    /// 囲まれて全員に一度に殴られると、構えを見てもよけようがないため。権利の無い雑魚は少し離れて回り込む
    /// </summary>
    public sealed partial class StageWorld
    {
        private readonly List<(float distanceSq, Unit enemy, Unit target)> _attackCandidates = new List<(float, Unit, Unit)>();
        private readonly Dictionary<int, int> _attackersByTarget = new Dictionary<int, int>();

        /// <summary>
        /// 刻みの頭で権利を配り直す。構え・突進の途中の雑魚は権利を持ったまま、残りの枠を近い順に渡す。
        /// ホストの結果をそのまま送るので、並べ替えは距離が同じなら Id の順にして毎回同じ結果にする
        /// </summary>
        private void AssignAttackRights()
        {
            _attackCandidates.Clear();
            _attackersByTarget.Clear();
            foreach (var unit in _units)
            {
                if (!UsesAttackRight(unit))
                {
                    continue;
                }

                if (KeepsAttackRight(unit))
                {
                    CountAttacker(unit.AttackRightTargetId);
                    continue;
                }

                unit.AttackRightTargetId = Unit.NoAttackTarget;
                var target = CanStartAttack(unit) ? FindNearestOpponent(unit) : null;
                if (target != null)
                {
                    _attackCandidates.Add(((target.Position - unit.Position).LengthSquared, unit, target));
                }
            }

            _attackCandidates.Sort(CompareAttackCandidates);
            foreach (var (_, enemy, target) in _attackCandidates)
            {
                if (_attackersByTarget.TryGetValue(target.Id, out var count) && count >= Balance.AttackersPerPlayer)
                {
                    continue;
                }

                enemy.AttackRightTargetId = target.Id;
                CountAttacker(target.Id);
            }
        }

        /// <summary>大ボスは赤い予告で攻撃を知らせ、ステージのボスは1体だけなので、どちらも順番待ちをしない。技を持たない的も数えない</summary>
        private static bool UsesAttackRight(Unit unit)
        {
            return unit.Team == Team.Enemy && unit.IsAlive && unit.Brain == null && !unit.IsBoss && unit.Skills.Length > 0;
        }

        /// <summary>構え・突進を始めたら、相手がダウンしない限り終わるまで権利を手放さない（途中で取り上げると数え方がずれる）</summary>
        private bool KeepsAttackRight(Unit unit)
        {
            if (!unit.HasAttackRight || (unit.WindUp == null && !unit.IsDashing))
            {
                return false;
            }

            var target = FindUnit(unit.AttackRightTargetId);
            return target != null && target.IsAlive;
        }

        /// <summary>
        /// 技を出せる雑魚だけが権利を取れる。雑魚の待ち時間は長いので、待ち時間中の雑魚が枠をふさぐと攻撃がほとんど来なくなるため。
        /// 撃ち終えた雑魚は待ち時間の間は外で回り込み、ほかの雑魚と攻め手が入れ替わる
        /// </summary>
        private static bool CanStartAttack(Unit unit) => !unit.IsFrozen && !unit.IsStunned && unit.Cooldowns[EnemySkillSlot] <= 0f;

        private void CountAttacker(int targetId)
        {
            _attackersByTarget.TryGetValue(targetId, out var count);
            _attackersByTarget[targetId] = count + 1;
        }

        private static int CompareAttackCandidates((float distanceSq, Unit enemy, Unit target) a, (float distanceSq, Unit enemy, Unit target) b)
        {
            var byDistance = a.distanceSq.CompareTo(b.distanceSq);
            return byDistance != 0 ? byDistance : a.enemy.Id.CompareTo(b.enemy.Id);
        }

        /// <summary>
        /// 権利の無い雑魚: 技が届く距離より少し外で止まり、ゆっくり横へ回り込む。囲んでいる見た目は残したまま、殴ってはこない。
        /// 回る向きは Id で決めて、全員が同じ向きに回って一列に流れないようにする
        /// </summary>
        private static void CircleWhileWaiting(Unit unit, Unit target, SkillDef skill, float dt)
        {
            var toTarget = target.Position - unit.Position;
            var distance = toTarget.Length;
            var toward = DirectionOr(toTarget, unit.Facing);
            var waitDistance = target.Radius + skill.Range + Balance.AttackWaitExtraDistance;
            if (distance > waitDistance + Balance.AttackWaitSlack)
            {
                MoveBy(unit, toward, dt);
                return;
            }

            var side = unit.Id % 2 == 0 ? new Vec2(-toward.Y, toward.X) : new Vec2(toward.Y, -toward.X);
            // 近すぎたら下がる。待つ距離の近くでは横に回るだけ
            var back = distance < waitDistance - Balance.AttackWaitSlack ? -toward : Vec2.Zero;
            MoveBy(unit, (side + back).Normalized * Balance.AttackWaitCircleSpeedRatio, dt);
            unit.Facing = toward;
        }
    }
}
