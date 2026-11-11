using PenmonScramble.Baseball.Core;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 走者の見せ場: 塁にいた走者が動く打席は、打球を少し見せてからカメラをダイヤモンドへ戻し、戻ってから走者を走らせる
    /// （打球を追っている間に走者が画面の外で走り終えると、何が起きたか分からないので）。
    /// 走り終えるまで結果を長めに見せ、ホームへ還った子ごとに「+1」と音を出す（BaseballRunners）
    /// </summary>
    public partial class BaseballMatch
    {
        [Tooltip("走者を見せる打席で、打球を見せてからカメラをダイヤモンドへ戻すまでの秒")]
        [SerializeField] private float _runnerWatchBallSeconds = 0.35f;
        [Tooltip("ホームランのときに、飛んでいく球を見せてからダイヤモンドへ戻すまでの秒")]
        [SerializeField] private float _runnerWatchHomeRunSeconds = 1f;
        [Tooltip("カメラがダイヤモンドへ戻り始めてから、塁にいた走者が走り出すまでの秒（戻りきってから走るのが見えるように）")]
        [SerializeField] private float _runnerStartLag = 0.25f;
        [Tooltip("走者を見せる打席で、結果を見せる秒に足す秒（走り終えて「+1」が読めるまで）")]
        [SerializeField] private float _runnerShowExtraSeconds = 0.8f;

        private bool _watchRunners;
        private float _runnerWatchWait;

        /// <summary>この打席の結果で、塁にいた走者が動くか（打者だけが塁に出るなら、見せ場は要らない）</summary>
        private bool RunnersMove()
        {
            foreach (var move in _game.LastMoves)
            {
                if (move.From > 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>走者の見せ場を始める（結果を見せる秒を足して返す。見せ場が無ければ 0）</summary>
        private float BeginRunnerWatch(bool homeRun)
        {
            _watchRunners = RunnersMove();
            if (!_watchRunners)
            {
                _runnerWatchWait = 0f;
                return 0f;
            }

            _runnerWatchWait = homeRun ? _runnerWatchHomeRunSeconds : _runnerWatchBallSeconds;
            return _runnerShowExtraSeconds + (_runnerWatchWait - _runnerWatchBallSeconds);
        }

        /// <summary>塁にいた走者が走り出すまでの秒（カメラがダイヤモンドへ戻ってから）</summary>
        private float RunnerStartDelay => _watchRunners ? _runnerWatchWait + _runnerStartLag : 0f;

        /// <summary>走者を見せる間なら、カメラが見るダイヤモンドの真ん中。打球を見せている間・見せ場が無いなら null</summary>
        private Vector3? RunnerWatchTarget(float dt)
        {
            if (!_watchRunners || _stage != BaseballStage.Result)
            {
                return null;
            }

            _runnerWatchWait -= dt;
            return _runnerWatchWait <= 0f ? _view.Figures.Runners.DiamondCenter : (Vector3?)null;
        }

        /// <summary>走者がホームへ還った: 1人ずつ音を高くして鳴らし、最初の1人で観客がわく</summary>
        private void OnRunnerScored(int index)
        {
            BaseballSounds.PlayScore(index);
            if (index == 0)
            {
                BaseballSoundCues.PlayRuns();
            }
        }
    }
}
