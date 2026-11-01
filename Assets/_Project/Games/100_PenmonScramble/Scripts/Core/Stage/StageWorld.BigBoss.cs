using System;
using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 大ボスの動き: 休みの間は一番近いプレイヤーへ寄り、時間が来たら赤い範囲で予告 → 攻撃、を順に繰り返す。
    /// 予告が出ている間は動かない（予告の場所と攻撃の場所がずれないように）。
    /// 本気の大ボスは HP が半分を切ると一度吠え（全体が止まる）、予告と休みが短くなり、攻撃が1つ増える（BossRage）
    /// </summary>
    public sealed partial class StageWorld
    {
        // 波の帯の上に並べる「はじける」演出の間隔（マス）
        private const float WaveBurstSpacing = 2f;

        private readonly List<BossWarning> _warnings = new List<BossWarning>();

        /// <summary>いま出ている赤い範囲（全部の大ボスの分）</summary>
        public IReadOnlyList<BossWarning> Warnings => _warnings;

        private void UpdateBigBoss(Unit boss, BigBossBrain brain, float dt)
        {
            if (brain.IsSerious && !brain.IsEnraged && boss.Hp < boss.MaxHp * Balance.RageHpRatio)
            {
                Enrage(boss, brain);
                return;
            }

            if (brain.Current == null && boss.IsSubmerged)
            {
                // 背びれで走り終えた（突進の間は大ボスの動きを進めないので、走り終えてからここへ来る）
                ContinueFin(boss, brain);
                return;
            }

            if (brain.Current == null)
            {
                RestAndApproach(boss, brain, dt);
                return;
            }

            UpdateBossTelegraph(boss, brain, dt);
        }

        /// <summary>予告の時間を進め、時間が来たら攻撃を出す。本気で続けて出す分が残っていれば、休まずに次の予告を出す</summary>
        private void UpdateBossTelegraph(Unit boss, BigBossBrain brain, float dt)
        {
            brain.TelegraphSeconds -= dt;
            foreach (var warning in brain.Warnings)
            {
                warning.Remaining = MathF.Max(0f, warning.Remaining - dt);
            }

            if (brain.TelegraphSeconds > 0f)
            {
                return;
            }

            var attack = brain.Current;
            ExecuteBossAttack(boss, brain);
            ClearWarnings(brain);
            brain.Current = null;
            brain.RestSeconds = brain.CurrentRestSeconds;
            var target = FindNearestOpponent(boss);
            if (brain.RepeatsLeft > 0 && target != null)
            {
                brain.RepeatsLeft--;
                StartBossAttack(boss, brain, target, attack);
            }
        }

        /// <summary>吠える: 全体を少し止め、まわりへ輪を広げて（画面が大きく揺れる）、ここから本気の攻撃にする</summary>
        private void Enrage(Unit boss, BigBossBrain brain)
        {
            brain.IsEnraged = true;
            _hitStopTicks = Math.Max(_hitStopTicks, Balance.RageRoarTicks);
            Burst?.Invoke(new BurstEvent(BurstShape.Ring, boss.Element, boss.Position, boss.Radius * 3f, true));
        }

        private void RestAndApproach(Unit boss, BigBossBrain brain, float dt)
        {
            brain.RestSeconds -= dt;
            var target = FindNearestOpponent(boss);
            if (target == null)
            {
                return;
            }

            var toTarget = target.Position - boss.Position;
            if (toTarget.LengthSquared > Balance.BigBossKeepDistance * Balance.BigBossKeepDistance)
            {
                MoveBy(boss, toTarget.Normalized, dt);
            }
            else
            {
                boss.Facing = DirectionOr(toTarget, boss.Facing);
            }

            if (brain.RestSeconds <= 0f)
            {
                StartBossAttack(boss, brain, target);
            }
        }

        private void StartBossAttack(Unit boss, BigBossBrain brain, Unit target)
        {
            var attack = brain.Attacks[brain.NextAttackIndex % brain.Attacks.Count];
            brain.NextAttackIndex++;
            // 本気で「続けて」増える攻撃は、1回目を出すときに残りの回数を決める
            var extra = brain.ExtraFor(attack);
            brain.RepeatsLeft = extra > 0 && !brain.Rage.Simultaneous ? extra : 0;
            StartBossAttack(boss, brain, target, attack);
        }

        private void StartBossAttack(Unit boss, BigBossBrain brain, Unit target, BossAttackDef attack)
        {
            brain.Current = attack;
            brain.TelegraphTotal = attack.TelegraphSeconds * brain.CurrentTelegraphScale;
            brain.TelegraphSeconds = brain.TelegraphTotal;
            var skill = attack.Skill;
            switch (attack.Kind)
            {
                case BossAttackKind.Gust:
                    StartGust(boss, brain, skill, target);
                    break;
                case BossAttackKind.Fin:
                    StartFin(boss, brain, skill, target);
                    break;
                case BossAttackKind.Charge:
                    brain.Direction = attack.CrossScreen ? HorizontalToward(boss, target) : AwayFrom(boss.Position, target, boss.Facing);
                    boss.Facing = brain.Direction;
                    // 柵に当たったら止まる。予告の道と実際に走る道をそろえ、柵に沿って予告の外へすべり込まないようにする
                    var wall = Area.DistanceToWall(boss.Position, brain.Direction, boss.Radius);
                    brain.DashDistance = attack.CrossScreen ? wall : MathF.Min(skill.Range, wall);
                    AddWarning(brain, boss.Position, boss.Position + brain.Direction * brain.DashDistance, boss.Radius + skill.AreaRadius);
                    break;
                case BossAttackKind.Rain:
                    StartRain(brain, skill);
                    break;
                case BossAttackKind.Ambush:
                    // もぐっている間は攻撃が効かない。飛び出す場所は、もぐった瞬間の一番近いプレイヤーの足元
                    boss.IsSubmerged = true;
                    boss.KnockbackVelocity = Vec2.Zero;
                    AddWarning(brain, target.Position, target.Position, skill.AreaRadius);
                    break;
                case BossAttackKind.Vortex:
                    StartVortex(brain, attack, target);
                    break;
                case BossAttackKind.Beam:
                    StartBeam(boss, brain, skill, target);
                    break;
                default:
                    StartWave(boss, brain, target, attack);
                    break;
            }
        }

        /// <summary>
        /// 元気なプレイヤー全員の足元と、エリアのばらばらの場所に円を出す（ソロでもよける所が分かりにくくならない数）。
        /// ばらばらの円は広い場ほど増やす。広さの比の平方根にして、闘技場（ふつうのエリアの約3倍）で 3 → 5 くらいにとどめる
        /// （広さどおりに増やすと、円だらけで逃げ場が読めなくなるため）
        /// </summary>
        private void StartRain(BigBossBrain brain, SkillDef skill)
        {
            foreach (var unit in _units)
            {
                if (unit.Team == Team.Player && unit.IsAlive)
                {
                    AddWarning(brain, unit.Position, unit.Position, skill.AreaRadius);
                }
            }

            var count = (int)MathF.Round(skill.Count * MathF.Sqrt(Area.Size / (Balance.AreaWidth * Balance.AreaHeight)));
            for (var i = 0; i < count; i++)
            {
                var point = RandomPointInArea(skill.AreaRadius);
                AddWarning(brain, point, point, skill.AreaRadius);
            }
        }

        /// <summary>
        /// 一番近いプレイヤーの高さを中心にした帯を、エリアの端から端まで知らせる。
        /// 帯の長さは、帯がかかる中で一番広い高さの柵から柵まで（楕円の上の方でも、帯の下側の端まで届くように）
        /// </summary>
        private void StartWave(Unit boss, BigBossBrain brain, Unit target, BossAttackDef attack)
        {
            brain.Direction = HorizontalToward(boss, target);
            var y = target.Position.Y;
            AddWaveBand(brain, y);

            // 本気のシャチは2本同時。2本目は1本目から少し離して、闘技場の広い側（真ん中寄り）へ置く（2本の間を抜けられるように）
            var extra = brain.Rage != null && brain.Rage.Simultaneous ? brain.ExtraFor(attack) : 0;
            var step = (Balance.BigBossWaveHalfWidth * 2f + Balance.RageWaveGap) * (y >= Area.Center.Y ? -1f : 1f);
            for (var i = 1; i <= extra; i++)
            {
                AddWaveBand(brain, y + step * i);
            }
        }

        private void AddWaveBand(BigBossBrain brain, float y)
        {
            var halfWidth = Balance.BigBossWaveHalfWidth;
            var widestY = Math.Clamp(Area.Center.Y, y - halfWidth, y + halfWidth);
            var (minX, maxX) = Area.SpanX(widestY, 0f);
            AddWarning(brain, new Vec2(minX, y), new Vec2(maxX, y), halfWidth);
        }

        /// <summary>竜巻: 1つは一番近いプレイヤーの足元、残りは闘技場のばらばらの場所。本気のオオフルマカモメは吠えた後に数が増える</summary>
        private void StartVortex(BigBossBrain brain, BossAttackDef attack, Unit target)
        {
            var skill = attack.Skill;
            AddWarning(brain, target.Position, target.Position, skill.AreaRadius);
            var count = skill.Count + brain.ExtraFor(attack);
            for (var i = 1; i < count; i++)
            {
                var point = RandomPointInArea(skill.AreaRadius);
                AddWarning(brain, point, point, skill.AreaRadius);
            }
        }

        /// <summary>稲妻: ボスの足元から一番近いプレイヤーの方へ、柵まで（Range があればそこまで）の帯</summary>
        private void StartBeam(Unit boss, BigBossBrain brain, SkillDef skill, Unit target)
        {
            brain.Direction = AwayFrom(boss.Position, target, boss.Facing);
            boss.Facing = brain.Direction;
            var wall = Area.DistanceToWall(boss.Position, brain.Direction, 0f);
            var length = skill.Range > 0f ? MathF.Min(skill.Range, wall) : wall;
            AddWarning(brain, boss.Position, boss.Position + brain.Direction * length, skill.AreaRadius);
        }

        private void ExecuteBossAttack(Unit boss, BigBossBrain brain)
        {
            var skill = brain.Current.Skill;
            var damage = CalcDamage(boss, skill);
            switch (brain.Current.Kind)
            {
                case BossAttackKind.Gust:
                    ExecuteGust(boss, brain, damage, skill);
                    break;
                case BossAttackKind.Fin:
                    ExecuteFin(boss, brain, damage, skill);
                    break;
                case BossAttackKind.Charge:
                    boss.Dash = new DashState(skill, brain.Direction, damage, brain.DashDistance);
                    break;
                case BossAttackKind.Rain:
                    foreach (var warning in brain.Warnings)
                    {
                        HitPlayersIn(boss, warning, damage, skill, null);
                        Burst?.Invoke(new BurstEvent(BurstShape.Blast, boss.Element, warning.From, warning.HalfWidth, false));
                    }

                    break;
                case BossAttackKind.Ambush:
                    var spot = brain.Warnings[0];
                    boss.Position = spot.From;
                    boss.IsSubmerged = false;
                    HitPlayersIn(boss, spot, damage, skill, null);
                    Burst?.Invoke(new BurstEvent(BurstShape.Ring, boss.Element, spot.From, spot.HalfWidth, false));
                    LeaveLeftovers(boss, brain.Current, spot);
                    break;
                case BossAttackKind.Vortex:
                    foreach (var warning in brain.Warnings)
                    {
                        _areaEffects.Add(new AreaEffect(_nextId++, boss, skill, AreaEffectKind.Vortex, warning.From, 0f));
                    }

                    break;
                case BossAttackKind.Beam:
                    ExecuteBand(boss, brain.Warnings[0], damage, skill, null);
                    break;
                default:
                    foreach (var band in brain.Warnings)
                    {
                        ExecuteBand(boss, band, damage, skill, brain.Direction);
                    }

                    break;
            }
        }

        /// <summary>帯の中のプレイヤーに当て、帯に沿って「はじける」演出を並べる（波・稲妻）</summary>
        /// <param name="direction">飛ばす向き。null なら帯の始まり（ボスの足元）から外へ</param>
        private void ExecuteBand(Unit boss, BossWarning band, int damage, SkillDef skill, Vec2? direction)
        {
            HitPlayersIn(boss, band, damage, skill, direction);
            var along = band.To - band.From;
            var length = along.Length;
            var step = along.Normalized;
            for (var d = 0f; d <= length; d += WaveBurstSpacing)
            {
                Burst?.Invoke(new BurstEvent(BurstShape.Blast, boss.Element, band.From + step * d, band.HalfWidth, false));
            }
        }

        /// <param name="direction">飛ばす向き。null なら範囲の中心から外へ</param>
        private void HitPlayersIn(Unit boss, BossWarning warning, int damage, SkillDef skill, Vec2? direction)
        {
            foreach (var target in _units)
            {
                if (IsOpponent(boss, target) && warning.Overlaps(target.Position, target.Radius))
                {
                    ApplyHit(boss.Id, target, damage, skill.Knockback, direction ?? AwayFrom(warning.From, target, boss.Facing), skill);
                }
            }
        }

        private void AddWarning(BigBossBrain brain, Vec2 from, Vec2 to, float halfWidth, float halfAngleDegrees = 0f)
        {
            var warning = new BossWarning(_nextId++, brain.Current.Kind, from, to, halfWidth, halfAngleDegrees, brain.TelegraphTotal);
            brain.Warnings.Add(warning);
            _warnings.Add(warning);
        }

        /// <summary>攻撃が終わった・大ボスが倒れたときに、その大ボスの予告を消す</summary>
        private void ClearWarnings(BigBossBrain brain)
        {
            foreach (var warning in brain.Warnings)
            {
                _warnings.Remove(warning);
            }

            brain.Warnings.Clear();
        }

        private static Vec2 HorizontalToward(Unit from, Unit target)
        {
            return new Vec2(target.Position.X >= from.Position.X ? 1f : -1f, 0f);
        }

        private Vec2 RandomPointInArea(float margin) => Area.RandomPoint(_random, margin);
    }
}
