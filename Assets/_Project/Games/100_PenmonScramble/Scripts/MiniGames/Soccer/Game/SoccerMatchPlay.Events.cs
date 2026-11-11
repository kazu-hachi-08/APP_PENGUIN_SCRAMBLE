using PenmonScramble.Soccer.Core;

namespace PenmonScramble.Soccer.Game
{
    /// <summary>
    /// 起きたことの見せ方: 音（SoccerSoundCues）と子・網の反応（SoccerView）に渡し、知らせ・点・カメラ・カットインを出す。
    /// オンラインのゲストは、スナップショットに載って届いたものを同じように見せる
    /// </summary>
    public partial class SoccerMatchPlay
    {
        private const string GoalMessage = "ゴール！";
        private const string KickoffMessage = "キックオフ！";
        private const string TimeUpMessage = "タイムアップ！";
        private const string ShootoutMessage = "PK戦！";
        private const string SaveMessage = "セーブ！";
        private const string MissMessage = "はずれ…";
        private const string SuddenDeathMessage = "サドンデス";
        private const string SpecialHeading = "の必殺シュート！";

        // 知らせを出しておく秒（キックオフ・PK戦の開始・抜けた人の知らせ。ゴールと時間切れは SoccerRules の止まる秒の長さで出す）
        private const float BannerSeconds = 1.6f;

        private void HandleEvents()
        {
            foreach (var e in _events)
            {
                OnEvent(e);
            }
        }

        private void OnEvent(SoccerEvent e)
        {
            CountScorer(e);
            SoccerSoundCues.Play(e);
            _view.OnEvent(e, _world);
            switch (e.Kind)
            {
                case SoccerEventKind.Goal:
                    OnGoal(e);
                    break;
                case SoccerEventKind.Kickoff:
                    _snap = true;
                    _camera.FocusGoal(null);
                    _hud.ShowBanner(KickoffMessage, BannerSeconds);
                    break;
                case SoccerEventKind.Countdown:
                    _hud.PulseTime();
                    break;
                case SoccerEventKind.TimeUp:
                    _hud.ShowBanner(TimeUpMessage, SoccerRules.TimeUpSeconds);
                    break;
                case SoccerEventKind.ShootoutStart:
                    _hud.ShowBanner(ShootoutMessage, BannerSeconds);
                    break;
                case SoccerEventKind.PenaltyReady:
                    OnPenaltyReady(e);
                    break;
                case SoccerEventKind.PenaltyGoal:
                    _hud.ShowBanner(GoalMessage, SoccerBalance.PenaltyResultSeconds);
                    break;
                case SoccerEventKind.PenaltySave:
                    _hud.ShowBanner(SaveMessage, SoccerBalance.PenaltyResultSeconds);
                    break;
                case SoccerEventKind.PenaltyMiss:
                    _hud.ShowBanner(MissMessage, SoccerBalance.PenaltyResultSeconds);
                    break;
                case SoccerEventKind.SpecialCutIn:
                    PlayCutIn(e);
                    break;
                case SoccerEventKind.MatchEnd:
                    OnMatchEnd(e);
                    break;
            }
        }

        private void OnGoal(SoccerEvent e)
        {
            _hud.SetScore(_world.Score(SoccerRules.Red), _world.Score(SoccerRules.Blue));
            _hud.ShowBanner(GoalMessage, SoccerRules.GoalPauseSeconds);
            var goal = _world.Pitch.GoalOf(SoccerRules.Opponent(e.Team));
            _camera.FocusGoal(SoccerSpace.ToWorld(goal.Center));
        }

        /// <summary>PK戦の1本の構え: 蹴る子の後ろからゴールを見て、何本目か（サドンデスならそう）を出す</summary>
        private void OnPenaltyReady(SoccerEvent e)
        {
            _snap = true;
            var shootout = _match.Shootout;
            var goal = _world.Pitch.GoalOf(shootout.KeeperTeam);
            _camera.ShowPenalty(SoccerSpace.ToWorld(e.Position), SoccerSpace.ToWorld(goal.Center));
            var label = shootout.IsSuddenDeath ? SuddenDeathMessage : $"{(int)e.Strength}本目";
            _hud.ShowBanner($"{SoccerColors.TeamName(e.Team)}  {label}", SoccerBalance.PenaltyReadySeconds);
        }

        /// <summary>本編の必殺技と同じ帯に、蹴る子の顔と必殺シュートの名前（カットインの間は Core が全体を止めている）</summary>
        private void PlayCutIn(SoccerEvent e)
        {
            var player = _world.Players[e.Player];
            var species = e.Player >= 0 && e.Player < _species.Count ? _species[e.Player] : SoccerMatchSetup.FallbackSpecies;
            _cutIn.Play(species, player.Element, SpecialHeading, SoccerSpecialNames.Of(player.Element), SoccerBalance.SpecialCutInSeconds);
        }
    }
}
