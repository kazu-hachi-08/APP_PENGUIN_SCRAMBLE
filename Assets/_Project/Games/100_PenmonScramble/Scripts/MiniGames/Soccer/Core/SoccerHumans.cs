using System.Collections.Generic;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// 人（席）ごとの操作の行き先。試合中は席ごとの SoccerControl が動かす子を決め（人が動かしている子へは替わらない）、
    /// PK戦では蹴る子・守る側の子へ、そのチームの人の入力を渡す（人が2人以上のチームは1本ずつ交互）。
    /// 人の入力が渡らなかった子は AI が動かす。ひとりで遊ぶときは席 0 だけの、みんなでのホストは全員の分を持つ。
    /// 抜けた人の子は AI に戻す（3対3 のまま続ける）
    /// </summary>
    public sealed class SoccerHumans
    {
        private readonly SoccerTeamPlan _plan;
        private readonly SoccerControl[] _controls;
        // この歩で人の入力を渡した子（AI に考えさせない）
        private readonly bool[] _driven;

        public SoccerHumans(SoccerTeamPlan plan, int playerCount)
        {
            _plan = plan;
            _controls = new SoccerControl[plan.SeatCount];
            _driven = new bool[playerCount];
            for (var seat = 0; seat < _controls.Length; seat++)
            {
                var team = plan.TeamOf(seat);
                if (team >= 0)
                {
                    var owner = seat;
                    _controls[seat] = new SoccerControl(team, plan.LeaderOf(seat), index => IsTakenByOther(owner, index));
                }
            }
        }

        public int SeatCount => _controls.Length;

        /// <summary>その席の人がいる（抜けていない）</summary>
        public bool Has(int seat) => Control(seat) != null;

        /// <summary>その席の人の切り替え。いなければ null</summary>
        public SoccerControl Control(int seat) => seat >= 0 && seat < _controls.Length ? _controls[seat] : null;

        /// <summary>その席の人が試合中に動かしている子。いなければ −1</summary>
        public int ControlledBy(int seat) => Control(seat)?.Controlled ?? SoccerBall.NoPlayer;

        /// <summary>試合中にその子を動かしている人の席。AI の子は −1</summary>
        public int SeatOf(int player)
        {
            for (var seat = 0; seat < _controls.Length; seat++)
            {
                if (_controls[seat] != null && _controls[seat].Controlled == player)
                {
                    return seat;
                }
            }

            return -1;
        }

        /// <summary>その子に、この歩で人の入力を渡した（AI は考えない）</summary>
        public bool IsDriven(int player) => player >= 0 && player < _driven.Length && _driven[player];

        /// <summary>人が抜けた: その人の子は AI が動かす</summary>
        public void Remove(int seat)
        {
            if (seat >= 0 && seat < _controls.Length)
            {
                _controls[seat] = null;
            }
        }

        /// <summary>
        /// 歩の前に呼ぶ。席ごとの入力（raw）と交代ボタン（switches）を、試合の段に合わせて子の入力にして inputs に書く。
        /// lastEvents は前の歩で起きたこと（パスの受け手・キーパーの投げ先へ替わる）
        /// </summary>
        public void Drive(SoccerMatch match, IReadOnlyList<SoccerInput> raw, IReadOnlyList<bool> switches, IReadOnlyList<SoccerEvent> lastEvents, SoccerInput[] inputs)
        {
            System.Array.Clear(_driven, 0, _driven.Length);
            switch (match.Phase)
            {
                case SoccerMatchPhase.Playing:
                    DrivePlaying(match.World, raw, switches, lastEvents, inputs);
                    break;
                case SoccerMatchPhase.Shootout:
                    DriveShootout(match.Shootout, raw, inputs);
                    break;
            }
        }

        private void DrivePlaying(SoccerWorld world, IReadOnlyList<SoccerInput> raw, IReadOnlyList<bool> switches, IReadOnlyList<SoccerEvent> lastEvents, SoccerInput[] inputs)
        {
            for (var seat = 0; seat < _controls.Length; seat++)
            {
                var control = _controls[seat];
                if (control == null)
                {
                    continue;
                }

                var input = control.Update(world, At(raw, seat), At(switches, seat), lastEvents);
                Give(control.Controlled, input, inputs);
            }
        }

        /// <summary>そのチームに人がいれば、蹴る子・守る側の子へ、その1本の番の人の入力を渡す</summary>
        private void DriveShootout(SoccerShootout shootout, IReadOnlyList<SoccerInput> raw, SoccerInput[] inputs)
        {
            if (shootout == null || shootout.Phase == SoccerShootoutPhase.Finished)
            {
                return;
            }

            var seatTeams = SeatTeams();
            for (var team = 0; team < SoccerRules.TeamCount; team++)
            {
                var seat = ShootoutSeat(shootout, team, seatTeams);
                if (seat >= 0)
                {
                    Give(shootout.InputIndex(team), At(raw, seat), inputs);
                }
            }
        }

        /// <summary>席ごとの、いるチーム（抜けた席は −1）。ゲストはスナップショットの SeatTeam が同じ形</summary>
        public int[] SeatTeams()
        {
            var teams = new int[_controls.Length];
            for (var seat = 0; seat < teams.Length; seat++)
            {
                teams[seat] = Has(seat) ? _plan.TeamOf(seat) : -1;
            }

            return teams;
        }

        public int ShootoutSeat(SoccerShootout shootout, int team) => ShootoutSeat(shootout, team, SeatTeams());

        public void ShootoutRole(SoccerShootout shootout, int seat, out bool kicking, out bool keeping) =>
            ShootoutRole(shootout, seat, SeatTeams(), out kicking, out keeping);

        /// <summary>
        /// PK戦の今の1本で、そのチームの入力を渡す人の席（いなければ −1）。蹴る側は蹴った本数で、守る側は相手の蹴った本数で
        /// チームの人を席の若い順に回す（1人のチームはいつもその人。2人のチームは交互）。seatTeams は席ごとのチーム（いない席は −1）
        /// </summary>
        public static int ShootoutSeat(SoccerShootout shootout, int team, IReadOnlyList<int> seatTeams)
        {
            if (shootout == null || seatTeams == null)
            {
                return -1;
            }

            var seats = new List<int>();
            for (var seat = 0; seat < seatTeams.Count; seat++)
            {
                if (seatTeams[seat] == team)
                {
                    seats.Add(seat);
                }
            }

            if (seats.Count == 0)
            {
                return -1;
            }

            var turn = shootout.Results(shootout.KickingTeam).Count;
            return seats[turn % seats.Count];
        }

        /// <summary>その席の人が、今の PK戦で蹴る（kicking）・守る（keeping）番か</summary>
        public static void ShootoutRole(SoccerShootout shootout, int seat, IReadOnlyList<int> seatTeams, out bool kicking, out bool keeping)
        {
            kicking = false;
            keeping = false;
            var team = seatTeams != null && seat >= 0 && seat < seatTeams.Count ? seatTeams[seat] : -1;
            if (shootout == null || shootout.IsFinished || team < 0 || ShootoutSeat(shootout, team, seatTeams) != seat)
            {
                return;
            }

            kicking = shootout.KickingTeam == team;
            keeping = shootout.KeeperTeam == team;
        }

        /// <summary>
        /// その席の人の印（足元の席の色の輪）を付ける子。試合中は動かしている子、PK戦は自分が蹴る番の蹴る子（守る番・ほかの人の番は無し）
        /// </summary>
        public int DisplayedBy(SoccerMatch match, int seat)
        {
            var shootout = match.Shootout;
            if (shootout == null)
            {
                return ControlledBy(seat);
            }

            ShootoutRole(shootout, seat, out var kicking, out _);
            return kicking ? shootout.Kicker : SoccerBall.NoPlayer;
        }

        private void Give(int player, SoccerInput input, SoccerInput[] inputs)
        {
            if (player < 0 || player >= inputs.Length)
            {
                return;
            }

            inputs[player] = input;
            _driven[player] = true;
        }

        private bool IsTakenByOther(int seat, int player)
        {
            for (var other = 0; other < _controls.Length; other++)
            {
                if (other != seat && _controls[other] != null && _controls[other].Controlled == player)
                {
                    return true;
                }
            }

            return false;
        }

        private static T At<T>(IReadOnlyList<T> list, int index) => list != null && index < list.Count ? list[index] : default;
    }
}
