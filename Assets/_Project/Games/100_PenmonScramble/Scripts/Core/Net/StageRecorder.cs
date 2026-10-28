using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>
    /// StageWorld のイベントを貯めておき、Capture でその時点の StageSnapshot にまとめる
    /// </summary>
    public sealed class StageRecorder
    {
        private readonly StageWorld _world;
        private readonly int _round;
        private readonly List<HitEvent> _hits = new List<HitEvent>();
        private readonly List<DefeatedEvent> _defeats = new List<DefeatedEvent>();
        private readonly List<SkillUsedEvent> _skillsUsed = new List<SkillUsedEvent>();
        private readonly List<SpecialUsedEvent> _specialsUsed = new List<SpecialUsedEvent>();
        private readonly List<BurstEvent> _bursts = new List<BurstEvent>();
        private readonly List<ComboEvent> _combos = new List<ComboEvent>();

        public StageRecorder(StageWorld world, int round)
        {
            _world = world;
            _round = round;
            _world.Hit += _hits.Add;
            _world.Defeated += _defeats.Add;
            _world.SkillUsed += _skillsUsed.Add;
            _world.SpecialUsed += _specialsUsed.Add;
            _world.Burst += _bursts.Add;
            _world.Combo += _combos.Add;
        }

        public void Detach()
        {
            _world.Hit -= _hits.Add;
            _world.Defeated -= _defeats.Add;
            _world.SkillUsed -= _skillsUsed.Add;
            _world.SpecialUsed -= _specialsUsed.Add;
            _world.Burst -= _bursts.Add;
            _world.Combo -= _combos.Add;
        }

        /// <summary>前回の Capture から後に起きたことを持たせる。貯めていたものは空にする</summary>
        public StageSnapshot Capture()
        {
            var snapshot = new StageSnapshot
            {
                Round = _round,
                StageNo = _world.StageNo,
                Tick = _world.TickCount,
                Result = _world.Result,
                AreaMin = _world.AreaMin,
                AreaMax = _world.AreaMax,
                AreaIndex = _world.AreaIndex,
                AreaCount = _world.AreaCount,
                IsGateOpen = _world.IsGateOpen,
                EnemiesDefeated = _world.EnemiesDefeated,
            };

            foreach (var unit in _world.Units)
            {
                snapshot.Units.Add(new UnitState(unit.Id, unit.Team, unit.Seat, unit.SpeciesNo, unit.Position, unit.Facing, unit.Hp, unit.MaxHp, unit.Radius, FlagsOf(unit),
                    unit.ReviveSeconds / Balance.ReviveSeconds));
                if (unit.Team == Team.Player)
                {
                    snapshot.Players.Add(PlayerStateOf(unit));
                }
            }

            foreach (var projectile in _world.Projectiles)
            {
                snapshot.Projectiles.Add(new ProjectileState(projectile.Id, projectile.Team, projectile.Element, projectile.Position, projectile.Radius));
            }

            foreach (var effect in _world.AreaEffects)
            {
                snapshot.Effects.Add(new EffectState(effect.Id, effect.Kind, effect.Element, effect.Position, effect.Radius));
            }

            foreach (var warning in _world.Warnings)
            {
                snapshot.Warnings.Add(new WarningState(warning.Id, warning.From, warning.To, warning.HalfWidth, warning.Progress));
            }

            MoveEvents(snapshot);
            return snapshot;
        }

        private void MoveEvents(StageSnapshot snapshot)
        {
            snapshot.Hits.AddRange(_hits);
            snapshot.Defeats.AddRange(_defeats);
            snapshot.SkillsUsed.AddRange(_skillsUsed);
            snapshot.SpecialsUsed.AddRange(_specialsUsed);
            snapshot.Bursts.AddRange(_bursts);
            snapshot.Combos.AddRange(_combos);
            _hits.Clear();
            _defeats.Clear();
            _skillsUsed.Clear();
            _specialsUsed.Clear();
            _bursts.Clear();
            _combos.Clear();
        }

        private static UnitFlags FlagsOf(Unit unit)
        {
            var flags = UnitFlags.None;
            if (unit.IsDashing)
            {
                flags |= UnitFlags.Dashing;
            }

            if (unit.IsFrozen)
            {
                flags |= UnitFlags.Frozen;
            }

            if (unit.IsInvincible)
            {
                flags |= UnitFlags.Invincible;
            }

            if (unit.IsDown)
            {
                flags |= UnitFlags.Down;
            }

            if (unit.IsBoss)
            {
                flags |= UnitFlags.Boss;
            }

            if (unit.IsSubmerged)
            {
                flags |= UnitFlags.Submerged;
            }

            return flags;
        }

        private static PlayerState PlayerStateOf(Unit unit)
        {
            var state = new PlayerState
            {
                Seat = unit.Seat,
                ActiveIndex = unit.ActiveIndex,
                SpecialGauge = unit.SpecialGauge,
                SwitchCooldown = unit.SwitchCooldown,
            };
            foreach (var member in unit.Members)
            {
                state.Members.Add(new MemberState(member.Stats.SpeciesNo, member.Hp, member.MaxHp));
            }

            return state;
        }
    }
}
