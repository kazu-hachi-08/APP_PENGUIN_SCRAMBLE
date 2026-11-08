using System;

namespace PenmonScramble.Core
{
    /// <summary>
    /// オオフルマカモメとサメの攻撃: 突風（扇で押し流す）・背びれ（もぐったまま床を走り回る）・食いつきの後の溶岩のしぶき・動く竜巻。
    /// 竜巻と飛び出しの本体は StageWorld.BigBoss / StageWorld.Areas
    /// </summary>
    public sealed partial class StageWorld
    {
        // 突風の見た目: 扇の中に並べる風の輪（向きの数と、要からの距離の割合）
        private const int GustBurstRays = 3;
        private static readonly float[] GustBurstSteps = { 0.3f, 0.6f, 0.9f };

        // 細くても床の上で見える、背びれの道の最小の幅の半分（マス）
        private const float MinFinHalfWidth = 0.3f;

        /// <summary>突風: 一番近いプレイヤーの方へ扇を出す</summary>
        private void StartGust(Unit boss, BigBossBrain brain, SkillDef skill, Unit target)
        {
            AimBossAway(boss, brain, target);
            AddWarning(brain, boss.Position, boss.Position + brain.Direction * skill.Range, 0f, skill.HalfAngleDegrees);
        }

        /// <summary>ボスが一番近いプレイヤーから離れる向きを向く（向きと攻撃の向きをそろえる）。重なっていたら今の向きのまま</summary>
        private void AimBossAway(Unit boss, BigBossBrain brain, Unit target)
        {
            brain.Direction = AwayFrom(boss.Position, target, boss.Facing);
            boss.Facing = brain.Direction;
        }

        /// <summary>扇の中のプレイヤーを、ボスから外へ押し流し始める。当たった瞬間は痛くない（柵に当たると痛い。HitWallByGust）</summary>
        private void ExecuteGust(Unit boss, BigBossBrain brain, int wallDamage, SkillDef skill)
        {
            var fan = brain.Warnings[0];
            foreach (var target in _units)
            {
                if (!IsOpponent(boss, target) || target.IsInvincible || !fan.Overlaps(target.Position, target.Radius))
                {
                    continue;
                }

                var push = AwayFrom(boss.Position, target, brain.Direction);
                target.Gust = new GustState(boss.Id, push * skill.DashSpeed, skill.Duration, wallDamage, skill);
            }

            EmitGustBursts(fan, skill);
        }

        private void EmitGustBursts(BossWarning fan, SkillDef skill)
        {
            var halfAngle = fan.HalfAngleDegrees * MathF.PI / 180f;
            var axis = (fan.To - fan.From).Normalized;
            for (var ray = 0; ray < GustBurstRays; ray++)
            {
                var direction = axis.Rotated(halfAngle * (ray - (GustBurstRays - 1) * 0.5f));
                foreach (var step in GustBurstSteps)
                {
                    var distance = skill.Range * step;
                    Burst?.Invoke(new BurstEvent(BurstShape.Swing, Element.Wind, fan.From + direction * distance, 0.6f + distance * 0.15f, false));
                }
            }
        }

        /// <summary>背びれ: もぐって、走る回数を決めてから1回目の道を知らせる</summary>
        private void StartFin(Unit boss, BigBossBrain brain, SkillDef skill, Unit target)
        {
            boss.IsSubmerged = true;
            boss.KnockbackVelocity = Vec2.Zero;
            brain.FinAttack = brain.Current;
            brain.FinLegsLeft = 1 + skill.Count;
            AddFinPaths(boss, brain, skill, target);
        }

        /// <summary>
        /// 一番近いプレイヤーの方へ、柵の手前までの道を知らせる。本気で吠えた後は、開いた向きに2本目の道も出す（2本目は Fin の範囲が走る）
        /// </summary>
        private void AddFinPaths(Unit boss, BigBossBrain brain, SkillDef skill, Unit target)
        {
            var halfWidth = FinHalfWidth(boss, skill);
            AimBossAway(boss, brain, target);
            brain.DashDistance = MathF.Min(skill.Range, Area.DistanceToWall(boss.Position, brain.Direction, boss.Radius));
            AddWarning(brain, boss.Position, boss.Position + brain.Direction * brain.DashDistance, halfWidth);

            var extra = brain.Rage != null && brain.Rage.Simultaneous ? brain.ExtraFor(brain.Current) : 0;
            for (var i = 1; i <= extra; i++)
            {
                // 2本目は左回り、3本目は右回り、…と交互に開く
                var side = i % 2 == 1 ? 1f : -1f;
                var angle = Balance.RageFinSpreadDegrees * side * ((i + 1) / 2) * MathF.PI / 180f;
                var direction = brain.Direction.Rotated(angle);
                var length = MathF.Min(skill.Range, Area.DistanceToWall(boss.Position, direction, halfWidth));
                AddWarning(brain, boss.Position, boss.Position + direction * length, halfWidth);
            }
        }

