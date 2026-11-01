using System;

namespace PenmonScramble.Core
{
    /// <summary>
    /// ステージの進み方: エリアに入ると出入口が閉じ、敵が何回かに分けて出る。全部倒すと開き、次のエリアへ入るとまた閉じる。
    /// 出入口は「動ける範囲」を次のエリアまで広げる・縮めるだけで表す。次が闘技場なら、前のエリア・通り道・闘技場を合わせた範囲にする。
    /// 闘技場で勝って次のエリアがあるとき（ボスラッシュの連戦）は、全員を少し回復し、ダウンしていた人を起こす（WinRound）
    /// </summary>
    public sealed partial class StageWorld
    {
        // 敵を出す場所を探す回数。見つからなければ最後に引いた場所に出す（出せずに止まる、を起こさない）
        private const int SpawnTries = 8;
        // 敵はエリアの左からこの割合より右に出す（プレイヤーは左の入口から入ってくるため）
        private const float SpawnMinXRatio = 0.35f;
        // ボスは右の壁からエリアの幅のこの割合だけ内側に出す（入ってきたプレイヤーの正面に見えるように）。闘技場では真ん中
        private const float BossXFromRightRatio = 0.25f;
        // ボスが何体か一緒に出るとき（巨大ペンモンの2体組）の縦の間（マス）。出た瞬間に押し合わない広さ
        private const float BossSpacing = 4f;

        // null はテスト用の1エリアだけの場（敵は自分で置く）
        private readonly StageDef _stage;
        private readonly int _playerCount = 1;
        private float _waveDelay;
        private int _roundsWon;

        /// <summary>ステージの番号。テスト用の場は 0</summary>
        public int StageNo => _stage?.No ?? 0;

        /// <summary>何番目のワールドか（1〜）。先のワールドほど敵が強い。テスト用の場は 1</summary>
        public int World => _stage?.World ?? 1;
        public int AreaIndex { get; private set; }
        public int AreaCount => _stage?.Areas.Count ?? 1;
        public int WaveIndex { get; private set; }
        public bool IsGateOpen { get; private set; }

        /// <summary>いまの闘技場が何戦目か（1〜）。闘技場でなければ 0</summary>
        public int ArenaRound => _stage?.ArenaRoundOf(AreaIndex) ?? 0;

        /// <summary>勝った闘技場の戦いの数（結果の「勝った戦い」に使う）。クリアしたら最後の戦いも数える</summary>
        public int ArenaRoundsWon => IsCleared && ArenaRound > 0 ? ArenaRound : _roundsWon;

        private AreaDef CurrentArea => _stage.Areas[AreaIndex];
        private bool IsLastArea => AreaIndex >= AreaCount - 1;
        private bool AllWavesSpawned => _stage == null || WaveIndex >= CurrentArea.Waves.Count;

        /// <summary>最後のエリアの敵を、全部出して全部倒した</summary>
        public bool IsCleared => IsLastArea && AllWavesSpawned && !HasAlive(Team.Enemy);

        private void UpdateStage(float dt)
        {
            if (_stage == null || IsCleared)
            {
                return;
            }

            if (IsGateOpen)
            {
                TryEnterNextArea();
            }
            else if (!AllWavesSpawned)
            {
                UpdateWaves(dt);
            }
            else if (!HasAlive(Team.Enemy))
            {
                if (CurrentArea.IsArena)
                {
                    WinRound();
                }

                OpenGate();
            }
        }

        /// <summary>
        /// 闘技場の1戦に勝った（次の闘技場がある）。元気な子はそれぞれ回復し、ダウンしていた人は先頭の子で起き上がる。
        /// 倒れた子は起こさない（3体の交代で粘った分の得を残すため）
        /// </summary>
        private void WinRound()
        {
            _roundsWon++;
            foreach (var unit in _units)
            {
                if (unit.Team != Team.Player)
                {
                    continue;
                }

                if (unit.IsDown)
                {
                    Revive(unit, Balance.RoundClearReviveRatio);
                    continue;
                }

                unit.HealAliveMembers(Balance.RoundClearHealRatio);
            }
        }

        private void UpdateWaves(float dt)
        {
            _waveDelay = MathF.Max(0f, _waveDelay - dt);
            // 最初の波は出入口が閉じてから少し待つ。2つ目からは、残りが少なくなったらすぐ出す
            var ready = WaveIndex == 0 ? _waveDelay <= 0f : CountAlive(Team.Enemy) <= Balance.NextWaveEnemiesLeft;
            if (ready)
            {
                SpawnWave(CurrentArea.Waves[WaveIndex++]);
            }
        }

