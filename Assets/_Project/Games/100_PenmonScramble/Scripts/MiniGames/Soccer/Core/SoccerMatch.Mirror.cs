namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// オンラインの写し: ホストは試合の流れ（段・時間・勝ち）を写し、ゲストは届いた物を書き込む。
    /// ゲストの写しの試合は Step を呼ばず、ApplySnapshot だけで進む
    /// </summary>
    public sealed partial class SoccerMatch
    {
        /// <summary>ゲストの写し（ホストから届いた物を書き込むだけで、計算はしない）</summary>
        public bool IsMirror { get; private set; }

        /// <summary>
        /// ゲストのみ: 最初のスナップショットから写しの試合を作る。スタジアムがこの版に無い・子の数が合わないときは null
        /// </summary>
        public static SoccerMatch CreateMirror(SoccerSnapshot snapshot)
        {
            var stadium = SoccerStadiumTable.Get(snapshot.Stadium);
            if (stadium == null || snapshot.PerTeam < 1 || snapshot.Species.Length != snapshot.PerTeam * SoccerRules.TeamCount)
            {
                return null;
            }

            var world = SoccerMatchSetup.CreateWorld(stadium.CreatePitch(), snapshot.Species, 0, snapshot.PerTeam);
            var match = new SoccerMatch(world, 0) { IsMirror = true };
            match.ApplySnapshot(snapshot);
            return match;
        }

        /// <summary>この写しの試合に書き込める（子の数が同じ）</summary>
        public bool Fits(SoccerSnapshot snapshot)
        {
            return snapshot.Players != null && snapshot.Players.Length == World.Players.Count
                && snapshot.Keepers != null && snapshot.Keepers.Length == World.Keepers.Count;
        }

        /// <summary>ゲストのみ: 届いた写しを書き込む（子の数が違う写しは無視する）</summary>
        public void ApplySnapshot(SoccerSnapshot snapshot)
        {
            if (!IsMirror || !Fits(snapshot))
            {
                return;
            }

            Phase = snapshot.Phase;
            PhaseSeconds = snapshot.PhaseSeconds;
            TimeLeft = snapshot.TimeLeft;
            Winner = snapshot.Winner;
            WonByShootout = snapshot.WonByShootout;
            World.ApplySnapshot(snapshot);
            if (snapshot.Shootout == null)
            {
                Shootout = null;
                return;
            }

            Shootout ??= new SoccerShootout(World, 0);
            Shootout.Apply(snapshot.Shootout);
        }

        internal void CaptureTo(SoccerSnapshot snapshot)
        {
            snapshot.Phase = Phase;
            snapshot.PhaseSeconds = PhaseSeconds;
            snapshot.TimeLeft = TimeLeft;
            snapshot.Winner = Winner;
            snapshot.WonByShootout = WonByShootout;
        }
    }
}
