using System;
using System.IO;
using PenmonScramble.Core;
using PenmonScramble.Game;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PenmonScramble.Editor
{
    /// <summary>
    /// StageCapture の Play 中に、ステージを台本どおりに戦わせて撮る。StageHost と同じく固定の時間刻みで Core を進め、
    /// スナップショットを StageView に渡す。プレイヤーは近い敵へ歩いて技を出し続け、ゲージがたまれば必殺技を出す。
    /// 状態の見た目の場面（PLAN_3D フェーズ4）は、スナップショットの状態を書き換えて、凍る・ダウン・交代を決めた時刻に起こす。
    /// Editor のコードは MonoBehaviour としてシーンに付けられないので、EditorApplication.update から1フレームに1回呼ぶ
    /// </summary>
    public sealed class StageCaptureDriver
    {
        private const int Width = 1280;
        private const int Height = 720;
        private const int Seed = 12345;
        // この距離まで近づいたら止まって技を出す（マス）
        private const float AttackDistance = 2f;
        // 技2はときどき押す（ずっと押すと技1が出ない）
        private const float Skill2Period = 1.5f;
        private const float Skill2Hold = 0.2f;

        // 状態の見た目の場面: P2 はずっとダウン、半分の敵はこの間だけ凍る、P1 はこの時刻に交代する
        private const float FreezeFrom = 0.4f;
        private const float FreezeUntil = 2.2f;
        private const float SwitchAt = 1f;
        private const float DownRevive = 0.5f;

        private readonly struct Scene
        {
            public readonly int StageNo;
            public readonly int Players;
            public readonly bool Status;
            public readonly float[] ShotSeconds;

            public Scene(int stageNo, int players, bool status, params float[] shotSeconds)
            {
                StageNo = stageNo;
                Players = players;
                Status = status;
                ShotSeconds = shotSeconds;
            }

            public string FileName(int shot) => Status ? $"status{StageNo}_{shot + 1}.png" : $"stage{StageNo}_{shot + 1}.png";
        }

        // ワールド1は2人で戦いの流れを、ワールド2・3は床の見た目を撮る。最後に状態の見た目
        // （status1_1: P1 の突進の残像・P2 のダウン、_2: 凍る・敵は暗い、_3: 交代の煙、_4: 氷が砕ける・被弾で白く光る）。
        // 突進は台本の戦いで最初に出るもの（種が同じなので毎回同じ時刻）を撮る
        private static readonly Scene[] Scenes =
        {
            new Scene(1, 2, false, 0.1f, 1.2f, 2.5f, 4f, 6f),
            new Scene(4, 1, false, 2f),
            new Scene(7, 1, false, 2f),
            new Scene(1, 2, true, 0.3f, 0.7f, SwitchAt + 0.05f, FreezeUntil + 0.06f),
        };

        private StageView _view;
        private Camera _camera;
        private StageWorld _world;
        private StageRecorder _recorder;
        private Unit[] _players;
        private int _scene = -1;
        private int _shot;
        private int _round;
        private float _sceneTime;
        private float _accumulator;
        private float _lastTime;

        public event Action Finished;

        public bool IsFinished { get; private set; }

        public void Begin()
        {
            _view = Object.FindAnyObjectByType<StageView>();
            _camera = Camera.main;
            // バッチモードの画面の大きさによらず、撮る絵（16:9）に合わせて構えさせる
            _camera.aspect = (float)Width / Height;
            // タイトルの流れ（StageRunner）は使わず、ここで直接進める
            Object.FindAnyObjectByType<StageRunner>().enabled = false;
            NextScene();
        }

        private void NextScene()
        {
            _scene++;
            if (_scene >= Scenes.Length)
            {
                IsFinished = true;
                Finished?.Invoke();
                return;
            }

            var scene = Scenes[_scene];
            var seats = new int[scene.Players];
            var parties = new UnitStats[scene.Players][];
            for (var i = 0; i < seats.Length; i++)
            {
                seats[i] = i;
                parties[i] = TrialStage.PartyStats(TrialStage.RotatingParty(1, i, seats.Length));
            }

            _players = TrialStage.Create(StageTable.Get(scene.StageNo), Seed, seats, parties, out _world);
            _round++;
            _recorder = new StageRecorder(_world, _round);
            // 状態の書き換え（Override）は場面の時刻で決めるので、最初のスナップショットより前に戻す
            _sceneTime = 0f;
            _view.Apply(Override(_recorder.Capture()));
            _view.Render(1f);
            _shot = 0;
            _sceneTime = 0f;
            _accumulator = 0f;
            _lastTime = Time.time;
        }

        /// <summary>
        /// 1フレーム分進める。ここで撮るのは前のフレームの Update・LateUpdate（ペンギンの動き・カメラの移動）が済んだ姿なので、
        /// 先に撮ってから Core を進める
        /// </summary>
        public void Step()
        {
            if (IsFinished)
            {
                return;
            }

            TryShoot();
            if (IsFinished)
            {
                return;
            }

            // update はフレームと1対1とは限らないので、ゲームの時刻の差で進める
            var dt = Time.time - _lastTime;
            _lastTime = Time.time;
            _sceneTime += dt;
            _accumulator += dt;
            while (_accumulator >= Balance.TickSeconds)
            {
                _accumulator -= Balance.TickSeconds;
                foreach (var player in _players)
                {
                    _world.SetInput(player.Id, InputFor(player));
                }

                _world.Tick();
                _view.Apply(Override(_recorder.Capture()));
            }

            _view.Render(_accumulator / Balance.TickSeconds);
        }

        private void TryShoot()
        {
            var scene = Scenes[_scene];
            if (_shot >= scene.ShotSeconds.Length || _sceneTime < scene.ShotSeconds[_shot])
            {
                return;
            }

            Save(scene.FileName(_shot));
            _shot++;
            if (_shot >= scene.ShotSeconds.Length)
            {
                NextScene();
            }
        }

        private PlayerInput InputFor(Unit player)
        {
            // 状態の見た目の場面では、ダウンに見せている P2 は動かさない
            if (Scenes[_scene].Status && player.Seat != 0)
            {
                return new PlayerInput(Vec2.Zero, false, false, false);
            }

            var move = Vec2.Zero;
            if (TryNearestEnemy(player.Position, out var enemy))
            {
                var toEnemy = enemy - player.Position;
                if (toEnemy.Length > AttackDistance)
                {
                    move = toEnemy.Normalized;
                }
            }

            var skill2 = _sceneTime % Skill2Period < Skill2Hold;
            return new PlayerInput(move, skill1: !skill2, skill2: skill2, special: true);
        }

        /// <summary>状態の見た目の場面だけ、スナップショットの状態を書き換える（Core は変えない。見た目だけを確かめるため）</summary>
        private StageSnapshot Override(StageSnapshot snapshot)
        {
            if (!Scenes[_scene].Status)
            {
                return snapshot;
            }

            var enemyIndex = 0;
            var units = snapshot.Units;
            for (var i = 0; i < units.Count; i++)
            {
                var u = units[i];
                var flags = u.Flags;
                var species = u.SpeciesNo;
                var revive = u.Revive;
                if (u.Team == Team.Enemy)
                {
                    if (enemyIndex++ % 2 == 0 && _sceneTime >= FreezeFrom && _sceneTime < FreezeUntil)
                    {
                        flags |= UnitFlags.Frozen;
                    }
                }
                else if (u.Seat == 0)
                {
                    // 交代先は次の番号の種類（どの種類から始まっても、見た目が変わるように）
                    species = _sceneTime >= SwitchAt ? species % SpeciesTable.Count + 1 : species;
                }
                else
                {
                    flags |= UnitFlags.Down;
                    revive = DownRevive;
                }

                units[i] = new UnitState(u.Id, u.Team, u.Seat, species, u.Position, u.Facing, u.Hp, u.MaxHp, u.Radius, flags, revive);
            }

            return snapshot;
        }

        private bool TryNearestEnemy(Vec2 from, out Vec2 nearest)
        {
            nearest = default;
            var best = float.MaxValue;
            foreach (var unit in _world.Units)
            {
                var distance = (unit.Position - from).LengthSquared;
                if (unit.Team == Team.Enemy && distance < best)
                {
                    best = distance;
                    nearest = unit.Position;
                }
            }

            return best < float.MaxValue;
        }

        private void Save(string fileName)
        {
            var target = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 4);
            var previous = _camera.targetTexture;
            _camera.targetTexture = target;
            _camera.Render();
            _camera.targetTexture = previous;
            RenderTexture.active = target;
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(target);
            File.WriteAllBytes(Path.Combine(StageCapture.CaptureFolder, fileName), texture.EncodeToPNG());
            Object.Destroy(texture);
        }
    }
}
