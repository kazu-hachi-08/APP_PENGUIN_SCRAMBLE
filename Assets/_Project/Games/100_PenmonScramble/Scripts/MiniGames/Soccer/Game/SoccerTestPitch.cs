using System.Collections.Generic;
using PenmonScramble.Common.Input;
using PenmonScramble.Core;
using PenmonScramble.Soccer.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// 試しのピッチ（Docs/MINIGAME_SOCCER_PLAN.md フェーズ 1）。仮の見た目の1体を動かして、ボールを運んでゴールへ蹴り込む。
    /// 計算は SoccerWorld を 1/30 秒の固定の刻みで進め、描くときは前の歩との間を補間する（刻みが粗くてもなめらかに見えるように）。
    /// 点が入ったら少し待ってキックオフに戻す（試合の流れはフェーズ 4 の SoccerMatch で作る）
    /// </summary>
    public class SoccerTestPitch : MonoBehaviour
    {
        private const string GoalMessage = "ゴール！";

        [SerializeField] private SoccerTestPitchView _view;
        [Tooltip("点・メッセージ・操作の説明。試しのピッチを始めるまで隠す（「準備中」の板と重ならないように）")]
        [SerializeField] private GameObject _hud;
        [SerializeField] private Text _scoreText;
        [SerializeField] private Text _messageText;
        [Tooltip("点が入ってからキックオフに戻すまでの秒")]
        [SerializeField] private float _goalPauseSeconds = 2f;

        private readonly List<SoccerEvent> _events = new List<SoccerEvent>();
        private readonly SoccerInput[] _inputs = new SoccerInput[1];
        private SoccerWorld _world;
        private SoccerFrame _previous;
        private SoccerFrame _current;
        private float _accumulator;
        private float _kickoffTimer;
        private int _nextKickoffTeam;

        private void Awake()
        {
            _messageText.text = string.Empty;
            _hud.SetActive(false);
            enabled = false;
        }

        public void Begin()
        {
            _world = new SoccerWorld(SoccerPitchTable.Standard(), 1, 0);
            _view.Build(_world);
            _current = SoccerFrame.Capture(_world);
            _previous = _current;
            RefreshScore();
            _hud.SetActive(true);
            enabled = true;
        }

        private void Update()
        {
            _inputs[0] = ReadInput();
            _accumulator += Time.deltaTime;
            while (_accumulator >= SoccerRules.StepSeconds)
            {
                _accumulator -= SoccerRules.StepSeconds;
                StepWorld();
            }

            _view.Draw(_world, _previous, _current, _accumulator / SoccerRules.StepSeconds);
        }

        private static SoccerInput ReadInput()
        {
            if (!InputManager.HasInstance)
            {
                return SoccerInput.None;
            }

            var input = InputManager.Instance;
            var move = input.MoveVector;
            return new SoccerInput(new Vec2(move.x, move.y), input.IsActionHeld(0));
        }

        private void StepWorld()
        {
            _previous = _current;
            _events.Clear();
            _world.Step(_inputs, _events);
            foreach (var e in _events)
            {
                OnEvent(e);
            }

            _current = SoccerFrame.Capture(_world);
            UpdateKickoff();
        }

        /// <summary>点が入ってから少し待ったら、取られた側のキックオフで並び直す（並び直しは一瞬で動かし、補間で滑らせない）</summary>
        private void UpdateKickoff()
        {
            if (!_world.IsGoalScored)
            {
                return;
            }

            _kickoffTimer -= SoccerRules.StepSeconds;
            if (_kickoffTimer > 0f)
            {
                return;
            }

            _world.Kickoff(_nextKickoffTeam, null);
            _messageText.text = string.Empty;
            _current = SoccerFrame.Capture(_world);
            _previous = _current;
        }

        private void OnEvent(SoccerEvent e)
        {
            // 音と動きはフェーズ 2。試しのピッチでは点だけ見せる
            if (e.Kind != SoccerEventKind.Goal)
            {
                return;
            }

            _messageText.text = GoalMessage;
            _kickoffTimer = _goalPauseSeconds;
            _nextKickoffTeam = SoccerRules.Opponent(e.Team);
            RefreshScore();
        }

        private void RefreshScore()
        {
            _scoreText.text = $"赤 {_world.Score(SoccerRules.Red)} - {_world.Score(SoccerRules.Blue)} 青";
        }
    }
}
