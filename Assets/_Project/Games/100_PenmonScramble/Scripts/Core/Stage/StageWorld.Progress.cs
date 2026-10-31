using System;

namespace PenmonScramble.Core
{
    /// <summary>
    /// ステージの進み方: エリアに入ると出入口が閉じ、敵が何回かに分けて出る。全部倒すと開き、次のエリアへ入るとまた閉じる。
    /// 出入口は「動ける範囲（AreaMin〜AreaMax）」を次のエリアまで広げる・縮めるだけで表す
    /// </summary>
    public sealed partial class StageWorld
    {
        // 敵を出す場所を探す回数。見つからなければ最後に引いた場所に出す（出せずに止まる、を起こさない）
        private const int SpawnTries = 8;
        // 敵はエリアの左からこの割合より右に出す（プレイヤーは左の入口から入ってくるため）
        private const float SpawnMinXRatio = 0.35f;
        // ボスは右の壁からエリアの幅のこの割合だけ内側に出す（入ってきたプレイヤーの正面に見えるように）
        private const float BossXFromRightRatio = 0.25f;

        // null はテスト用の1エリアだけの場（敵は自分で置く）
        private readonly StageDef _stage;
        private readonly int _playerCount = 1;
        private float _waveDelay;

        /// <summary>ステージの番号。テスト用の場は 0</summary>
        public int StageNo => _stage?.No ?? 0;

        /// <summary>何番目のワールドか（1〜）。先のワールドほど敵が強い。テスト用の場は 1</summary>
        public int World => _stage != null ? StageTable.WorldOf(_stage.No) : 1;
        public int AreaIndex { get; private set; }
        public int AreaCount => _stage?.Areas.Count ?? 1;
        public int WaveIndex { get; private set; }
        public bool IsGateOpen { get; private set; }

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
                OpenGate();
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
            AreaMax = StageLayout.AreaMax(AreaIndex + 1);
        }

        /// <summary>誰か1人でも次のエリアへ入ったら始める。遅れた人は、閉じた出入口の内側へ押し出される（KeepInsideArea）</summary>
        private void TryEnterNextArea()
        {
            var entrance = StageLayout.AreaMin(AreaIndex + 1).X + Balance.AreaEnterDepth;
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
            AreaMin = StageLayout.AreaMin(index);
            AreaMax = StageLayout.AreaMax(index);
            _waveDelay = Balance.FirstWaveDelaySeconds;
        }

        private void SpawnWave(WaveDef wave)
        {
            if (wave.BossSpeciesNo > 0)
            {
                SpawnBoss(wave.BossSpeciesNo);
            }

            for (var i = 0; i < wave.Count; i++)
            {
                var species = SpeciesTable.Get(wave.SpeciesPool[_random.Next(wave.SpeciesPool.Count)]);
                AddUnit(Team.Enemy, SpeciesTable.EnemyStats(species, _playerCount, World), FindSpawnPoint());
            }
        }

        private void SpawnBoss(int speciesNo)
        {
            var position = new Vec2(AreaMax.X - Balance.AreaWidth * BossXFromRightRatio, (AreaMin.Y + AreaMax.Y) * 0.5f);
            var bigBoss = BigBossTable.Get(speciesNo);
            var stats = bigBoss != null ? BigBossTable.Stats(bigBoss, _playerCount, World) : SpeciesTable.BossStats(SpeciesTable.Get(speciesNo), _playerCount, World);
            AddUnit(Team.Enemy, stats, position);
        }

        /// <summary>エリアの右寄りで、プレイヤーから離れた場所</summary>
        private Vec2 FindSpawnPoint()
        {
            var minX = AreaMin.X + (AreaMax.X - AreaMin.X) * SpawnMinXRatio;
            var maxX = AreaMax.X - Balance.SpawnWallMargin;
            var minY = AreaMin.Y + Balance.SpawnWallMargin;
            var maxY = AreaMax.Y - Balance.SpawnWallMargin;
            var point = Vec2.Zero;
            for (var i = 0; i < SpawnTries; i++)
            {
                point = new Vec2(minX + (float)_random.NextDouble() * (maxX - minX), minY + (float)_random.NextDouble() * (maxY - minY));
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