        private void OpenGate()
        {
            IsGateOpen = true;
            var next = StageLayout.Area(_stage, AreaIndex + 1);
            if (next.IsEllipse)
            {
                SetBounds(Area, StageLayout.Passage(_stage, AreaIndex + 1), next);
            }
            else
            {
                SetBounds(AreaShape.Rect(Area.Min, next.Max));
            }
        }

        /// <summary>誰か1人でも次のエリアへ入ったら始める。遅れた人は、閉じた出入口の内側へ押し出される（KeepInsideArea）</summary>
        private void TryEnterNextArea()
        {
            var entrance = StageLayout.Area(_stage, AreaIndex + 1).Min.X + Balance.AreaEnterDepth;
            foreach (var unit in _units)
            {
                if (unit.Team == Team.Player && unit.IsAlive && unit.Position.X >= entrance)
                {
                    StartArea(AreaIndex + 1);
                    return;
                }
            }
        }

        private void StartArea(int index)
        {
            AreaIndex = index;
            WaveIndex = 0;
            IsGateOpen = false;
            SetBounds(StageLayout.Area(_stage, index));
            _waveDelay = Balance.FirstWaveDelaySeconds;
        }

        private void SpawnWave(WaveDef wave)
        {
            for (var i = 0; i < wave.Bosses.Count; i++)
            {
                var offset = (i - (wave.Bosses.Count - 1) * 0.5f) * BossSpacing;
                SpawnBoss(wave.Bosses[i], offset);
            }

            for (var i = 0; i < wave.Count; i++)
            {
                var species = SpeciesTable.Get(wave.SpeciesPool[_random.Next(wave.SpeciesPool.Count)]);
                AddEnemy(SpeciesTable.EnemyStats(species, _playerCount, World), FindSpawnPoint());
            }
        }

        /// <param name="offsetY">いくつも出すときの、真ん中からの縦のずれ</param>
        private void SpawnBoss(BossSpawn boss, float offsetY)
        {
            var x = Area.IsEllipse ? Area.Center.X : Area.Max.X - Area.Width * BossXFromRightRatio;
            AddEnemy(BossStatsOf(boss), new Vec2(x, Area.Center.Y + offsetY));
        }

        private UnitStats BossStatsOf(BossSpawn boss)
        {
            var bigBoss = BigBossTable.Get(boss.SpeciesNo);
            if (bigBoss != null)
            {
                return BigBossTable.Stats(bigBoss, _playerCount, World, serious: boss.Rank == BossRank.Serious);
            }

            var species = SpeciesTable.Get(boss.SpeciesNo);
            return boss.Rank == BossRank.Giant ? SpeciesTable.GiantStats(species, _playerCount, World) : SpeciesTable.BossStats(species, _playerCount, World);
        }

        /// <summary>難易度とエリアの強さの倍率を掛けてから場に出す（定義表の数値は「ふつう」・倍率 1 のまま持つ）</summary>
        private void AddEnemy(UnitStats stats, Vec2 position)
        {
            var scale = CurrentArea.EnemyScale * (Difficulty == Difficulty.Extreme ? CurrentArea.ExtremeScale : 1f);
            stats.MaxHp = UnitStats.HpOf(stats.MaxHp * Balance.DifficultyHpScale(Difficulty) * scale);
            stats.Attack *= Balance.DifficultyAttackScale(Difficulty) * scale;
            AddUnit(Team.Enemy, stats, position);
        }

        /// <summary>エリアの右寄りで、プレイヤーから離れた場所</summary>
        private Vec2 FindSpawnPoint()
        {
            var minX = Area.Min.X + Area.Width * SpawnMinXRatio;
            var point = Vec2.Zero;
            for (var i = 0; i < SpawnTries; i++)
            {
                point = Area.RandomPointRightOf(_random, minX, Balance.SpawnWallMargin);
                if (IsFarFromPlayers(point))
                {
                    break;
                }
            }

            return point;
        }

        private bool IsFarFromPlayers(Vec2 point)
        {
            const float minSq = Balance.SpawnMinDistance * Balance.SpawnMinDistance;
            foreach (var unit in _units)
            {
                if (unit.Team == Team.Player && (unit.Position - point).LengthSquared < minSq)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
