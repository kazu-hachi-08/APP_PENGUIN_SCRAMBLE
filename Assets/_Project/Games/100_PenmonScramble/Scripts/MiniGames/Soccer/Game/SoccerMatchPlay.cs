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
    /// 1試合を遊ぶ。ひとりでは自分の編成の3体（赤）対 スタジアムの野生のペンモン3体（青。CPU）。
    /// みんなではチームの組み方（SoccerTeamPlan）で並べ、ホストが計算してゲストへ配る（SoccerMatchPlay.Online.cs）。
    /// 人が動かす子は席ごとの SoccerControl が切り替え（SoccerHumans）、残りは AI（SoccerAi）。
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
        [Tooltip("みんなでのとき、人が動かしている子の頭の上に出す名前")]
        [SerializeField] private SoccerNameTags _nameTags;

        private readonly List<SoccerEvent> _events = new List<SoccerEvent>();
        private SoccerPlayMode _mode = SoccerPlayMode.Solo;
        private SoccerInput[] _inputs;
        private IReadOnlyList<int> _species;
        private SoccerStadiumDef _stadium;
        private SoccerWorld _world;
        private SoccerMatch _match;
        private SoccerAi _ai;
        private SoccerHumans _humans;
        private SoccerFrame _previous;
        private SoccerFrame _current;
        private float _accumulator;
        private bool _switchQueued;
        // 子とボールを並べ直した歩（キックオフ・PK戦の構え）。補間で滑らせずに一瞬で動かす
        private bool _snap;

        // 席ごと: その歩の入力・交代ボタン・いま印を付ける子・いるチーム（抜けたら −1）
        private SoccerInput[] _seatRaw;
        private bool[] _seatSwitch;
        private int[] _seatShown;
        private int[] _seatTeams;
        private int[] _shownSeats;

        private void Awake()
        {
            enabled = false;
            BindFlow();
        }

        /// <summary>この端末の人のチーム</summary>
        private int OwnTeam => _seatTeams != null && _mode.LocalSeat < _seatTeams.Length && _seatTeams[_mode.LocalSeat] >= 0
            ? _seatTeams[_mode.LocalSeat]
            : SoccerRules.Red;

        /// <summary>ひとりで試合を始める</summary>
        /// <param name="record">結果を記録に書く（開発用の試しのピッチ・PK戦の確認では書かない）</param>
        /// <param name="shootoutOnly">試合を飛ばして PK戦から始める（開発用）</param>
        public void Begin(SoccerStadiumDef stadium, bool record, bool shootoutOnly = false)
        {
            _stadium = stadium;
            _record = record;
            _shootoutOnly = shootoutOnly;
            _mode = SoccerPlayMode.Solo;
            StartSolo();
        }

        /// <summary>同じスタジアムで、新しい相手と試合を始める（はじめ・もう一度・試合をやり直す）</summary>
        private void StartSolo()
        {
            var seed = Environment.TickCount;
            var plan = SoccerTeamPlan.Solo(PartySpecies(), _stadium.PickWild(SoccerRules.PlayersPerTeam, seed));
            StartComputed(plan, seed);
        }

        /// <summary>自分の編成の並び（ひとりの赤・みんなで送る「準備できた」の中身）</summary>
        public static List<int> PartySpecies()
        {
            var species = new List<int>();
            foreach (var penguin in GameSave.Record.PartyPenguins())
            {
                species.Add(penguin.speciesNo);
            }

            return species;
        }

        /// <summary>
        /// この端末で試合を計算する（ひとり・ホスト）。人のいないチームは CPU（スタジアムの強さ。協力では人数で上がる）が、
        /// 人のチームの人が動かしていない子は「ふつう」の AI が動かす。必殺シュートは CPU のチームの AI だけが使う
        /// （人のチームの AI の味方が、人のためたゲージを使わないように）
        /// </summary>
        private void StartComputed(SoccerTeamPlan plan, int seed)
        {
            var world = SoccerMatchSetup.CreateWorld(_stadium.CreatePitch(), plan.Species, seed, plan.PerTeam);
            var match = new SoccerMatch(world, seed + 2);
            _ai = new SoccerAi(world, seed + 1);
            if (plan.CpuTeam >= 0)
            {
                var humans = plan.SeatsOf(SoccerRules.Opponent(plan.CpuTeam)).Count;
                _ai.SetLevel(plan.CpuTeam, SoccerBalance.AiLevel(SoccerBalance.CoopCpuStars(_stadium.CpuStars, humans)));
                _ai.SetUsesSpecial(plan.CpuTeam, true);
            }

            _plan = plan;
            _humans = new SoccerHumans(plan, world.Players.Count);
            _seatTeams = _humans.SeatTeams();
            SetupMatch(match, plan.Species, plan.CpuTeam);

            _events.Clear();
            _match.Begin(_events);
            if (_shootoutOnly)
            {
                _match.StartShootoutNow(_events);
            }

            HandleEvents();
            _current = SoccerFrame.Capture(_world);
            _previous = _current;
            enabled = true;
        }

        /// <summary>見た目・カメラ・HUD・席の入れ物を、この試合に合わせて用意する（計算する側も写しの側も同じ）</summary>
        private void SetupMatch(SoccerMatch match, IReadOnlyList<int> species, int cpuTeam)
        {
            _match = match;
            _world = match.World;
            _species = species;
            _inputs = new SoccerInput[_world.Players.Count];
            _accumulator = 0f;
            _switchQueued = false;
            var seats = _seatTeams.Length;
            _seatRaw = new SoccerInput[seats];
            _seatSwitch = new bool[seats];
            _seatShown = new int[seats];
            _shownSeats = null;
            _seatGoals = new int[seats];
            _kickSeat = new int[_world.Players.Count];
            Array.Fill(_kickSeat, -1);

            _view.Build(_world, _stadium.World, _species, cpuTeam);
            _camera.ShowPitch(_world.Pitch);
            _hud.SetPlaying(true);
            _hud.SetScore(0, 0);
            _hud.ShowShootout(null);
            _hud.HideMvp();
            _nameTags.Clear();
            BeginFlow();
        }

        private void Update()
        {
            var dt = Time.deltaTime;
            var raw = IsMenuOpen ? SoccerInput.None : ReadInput();
            var switchDown = !IsMenuOpen && InputManager.HasInstance && InputManager.Instance.IsActionDown(SwitchAction);
            if (_mode.IsGuest)
            {
                TickGuest(raw, switchDown, dt);
            }
            else
            {
                _switchQueued |= switchDown;
                // ひとりでメニューを開いている間は Time.timeScale が 0 なので、試合は進まない
                _accumulator += dt;
                while (_accumulator >= SoccerRules.StepSeconds)
                {
                    _accumulator -= SoccerRules.StepSeconds;
                    StepMatch(raw);
                }
            }

            var t = _mode.IsGuest ? GuestBlend : _accumulator / SoccerRules.StepSeconds;
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
        /// 席ごとの入力（この端末の人は手元、ゲストはホストに届いたもの）を、人の動かす子へ渡し（SoccerHumans）、
        /// 残りの子の入力を AI が決めてから1歩進める
        /// </summary>
        private void StepMatch(SoccerInput raw)
        {
            for (var seat = 0; seat < _seatRaw.Length; seat++)
            {
                if (seat == _mode.LocalSeat)
                {
                    _seatRaw[seat] = raw;
                    _seatSwitch[seat] = _switchQueued;
                    continue;
                }

                _seatRaw[seat] = TakeRemoteInput(seat, out _seatSwitch[seat]);
            }

            _switchQueued = false;
            // 動かす子は前の歩で起きたこと（パス・キーパーの投げ）を見て決める
            _humans.Drive(_match, _seatRaw, _seatSwitch, _events, _inputs);
            _ai.ThinkMatch(_match, _inputs, _humans.IsDriven);

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
            ShowControllers();
            var own = OwnTeam;
            _hud.SetTimeLeft(_match.TimeLeft);
            _hud.SetGauges(_world, own, _match.IsPlaying);
            var control = LocalControl;
            if (control != null)
            {
                _hud.ShowControls(_world, control, _species);
            }

            var shootout = _match.Shootout;
            _hud.ShowShootout(shootout);
            SoccerHumans.ShootoutRole(shootout, _mode.LocalSeat, _seatTeams, out var kicking, out var keeping);
            _hud.ShowPenaltyAim(shootout, kicking, keeping);
        }

        /// <summary>
        /// 人が動かしている子の足元に席の色の輪（PK戦では、蹴る番の人の蹴る子）。みんなでは頭の上に名前も出す
        /// </summary>
        private void ShowControllers()
        {
            for (var seat = 0; seat < _seatShown.Length; seat++)
            {
                _seatShown[seat] = _mode.IsGuest ? GuestShown(seat) : _seatTeams[seat] >= 0 ? _humans.DisplayedBy(_match, seat) : SoccerBall.NoPlayer;
            }

            if (_shownSeats == null || !SameSeats(_shownSeats, _seatShown))
            {
                _shownSeats = (int[])_seatShown.Clone();
                _view.SetControllers(_seatShown);
            }

            if (_mode.IsOnline)
            {
                _nameTags.Sync(_seatShown, _view);
            }
        }

        private static bool SameSeats(int[] a, int[] b)
        {
            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
