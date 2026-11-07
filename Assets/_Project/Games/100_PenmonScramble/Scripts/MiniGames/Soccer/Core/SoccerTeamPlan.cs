using System;
using System.Collections.Generic;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// チームの組み方（Docs/MINIGAME_SOCCER_PLAN.md「オンライン」）。席ごとのチームと編成から、フィールドの子の並び・種類・持ち主を決める。
    /// - 人が1人のチーム: その人の編成の3体（切り替えて動かす）
    /// - 2人のチーム: 各自の編成の先頭＋席の若い人の編成の2番目（AI）
    /// - 3人のチーム: 各自の先頭
    /// - 協力で4人: 各自の先頭の4対4（3対3 のまま1人あぶれる形にはしない）
    /// - 人のいないチーム: CPU（そのスタジアムの野生のペンモン）
    /// 子の並びは SoccerWorld と同じく赤の子の後に青の子。チームの中は席の若い順の先頭、続けて AI の子
    /// </summary>
    public sealed class SoccerTeamPlan
    {
        private readonly int[] _seatTeam;
        private readonly int[] _leader;

        private SoccerTeamPlan(MiniGameOnlineMode mode, int perTeam, int[] seatTeam, int[] species, int[] owner, int[] leader)
        {
            Mode = mode;
            PerTeam = perTeam;
            _seatTeam = seatTeam;
            Species = species;
            Owner = owner;
            _leader = leader;
        }

        public MiniGameOnlineMode Mode { get; }

        /// <summary>1チームのフィールドの子の数（両チーム同じ）</summary>
        public int PerTeam { get; }

        /// <summary>子ごとのペンモンの種類（SoccerWorld.Players と同じ並び）</summary>
        public IReadOnlyList<int> Species { get; }

        /// <summary>子ごとの持ち主の席（その人の先頭の子）。AI の子は −1</summary>
        public IReadOnlyList<int> Owner { get; }

        public int SeatCount => _seatTeam.Length;

        /// <summary>CPU のチーム（協力の青）。人だけの対戦なら −1</summary>
        public int CpuTeam => Mode == MiniGameOnlineMode.Coop ? SoccerRules.Blue : -1;

        /// <summary>その席のチーム。抜けた席・知らない席は −1</summary>
        public int TeamOf(int seat) => seat >= 0 && seat < _seatTeam.Length ? _seatTeam[seat] : -1;

        /// <summary>その席が始めに動かす子（その人の先頭）</summary>
        public int LeaderOf(int seat) => seat >= 0 && seat < _leader.Length ? _leader[seat] : SoccerBall.NoPlayer;

        /// <summary>そのチームの人の席（若い順）</summary>
        public List<int> SeatsOf(int team)
        {
            var seats = new List<int>();
            for (var seat = 0; seat < _seatTeam.Length; seat++)
            {
                if (_seatTeam[seat] == team)
                {
                    seats.Add(seat);
                }
            }

            return seats;
        }

        /// <summary>1チームの子の数。人が4人のチームだけ4体（各自の先頭）、ほかは3体</summary>
        public static int PlayersPerTeam(MiniGameOnlineMode mode, IReadOnlyList<int> seatTeams, int seatCount)
        {
            return Math.Max(SoccerRules.PlayersPerTeam, MiniGameTeams.LargestTeam(mode, seatTeams, seatCount));
        }

        /// <summary>ひとりで: 自分（席 0）が赤で、CPU が青</summary>
        public static SoccerTeamPlan Solo(IReadOnlyList<int> party, IReadOnlyList<int> wild)
        {
            return Create(MiniGameOnlineMode.Coop, new[] { SoccerRules.Red }, new[] { party }, wild);
        }

        /// <param name="seatTeams">席ごとのチーム（協力では無視して全員赤）</param>
        /// <param name="parties">席ごとの編成のペンモンの種類（届いていない席は null。足りない分は仮の種類で埋める）</param>
        /// <param name="wild">CPU のチームの種類（PlayersPerTeam 体ぶん。足りない分は仮の種類）</param>
        public static SoccerTeamPlan Create(MiniGameOnlineMode mode, IReadOnlyList<int> seatTeams, IReadOnlyList<IReadOnlyList<int>> parties, IReadOnlyList<int> wild)
        {
            var seatCount = parties.Count;
            var teams = MiniGameTeams.Resolve(mode, seatTeams, seatCount);
            var perTeam = PlayersPerTeam(mode, teams, seatCount);
            var species = new int[perTeam * SoccerRules.TeamCount];
            var owner = new int[species.Length];
            var leader = new int[seatCount];
            for (var team = 0; team < SoccerRules.TeamCount; team++)
            {
                FillTeam(team, team * perTeam, perTeam, teams, parties, wild, species, owner, leader);
            }

            return new SoccerTeamPlan(mode, perTeam, teams, species, owner, leader);
        }

        private static void FillTeam(int team, int first, int perTeam, int[] teams, IReadOnlyList<IReadOnlyList<int>> parties, IReadOnlyList<int> wild,
            int[] species, int[] owner, int[] leader)
        {
            var seats = new List<int>();
            for (var seat = 0; seat < teams.Length; seat++)
            {
                if (teams[seat] == team)
                {
                    seats.Add(seat);
                }
            }

            for (var i = 0; i < perTeam; i++)
            {
                var body = first + i;
                owner[body] = -1;
                species[body] = SpeciesFor(i, seats, parties, wild);
                if (i < seats.Count)
                {
                    owner[body] = seats[i];
                    leader[seats[i]] = body;
                }
            }
        }

        /// <summary>
        /// チームの i 番目の子の種類。人がいれば先頭の子を人の数だけ並べ、残りは1人なら2番目・3番目、2人なら席の若い人の2番目。
        /// 人がいなければ CPU の種類
        /// </summary>
        private static int SpeciesFor(int i, List<int> seats, IReadOnlyList<IReadOnlyList<int>> parties, IReadOnlyList<int> wild)
        {
            if (seats.Count == 0)
            {
                return wild != null && i < wild.Count ? wild[i] : SoccerMatchSetup.FallbackSpecies;
            }

            if (i < seats.Count)
            {
                return PartyMember(parties[seats[i]], 0);
            }

            // 残りの子は、席の若い人の編成の続き（1人なら 2・3番目、2人なら若い人の 2番目）
            return PartyMember(parties[seats[0]], i - seats.Count + 1);
        }

        private static int PartyMember(IReadOnlyList<int> party, int index)
        {
            return party != null && index < party.Count && party[index] > 0 ? party[index] : SoccerMatchSetup.FallbackSpecies;
        }
    }
}
