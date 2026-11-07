using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Common.Input;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 試しの打席（Docs/MINIGAME_BASEBALL_PLAN.md フェーズ 1）。CPU が同じ球をまっすぐくり返し投げ、自分は振る・バントだけ。
    /// 計算は BaseballBoard を 1/30 秒の固定の刻みで進め、描くときは前の歩との間を補間する。
    /// 1球の結果を大きな文字で見せたら、少し待って次の球（カウント・走者はフェーズ 3 の BaseballAtBat で作る）
    /// </summary>
    public class BaseballTestAtBat : MonoBehaviour
    {
        [SerializeField] private BaseballTestAtBatView _view;
        [Tooltip("結果・集計・操作の説明。試しの打席を始めるまで隠す（「準備中」の板と重ならないように）")]
        [SerializeField] private GameObject _hud;
        [SerializeField] private Text _messageText;
        [SerializeField] private Text _tallyText;
        [Tooltip("CPU がくり返し投げる球種")]
        [SerializeField] private BaseballPitchType _pitchType = BaseballPitchType.Curve;
        [Tooltip("構えてから投げるまでの秒")]
        [SerializeField] private float _windUpSeconds = 1.2f;
        [Tooltip("結果を見せてから次の球の構えに戻すまでの秒")]
        [SerializeField] private float _resultSeconds = 1.8f;

        private readonly List<BaseballEvent> _events = new List<BaseballEvent>();
        private BaseballBoard _board;
        private BaseballFrame _previous;
        private BaseballFrame _current;
        private float _accumulator;
        private float _timer;
        private bool _swingQueued;
        private int _pitches;
        private int _hits;
        private int _outs;
        private int _homeRuns;

        private void Awake()
        {
            _messageText.text = string.Empty;
            _hud.SetActive(false);
            enabled = false;
        }

        public void Begin()
        {
            _board = new BaseballBoard(BaseballFieldTable.Test());
            _view.Build(_board.Field);
            SnapFrames();
            _timer = _windUpSeconds;
            RefreshTally();
            _hud.SetActive(true);
            enabled = true;
        }

        private void Update()
        {
            ReadSwing();
            _accumulator += Time.deltaTime;
            while (_accumulator >= BaseballRules.StepSeconds)
            {
                _accumulator -= BaseballRules.StepSeconds;
                StepBoard();
            }

            _view.Draw(_board, BaseballFrame.Lerp(_previous, _current, _accumulator / BaseballRules.StepSeconds));
        }

        /// <summary>押した瞬間は描画のフレームで読むので、次の計算の歩まで取っておく（歩の間に押して離しても取りこぼさない）</summary>
        private void ReadSwing()
        {
            if (_board.Phase != BaseballBoardPhase.Pitching)
            {
                // 投げる前に押した分は持ち越さない（構えのうちに押しておけば当たる、にならないように）
                _swingQueued = false;
                return;
            }

            if (InputManager.HasInstance && InputManager.Instance.IsActionDown(0))
            {
                _swingQueued = true;
            }
        }

        private BaseballInput TakeInput()
        {
            var bunt = InputManager.HasInstance && InputManager.Instance.IsActionHeld(1);
            var input = new BaseballInput(0f, _swingQueued, bunt);
            _swingQueued = false;
            return input;
        }

        private void StepBoard()
        {
            _previous = _current;
            _events.Clear();
            switch (_board.Phase)
            {
                case BaseballBoardPhase.Ready:
                    WaitToPitch();
                    break;
                case BaseballBoardPhase.Pitching:
                case BaseballBoardPhase.BallInPlay:
                    _board.Step(TakeInput(), _events);
                    break;
                case BaseballBoardPhase.Done:
                    WaitForNextPitch();
                    return;
            }

            _current = BaseballFrame.Capture(_board);
            if (_board.Phase == BaseballBoardPhase.Done)
            {
                ShowOutcome(_board.Outcome);
            }
        }

        private void WaitToPitch()
        {
            _timer -= BaseballRules.StepSeconds;
            if (_timer > 0f)
            {
                return;
            }

            _messageText.text = string.Empty;
            _board.Throw(_pitchType, _events);
        }

        /// <summary>結果を見せ終えたら構えに戻す（球とバットは一瞬で戻し、補間で滑らせない）</summary>
        private void WaitForNextPitch()
        {
            _timer -= BaseballRules.StepSeconds;
            if (_timer > 0f)
            {
                return;
            }

            _board.Reset();
            SnapFrames();
            _timer = _windUpSeconds;
        }

        private void SnapFrames()
        {
            _current = BaseballFrame.Capture(_board);
            _previous = _current;
        }

        private void ShowOutcome(BaseballPitchOutcome outcome)
        {
            _messageText.text = BaseballResults.Label(outcome);
            _timer = _resultSeconds;
            Count(outcome);
            RefreshTally();
        }

        private void Count(BaseballPitchOutcome outcome)
        {
            _pitches++;
            if (outcome.Kind != BaseballPitchOutcomeKind.InPlay)
            {
                return;
            }

            if (BaseballResults.IsOut(outcome.Hit))
            {
                _outs++;
                return;
            }

            _hits++;
            if (outcome.Hit == BaseballHitResult.HomeRun)
            {
                _homeRuns++;
            }
        }

        private void RefreshTally()
        {
            _tallyText.text = $"{_pitches}球  ヒット {_hits}（ホームラン {_homeRuns}）  アウト {_outs}";
        }
    }
}
