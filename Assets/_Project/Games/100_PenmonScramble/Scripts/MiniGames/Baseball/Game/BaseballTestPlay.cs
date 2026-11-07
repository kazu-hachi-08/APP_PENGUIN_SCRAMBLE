using System.Collections.Generic;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Game;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>試しの打席で、この端末の人が打つか投げるか</summary>
    public enum BaseballTestSide
    {
        /// <summary>自分が打つ（CPU が投げる）</summary>
        Bat,
        /// <summary>自分が投げる（CPU が打つ）</summary>
        Pitch,
    }

    /// <summary>
    /// 試しの打席（Docs/MINIGAME_BASEBALL_PLAN.md フェーズ 2）。本番の見た目と操作で、投げる・曲げる・振る・バントを試す。
    /// 自分が打つときは CPU が球種と曲げ方を選んで投げ、自分が投げるときは CPU が読んで振る。カウント（B・S・O）を数え、
    /// 打席が終わったら次の打者へ（3アウトでアウトを戻す。イニング・走者・点はフェーズ 3）。記録には何も書かない。
    /// 計算は BaseballBoard を 1/30 秒の固定の刻みで進め、描くときは前の歩との間を補間する。
    /// 1球の流れは BaseballTestPlay.Flow.cs、入力（人と CPU）は BaseballTestPlay.Input.cs
    /// </summary>
    public partial class BaseballTestPlay : MonoBehaviour
    {
        // 試しの打席の相手（CPU）のチーム。ワールド 1 の野生のペンモン（球場1の CPU はフェーズ 4 で球場の表に持たせる）
        private static readonly int[] TestWildSpecies = { 1, 6, 11 };
        private static readonly int[] FallbackParty = { 1, 2, 3 };

        [SerializeField] private BaseballView _view;
        [SerializeField] private BaseballCamera _camera;
        [SerializeField] private BaseballHud _hud;
        [Tooltip("景色のワールド（まわりの地面の絵・フェンスの色）")]
        [SerializeField] private int _world = 1;
        [Tooltip("CPU が投げるとき、構えてから振りかぶり始めるまでの秒")]
        [SerializeField] private float _cpuReadySeconds = 1f;
        [Tooltip("振りかぶってから球が手を離れるまでの秒（打つ人がタイミングを取る間）")]
        [SerializeField] private float _windUpSeconds = 0.55f;
        [Tooltip("1球の結果を見せてから次の球の構えに戻すまでの秒")]
        [SerializeField] private float _resultSeconds = 1.5f;
        [Tooltip("打席が終わった（三振・四球・打った）ときに結果を見せる秒")]
        [SerializeField] private float _plateEndSeconds = 2.2f;

        private readonly List<BaseballEvent> _events = new List<BaseballEvent>();
        private readonly BaseballCount _count = new BaseballCount();
        private BaseballBoard _board;
        private BaseballAi _ai;
        private BaseballTestSide _side;
        private BaseballFrame _previous;
        private BaseballFrame _current;
        private float _accumulator;
        private IReadOnlyList<int> _offense;
        private int _batterIndex;

        private void Awake()
        {
            enabled = false;
            _hud.PitchChosen += OnPitchCardChosen;
            _hud.gameObject.SetActive(false);
        }

        public void Begin(BaseballTestSide side)
        {
            _side = side;
            _board = new BaseballBoard(BaseballFieldTable.Test());
            _ai = new BaseballAi(System.Environment.TickCount, BaseballBalance.AiTest);
            _view.Build(_board.Field, _world);
            var party = PartySpecies();
            var batting = side == BaseballTestSide.Bat;
            _offense = batting ? party : TestWildSpecies;
            var defense = batting ? (IReadOnlyList<int>)TestWildSpecies : party;
            _view.Figures.SetDefense(defense, batting);
            _view.Figures.SetPitcher(defense[0], batting);
            _batterIndex = 0;
            _view.Figures.SetBatter(_offense[0], !batting);
            _hud.SetSide(batting);
            _hud.ShowCount(_count);
            _camera.Snap();
            SnapFrames();
            EnterReady();
            enabled = true;
        }

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
