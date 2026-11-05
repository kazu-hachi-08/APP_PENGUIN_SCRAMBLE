using System;
using System.Collections.Generic;
using PenmonScramble.Common.Input;
using PenmonScramble.Core;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Game;
using PenmonScramble.Soccer.Core;
using UnityEngine;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// 試しのピッチ（Docs/MINIGAME_SOCCER_PLAN.md フェーズ 2）。編成の先頭のペンモンを動かして、キーパーのいるゴールへ蹴り込む。
    /// 計算は SoccerWorld を 1/30 秒の固定の刻みで進め、描くときは前の歩との間を補間する（刻みが粗くてもなめらかに見えるように）。
    /// 点が入ったら少し待ってキックオフに戻す（試合の流れはフェーズ 4 の SoccerMatch で作る）
    /// </summary>
    public class SoccerTestPitch : MonoBehaviour
    {
        private const string GoalMessage = "ゴール！";
        private const string KickoffMessage = "キックオフ！";
        private const int StadiumWorld = 1;
        private const int FallbackSpecies = 1;

        [SerializeField] private SoccerView _view;
        [SerializeField] private SoccerCamera _camera;
        [SerializeField] private SoccerHud _hud;
        [Tooltip("点が入ってからキックオフに戻すまでの秒")]
        [SerializeField] private float _goalPauseSeconds = 2.5f;
        [SerializeField] private float _bannerSeconds = 1.6f;

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
            enabled = false;
        }

        public void Begin()
        {
            _world = new SoccerWorld(SoccerPitchTable.Standard(), 1, 0, keepers: true, seed: Environment.TickCount);
            _view.Build(_world, StadiumWorld, new[] { LeadSpecies() });
            _view.SetControlled(0, MiniGameSeats.ColorOf(0));
            _camera.ShowPitch(_world.Pitch);
            _current = SoccerFrame.Capture(_world);
            _previous = _current;
            _hud.SetPlaying(true);
            _hud.SetScore(0, 0);
            _hud.ShowBanner(KickoffMessage, _bannerSeconds);
            SoccerSounds.Play(SoccerSound.Whistle);
            enabled = true;
        }

        /// <summary>編成の先頭のペンモン（記録が読めないときは1番）</summary>
        private static int LeadSpecies()
        {
            foreach (var penguin in GameSave.Record.PartyPenguins())
            {
                return penguin.speciesNo;
            }

            return FallbackSpecies;
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

            var t = _accumulator / SoccerRules.StepSeconds;
            _view.Draw(_world, _previous, _current, t, Time.deltaTime);
            _camera.Follow(Vector3.Lerp(_previous.Ball, _current.Ball, t), Time.deltaTime);
            _hud.SetTime(_world.Time);
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
            UpdateKickoff();
            foreach (var e in _events)
            {
                OnEvent(e);
            }

            _current = SoccerFrame.Capture(_world);
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

            _world.Kickoff(_nextKickoffTeam, _events);
            _previous = SoccerFrame.Capture(_world);
        }

        private void OnEvent(SoccerEvent e)
        {
            SoccerSoundCues.Play(e);
            _view.OnEvent(e, _world);
            switch (e.Kind)
            {
                case SoccerEventKind.Goal:
                    OnGoal(e);
                    break;
                case SoccerEventKind.Kickoff:
                    _camera.FocusGoal(null);
                    _hud.ShowBanner(KickoffMessage, _bannerSeconds);
                    break;
            }
        }

        private void OnGoal(SoccerEvent e)
        {
            _kickoffTimer = _goalPauseSeconds;
            _nextKickoffTeam = SoccerRules.Opponent(e.Team);
            _hud.SetScore(_world.Score(SoccerRules.Red), _world.Score(SoccerRules.Blue));
            _hud.ShowBanner(GoalMessage, _goalPauseSeconds);
            var goal = _world.Pitch.GoalOf(SoccerRules.Opponent(e.Team));
            _camera.FocusGoal(SoccerSpace.ToWorld(goal.Center));
        }
    }
}
