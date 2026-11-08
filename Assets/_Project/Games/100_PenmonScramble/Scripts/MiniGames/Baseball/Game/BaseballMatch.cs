using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Game;
using PenmonScramble.MiniGames.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>どう遊ぶか（試しの打席は同じ側をくり返す。試合は3イニングで攻守を替える）</summary>
    public enum BaseballPlayMode
    {
        /// <summary>試しの打席: 自分が打ち続ける（CPU が投げる）</summary>
        PracticeBat,
        /// <summary>試しの打席: 自分が投げ続ける（CPU が打つ）</summary>
        PracticePitch,
        /// <summary>試合: 自分が先攻（表に打つ）</summary>
        MatchAway,
        /// <summary>試合: 自分が後攻（裏に打つ）</summary>
        MatchHome,
    }

    /// <summary>この端末が試合をどう受け持つか</summary>
    public enum BaseballNetRole
    {
        /// <summary>ひとりで（開発用の試しの打席・試しの試合も）</summary>
        Solo,
        /// <summary>みんなでのホスト: 試合を計算して全員へ配る</summary>
        Host,
        /// <summary>みんなでのゲスト: ホストから届いた試合を写し、自分の番の入力をホストへ送る</summary>
        Guest,
    }

    /// <summary>
    /// 野球の試合。ひとりでは自分の3体 対 球場の野生のペンモン3体（CPU）、
    /// みんなでは赤・青のチーム（BaseballTeamPlan）で、3イニングの試合を最後まで遊ぶ（試しの打席は同じ側をくり返すだけで終わらない）。
    /// ルール（カウント・走者・点・攻守交代・試合の終わり・魔球と必殺打の残り）は Core の BaseballGame、
    /// 1球は投げ方と2人の入力の記録から BaseballPitchSession が 1/30 秒の固定の刻みで計算し、描くときは前の歩との間を補間する。
    /// 投げる・打つのは、その子の持ち主（この端末の人・ほかの端末の人・CPU）。
    /// 1球の流れは .Flow.cs、入力（人と CPU）は .Input.cs、受け持ちは .Roles.cs、魔球・必殺打は .Specials.cs、
    /// 打席の終わり・攻守交代は .Innings.cs、走者の見せ場は .Runners.cs、結果・記録・メニューは .Result.cs、ホストは .Online.cs、ゲストは .Guest.cs
    /// </summary>
    // InputManager がそのフレームの入力を読んだ後、StageMenu が開け閉めした後に動く
    [DefaultExecutionOrder(60)]
    public partial class BaseballMatch : MonoBehaviour
    {
        // 試しの球場の相手（CPU）のチーム。ワールド 1 の野生のペンモン（球場の試合は球場の表から選ぶ）
        private static readonly int[] TestWildSpecies = { 1, 6, 11 };
        private static readonly int[] FallbackParty = { 1, 2, 3 };

        [SerializeField] private BaseballView _view;
        [SerializeField] private BaseballCamera _camera;
        [SerializeField] private BaseballHud _hud;
        [Tooltip("試しの打席・試しの試合の景色のワールド（球場の試合は球場の景色）")]
        [SerializeField] private int _testWorld = 1;
        [Tooltip("試しの試合の CPU の強さ（★1〜5。球場1と同じ ★1）")]
        [SerializeField] private int _testCpuStars = 1;
        [Tooltip("CPU が投げるとき、構えてから振りかぶり始めるまでの秒")]
        [SerializeField] private float _cpuReadySeconds = BaseballMatchCheck.ReadySeconds;
        [Tooltip("振りかぶってから球が手を離れるまでの秒（打つ人がタイミングを取る間）")]
        [SerializeField] private float _windUpSeconds = BaseballMatchCheck.WindUpSeconds;
        [Tooltip("1球の結果を見せてから次の球の構えに戻すまでの秒")]
        [SerializeField] private float _resultSeconds = BaseballMatchCheck.ResultSeconds;
        [Tooltip("打席が終わった（三振・四球・打った）ときに結果を見せる秒")]
        [SerializeField] private float _plateEndSeconds = BaseballMatchCheck.PlateEndSeconds;
        [Tooltip("「チェンジ」を見せてから攻守を替えるまでの秒")]
        [SerializeField] private float _changeSeconds = BaseballMatchCheck.ChangeSeconds;
        [Tooltip("計算し直して球の位置が跳んだとき、描く位置を寄せる速さ（1秒あたり。大きいほど早く追いつく）")]
        [SerializeField] private float _ballCatchUp = 14f;
        [Tooltip("これより大きく跳んだら寄せずに置き直す（マス）")]
        [SerializeField] private float _ballSnapDistance = 2.5f;

        private readonly List<BaseballEvent> _events = new List<BaseballEvent>();
        private readonly IReadOnlyList<int>[] _teams = new IReadOnlyList<int>[BaseballRules.TeamCount];
        private BaseballGame _game;
        private BaseballPitchSession _session;
        private BaseballTeamPlan _plan;
        private BaseballAi _ai;
        private BaseballStadiumDef _stadium;
        private BaseballPlayMode _mode;
        private BaseballNetRole _role;
        private int _localSeat;
        private int _localTeam;
        private int _cpuStars;
        private int _seed;
        private BaseballFrame _previous;
        private BaseballFrame _current;
        private Vector3 _ballOffset;
        private float _accumulator;

        private BaseballBoard Board => _session.Board;

        private bool IsPractice => _mode == BaseballPlayMode.PracticeBat || _mode == BaseballPlayMode.PracticePitch;

        private bool IsOnline => _role != BaseballNetRole.Solo;

        private bool IsGuest => _role == BaseballNetRole.Guest;

        private bool IsHost => _role == BaseballNetRole.Host;

        private void Awake()
        {
            enabled = false;
            _hud.PitchChosen += OnPitchCardChosen;
            _hud.PitcherChosen += OnPitcherCardChosen;
            _hud.SpecialToggled += ToggleSpecial;
            _hud.gameObject.SetActive(false);
            BindResult();
        }

        /// <summary>ひとりで遊ぶ</summary>
        /// <param name="stadium">球場（null なら試しの盤。開発用の試しの打席・試しの試合）</param>
        /// <param name="record">結果を記録に書く（開発用の試しの打席・試しの試合では書かない）</param>
        public void Begin(BaseballPlayMode mode, BaseballStadiumDef stadium, bool record)
        {
            _mode = mode;
            _role = BaseballNetRole.Solo;
            _localSeat = 0;
            Build(stadium, record);
            StartGame();
            enabled = true;
        }

        private void Build(BaseballStadiumDef stadium, bool record)
        {
            _stadium = stadium;
            _record = record && stadium != null;
            var field = stadium != null ? stadium.CreateField() : BaseballFieldTable.Test();
            _session = new BaseballPitchSession(field);
            _view.Build(field, stadium != null ? stadium.World : _testWorld);
            _view.Figures.Runners.Scored += OnRunnerScored;
        }

        /// <summary>
        /// ひとりで試合を始める（もう一度・試合をやり直すときも）。試しの打席は自分が打つ側・投げる側をくり返し、試合は今の先攻・後攻で始める。
        /// 球場の試合は、相手の3体を球場の野生のペンモンから選び直す
        /// </summary>
        private void StartGame()
        {
            _seed = System.Environment.TickCount;
            var humanTeam = _mode == BaseballPlayMode.MatchHome || _mode == BaseballPlayMode.PracticePitch ? BaseballRules.Home : BaseballRules.Away;
            var wild = _stadium != null ? _stadium.PickWild(BaseballStadiumTable.CpuLineup, _seed) : TestWildSpecies;
            _cpuStars = SoloCpuStars();
            var plan = BaseballTeamPlan.Solo(PartySpecies(), wild, humanTeam);
            var battingTeam = _mode == BaseballPlayMode.PracticeBat ? humanTeam : 1 - humanTeam;
            StartWithPlan(plan, IsPractice ? battingTeam : -1);
        }

        /// <summary>組み方が決まった試合を始める（ひとり・ホスト・ゲスト共通）</summary>
        /// <param name="practiceBattingTeam">試しの打席で打ち続けるチーム（試合は −1）</param>
        private void StartWithPlan(BaseballTeamPlan plan, int practiceBattingTeam)
        {
            _plan = plan;
            _localTeam = Mathf.Max(0, plan.TeamOf(_localSeat));
            for (var team = 0; team < BaseballRules.TeamCount; team++)
            {
                _teams[team] = plan.Lineup(team);
            }

            var away = _teams[BaseballRules.Away].Count;
            var home = _teams[BaseballRules.Home].Count;
            _game = practiceBattingTeam >= 0 ? BaseballGame.Practice(away, home, practiceBattingTeam) : new BaseballGame(away, home);
            _ai = new BaseballAi(_seed, BaseballBalance.AiSkill(_cpuStars));
            _specialRandom = new System.Random(_seed + 1);
            ResetRoles();
            _hud.SetTeams(_teams, _localTeam, TeamName(BaseballRules.Away), TeamName(BaseballRules.Home));
            ShowOwners();
            BeginResultFlow();
            SetUpHalf();
            _camera.Snap();
            if (!_game.IsPractice)
            {
                BaseballSoundCues.PlayBall();
            }

            if (!IsGuest)
            {
                EnterPlate();
            }
        }

        private int SoloCpuStars()
        {
            if (IsPractice)
            {
                return BaseballBalance.AiTestStars;
            }

            return _stadium != null ? _stadium.CpuStars : _testCpuStars;
        }

        /// <summary>この端末の編成のペンモンの種類（空なら仮の3体）</summary>
        public static IReadOnlyList<int> PartySpecies()
        {
            IReadOnlyList<int> species = MiniGameLaunch.PartySpecies();
            return species.Count > 0 ? species : FallbackParty;
        }

        private void Update()
        {
            if (!IsMenuOpen)
            {
                ReadPitchKeys();
            }

            // ひとりでメニューを開いている間は Time.timeScale が 0 なので、試合は進まない
            _accumulator += Time.deltaTime;
            while (_accumulator >= BaseballRules.StepSeconds)
            {
                _accumulator -= BaseballRules.StepSeconds;
                Step();
            }

            if (!IsMenuOpen)
            {
                ReadSwing();
            }

            Draw(Time.deltaTime);
            TickResult(Time.deltaTime);
        }

        /// <summary>前の歩と今の歩の間を補間して描く。計算し直して球が跳んだ分は、少しずつ寄せて消す</summary>
        private void Draw(float dt)
        {
            var alpha = _accumulator / BaseballRules.StepSeconds;
            var frame = BaseballFrame.Lerp(_previous, _current, alpha);
            _ballOffset = Vector3.Lerp(_ballOffset, Vector3.zero, 1f - Mathf.Exp(-_ballCatchUp * dt));
            _view.Draw(Board, frame.WithBallOffset(_ballOffset), dt, ShownClock(alpha));
            _camera.Follow(RunnerWatchTarget(dt) ?? CameraTarget());
        }

        /// <summary>
        /// 描く時点の盤の時計（仕掛け・動く穴の様子）。投げる前は次の球の投げた瞬間の時計で止めておき（仕掛けは投げたら動き出す）、
        /// 球が動いている間は、補間して1歩遅れて描いている球に合わせて戻す
        /// </summary>
        private float ShownClock(float alpha)
        {
            switch (Board.Phase)
            {
                case BaseballBoardPhase.Ready:
                    return BaseballClock.AtThrow(_throwCount + 1);
                case BaseballBoardPhase.Done:
                    return Board.Clock;
                default:
                    return Board.Clock - (1f - alpha) * BaseballRules.StepSeconds;
            }
        }

        /// <summary>打球が出ている間（とその結果を見せる間）だけ球を追う。ホームランは飛んでいく球を追う（走者の見せ場では、途中からダイヤモンドを見る。.Runners.cs）</summary>
        private Vector3? CameraTarget()
        {
            var inPlay = Board.Phase == BaseballBoardPhase.BallInPlay
                || (Board.Phase == BaseballBoardPhase.Done && Board.LastHit.HasValue);
            return inPlay && _view.BallVisible ? _view.BallPosition : (Vector3?)null;
        }

        private void SnapFrames()
        {
            _current = BaseballFrame.Capture(Board);
            _previous = _current;
            _ballOffset = Vector3.zero;
        }

        /// <summary>計算し直したら、跳ぶ前に描いていた所から寄せる（大きく跳んだら置き直す）</summary>
        private void CarryBallOffset(BaseballFrame before)
        {
            var offset = before.Ball + _ballOffset - _current.Ball;
            _ballOffset = offset.magnitude > _ballSnapDistance ? Vector3.zero : offset;
            _previous = _current;
        }
    }
}
