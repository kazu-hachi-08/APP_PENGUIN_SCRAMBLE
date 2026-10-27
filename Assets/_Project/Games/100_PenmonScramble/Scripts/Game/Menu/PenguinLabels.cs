using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>ガチャ・一覧で使う、個体の見せ方（★・属性・色）</summary>
    public static class PenguinLabels
    {
        private static readonly Color Star1Color = new Color(0.85f, 0.85f, 0.85f);
        private static readonly Color Star2Color = new Color(0.55f, 0.80f, 1.00f);
        private static readonly Color Star3Color = new Color(1.00f, 0.80f, 0.20f);

        public static string Stars(int stars) => new string('★', Mathf.Clamp(stars, 1, Balance.MaxStars));

        public static Color StarColor(int stars)
        {
            return stars >= 3 ? Star3Color : stars == 2 ? Star2Color : Star1Color;
        }

        public static string ElementName(Element element)
        {
            switch (element)
            {
                case Element.Ice: return "氷";
                case Element.Water: return "水";
                case Element.Wind: return "風";
                default: return "雷";
            }
        }

        /// <summary>「★★ マゼラ（水）」</summary>
        public static string Title(SpeciesDef species)
        {
            return $"{Stars(species.Stars)} {species.Name}（{ElementName(species.Element)}）";
        }

        /// <summary>カードに出す2行。知らない種類（新しい版で増えた子）は「？」</summary>
        public static string Card(OwnedPenguin penguin)
        {
            var species = SpeciesTable.Get(penguin.speciesNo);
            var name = species != null ? $"{Stars(species.Stars)}\n{species.Name}" : "？\n？";
            return $"{name}\nパワー {penguin.power}";
        }
    }
}
