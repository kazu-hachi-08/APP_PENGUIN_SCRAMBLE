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
    /// 試しのピッチ（Docs/MINIGAME_SOCCER_PLAN.md フェーズ 3）。自分の編成の3体（赤）対 CPU の3体（青。海岸の野生のペンモン）の 3対3。
    /// 動かすのは1体で、残りは AI（SoccerAi）。動かす子は SoccerControl が切り替える（自動・パスの受け手・交代ボタン）。
    /// 計算は SoccerWorld を 1/30 秒の固定の刻みで進め、描くときは前の歩との間を補間する（刻みが粗くてもなめらかに見えるように）。
    /// 点が入ったら少し待ってキックオフに戻す（時間・試合の終わり・PK戦はフェーズ 4 の SoccerMatch で作る）
    /// </summary>
    public class SoccerTestPitch : MonoBehaviour
    {
        private const string GoalMessage = "ゴール！";
        private const string KickoffMessage = "キックオフ！";
        private const int StadiumWorld = 1;
        private const int FallbackSpecies = 1;

        // InputManager の Action の番号（J / × ・K / ○ ・Space / L1）
        private const int ShootAction = 0;
        private const int PassAction = 1;
        private const int SwitchAction = 3;

        [SerializeField] private SoccerView _view;
        [SerializeField] private SoccerCamera _camera;
        [SerializeField] private SoccerHud _hud;
        [Tooltip("点が入ってからキックオフに戻すまでの秒")]
        [SerializeField] private float _goalPauseSeconds = 2.5f;
        [SerializeField] private float _bannerSeconds = 1.6f;
        [Tooltip("CPU のチームの強さ（★1〜★5。スタジアム1は ★1）")]
        [Range(SoccerBalance.AiMinStars, SoccerBalance.AiMaxStars)]
        [SerializeField] private int _cpuStars = 1;

        private readonly List<SoccerEvent> _events = new List<SoccerEvent>();
        private SoccerInput[] _inputs;
        private int[] _species;
        private SoccerWorld _world;
        private SoccerAi _ai;
        private SoccerControl _control;
        private SoccerFrame _previous;
        private SoccerFrame _current;
        private float _accumulator;
        private float _kickoffTimer;
        private int _nextKickoffTeam;
        private int _shownControlled = -1;
        private bool _switchQueued;

        private void Awake()
        {
            enabled = false;
        }

        public void Begin()
        {
            var seed = Environment.TickCount;
            var perTeam = SoccerRules.PlayersPerTeam;
            _world = new SoccerWorld(SoccerPitchTable.Standard(), perTeam, perTeam, keepers: true, seed: seed);
            _ai = new SoccerAi(_world, seed + 1);
            _ai.SetLevel(SoccerRules.Red, SoccerBalance.AiLevel(SoccerBalance.TeammateAiStars));
            _ai.SetLevel(SoccerRules.Blue, SoccerBalance.AiLevel(_cpuStars));
            _control = new SoccerControl(SoccerRules.Red, 0);
            _inputs = new SoccerInput[_world.Players.Count];
            _species = TeamSpecies(seed);

            _view.Build(_world, StadiumWorld, _species, SoccerRules.Blue);
            ShowControlled();
            _camera.ShowPitch(_world.Pitch);
            _current = SoccerFrame.Capture(_world);
            _previous = _current;
            _hud.SetPlaying(true);
            _hud.SetScore(0, 0);
            _hud.ShowBanner(KickoffMessage, _bannerSeconds);
            SoccerSounds.Play(SoccerSound.Whistle);
            enabled = true;
        }

        /// <summary>赤は編成の3体（足りなければ1番で埋める）、青は海岸の野生のペンモンから乱数で3体</summary>
        private static int[] TeamSpecies(int seed)
        {
            var perTeam = SoccerRules.PlayersPerTeam;
            var species = new int[perTeam * SoccerRules.TeamCount];
            var party = GameSave.Record.PartyPenguins();
            for (var i = 0; i < perTeam; i++)
            {
                species[i] = i < party.Count ? party[i].speciesNo : FallbackSpecies;
            }

            var wild = StageTable.Get(1).Areas[0].Waves[0].SpeciesPool;
            var random = new System.Random(seed);
            for (var i = 0; i < perTeam; i++)
            {
                species[perTeam + i] = wild[random.Next(wild.Count)];
            }

            return species;
        }

        private void Update()
        {
            var raw = ReadInput();
            _switchQueued |= InputManager.HasInstance && InputManager.Instance.IsActionDown(SwitchAction);
            _accumulator += Time.deltaTime;
            while (_accumulator >= SoccerRules.StepSeconds)
            {
                _accumulator -= SoccerRules.StepSeconds;
                StepWorld(raw);
            }

            var t = _accumulator / SoccerRules.StepSeconds;
            _view.Draw(_world, _previous, _current, t, Time.deltaTime);
            _camera.Follow(Vector3.Lerp(_previous.Ball, _current.Ball, t), Time.deltaTime);
            _hud.SetTime(_world.Time);
            _hud.ShowControls(_world, _control, _species);
        }

        private static SoccerInput ReadInput()
        {
            if (!InputManager.HasInstance)
            {
                return SoccerInput.None;
            }

            var input = InputManager.Instance;
            var move = input.MoveVector;
            return new SoccerInput(new Vec2(move.x, move.y), input.IsActionHeld(ShootAction), input.IsActionHeld(PassAction));
        }

        /// <summary>動かす子を決め（前の歩で起きたことを見る）、AI の子の入力を決めてから1歩進める</summary>
        private void StepWorld(SoccerInput raw)
        {
            var human = _control.Update(_world, raw, _switchQueued, _events);
            _switchQueued = false;
            _ai.Think(_world, _inputs, index => index == _control.Controlled);
            _inputs[_control.Controlled] = human;
            ShowControlled();

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

        private void ShowControlled()
        {
            if (_control.Controlled == _shownControlled)
            {
                return;
            }

            _shownControlled = _control.Controlled;
            _view.SetControlled(_shownControlled, MiniGameSeats.ColorOf(0));
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
