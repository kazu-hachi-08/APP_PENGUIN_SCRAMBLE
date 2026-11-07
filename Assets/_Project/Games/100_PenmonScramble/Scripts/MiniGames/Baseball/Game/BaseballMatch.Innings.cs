using PenmonScramble.Baseball.Core;
using UnityEngine;

namespace PenmonScramble.Baseball.Game
{
    /// <summary>
    /// 試合の区切り: 打席の始め（投げる人と投げる子を決める・打者を立たせる）、1球の結果（カウント・走者・点）、
    /// 3アウトの「チェンジ」（攻守を替えて子を並べ直す）。決めるのはひとり・ホストで、見た目はゲストも同じ物を使う
    /// </summary>
    public partial class BaseballMatch
    {
        private static readonly Color HomeRunColor = new Color(1f, 0.82f, 0.2f);
        private static readonly Color HitColor = new Color(1f, 0.95f, 0.55f);
        private static readonly Color OutColor = new Color(0.62f, 0.82f, 1f);
        private static readonly Color WalkColor = new Color(0.6f, 1f, 0.6f);
        private static readonly Color ChangeColor = new Color(1f, 0.7f, 0.35f);
        private static readonly Color HomeRunBandColor = new Color(0.55f, 0.18f, 0.05f, 0.85f);
        private static readonly Color ChangeBandColor = new Color(0.08f, 0.16f, 0.38f, 0.85f);

        /// <summary>攻守が替わった（試合の始めも）: 守る側を守備に、打つ側の打者と走者の子を用意する</summary>
        private void SetUpHalf()
        {
            var batting = _game.BattingTeam;
            var fielding = _game.FieldingTeam;
            var figures = _view.Figures;
            figures.SetDefense(_teams[fielding], _plan.IsCpuTeam(fielding));
            figures.Runners.Use(_teams[batting], _plan.IsCpuTeam(batting), batting == _localTeam);
            ShowScore();
            ResetBoard();
        }

        /// <summary>打席の始め（ひとり・ホスト）: 守る側の投げる人と投げる子を決め（CPU はイニングごとに替える。人は札で選べる）、打者を打席に立たせる</summary>
        private void EnterPlate()
        {
            PickPitchingSeat();
            ShowPlate();
            EnterReady();
        }

        /// <summary>打席の始めの見た目（ゲストも）: 投げる子と打者を立たせ、HUD を自分の番に合わせる</summary>
        private void ShowPlate()
        {
            ResetSpecialToggles();
            ShowPitcher();
            var batting = _game.BattingTeam;
            _view.Figures.SetBatter(_teams[batting][_game.CurrentBatter], _plan.IsCpuTeam(batting));
            _hud.ShowLineups(_game);
            _hud.SetSteerStick(false);
            ShowTurn();
        }

        /// <summary>HUD を、打つ番・投げる番・ほかの人の番に合わせる（説明・スマホのボタン・スティック）</summary>
        private void ShowTurn()
        {
            var batting = LocalTeamBats;
            var mine = batting ? LocalBats : LocalPitches;
            var turnSeat = batting ? BatterSeat : PitcherSeat;
            _hud.SetSide(batting, mine, SeatLabel(turnSeat));
        }

        private void ShowPitcher()
        {
            var fielding = _game.FieldingTeam;
            _view.Figures.SetPitcher(_teams[fielding][_game.CurrentPitcher], _plan.IsCpuTeam(fielding));
        }

        /// <summary>自分が投げる打席の始めだけ、自分の子の札を押して投げる子を替えられる</summary>
        private void ShowPitcherChoice(bool shown)
        {
            var fielding = _game.FieldingTeam;
            _hud.SetPitcherChoice(shown, slot => _plan.CanPitch(_localSeat, fielding, slot));
        }

        /// <summary>投げる子を替える（自分が投げる打席の、1球目を投げる前だけ。ゲストはホストへ知らせて、届いた所で替わる）</summary>
        private void ChoosePitcher(int slot)
        {
            if (_stage != BaseballStage.Ready || !LocalPitches || !_game.IsPlateStart || slot == _game.CurrentPitcher || IsMenuOpen
                || !_plan.CanPitch(_localSeat, _game.FieldingTeam, slot))
            {
                return;
            }

            if (IsGuest)
            {
                RequestPitcher(slot);
                return;
            }

            ApplyPitcher(slot);
        }

        /// <summary>投げる子を替えて見せる（ホストは、ゲストが選んだときもここ）</summary>
        private void ApplyPitcher(int slot)
        {
            _game.ChoosePitcher(slot);
            ShowPitcher();
            _hud.ShowLineups(_game);
            RefreshSpecialToggle();
            NotifyChanged();
        }

        /// <summary>1球の結果を試合に入れて、大きな文字・音・子の反応・走者の動き・点を出す</summary>
        private void ShowOutcome(BaseballPitchOutcome outcome)
        {
            var batterSeat = BatterSeat;
            SpendSpecialHit();
            var play = _game.Apply(outcome);
            _lastPlay = play;
            CountRbi(batterSeat, play.Runs);
            ShowPlay(outcome, play);
        }

        /// <summary>1球の結果の見た目（ゲストは、ホストが決めた結果と試合の様子を写してから呼ぶ）</summary>
        private void ShowPlay(BaseballPitchOutcome outcome, BaseballGamePlay play)
        {
            var plateLabel = BaseballResults.Label(play.Plate);
            var text = plateLabel.Length > 0 ? plateLabel : BaseballResults.Label(outcome);
            if (play.Runs > 0)
            {
                text += $"\n<size=72>{play.Runs}点！</size>";
            }

            var homeRun = play.Plate == BaseballPlateResult.Hit && outcome.Hit == BaseballHitResult.HomeRun;
            _timer = (play.PlateEnded ? _plateEndSeconds : _resultSeconds) + BeginRunnerWatch(homeRun);
            _hud.ShowBanner(text, BannerColor(outcome, play.Plate), _timer, homeRun ? HomeRunBandColor : (Color?)null);
            ReactToPlay(outcome, play);
            // 点が入った音と観客の声は、走者がホームへ還ったときに1人ずつ出す（OnRunnerScored）
            BaseballSoundCues.PlayOutcome(outcome, play.Plate);
            ShowScore();
            _hud.ShowLineups(_game);
            _hud.SetSteerStick(false);
            _stage = BaseballStage.Result;
            RefreshSpecialToggle();
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

                figures.Runners.Apply(_game.LastMoves, batterFrom, play.Plate == BaseballPlateResult.Walk, RunnerStartDelay);
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

        /// <summary>3アウト: 「チェンジ」を帯つきで見せてから攻守を替える</summary>
        private void EnterChange()
        {
            _stage = BaseballStage.Change;
            _timer = _changeSeconds;
            _hud.ShowBanner(_game.IsPractice ? "3アウト" : "チェンジ", ChangeColor, _timer, ChangeBandColor);
            if (!_game.IsPractice)
            {
                BaseballSounds.Play(BaseballSound.Change);
            }

            RefreshSpecialToggle();
            NotifyChanged();
        }

        private void StepChange()
        {
            if (IsGuest)
            {
                return;
            }

            _timer -= BaseballRules.StepSeconds;
            if (_timer > 0f)
            {
                return;
            }

            _game.StartNextHalf();
            SetUpHalf();
            EnterPlate();
        }
    }
}
