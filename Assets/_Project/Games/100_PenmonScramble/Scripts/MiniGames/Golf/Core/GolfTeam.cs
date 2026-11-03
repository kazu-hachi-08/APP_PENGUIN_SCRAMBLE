using System;
using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// 打つ子1体（種類・属性・★）。ふつうのショットはどの子でも同じで、子による違いは属性と★の特殊ショットだけ
    /// （パワーは効かせない。オンラインで育成の差で勝負が決まらないように）
    /// </summary>
    public readonly struct GolfMember
    {
        public readonly int SpeciesNo;
        public readonly Element Element;
        public readonly int Stars;

        public GolfMember(int speciesNo, Element element, int stars)
        {
            SpeciesNo = speciesNo;
            Element = element;
            Stars = stars;
        }

        /// <summary>種類の番号から作る。表に無い番号（新しい版で増えた子など）は null</summary>
        public static GolfMember? FromSpecies(int speciesNo)
        {
            var species = SpeciesTable.Get(speciesNo);
            return species != null ? new GolfMember(species.No, species.Element, species.Stars) : (GolfMember?)null;
        }
    }

    /// <summary>
    /// 1人の打つ子の3体（本編の編成の3体。ゴルフ専用の編成は持たない）と、いま選んでいる子。1打ごとに選び直せる
    /// </summary>
    public sealed class GolfTeam
    {
        private readonly List<GolfMember> _members = new List<GolfMember>();

        public GolfTeam(IEnumerable<int> speciesNos)
        {
            foreach (var no in speciesNos ?? Array.Empty<int>())
            {
                var member = GolfMember.FromSpecies(no);
                if (member.HasValue && _members.Count < GolfRules.TeamSize)
                {
                    _members.Add(member.Value);
                }
            }

            // 編成が空（記録が壊れた等）でも打てるよう、決まった3体にする
            if (_members.Count == 0)
            {
                foreach (var no in GolfRules.DefaultSpecies)
                {
                    _members.Add(GolfMember.FromSpecies(no).Value);
                }
            }
        }

        public IReadOnlyList<GolfMember> Members => _members;

        public int Selected { get; private set; }

        public GolfMember Current => _members[Selected];

        /// <summary>選ぶ。範囲の外や、いま選んでいる子なら何もせず false</summary>
        public bool Select(int index)
        {
            if (index < 0 || index >= _members.Count || index == Selected)
            {
                return false;
            }

            Selected = index;
            return true;
        }

        /// <summary>次の子へ（最後の次は最初）。1体しかいなければ false</summary>
        public bool SelectNext() => Select((Selected + 1) % _members.Count);

        // ---- 特殊ショット（各子、1コースに1回） ----

        private bool[] _used = new bool[GolfRules.TeamSize];

        /// <summary>その子の特殊ショットをこのコースでもう使った</summary>
        public bool IsSpecialUsed(int index) => index >= 0 && index < _members.Count && _used[index];

        public bool CanUseSpecial(int index) => index >= 0 && index < _members.Count && !_used[index];

        internal void MarkSpecialUsed(int index) => _used[index] = true;

        /// <summary>いまの使った・使っていないを写す（ホールをやり直すときに、そのホールで使った分を戻すため）</summary>
        internal bool[] SaveSpecials() => (bool[])_used.Clone();

        internal void RestoreSpecials(bool[] used) => _used = (bool[])used.Clone();

        /// <summary>コースの始めに戻す（全員の特殊ショットがまた使える）</summary>
        internal void ResetSpecials() => _used = new bool[GolfRules.TeamSize];
    }
}
