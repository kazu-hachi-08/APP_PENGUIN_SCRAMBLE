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
        /// <summary>試しの試合: 自分が先攻（表に打つ）</summary>
        MatchAway,
        /// <summary>試しの試合: 自分が後攻（裏に打つ）</summary>
        MatchHome,
    }

    /// <summary>
    /// 野球の試合（Docs/MINIGAME_BASEBALL_PLAN.md フェーズ 3）。自分の3体 対 CPU の3体で、3イニングの試合を最後まで遊ぶ
    /// （試しの打席は同じ側をくり返すだけで終わらない）。ルール（カウント・走者・点・攻守交代・試合の終わり）は Core の BaseballGame、
    /// 1球は BaseballBoard を 1/30 秒の固定の刻みで進め、描くときは前の歩との間を補間する。記録には何も書かない（結果と報酬はフェーズ 4）。
    /// 1球の流れは BaseballMatch.Flow.cs、入力（人と CPU）は .Input.cs、打席の終わり・攻守交代・試合の終わりは .Innings.cs
    /// </summary>
    public partial class BaseballMatch : MonoBehaviour
    {
        // 試しの球場の相手（CPU）のチーム。ワールド 1 の野生のペンモン（球場1の CPU はフェーズ 4 で球場の表に持たせる）
        private static readonly int[] TestWildSpecies = { 1, 6, 11 };
        private static readonly int[] FallbackParty = { 1, 2, 3 };

        [SerializeField] private BaseballView _view;
        [SerializeField] private BaseballCamera _camera;
        [SerializeField] private BaseballHud _hud;
        [Tooltip("景色のワールド（まわりの地面の絵・フェンスの色）")]
        [SerializeField] private int _world = 1;
        [Tooltip("試しの試合の CPU の強さ（★1〜5。球場1と同じ ★1）")]
        [SerializeField] private int _matchCpuStars = 1;
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
        private BaseballPlayMode _mode;
        private int _humanTeam;
        private BaseballFrame _previous;
        private BaseballFrame _current;
        private float _accumulator;

        /// <summary>この端末の人が打つ番か（そうでなければ投げる番）</summary>
        private bool HumanBats => _game.BattingTeam == _humanTeam;

        private void Awake()
        {
            enabled = false;
            _hud.PitchChosen += OnPitchCardChosen;
            _hud.PitcherChosen += OnPitcherCardChosen;
            _hud.RetryChosen += Retry;
            _hud.LeaveChosen += Leave;
            _hud.gameObject.SetActive(false);
        }

        public void Begin(BaseballPlayMode mode)
        {
            _mode = mode;
            _board = new BaseballBoard(BaseballFieldTable.Test());
            _view.Build(_board.Field, _world);
            StartGame();
            enabled = true;
        }

        /// <summary>
        /// 試合を始める（もう一度のときも）。試しの打席は自分が打つ側・投げる側をくり返し、試合は先攻・後攻を選んだとおりにする
        /// （もう一度は先攻と後攻を入れ替える。ひとりでは試合ごとに交互の案）
        /// </summary>
        private void StartGame()
        {
            _humanTeam = _mode == BaseballPlayMode.MatchHome || _mode == BaseballPlayMode.PracticePitch ? BaseballRules.Home : BaseballRules.Away;
            var cpuTeam = 1 - _humanTeam;
            _teams[_humanTeam] = PartySpecies();
            _teams[cpuTeam] = TestWildSpecies;
            var away = _teams[BaseballRules.Away].Count;
            var home = _teams[BaseballRules.Home].Count;
            var practice = _mode == BaseballPlayMode.PracticeBat || _mode == BaseballPlayMode.PracticePitch;
            _game = practice ? BaseballGame.Practice(away, home, _mode == BaseballPlayMode.PracticeBat ? _humanTeam : cpuTeam) : new BaseballGame(away, home);
            var skill = practice ? BaseballBalance.AiTest : BaseballBalance.AiSkill(_matchCpuStars);
            _ai = new BaseballAi(System.Environment.TickCount, skill);
            _hud.HideEnd();
            _hud.SetTeams(TeamName(BaseballRules.Away), TeamName(BaseballRules.Home), _humanTeam);
            SetUpHalf();
            _camera.Snap();
            EnterPlate();
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
            ReadPitchKeys();
            _accumulator += Time.deltaTime;
            while (_accumulator >= BaseballRules.StepSeconds)
            {
                _accumulator -= BaseballRules.StepSeconds;
                Step();
            }

            ReadSwing();
            var frame = BaseballFrame.Lerp(_previous, _current, _accumulator / BaseballRules.StepSeconds);
            _view.Draw(_board, frame, Time.deltaTime);
            _camera.Follow(CameraTarget());
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
