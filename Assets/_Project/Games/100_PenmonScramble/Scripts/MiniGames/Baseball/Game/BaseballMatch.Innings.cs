using System;
using PenmonScramble.Baseball.Core;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 試合の区切り: 打席の始め（投げる子を選ぶ・打者を立たせる）、1球の結果（カウント・走者・点）、
    /// 3アウトの「チェンジ」（攻守を替えて子を並べ直す）、試合の終わり（ゲームセットと、もう一度・もどる）
    /// </summary>
    public partial class BaseballMatch
    {
        private static readonly Color HomeRunColor = new Color(1f, 0.82f, 0.2f);
        private static readonly Color HitColor = new Color(1f, 0.95f, 0.55f);
        private static readonly Color OutColor = new Color(0.62f, 0.82f, 1f);
        private static readonly Color WalkColor = new Color(0.6f, 1f, 0.6f);
        private static readonly Color ChangeColor = new Color(1f, 0.7f, 0.35f);
        private static readonly Color WinColor = new Color(1f, 0.82f, 0.2f);
        private static readonly Color LoseColor = new Color(0.62f, 0.82f, 1f);

        /// <summary>試合が終わって「もどる」を押した（ミニゲーム画面へ戻るのはシーンの入口）</summary>
        public event Action LeaveRequested;

        /// <summary>攻守が替わった（試合の始めも）: 守る側を守備に、打つ側の打者と走者の子を用意し、HUD を打つ番・投げる番に替える</summary>
        private void SetUpHalf()
        {
            var batting = _game.BattingTeam;
            var fielding = _game.FieldingTeam;
            var figures = _view.Figures;
            figures.SetDefense(_teams[fielding], fielding != _humanTeam);
            figures.Runners.Use(_teams[batting], batting != _humanTeam);
            _hud.SetSide(HumanBats);
            ShowScore();
            ResetBoard();
        }

        /// <summary>打席の始め: 守る側が投げる子を選び（CPU はイニングごとに替える。人は札で選べる）、打者を打席に立たせる</summary>
        private void EnterPlate()
        {
            if (HumanBats)
            {
                _game.ChoosePitcher(BaseballAi.ChoosePitcher(_game));
            }

            ShowPitcher();
            var batting = _game.BattingTeam;
            _view.Figures.SetBatter(_teams[batting][_game.CurrentBatter], batting != _humanTeam);
            EnterReady();
        }

        private void ShowPitcher()
        {
            var fielding = _game.FieldingTeam;
            _view.Figures.SetPitcher(_teams[fielding][_game.CurrentPitcher], fielding != _humanTeam);
        }

        /// <summary>人が守る打席の始めだけ、投げる子の札を出す（今の子を光らせる）</summary>
        private void ShowPitcherCards(bool shown)
        {
            if (shown)
            {
                _hud.ShowPitchers(_teams[_humanTeam], _game.CurrentPitcher);
            }
            else
            {
                _hud.HidePitchers();
            }
        }

        /// <summary>投げる子を替える（人が守る打席の、1球目を投げる前だけ）</summary>
        private void ChoosePitcher(int slot)
        {
            if (_stage != Stage.Ready || HumanBats || !_game.IsPlateStart || slot == _game.CurrentPitcher)
            {
                return;
            }

            _game.ChoosePitcher(slot);
            ShowPitcher();
            ShowPitcherCards(true);
        }

        /// <summary>1球の結果を試合に入れて、大きな文字・音・子の反応・走者の動き・点を出す</summary>
        private void ShowOutcome(BaseballPitchOutcome outcome)
        {
            var play = _game.Apply(outcome);
            var plateLabel = BaseballResults.Label(play.Plate);
            var text = plateLabel.Length > 0 ? plateLabel : BaseballResults.Label(outcome);
            if (play.Runs > 0)
            {
                text += $"\n<size=72>{play.Runs}点！</size>";
            }

            _timer = play.PlateEnded ? _plateEndSeconds : _resultSeconds;
            _hud.ShowBanner(text, BannerColor(outcome, play.Plate), _timer);
            ReactToPlay(outcome, play);
            BaseballSoundCues.PlayOutcome(outcome, play.Plate);
            if (play.Runs > 0)
            {
                BaseballSoundCues.PlayRuns();
            }

            ShowScore();
            _stage = Stage.Result;
        }

        /// <summary>子の反応と走者の動き（塁に出た打者は、打席の子から走者の子へ入れ替わって走る）</summary>
        private void ReactToPlay(BaseballPitchOutcome outcome, BaseballGamePlay play)
        {
            var figures = _view.Figures;
            var reachedBase = play.Plate == BaseballPlateResult.Hit || play.Plate == BaseballPlateResult.Walk;
            if (!reachedBase)
            {
                figures.React(outcome, play.Plate);
            }
            else
            {
                figures.PitcherReact(false, play.Plate == BaseballPlateResult.Hit);
            }

            if (_game.LastMoves.Count > 0)
            {
                var batterFrom = figures.BatterPosition;
                if (reachedBase)
                {
                    figures.HideBatter();
                }

                figures.Runners.Apply(_game.LastMoves, batterFrom, play.Plate == BaseballPlateResult.Walk);
            }
        }

        private static Color BannerColor(BaseballPitchOutcome outcome, BaseballPlateResult plate)
        {
            switch (plate)
            {
                case BaseballPlateResult.Hit:
                    return outcome.Hit == BaseballHitResult.HomeRun ? HomeRunColor : HitColor;
                case BaseballPlateResult.Out:
                case BaseballPlateResult.Strikeout:
                    return OutColor;
                case BaseballPlateResult.Walk:
                    return WalkColor;
                default:
                    return Color.white;
            }
        }

        private void ShowScore()
        {
            _hud.ShowCount(_game.Count);
            _hud.ShowScore(_game);
        }

        /// <summary>3アウト: 「チェンジ」を見せてから攻守を替える</summary>
        private void EnterChange()
        {
            _stage = Stage.Change;
            _timer = _changeSeconds;
            _hud.ShowBanner(_game.IsPractice ? "3アウト" : "チェンジ", ChangeColor, _timer);
        }

        private void StepChange()
        {
            _timer -= BaseballRules.StepSeconds;
            if (_timer > 0f)
            {
                return;
            }

            _game.StartNextHalf();
            SetUpHalf();
            EnterPlate();
        }

        /// <summary>ゲームセット: 勝ち負けと点を出し、もう一度・もどるを待つ（結果と報酬の欄はフェーズ 4）</summary>
        private void EnterOver()
        {
            _stage = Stage.Over;
            var mine = _game.Score(_humanTeam);
            var theirs = _game.Score(1 - _humanTeam);
            var winner = _game.Winner;
            var verdict = winner < 0 ? "引き分け" : winner == _humanTeam ? "勝ち！" : "負け…";
            var reason = _game.EndedByMercy ? "（点差で早く終わりました）" : string.Empty;
            var color = winner < 0 ? Color.white : winner == _humanTeam ? WinColor : LoseColor;
            _hud.ShowEnd($"ゲームセット\n<size=110>{verdict}  {mine} − {theirs}</size>\n<size=40>{reason}</size>", color);
            BaseballSoundCues.PlayGameOver(winner == _humanTeam);
        }

        /// <summary>もう一度: 試合なら先攻と後攻を入れ替えて始め直す</summary>
        private void Retry()
        {
            if (_stage != Stage.Over)
            {
                return;
            }

            _mode = _mode switch
            {
                BaseballPlayMode.MatchAway => BaseballPlayMode.MatchHome,
                BaseballPlayMode.MatchHome => BaseballPlayMode.MatchAway,
                _ => _mode,
            };
            StartGame();
        }

        private void Leave() => LeaveRequested?.Invoke();
    }
}
