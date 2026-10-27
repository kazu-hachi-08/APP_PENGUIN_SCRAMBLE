using System.Collections.Generic;

namespace PenmonScramble.Core
{
    public enum PenguinSortMode
    {
        Power,
        Number,
        Newest,
    }

    /// <summary>ペンギン一覧の並べ替え。同じ値どうしは新しい順にして、並びが毎回変わらないようにする</summary>
    public static class PenguinSort
    {
        public static PenguinSortMode Next(PenguinSortMode mode)
        {
            return mode == PenguinSortMode.Newest ? PenguinSortMode.Power : mode + 1;
        }

        public static string Label(PenguinSortMode mode)
        {
            switch (mode)
            {
                case PenguinSortMode.Power: return "パワー順";
                case PenguinSortMode.Number: return "番号順";
                default: return "新しい順";
            }
        }

        public static List<OwnedPenguin> Sorted(IEnumerable<OwnedPenguin> penguins, PenguinSortMode mode)
        {
            var list = new List<OwnedPenguin>(penguins);
            list.Sort((a, b) =>
            {
                var order = Compare(a, b, mode);
                return order != 0 ? order : b.id.CompareTo(a.id);
            });
            return list;
        }

        private static int Compare(OwnedPenguin a, OwnedPenguin b, PenguinSortMode mode)
        {
            switch (mode)
            {
                case PenguinSortMode.Power: return b.power.CompareTo(a.power);
                case PenguinSortMode.Number: return a.speciesNo.CompareTo(b.speciesNo);
                default: return 0;
            }
        }
    }
}
