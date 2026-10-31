using PenmonScramble.Common.Online;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 重さの計測。4人をボット（StageBot）で戦わせ、場面ごとに fps と描画の回数を測って画面に出す。
    /// 場面は「1エリアの敵が一番多い所」と「最後の大ボス戦」。タイトルの流れ（StageRunner）は使わず、ここで直接 Core を進める。
    /// 計測用のビルド（PENMON_BENCHMARK。Tools/PenmonScramble/Benchmark/Build *）と、Editor の Tools/PenmonScramble/Benchmark/Editor で動く。
    /// 配る版では何もしない
    /// </summary>
    public partial class StageBenchmark : MonoBehaviour
    {
        public const string EditorRequestKey = "PenmonScramble.Benchmark.Pending";
        // 計測ビルドをこの引数で起動すると、測り終えたら閉じる（Windows で続けて測るため。結果は Player.log に残る）
        private const string QuitArgument = "-benchmark-quit";

        private const float WarmupSeconds = 2f;
        private const float MeasureSeconds = 20f;
        private const int Seed = 12345;
        // 世界3の敵に負けて場面が軽くならないよう、ボットは強めにする（敵が減る速さは、次の波が来るので変わらない）
        private const int BotPower = 400;
        // 作り直した直後のフレームは組み立ての分だけ長いので、数えない
        private const int SkipFramesAfterRebuild = 3;
        // これより長く止まったフレームは、その場でログに出す（何が起きた瞬間か追えるように）
        private const float HitchSeconds = 0.1f;

        private StageView _view;
        private Scenario[] _scenarios;
        private int _index = -1;
        private StageWorld _world;
        private StageRecorder _recorder;
        private Unit[] _players;
        private int _round;
        private float _time;
        private float _accumulator;
        private int _skipFrames;
        private BenchCounters _counters;

        public bool IsFinished { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInstall()
        {
#if PENMON_BENCHMARK
            Install();
#elif UNITY_EDITOR
            if (UnityEditor.SessionState.GetBool(EditorRequestKey, false))
            {
                UnityEditor.SessionState.SetBool(EditorRequestKey, false);
                Install();
            }
#endif
        }

        private static void Install()
        {
            var view = FindAnyObjectByType<StageView>();
            if (view == null)
            {
                return;
            }

            var runner = FindAnyObjectByType<StageRunner>();
            if (runner != null)
            {
                runner.enabled = false;
            }

            var title = FindAnyObjectByType<ModeSelectPanel>();
            if (title != null)
            {
                title.gameObject.SetActive(false);
            }

            // どこまで速く描けるかを見るため、フレームの上限を外す（ブラウザは画面の書き換えに合わせるしかない）
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Application.platform == RuntimePlatform.WebGLPlayer ? -1 : 1000;

            var benchmark = new GameObject(nameof(StageBenchmark)).AddComponent<StageBenchmark>();
            benchmark._view = view;
        }

        private void Start()
        {
            _scenarios = Scenario.CreateAll();
            _counters = new BenchCounters();
            NextScenario();
        }

        private void OnDestroy()
        {
            _counters?.Dispose();
        }

        private void Update()
        {
            if (IsFinished)
            {
                return;
            }

            var dt = Time.unscaledDeltaTime;
            _time += dt;
            Measure(dt);
            StepWorld(dt);

            if (_time >= WarmupSeconds + MeasureSeconds)
            {
                NextScenario();
            }
        }

        private void Measure(float dt)
        {
            if (_skipFrames > 0)
            {
                _skipFrames--;
                return;
            }

            if (_time < WarmupSeconds)
            {
                return;
            }

            var scenario = _scenarios[_index];
            scenario.Frames.Add(dt);
            _counters.Sample(scenario);
            scenario.SampleUnits(_world.Units.Count);
            scenario.SampleHitch(dt, _time - WarmupSeconds, _world.Units.Count);
            if (dt >= HitchSeconds)
            {
                Debug.Log($"[Benchmark] hitch {dt * 1000f:0} ms at {_time - WarmupSeconds:0.00}s in {scenario.Name}: units {_world.Units.Count}, warnings {_world.Warnings.Count}, gc {scenario.GcCollections}");
            }
        }

        private void StepWorld(float dt)
        {
            // 処理の上限と刻みは StageHost と同じ（計測の結果が本番の進み方と食い違わないように）
            var interpolation = StageHost.StepFixedTicks(ref _accumulator, dt, null, () =>
            {
                foreach (var player in _players)
                {
                    _world.SetInput(player.Id, StageBot.InputFor(_world, player, _time));
                }

                _world.Tick();
                _view.Apply(_recorder.Capture());
            });
            _view.Render(interpolation);

            // 全滅・全部倒したら同じ場面を始め直して、測り続ける
            if (_world.Result != StageResult.Playing)
            {
                StartRound();
            }
        }

        private void NextScenario()
        {
            if (_index >= 0)
            {
                Log(_scenarios[_index]);
            }

            _index++;
            if (_index >= _scenarios.Length)
            {
                IsFinished = true;
                if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), QuitArgument) >= 0)
                {
                    Application.Quit();
                }

                return;
            }

            _time = 0f;
            StartRound();
        }

        private void StartRound()
        {
            var seats = new int[Balance.MaxPlayers];
            var parties = new UnitStats[seats.Length][];
            for (var i = 0; i < seats.Length; i++)
            {
                seats[i] = i;
                parties[i] = BotParty(i, seats.Length);
            }

            _players = TrialStage.Create(_scenarios[_index].Stage, Seed + _round, seats, parties, out _world);
            _round++;
            _recorder = new StageRecorder(_world, _round);
            _accumulator = 0f;
            _skipFrames = SkipFramesAfterRebuild;
            _view.Apply(_recorder.Capture());
            _view.Render(1f);
        }

        private static UnitStats[] BotParty(int playerIndex, int playerCount)
        {
            var party = TrialStage.RotatingParty(1, playerIndex, playerCount);
            var stats = new UnitStats[party.Length];
            for (var i = 0; i < party.Length; i++)
            {
                stats[i] = SpeciesTable.PlayerStats(party[i], BotPower);
            }

            return stats;
        }
    }
}
