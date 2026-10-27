using PenmonScramble.Common.Input;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ソロのステージを進める: 入力を集め、Core を固定の時間刻みで更新し、見た目と HUD に渡す。
    /// フェーズ2で、ホストはこれと同じ流れで全員分の入力を入れるようにする
    /// </summary>
    // InputManager がそのフレームの入力を読んだ後に読む
    [DefaultExecutionOrder(100)]
    public class StageRunner : MonoBehaviour
    {
        // 重い端末で処理が追いつかないときに、追いつこうとして余計に重くならないよう上限を決める
        private const int MaxTicksPerFrame = 5;
        // 結果が出た直後の連打で、すぐやり直しにならないようにする
        private const float RetryInputDelaySeconds = 1f;
        private const int RetryAction = 0;

        [SerializeField] private StageView _view;
        [SerializeField] private StageHud _hud;

        private readonly LocalInputSampler _input = new LocalInputSampler();
        private StageWorld _world;
        private Unit _player;
        private float _accumulator;
        private float _resultShownAt = -1f;

        private bool IsFinished => _world.IsCleared || _world.IsFailed;

        private void Start()
        {
            _hud.RetryRequested += StartStage;
            StartStage();
        }

        private void OnDestroy()
        {
            _hud.RetryRequested -= StartStage;
        }

        private void StartStage()
        {
            _player = TrialStage.Create(System.Environment.TickCount, out _world);
            _view.Bind(_world);
            _hud.HideResult();
            _accumulator = 0f;
            _resultShownAt = -1f;
            UpdateHud();
        }

        private void Update()
        {
            var input = InputManager.Instance;
            _input.Sample(input);

            if (IsFinished)
            {
                UpdateResult(input);
                return;
            }

            RunTicks();
            UpdateHud();
        }

        private void RunTicks()
        {
            _accumulator += Time.deltaTime;
            var ticks = 0;
            while (_accumulator >= Balance.TickSeconds && ticks < MaxTicksPerFrame)
            {
                _world.SetInput(_player.Id, _input.Consume());
                _world.Tick();
                _view.AfterTick();
                _accumulator -= Balance.TickSeconds;
                ticks++;
            }

            // 上限で打ち切った分は捨てる（あとで一気に進んで見えるより、少し遅くなる方が遊びやすい）
            _accumulator = Mathf.Min(_accumulator, Balance.TickSeconds);
            _view.Render(_accumulator / Balance.TickSeconds);
        }

        private void UpdateHud()
        {
            _hud.SetPlayerHp(_player.Hp, _player.MaxHp);
            _hud.SetEnemiesLeft(_world.CountAlive(Team.Enemy));
        }

        private void UpdateResult(InputManager input)
        {
            if (_resultShownAt < 0f)
            {
                _resultShownAt = Time.time;
                _view.Render(1f);
                _hud.ShowResult(_world.IsCleared);
                return;
            }

            if (Time.time - _resultShownAt >= RetryInputDelaySeconds && input.IsActionDown(RetryAction))
            {
                StartStage();
            }
        }
    }
}
