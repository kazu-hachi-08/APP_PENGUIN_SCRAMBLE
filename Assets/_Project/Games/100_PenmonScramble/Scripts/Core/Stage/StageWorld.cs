using System;
using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>
    /// ステージの戦闘を固定の時間刻みで進める。オンラインではホストだけがこれを動かし、結果を全員へ送る。
    /// 表示・音はここで行わず、イベントを外へ出す
    /// </summary>
    public sealed partial class StageWorld
    {
        private readonly List<Unit> _units = new List<Unit>();
        private readonly List<Projectile> _projectiles = new List<Projectile>();
        private readonly List<AreaEffect> _areaEffects = new List<AreaEffect>();
        private readonly Random _random;
        private int _nextId = 1;

        public StageWorld(Vec2 areaMin, Vec2 areaMax, int seed)
        {
            AreaMin = areaMin;
            AreaMax = areaMax;
            _random = new Random(seed);
        }

        /// <summary>ステージ定義どおりにエリアを順に進める。敵は波ごとに出し、HP は人数で増やす</summary>
        public StageWorld(StageDef stage, int playerCount, int seed)
            : this(StageLayout.AreaMin(0), StageLayout.AreaMax(0), seed)
        {
            _stage = stage;
            _playerCount = playerCount;
            StartArea(0);
        }

        public event Action<HitEvent> Hit;
        public event Action<DefeatedEvent> Defeated;
        public event Action<SkillUsedEvent> SkillUsed;
        public event Action<SpecialUsedEvent> SpecialUsed;
        public event Action<BurstEvent> Burst;
        public event Action<ComboEvent> Combo;

        /// <summary>いま動ける範囲。出入口が開いている間は、次のエリアまで広がる</summary>
        public Vec2 AreaMin { get; private set; }
        public Vec2 AreaMax { get; private set; }
        public IReadOnlyList<Unit> Units => _units;
        public IReadOnlyList<Projectile> Projectiles => _projectiles;
        public IReadOnlyList<AreaEffect> AreaEffects => _areaEffects;
        public int TickCount { get; private set; }

        /// <summary>倒した敵の数（結果画面に出す）</summary>
        public int EnemiesDefeated { get; private set; }

        /// <summary>全員がダウンした（ソロならダウンした）</summary>
        public bool IsFailed => !HasAlive(Team.Player);

        /// <summary>最後の敵と最後のプレイヤーが同時に倒れたら、クリアにする（がんばった方を取る）</summary>
        public StageResult Result => IsCleared ? StageResult.Cleared : IsFailed ? StageResult.Failed : StageResult.Playing;

        public Unit AddUnit(Team team, UnitStats stats, Vec2 position, int seat = Unit.NoSeat)
        {
            return AddUnit(team, new[] { stats }, position, seat);
        }

        /// <summary>party の先頭の子から場に出る</summary>
        public Unit AddUnit(Team team, IReadOnlyList<UnitStats> party, Vec2 position, int seat = Unit.NoSeat)
        {
            var unit = new Unit(_nextId++, team, party, position, seat);
            if (party[0].BigBoss != null)
            {
                unit.Brain = new BigBossBrain(party[0].BigBoss);
            }

            if (team == Team.Enemy)
            {
                // 敵が全員そろって同時に殴ってこないよう、最初の待ち時間をばらけさせる
                for (var i = 0; i < unit.Cooldowns.Length; i++)
                {
                    unit.Cooldowns[i] = (float)_random.NextDouble() * unit.Skills[i].Cooldown * unit.CooldownScale;
                }
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
            TickCount++;
            // 必殺技のカットインの間は全体を止める（ヒットストップ）。全員の画面で同じだけ止まるよう、計算の側で止める
            if (_hitStopTicks > 0)
            {
                _hitStopTicks--;
                return;
            }

            const float dt = Balance.TickSeconds;
            FirePendingSpecials();
            UpdateTimers(dt);
            UpdateActions(dt);
            ApplyKnockback(dt);
            ResolveChains();
            ResolveOverlaps();
            KeepInsideArea();
            UpdateProjectiles(dt);
            // 弾・範囲の更新の途中では弾や範囲を増やせないので、合体技はここでまとめて出す
            FirePendingCombos();
            UpdateAreaEffects(dt);
            ReplaceOrRemoveFallen();
            UpdateRevives(dt);
            UpdateStage(dt);
        }

        private void UpdateTimers(float dt)
        {
            foreach (var unit in _units)
            {
                // 控えの子の待ち時間も進める。交代した直後に技が出せないと、交代する意味が薄れるため
                foreach (var member in unit.Members)
                {
                    for (var i = 0; i < member.Cooldowns.Length; i++)
                    {
                        member.Cooldowns[i] = MathF.Max(0f, member.Cooldowns[i] - dt);
                    }
                }

                unit.SwitchCooldown = MathF.Max(0f, unit.SwitchCooldown - dt);
                unit.FrozenSeconds = MathF.Max(0f, unit.FrozenSeconds - dt);
                unit.InvincibleSeconds = MathF.Max(0f, unit.InvincibleSeconds - dt);
                unit.SlowSeconds = MathF.Max(0f, unit.SlowSeconds - dt);
                unit.ComboWindowSeconds = MathF.Max(0f, unit.ComboWindowSeconds - dt);
                unit.ComboCooldownSeconds = MathF.Max(0f, unit.ComboCooldownSeconds - dt);
            }
        }

        private void UpdateActions(float dt)
        {
            // 技で弾が増えることはあっても、ユニットはこのループの中で増減しない（消すのは ReplaceOrRemoveFallen）
            foreach (var unit in _units)
            {
                // 動けない間に押した交代が、動けるようになった瞬間に出ないよう、押した瞬間の判定は毎回進める
                var switchPressed = unit.Team == Team.Player && unit.ConsumeSwitchPress();
                if (!unit.IsAlive || unit.IsFrozen)
                {
                    continue;
                }

                if (unit.IsStunned)
                {
                    // 突進中に飛ばされたら、突進は止まる
                    unit.Dash = null;
                    continue;
                }

                if (unit.IsDashing)
                {
                    UpdateDash(unit, dt);
                    continue;
                }

                if (unit.Team == Team.Player)
                {
                    UpdatePlayer(unit, switchPressed, dt);
                }
                else
                {
                    UpdateEnemy(unit, dt);
                }
            }
        }

        private void UpdatePlayer(Unit unit, bool switchPressed, float dt)
        {
            if (switchPressed)
            {
                TrySwitch(unit);
            }

            if (unit.Input.Special && TryUseSpecial(unit))
            {
                return;
            }

            var move = unit.Input.Move.ClampLength(1f);
            MoveBy(unit, move, dt);

            for (var slot = 0; slot < unit.Skills.Length; slot++)
            {
                // 突進が始まったら、同じ更新でもう1つの技は出さない
                if (unit.Input.IsSkillHeld(slot) && !unit.IsDashing)
                {
                    TryUseSkill(unit, slot, faceNearestWhenIdle: move.IsNearlyZero);
                }
            }
        }

        /// <summary>
        /// 場に出ている子が倒れたら、控えがいれば次の子を出す。
        /// プレイヤーは3体とも倒れたらその場でダウンして残り、敵は場から消す
        /// </summary>
        private void ReplaceOrRemoveFallen()
        {
            for (var i = _units.Count - 1; i >= 0; i--)
            {
                var unit = _units[i];
                if (unit.IsAlive || unit.SwitchToNextAlive() || unit.Team == Team.Player)
                {
                    continue;
                }

                _units.RemoveAt(i);
                if (unit.Brain != null)
                {
                    ClearWarnings(unit.Brain);
                }

                EnemiesDefeated++;
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

        private Unit FindNearestOpponent(Unit from) => FindNearestOpponent(from.Team, from.Position);

        /// <param name="team">探す側の陣営。これと違う陣営の、元気なユニットから探す</param>
        private Unit FindNearestOpponent(Team team, Vec2 position)
        {
            Unit nearest = null;
            var nearestSq = float.MaxValue;
            foreach (var other in _units)
            {
                if (other.Team == team || !other.IsAlive)
                {
                    continue;
                }

                var sq = (other.Position - position).LengthSquared;
                if (sq < nearestSq)
                {
                    nearestSq = sq;
                    nearest = other;
                }
            }

            return nearest;
        }

        private bool IsOpponent(Unit user, Unit target) => target.Team != user.Team && target.IsAlive;
    }
}
