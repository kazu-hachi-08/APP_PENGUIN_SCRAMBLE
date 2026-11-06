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
    /// ひとりで1試合（Docs/MINIGAME_SOCCER_PLAN.md フェーズ 4）。自分の編成の3体（赤）対 スタジアムの野生のペンモン3体（青。CPU）。
    /// 動かすのは1体で、残りは AI（SoccerAi）。動かす子は SoccerControl が切り替える（自動・パスの受け手・交代ボタン）。
    /// 試合の流れ（キックオフ・3分・ゴールの間・時間切れ・PK戦）は SoccerMatch を 1/30 秒の固定の刻みで進め、描くときは前の歩との間を補間する。
    /// 起きたことの見せ方は SoccerMatchPlay.Events.cs、結果・記録・メニューは SoccerMatchPlay.Flow.cs
    /// </summary>
    // InputManager がそのフレームの入力を読んだ後、StageMenu が開け閉めした後に動く
    [DefaultExecutionOrder(60)]
    public partial class SoccerMatchPlay : MonoBehaviour
    {
        // InputManager の Action の番号（J / × ・K / ○ ・L / △ ・Space / L1）
        private const int ShootAction = 0;
        private const int PassAction = 1;
        private const int SpecialAction = 2;
        private const int SwitchAction = 3;

        [SerializeField] private SoccerView _view;
        [SerializeField] private SoccerCamera _camera;
        [SerializeField] private SoccerHud _hud;
        [SerializeField] private SpecialCutIn _cutIn;

        private readonly List<SoccerEvent> _events = new List<SoccerEvent>();
        private SoccerInput[] _inputs;
        private int[] _species;
        private SoccerStadiumDef _stadium;
        private SoccerWorld _world;
        private SoccerMatch _match;
        private SoccerAi _ai;
        private SoccerControl _control;
        private SoccerFrame _previous;
        private SoccerFrame _current;
        private float _accumulator;
        private int _shownControlled = -1;
        private bool _switchQueued;
        // 子とボールを並べ直した歩（キックオフ・PK戦の構え）。補間で滑らせずに一瞬で動かす
        private bool _snap;

        private void Awake()
        {
            enabled = false;
            BindFlow();
        }

        /// <summary>試合を始める</summary>
        /// <param name="record">結果を記録に書く（開発用の試しのピッチ・PK戦の確認では書かない）</param>
        /// <param name="shootoutOnly">試合を飛ばして PK戦から始める（開発用）</param>
        public void Begin(SoccerStadiumDef stadium, bool record, bool shootoutOnly = false)
        {
            _stadium = stadium;
            _record = record;
            _shootoutOnly = shootoutOnly;
            StartMatch();
        }

        /// <summary>同じスタジアムで、新しい相手と試合を始める（はじめ・もう一度・試合をやり直す）</summary>
        private void StartMatch()
        {
            var seed = Environment.TickCount;
            _species = TeamSpecies(seed);
            _world = SoccerMatchSetup.CreateWorld(_stadium.CreatePitch(), _species, seed);
            _match = new SoccerMatch(_world, seed + 2);
            _ai = new SoccerAi(_world, seed + 1);
            _ai.SetLevel(SoccerRules.Red, SoccerBalance.AiLevel(SoccerBalance.TeammateAiStars));
            _ai.SetLevel(SoccerRules.Blue, SoccerBalance.AiLevel(_stadium.CpuStars));
            // 必殺シュートは CPU のチームだけ AI が使う（人のチームの AI の味方が、人のためたゲージを使わないように）
            _ai.SetUsesSpecial(SoccerRules.Blue, true);
            _control = new SoccerControl(SoccerRules.Red, 0);
            _inputs = new SoccerInput[_world.Players.Count];
            _accumulator = 0f;
            _shownControlled = -1;
            _switchQueued = false;

            _view.Build(_world, _stadium.World, _species, SoccerRules.Blue);
            _camera.ShowPitch(_world.Pitch);
            _hud.SetPlaying(true);
            _hud.SetScore(0, 0);
            _hud.ShowShootout(null);
            BeginFlow();

            _events.Clear();
            _match.Begin(_events);
            if (_shootoutOnly)
            {
                _match.StartShootoutNow(_events);
            }

            HandleEvents();
            _current = SoccerFrame.Capture(_world);
            _previous = _current;
            ShowControlled();
            enabled = true;
        }

        /// <summary>赤は編成の3体（足りなければ1番で埋める）、青はスタジアムの野生のペンモンから乱数で3体</summary>
        private int[] TeamSpecies(int seed)
        {
            var perTeam = SoccerRules.PlayersPerTeam;
            var species = new int[perTeam * SoccerRules.TeamCount];
            var party = GameSave.Record.PartyPenguins();
            for (var i = 0; i < perTeam; i++)
            {
                species[i] = i < party.Count ? party[i].speciesNo : SoccerMatchSetup.FallbackSpecies;
            }

            var wild = _stadium.PickWild(perTeam, seed);
            Array.Copy(wild, 0, species, perTeam, perTeam);
            return species;
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            var raw = IsMenuOpen ? SoccerInput.None : ReadInput();
            _switchQueued |= !IsMenuOpen && InputManager.HasInstance && InputManager.Instance.IsActionDown(SwitchAction);
            // ひとりでメニューを開いている間は Time.timeScale が 0 なので、試合は進まない
            _accumulator += dt;
            while (_accumulator >= SoccerRules.StepSeconds)
            {
                _accumulator -= SoccerRules.StepSeconds;
                StepMatch(raw);
            }

            var t = _accumulator / SoccerRules.StepSeconds;
            _view.Draw(_world, _previous, _current, t, dt);
            _camera.Follow(Vector3.Lerp(_previous.Ball, _current.Ball, t), dt);
            UpdateHud();
            TickResult(dt);
        }

        private static SoccerInput ReadInput()
        {
            if (!InputManager.HasInstance)
            {
                return SoccerInput.None;
            }

            var input = InputManager.Instance;
            var move = input.MoveVector;
            return new SoccerInput(new Vec2(move.x, move.y), input.IsActionHeld(ShootAction), input.IsActionHeld(PassAction), input.IsActionHeld(SpecialAction));
        }

        /// <summary>
        /// 人の入力を渡す子を決め（試合中は SoccerControl が決める動かす子、PK戦は蹴る子か守る側の子）、
        /// 残りの子の入力を AI が決めてから1歩進める
        /// </summary>
        private void StepMatch(SoccerInput raw)
        {
            var human = SoccerBall.NoPlayer;
            var humanInput = raw;
            switch (_match.Phase)
            {
                case SoccerMatchPhase.Playing:
                    // 動かす子は前の歩で起きたこと（パス・キーパーの投げ）を見て決める
                    humanInput = _control.Update(_world, raw, _switchQueued, _events);
                    human = _control.Controlled;
                    break;
                case SoccerMatchPhase.Shootout:
                    human = _match.Shootout.InputIndex(SoccerRules.Red);
                    break;
            }

            _switchQueued = false;
            _ai.ThinkMatch(_match, _inputs, index => index == human);
            if (human != SoccerBall.NoPlayer)
            {
                _inputs[human] = humanInput;
            }

            _previous = _current;
            _events.Clear();
            _match.Step(_inputs, _events);
            HandleEvents();
            _current = SoccerFrame.Capture(_world);
            if (_snap)
            {
                _previous = _current;
                _snap = false;
            }
        }

        private void UpdateHud()
        {
            ShowControlled();
            _hud.SetTimeLeft(_match.TimeLeft);
            _hud.SetGauges(_world, SoccerRules.Red, _match.IsPlaying);
            _hud.ShowControls(_world, _control, _species);
            var shootout = _match.Shootout;
            _hud.ShowShootout(shootout);
            var aiming = shootout != null && !_match.IsFinished;
            _hud.ShowPenaltyAim(shootout, aiming && shootout.KickingTeam == SoccerRules.Red, aiming && shootout.KeeperTeam == SoccerRules.Red);
        }

        /// <summary>動かしている子の足元に席の色の輪（PK戦では、自分のチームが蹴るときの蹴る子）</summary>
        private void ShowControlled()
        {
            var shootout = _match.Shootout;
            var controlled = shootout == null
                ? _control.Controlled
                : shootout.KickingTeam == SoccerRules.Red && !shootout.IsFinished ? shootout.Kicker : SoccerBall.NoPlayer;
            if (controlled == _shownControlled)
            {
                return;
            }

            _shownControlled = controlled;
            _view.SetControlled(controlled, MiniGameSeats.ColorOf(0));
        }
    }
}
