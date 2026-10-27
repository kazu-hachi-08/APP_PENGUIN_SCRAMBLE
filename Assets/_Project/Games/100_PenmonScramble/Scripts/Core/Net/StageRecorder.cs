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

        public StageRecorder(StageWorld world, int round)
        {
            _world = world;
            _round = round;
            _world.Hit += _hits.Add;
            _world.Defeated += _defeats.Add;
            _world.SkillUsed += _skillsUsed.Add;
        }

        public void Detach()
        {
            _world.Hit -= _hits.Add;
            _world.Defeated -= _defeats.Add;
            _world.SkillUsed -= _skillsUsed.Add;
        }

        /// <summary>前回の Capture から後に起きたことを持たせる。貯めていたものは空にする</summary>
        public StageSnapshot Capture()
        {
            var snapshot = new StageSnapshot
            {
                Round = _round,
                Tick = _world.TickCount,
                Result = ResultOf(_world),
                AreaMin = _world.AreaMin,
                AreaMax = _world.AreaMax,
            };

            foreach (var unit in _world.Units)
            {
                snapshot.Units.Add(new UnitState(unit.Id, unit.Team, unit.Seat, unit.Position, unit.Facing, unit.Hp, unit.MaxHp, unit.Radius));
            }

            foreach (var projectile in _world.Projectiles)
            {
                snapshot.Projectiles.Add(new ProjectileState(projectile.Id, projectile.Position, projectile.Radius));
            }

            snapshot.Hits.AddRange(_hits);
            snapshot.Defeats.AddRange(_defeats);
            snapshot.SkillsUsed.AddRange(_skillsUsed);
            _hits.Clear();
            _defeats.Clear();
            _skillsUsed.Clear();
            return snapshot;
        }

        /// <summary>最後の敵と最後のプレイヤーが同時に倒れたら、クリアにする（がんばった方を取る）</summary>
        private static StageResult ResultOf(StageWorld world)
        {
            if (world.IsCleared)
            {
                return StageResult.Cleared;
            }

            return world.IsFailed ? StageResult.Failed : StageResult.Playing;
        }
    }
}
