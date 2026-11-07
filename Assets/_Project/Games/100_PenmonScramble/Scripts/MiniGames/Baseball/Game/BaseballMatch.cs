using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Game;
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

    /// <summary>
    /// 野球の試合（Docs/MINIGAME_BASEBALL_PLAN.md フェーズ 4）。自分の3体 対 球場の野生のペンモン3体（CPU）で、3イニングの試合を最後まで遊ぶ
    /// （試しの打席は同じ側をくり返すだけで終わらない）。ルール（カウント・走者・点・攻守交代・試合の終わり・魔球と必殺打の残り）は Core の BaseballGame、
    /// 1球は BaseballBoard を 1/30 秒の固定の刻みで進め、描くときは前の歩との間を補間する。
    /// 1球の流れは BaseballMatch.Flow.cs、入力（人と CPU）は .Input.cs、魔球・必殺打は .Specials.cs、
    /// 打席の終わり・攻守交代は .Innings.cs、結果・記録・メニューは .Result.cs
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

        private readonly List<BaseballEvent> _events = new List<BaseballEvent>();
        private readonly IReadOnlyList<int>[] _teams = new IReadOnlyList<int>[BaseballRules.TeamCount];
        private BaseballGame _game;
        private BaseballBoard _board;
        private BaseballAi _ai;
        private BaseballStadiumDef _stadium;
        private BaseballPlayMode _mode;
        private int _humanTeam;
        private int _seed;
        private BaseballFrame _previous;
        private BaseballFrame _current;
        private float _accumulator;

        /// <summary>この端末の人が打つ番か（そうでなければ投げる番）</summary>
        private bool HumanBats => _game.BattingTeam == _humanTeam;

        private bool IsPractice => _mode == BaseballPlayMode.PracticeBat || _mode == BaseballPlayMode.PracticePitch;

        private void Awake()
        {
            enabled = false;
            _hud.PitchChosen += OnPitchCardChosen;
            _hud.PitcherChosen += OnPitcherCardChosen;
            _hud.SpecialToggled += ToggleSpecial;
            _hud.gameObject.SetActive(false);
            BindResult();
        }

        /// <param name="stadium">球場（null なら試しの盤。開発用の試しの打席・試しの試合）</param>
        /// <param name="record">結果を記録に書く（開発用の試しの打席・試しの試合では書かない）</param>
        public void Begin(BaseballPlayMode mode, BaseballStadiumDef stadium, bool record)
        {
            _mode = mode;
            _stadium = stadium;
            _record = record && stadium != null;
            _board = new BaseballBoard(stadium != null ? stadium.CreateField() : BaseballFieldTable.Test());
            _view.Build(_board.Field, stadium != null ? stadium.World : _testWorld);
            StartGame();
            enabled = true;
        }

        /// <summary>
        /// 試合を始める（もう一度・試合をやり直すときも）。試しの打席は自分が打つ側・投げる側をくり返し、試合は今の先攻・後攻で始める。
        /// 球場の試合は、相手の3体を球場の野生のペンモンから選び直す
        /// </summary>
        private void StartGame()
        {
            _seed = System.Environment.TickCount;
            _humanTeam = _mode == BaseballPlayMode.MatchHome || _mode == BaseballPlayMode.PracticePitch ? BaseballRules.Home : BaseballRules.Away;
            var cpuTeam = 1 - _humanTeam;
            _teams[_humanTeam] = PartySpecies();
            _teams[cpuTeam] = _stadium != null ? _stadium.PickWild(BaseballStadiumTable.CpuLineup, _seed) : TestWildSpecies;
            var away = _teams[BaseballRules.Away].Count;
            var home = _teams[BaseballRules.Home].Count;
            _game = IsPractice ? BaseballGame.Practice(away, home, _mode == BaseballPlayMode.PracticeBat ? _humanTeam : cpuTeam) : new BaseballGame(away, home);
            _ai = new BaseballAi(_seed, BaseballBalance.AiSkill(CpuStars()));
            _specialRandom = new System.Random(_seed + 1);
            _hud.SetTeams(_teams, _humanTeam, TeamName(BaseballRules.Away), TeamName(BaseballRules.Home));
            BeginResultFlow();
            SetUpHalf();
            _camera.Snap();
            EnterPlate();
        }

        private int CpuStars()
        {
            if (IsPractice)
            {
                return BaseballBalance.AiTestStars;
            }

            return _stadium != null ? _stadium.CpuStars : _testCpuStars;
        }

        private string TeamName(int team) => team == _humanTeam ? "あなた" : "CPU";

        private static IReadOnlyList<int> PartySpecies()
        {
            var species = new List<int>();
            foreach (var penguin in GameSave.Record.PartyPenguins())
            {
                species.Add(penguin.speciesNo);
            }

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

            var frame = BaseballFrame.Lerp(_previous, _current, _accumulator / BaseballRules.StepSeconds);
            _view.Draw(_board, frame, Time.deltaTime);
            _camera.Follow(CameraTarget());
            TickResult(Time.deltaTime);
        }

        /// <summary>打球が出ている間（とその結果を見せる間）だけ球を追う。ホームランは飛んでいく球を追う</summary>
        private Vector3? CameraTarget()
        {
            var inPlay = _board.Phase == BaseballBoardPhase.BallInPlay
                || (_board.Phase == BaseballBoardPhase.Done && _board.LastHit.HasValue);
            return inPlay && _view.BallVisible ? _view.BallPosition : (Vector3?)null;
        }

        private void SnapFrames()
        {
            _current = BaseballFrame.Capture(_board);
            _previous = _current;
        }
    }
}