        /// <summary>背びれの当たりの幅の半分。体全体ではなく背びれの道だけが当たる幅で、体の半径に背びれの当たる分（AreaRadius）を足す（最低でも MinFinHalfWidth）</summary>
        private static float FinHalfWidth(Unit boss, SkillDef skill) => MathF.Max(MinFinHalfWidth, boss.Radius + skill.AreaRadius);

        /// <summary>1本目はボス自身がもぐったまま走る（突進と同じ当たり）。2本目からは床を走る範囲を出す</summary>
        private void ExecuteFin(Unit boss, BigBossBrain brain, int damage, SkillDef skill)
        {
            boss.Dash = new DashState(skill, brain.Direction, damage, brain.DashDistance);
            brain.FinLegsLeft--;
            for (var i = 1; i < brain.Warnings.Count; i++)
            {
                var path = brain.Warnings[i];
                var along = path.To - path.From;
                var runner = new AreaEffect(_nextId++, boss, skill, AreaEffectKind.Fin, path.From, 0f, path.HalfWidth)
                {
                    Velocity = along.Normalized * skill.DashSpeed,
                    Remaining = along.Length / skill.DashSpeed,
                };
                _areaEffects.Add(runner);
            }
        }

        /// <summary>走り終えた: 残りがあれば向きを変えて次の道を知らせ、無ければ浮き上がる</summary>
        private void ContinueFin(Unit boss, BigBossBrain brain)
        {
            var target = FindNearestOpponent(boss);
            if (brain.FinLegsLeft > 0 && brain.FinAttack != null && target != null)
            {
                brain.Current = brain.FinAttack;
                brain.TelegraphTotal = Balance.FinLegTelegraphSeconds * brain.CurrentTelegraphScale;
                brain.TelegraphSeconds = brain.TelegraphTotal;
                AddFinPaths(boss, brain, brain.Current.Skill, target);
                return;
            }

            brain.FinLegsLeft = 0;
            boss.IsSubmerged = false;
            Burst?.Invoke(new BurstEvent(BurstShape.Ring, boss.Element, boss.Position, boss.Radius * 1.5f, false));
        }

        /// <summary>2本目の背びれ: まっすぐ走り、触れたプレイヤーを走る向きへ1回ずつ飛ばす</summary>
        private void UpdateFinRunner(AreaEffect fin, float dt)
        {
            fin.Position += fin.Velocity * dt;
            var direction = fin.Velocity.Normalized;
            foreach (var target in _units)
            {
                if (target.Team == fin.Team || !target.IsAlive || fin.HitUnitIds.Contains(target.Id) || !IsWithin(target, fin.Position, fin.Radius))
                {
                    continue;
                }

                fin.HitUnitIds.Add(target.Id);
                ApplyHit(fin.OwnerId, target, fin.Damage, fin.Skill.Knockback, direction, fin.Skill);
            }

            AgeEffect(fin, dt);
        }

        /// <summary>攻撃の後に、攻撃の円の縁のすぐ外へ残す範囲（サメの食いつきの溶岩のしぶき）。向きは毎回ずらす</summary>
        private void LeaveLeftovers(Unit boss, BossAttackDef attack, BossWarning spot)
        {
            var leftover = attack.Leftover;
            if (leftover == null)
            {
                return;
            }

            var distance = spot.HalfWidth * Balance.LeftoverSpreadRatio;
            var start = (float)_random.NextDouble() * MathF.PI * 2f;
            for (var i = 0; i < leftover.Count; i++)
            {
                var angle = start + MathF.PI * 2f * i / leftover.Count;
                var point = Area.ClosestInside(spot.From + new Vec2(MathF.Cos(angle), MathF.Sin(angle)) * distance, leftover.AreaRadius);
                // しぶきは食いつきの円の縁のすぐ外（よけた人が立っている所）に出るので、出た瞬間には当てない。
                // 予告なしに足元へ出てすぐ当たると、よけようがないため（1回目は間隔の分だけ待つ。その間に歩いて出られる）
                _areaEffects.Add(new AreaEffect(_nextId++, boss, leftover, AreaEffectKind.Lava, point, 0f) { IntervalTimer = leftover.Interval });
            }
        }

        /// <summary>動く竜巻（ChaseSpeed のある竜巻）: 一番近いプレイヤーの方へゆっくり寄る。闘技場の外へは出ない</summary>
        private void DriftVortex(AreaEffect vortex, float dt)
        {
            var speed = vortex.Skill.ChaseSpeed;
            var target = speed > 0f ? FindNearestOpponent(vortex.Team, vortex.Position) : null;
            if (target == null)
            {
                return;
            }

            var toTarget = target.Position - vortex.Position;
            var step = MathF.Min(toTarget.Length, speed * dt);
            vortex.Position = Area.ClosestInside(vortex.Position + DirectionOr(toTarget, Vec2.Zero) * step, vortex.Radius);
        }
    }
}
