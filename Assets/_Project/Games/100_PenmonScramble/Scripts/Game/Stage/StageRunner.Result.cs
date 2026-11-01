using PenmonScramble.Common.Input;
using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 結果の流れ: 決着の瞬間に記録して保存し、BGM を止めてジングルを鳴らし、結果の画面（ResultScreen）を出す。
    /// ソロは最後の一撃をスローで見せる。見出しが右へ移るのに合わせて、カメラがペンモンへ寄る（StageView.ShowResultStage）。
    /// 攻撃のボタンは、演出の途中なら最後まで飛ばし、出そろった後なら左のボタン（ソロは「もう一度」、協力のホストは「ステージを選ぶ」）
    /// </summary>
    public partial class StageRunner
    {
        // 出そろった直後の連打で、すぐやり直しにならないようにする
        private const float RetryInputDelaySeconds = 0.5f;
        private const int RetryAction = 0;
        // 最後の一撃（倒れて飛んでいく敵・散るコイン）をゆっくり見せる。実時間でこの間スローにしてから、なめらかに戻す
        private const float SlowMotionScale = 0.3f;
        private const float SlowMotionHoldSeconds = 0.5f;
        private const float SlowMotionRecoverSeconds = 0.4f;

        // 結果を出した時刻（実時間）。出していなければ負
        private float _resultStartedAt = -1f;
        private bool _resultCleared;
        private bool _resultStageShown;
        private bool _slowMotion;

        private bool IsShowingResult => _resultStartedAt >= 0f;

        private void UpdateResult(StageSnapshot snapshot, InputManager input)
        {
            if (snapshot.Result == StageResult.Playing)
            {
                // ステージ中のメニューの「やり直す」で始め直したら、新しい挑戦のスナップショットが届いてここに来る
                if (IsShowingResult)
                {
                    _resultStartedAt = -1f;
                    EndSlowMotion();
                    _hud.HideResult();
                }

                return;
            }

            if (!IsShowingResult)
            {
                BeginResult(snapshot);
                return;
            }

            var elapsed = Time.unscaledTime - _resultStartedAt;
            UpdateSlowMotion(elapsed);
            if (elapsed >= ResultScreen.StageCueSeconds)
            {
                ShowResultStage(snapshot);
            }

            if (input.IsActionDown(RetryAction))
            {
                OnResultAction(snapshot);
            }
        }

        private void BeginResult(StageSnapshot snapshot)
        {
            _resultStartedAt = Time.unscaledTime;
            _resultStageShown = false;
            _resultCleared = snapshot.Result == StageResult.Cleared;
            var reward = RecordResult(snapshot, _resultCleared);
            _music = null;
            GameMusic.Stop();
            GameSounds.Play(_resultCleared ? GameSound.Clear : GameSound.Fail);
            BeginSlowMotion();

            _hud.ShowResult(ResultInfoOf(snapshot, reward));
        }

        private ResultInfo ResultInfoOf(StageSnapshot snapshot, StageReward reward)
        {
            var resultNext = NextOnResult;
            var stage = Stages.Get(snapshot.StageNo);
            if (!RushTable.IsRush(snapshot.StageNo))
            {
                var unlocked = StageTable.Get(reward.UnlockedStageNo)?.Name;
                var names = unlocked != null ? new[] { unlocked } : null;
                return new ResultInfo(_resultCleared, resultNext, stage?.Name ?? string.Empty, snapshot.EnemiesDefeated, reward, names);
            }

            var difficulty = DifficultyNames.NameOf(snapshot.Difficulty);
            var title = $"ボスラッシュ {stage?.Name} {difficulty}";
            var rushNames = new System.Collections.Generic.List<string>();
            var next = RushTable.Get(reward.UnlockedStageNo);
            if (next != null)
            {
                rushNames.Add(next.Name);
            }

            if (reward.UnlockedDifficulty.HasValue)
            {
                rushNames.Add($"{stage?.Name} {DifficultyNames.NameOf(reward.UnlockedDifficulty.Value)}");
            }

            return new ResultInfo(_resultCleared, resultNext, title, snapshot.EnemiesDefeated, reward, rushNames.ToArray(), snapshot.ArenaRoundsWon, stage?.ArenaCount ?? 0);
        }

        /// <summary>
        /// ソロは同じステージを「もう一度」。協力はボスラッシュのステージと難易度を選び直したいことが多いので、
        /// ホストが「ステージを選ぶ」で全員を待ちの窓へ戻す（同じものにもう一度挑むときも、待ちの窓でそのまま「開始」）
        /// </summary>
        private ResultNext NextOnResult => _host == null ? ResultNext.None : _link == null ? ResultNext.Retry : ResultNext.ChooseStage;

        private void OnResultNext()
        {
            switch (NextOnResult)
            {
                case ResultNext.Retry:
                    Retry();
                    break;
                case ResultNext.ChooseStage:
                    ReturnToRoom();
                    break;
            }
        }

        /// <summary>
        /// 結果を自分の記録に書いて保存する。協力ではホストのクリアが全員の記録に付く（ボスラッシュも、ゲストが解放していなくても付く）。
        /// 開発用のステージは何も書かない
        /// </summary>
        private static StageReward RecordResult(StageSnapshot snapshot, bool cleared)
        {
            if (Stages.IsDev(snapshot.StageNo))
            {
                return new StageReward(0, 0, 0);
            }

            var record = GameSave.Record;
            var reward = RushTable.IsRush(snapshot.StageNo)
                ? record.RecordRush(snapshot.StageNo, snapshot.Difficulty, cleared, snapshot.ArenaRoundsWon, snapshot.EnemiesDefeated)
                : record.RecordStage(snapshot.StageNo, cleared, snapshot.EnemiesDefeated);
            GameSave.Save();
            return reward;
        }

        /// <summary>一度だけ、カメラをペンモンへ寄せて喜ばせる。ソロは控えの2体も並べる</summary>
        private void ShowResultStage(StageSnapshot snapshot)
        {
            if (_resultStageShown)
            {
                return;
            }

            _resultStageShown = true;
            _view.ShowResultStage(snapshot, _lastPlayerState, _resultCleared, showBench: _link == null);
        }

        private void OnResultAction(StageSnapshot snapshot)
        {
            if (!_hud.IsResultSettled)
            {
                // 飛ばしたら、スローもカメラも最後の姿に合わせる
                _hud.SkipResult();
                EndSlowMotion();
                ShowResultStage(snapshot);
                return;
            }

            if (_hud.ResultSettledFor >= RetryInputDelaySeconds)
            {
                OnResultNext();
            }
        }

        /// <summary>ソロだけ。協力は止めずに続ける（自分の画面だけ時間を変えると、通信の時間とずれるため。メニューと同じ考え）</summary>
        private void BeginSlowMotion()
        {
            if (_link != null)
            {
                return;
            }

            _slowMotion = true;
            Time.timeScale = SlowMotionScale;
        }

        private void UpdateSlowMotion(float elapsed)
        {
            if (!_slowMotion)
            {
                return;
            }

            var t = (elapsed - SlowMotionHoldSeconds) / SlowMotionRecoverSeconds;
            if (t >= 1f)
            {
                EndSlowMotion();
                return;
            }

            Time.timeScale = Mathf.Lerp(SlowMotionScale, 1f, Mathf.Clamp01(t));
        }

        /// <summary>やり直し・タイトルへ・シーンを閉じるときは必ず戻す（スローのまま次の挑戦やタイトルに入らないように）</summary>
        private void EndSlowMotion()
        {
            if (!_slowMotion)
            {
                return;
            }

            _slowMotion = false;
            Time.timeScale = 1f;
        }
    }
}
