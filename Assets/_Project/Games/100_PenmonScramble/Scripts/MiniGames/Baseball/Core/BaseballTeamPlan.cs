using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>
    /// チームの組み方。席ごとのチームと編成から、打順（子の種類）とその子の持ち主の席を決める。
    /// 赤（MiniGameTeams.Red）が先攻、青が後攻（BaseballRules.Away / Home と同じ番号）。
    /// - 人が1人のチーム: その人の編成（1〜3体）
    /// - 2人のチーム: A1・B1・A2・B2（席の若い人を A。編成にいない子は飛ばす）
    /// - 3人・4人のチーム: 各自の先頭
    /// - 人のいないチーム: CPU（その球場の野生のペンモン）
    /// 打席の子を動かすのはその子の持ち主（抜けた人の子は CPU）。投げる子は、守る側の人が打席ごとに交代で、自分の子から選ぶ
    /// </summary>
    public sealed class BaseballTeamPlan
    {
        /// <summary>種類の表に無い番号・空の編成のときに使う種類</summary>
        public const int FallbackSpecies = 1;

        private readonly int[] _seatTeam;
        private readonly bool[] _left;
        private readonly int[][] _species;
        private readonly int[][] _owner;

        private BaseballTeamPlan(MiniGameOnlineMode mode, int[] seatTeam, int[][] species, int[][] owner)
        {
            Mode = mode;
            _seatTeam = seatTeam;
            _left = new bool[seatTeam.Length];
            _species = species;
            _owner = owner;
        }

        public MiniGameOnlineMode Mode { get; }

        public int SeatCount => _seatTeam.Length;

        /// <summary>そのチームの打順（子の種類）</summary>
        public IReadOnlyList<int> Lineup(int team) => _species[team];

        /// <summary>その子の持ち主の席（CPU・抜けた人の子は −1）</summary>
        public int OwnerOf(int team, int slot)
        {
            var owners = _owner[team];
            return slot >= 0 && slot < owners.Length ? owners[slot] : -1;
        }

        /// <summary>その席のチーム。知らない席は −1（抜けた席もチームは残す。記録と結果に使う）</summary>
        public int TeamOf(int seat) => seat >= 0 && seat < _seatTeam.Length ? _seatTeam[seat] : -1;

        public bool HasLeft(int seat) => seat >= 0 && seat < _left.Length && _left[seat];

        /// <summary>そのチームにいる人の席（若い順。抜けた人は除く）</summary>
        public List<int> SeatsOf(int team)
        {
            var seats = new List<int>();
            for (var seat = 0; seat < _seatTeam.Length; seat++)
            {
                if (_seatTeam[seat] == team && !_left[seat])
                {
                    seats.Add(seat);
                }
            }

            return seats;
        }

        /// <summary>人がいない（CPU が動かす）チーム。野生のペンモンの見た目で出す</summary>
        public bool IsCpuTeam(int team) => SeatsOf(team).Count == 0;

        /// <summary>その打席に投げる人: 守る側の人が打席ごとに交代（人がいなければ −1 = CPU）</summary>
        public int PitchingSeat(int team, int plateNumber)
        {
            var seats = SeatsOf(team);
            if (seats.Count == 0)
            {
                return -1;
            }

            var index = plateNumber % seats.Count;
            return seats[index < 0 ? index + seats.Count : index];
        }

        /// <summary>その席が投げる子に選べるか（自分の子だけ。CPU が投げるときはどの子でも）</summary>
        public bool CanPitch(int seat, int team, int slot)
        {
            if (slot < 0 || slot >= _species[team].Length)
            {
                return false;
            }

            return seat < 0 ? OwnerOf(team, slot) < 0 || IsCpuTeam(team) : OwnerOf(team, slot) == seat;
        }

        /// <summary>その席が投げられる、打順のいちばん若い子（無ければ −1）</summary>
        public int FirstPitchable(int seat, int team)
        {
            for (var slot = 0; slot < _species[team].Length; slot++)
            {
                if (CanPitch(seat, team, slot))
                {
                    return slot;
                }
            }

            return -1;
        }

        /// <summary>席が抜けた: その人の子は CPU が動かす（打順は変えない）</summary>
        public void RemoveSeat(int seat)
        {
            if (seat < 0 || seat >= _left.Length)
            {
                return;
            }

            _left[seat] = true;
            foreach (var owners in _owner)
            {
                for (var slot = 0; slot < owners.Length; slot++)
                {
                    if (owners[slot] == seat)
                    {
                        owners[slot] = -1;
                    }
                }
            }
        }

        /// <summary>ひとりで: 自分（席 0）が humanTeam で、CPU がもう片方</summary>
        public static BaseballTeamPlan Solo(IReadOnlyList<int> party, IReadOnlyList<int> wild, int humanTeam)
        {
            var seatTeam = new[] { humanTeam };
            return Build(MiniGameOnlineMode.Coop, seatTeam, new[] { party }, wild);
        }

        /// <param name="seatTeams">席ごとのチーム（協力では無視して全員赤）</param>
        /// <param name="parties">席ごとの編成のペンモンの種類（届いていない席は null）</param>
        /// <param name="wild">CPU のチームの種類</param>
        public static BaseballTeamPlan Create(MiniGameOnlineMode mode, IReadOnlyList<int> seatTeams, IReadOnlyList<IReadOnlyList<int>> parties, IReadOnlyList<int> wild)
        {
            return Build(mode, MiniGameTeams.Resolve(mode, seatTeams, parties.Count), parties, wild);
        }

        /// <summary>写し（ゲスト）: ホストから届いた打順と持ち主をそのまま使う</summary>
        public static BaseballTeamPlan FromParts(MiniGameOnlineMode mode, IReadOnlyList<int> seatTeam, IReadOnlyList<int>[] species, IReadOnlyList<int>[] owners)
        {
            var teams = new int[seatTeam.Count];
            for (var seat = 0; seat < teams.Length; seat++)
            {
                teams[seat] = seatTeam[seat];
            }

            var lineups = new int[BaseballRules.TeamCount][];
            var owner = new int[BaseballRules.TeamCount][];
            for (var team = 0; team < BaseballRules.TeamCount; team++)
            {
                lineups[team] = new int[species[team].Count];
                owner[team] = new int[species[team].Count];
                for (var slot = 0; slot < lineups[team].Length; slot++)
                {
                    lineups[team][slot] = species[team][slot];
                    owner[team][slot] = slot < owners[team].Count ? owners[team][slot] : -1;
                }
            }

            return new BaseballTeamPlan(mode, teams, lineups, owner);
        }

        private static BaseballTeamPlan Build(MiniGameOnlineMode mode, int[] seatTeam, IReadOnlyList<IReadOnlyList<int>> parties, IReadOnlyList<int> wild)
        {
            var species = new int[BaseballRules.TeamCount][];
            var owner = new int[BaseballRules.TeamCount][];
            for (var team = 0; team < BaseballRules.TeamCount; team++)
            {
                var lineup = new List<int>();
                var owners = new List<int>();
                FillTeam(MiniGameTeams.SeatsOf(seatTeam, team), parties, wild, lineup, owners);
                species[team] = lineup.ToArray();
                owner[team] = owners.ToArray();
            }

            return new BaseballTeamPlan(mode, seatTeam, species, owner);
        }

        private static void FillTeam(List<int> seats, IReadOnlyList<IReadOnlyList<int>> parties, IReadOnlyList<int> wild, List<int> lineup, List<int> owners)
        {
            if (seats.Count == 0)
            {
                var count = wild != null && wild.Count > 0 ? wild.Count : BaseballStadiumTable.CpuLineup;
                for (var i = 0; i < count; i++)
                {
                    lineup.Add(wild != null && i < wild.Count ? Valid(wild[i]) : FallbackSpecies);
                    owners.Add(-1);
                }

                return;
            }

            if (seats.Count == 1)
            {
                var members = Members(parties[seats[0]]);
                foreach (var member in members)
                {
                    lineup.Add(member);
                    owners.Add(seats[0]);
                }

                return;
            }

            // 2人なら A1・B1・A2・B2、3人・4人なら各自の先頭だけ
            var rounds = seats.Count == 2 ? 2 : 1;
            for (var round = 0; round < rounds; round++)
            {
                foreach (var seat in seats)
                {
                    var members = Members(parties[seat]);
                    if (round < members.Count)
                    {
                        lineup.Add(members[round]);
                        owners.Add(seat);
                    }
                }
            }
        }

        /// <summary>編成の中の種類のわかる子（3体まで）。1体もいなければ仮の1体</summary>
        private static List<int> Members(IReadOnlyList<int> party)
        {
            var members = new List<int>();
            for (var i = 0; i < (party?.Count ?? 0) && members.Count < BaseballStadiumTable.CpuLineup; i++)
            {
                if (party[i] > 0 && SpeciesTable.Get(party[i]) != null)
                {
                    members.Add(party[i]);
                }
            }

            if (members.Count == 0)
            {
                members.Add(FallbackSpecies);
            }

            return members;
        }

        private static int Valid(int species) => species > 0 && SpeciesTable.Get(species) != null ? species : FallbackSpecies;
    }
}
