using System;
using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>
    /// 1エリアの戦闘を固定の時間刻みで進める。オンラインではホストだけがこれを動かし、結果を全員へ送る（§8）。
    /// 表示・音はここで行わず、イベントを外へ出す
    /// </summary>
    public sealed partial class StageWorld
    {
        private readonly List<Unit> _units = new List<Unit>();
        private readonly List<Projectile> _projectiles = new List<Projectile>();
        private readonly Random _random;
        private int _nextId = 1;

        public StageWorld(Vec2 areaMin, Vec2 areaMax, int seed)
        {
            AreaMin = areaMin;
            AreaMax = areaMax;
            _random = new Random(seed);
        }

        public event Action<HitEvent> Hit;
        public event Action<DefeatedEvent> Defeated;
        public event Action<SkillUsedEvent> SkillUsed;

        public Vec2 AreaMin { get; }
        public Vec2 AreaMax { get; }
        public IReadOnlyList<Unit> Units => _units;
        public IReadOnlyList<Projectile> Projectiles => _projectiles;
        public int TickCount { get; private set; }

        public bool IsCleared => !HasAlive(Team.Enemy);
        public bool IsFailed => !HasAlive(Team.Player);

        public Unit AddUnit(Team team, UnitStats stats, Vec2 position, int seat = Unit.NoSeat)
        {
            var unit = new Unit(_nextId++, team, stats, position, seat);
            if (team == Team.Enemy)
            {
                // 敵が全員そろって同時に殴ってこないよう、最初の待ち時間をばらけさせる
                unit.Cooldowns[0] = (float)_random.NextDouble() * stats.Skills[0].Cooldown;
            }

            _units.Add(unit);
            return unit;
        }

        public void SetInput(int unitId, PlayerInput input)
        {
            var unit = FindUnit(unitId);
            if (unit != null)
            {
                unit.Input = input;
            }
        }

        /// <summary>
        /// 倒されたのではなく場から取り除く（オンラインでゲストが抜けたとき）。Defeated は出さない。
        /// Tick の外から呼ぶこと
        /// </summary>
        public void RemoveUnit(int unitId)
        {
            var unit = FindUnit(unitId);
            if (unit != null)
            {
                _units.Remove(unit);
            }
        }

        public Unit FindUnit(int id)
        {
            foreach (var unit in _units)
            {
                if (unit.Id == id)
                {
                    return unit;
                }
            }

            return null;
        }

        public void Tick()
        {
            const float dt = Balance.TickSeconds;
            UpdateCooldowns(dt);
            UpdateActions(dt);
            ApplyKnockback(dt);
            ResolveOverlaps();
            KeepInsideArea();
            UpdateProjectiles(dt);
            RemoveDefeated();
            TickCount++;
        }

        private void UpdateCooldowns(float dt)
        {
            foreach (var unit in _units)
            {
                for (var i = 0; i < unit.Cooldowns.Length; i++)
                {
                    unit.Cooldowns[i] = MathF.Max(0f, unit.Cooldowns[i] - dt);
                }
            }
        }

        private void UpdateActions(float dt)
        {
            // 技で弾が増えることはあっても、ユニットはこのループの中で増減しない（消すのは RemoveDefeated）
            foreach (var unit in _units)
            {
                if (!unit.IsAlive || unit.IsStunned)
                {
                    continue;
                }

                if (unit.Team == Team.Player)
                {
                    UpdatePlayer(unit, dt);
                }
                else
                {
                    UpdateEnemy(unit, dt);
                }
            }
        }

        private void UpdatePlayer(Unit unit, float dt)
        {
            var move = unit.Input.Move.ClampLength(1f);
            MoveBy(unit, move, dt);

            for (var slot = 0; slot < unit.Skills.Length; slot++)
            {
                if (unit.Input.IsSkillHeld(slot))
                {
                    TryUseSkill(unit, slot, faceNearestWhenIdle: move.IsNearlyZero);
                }
            }
        }

        private void RemoveDefeated()
        {
            for (var i = _units.Count - 1; i >= 0; i--)
            {
                var unit = _units[i];
                if (unit.IsAlive)
                {
                    continue;
                }

                _units.RemoveAt(i);
                Defeated?.Invoke(new DefeatedEvent(unit.Id, unit.Team, unit.Position));
            }

            _projectiles.RemoveAll(p => p.IsDead);
        }

        public int CountAlive(Team team)
        {
            var count = 0;
            foreach (var unit in _units)
            {
                if (unit.Team == team && unit.IsAlive)
                {
                    count++;
                }
            }

            return count;
        }

        private bool HasAlive(Team team) => CountAlive(team) > 0;

        private Unit FindNearestOpponent(Unit from)
        {
            Unit nearest = null;
            var nearestSq = float.MaxValue;
            foreach (var other in _units)
            {
                if (other.Team == from.Team || !other.IsAlive)
                {
                    continue;
                }

                var sq = (other.Position - from.Position).LengthSquared;
                if (sq < nearestSq)
                {
                    nearestSq = sq;
                    nearest = other;
                }
            }

            return nearest;
        }
    }
}
