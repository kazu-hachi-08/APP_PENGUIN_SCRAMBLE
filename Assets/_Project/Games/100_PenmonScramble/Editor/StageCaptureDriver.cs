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

        private readonly struct Scene
        {
            public readonly int StageNo;
            public readonly int Players;
            public readonly float[] ShotSeconds;

            public Scene(int stageNo, int players, params float[] shotSeconds)
            {
                StageNo = stageNo;
                Players = players;
                ShotSeconds = shotSeconds;
            }
        }

        // ワールド1は2人で戦いの流れを、ワールド2・3は床の見た目を撮る
        private static readonly Scene[] Scenes =
        {
            new Scene(1, 2, 0.1f, 1.2f, 2.5f, 4f, 6f),
            new Scene(4, 1, 2f),
            new Scene(7, 1, 2f),
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
            _view.Apply(_recorder.Capture());
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
                _view.Apply(_recorder.Capture());
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

            Save($"stage{scene.StageNo}_{_shot + 1}.png");
            _shot++;
            if (_shot >= scene.ShotSeconds.Length)
            {
                NextScene();
            }
        }

        private PlayerInput InputFor(Unit player)
        {
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
