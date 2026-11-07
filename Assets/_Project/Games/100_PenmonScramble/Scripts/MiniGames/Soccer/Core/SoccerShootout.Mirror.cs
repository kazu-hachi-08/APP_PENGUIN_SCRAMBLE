namespace PenmonScramble.Soccer.Core
{
    /// <summary>オンラインの写し: ホストは PK戦の今を写し、ゲストは届いた物を書き込む（ゲストは計算しない）</summary>
    public sealed partial class SoccerShootout
    {
        internal SoccerShootoutSnap Capture()
        {
            var snap = new SoccerShootoutSnap
            {
                Phase = Phase,
                PhaseSeconds = PhaseSeconds,
                KickingTeam = KickingTeam,
                Kicker = Kicker,
                Charge01 = Charge01,
                IsCharging = IsCharging,
                KeeperSide = KeeperSide,
                LastResult = LastResult,
                Winner = Winner,
            };
            for (var team = 0; team < SoccerRules.TeamCount; team++)
            {
                snap.Results[team].AddRange(_results[team]);
            }

            return snap;
        }

        internal void Apply(SoccerShootoutSnap snap)
        {
            Phase = snap.Phase;
            PhaseSeconds = snap.PhaseSeconds;
            KickingTeam = snap.KickingTeam == SoccerRules.Blue ? SoccerRules.Blue : SoccerRules.Red;
            Kicker = snap.Kicker >= 0 && snap.Kicker < _world.Players.Count ? snap.Kicker : SoccerBall.NoPlayer;
            Charge01 = snap.Charge01;
            IsCharging = snap.IsCharging;
            KeeperSide = snap.KeeperSide;
            LastResult = snap.LastResult;
            Winner = snap.Winner;
            for (var team = 0; team < SoccerRules.TeamCount; team++)
            {
                _results[team].Clear();
                _results[team].AddRange(snap.Results[team]);
            }
        }
    }
}
