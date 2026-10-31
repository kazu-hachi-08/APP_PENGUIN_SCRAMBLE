using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 属性の色（技・弾・範囲・飾り）と、種類の名前
    /// </summary>
    public static class PenguinPalette
    {
        public static Color ElementColor(Element element)
        {
            switch (element)
            {
                case Element.Ice: return new Color(0.60f, 0.90f, 1.00f);
                case Element.Water: return new Color(0.25f, 0.50f, 1.00f);
                case Element.Wind: return new Color(0.45f, 0.90f, 0.50f);
                default: return new Color(1.00f, 0.85f, 0.20f);
            }
        }

        /// <summary>種類の属性の色（HUD の顔の丸）。種類の無い番号（大ボスなど）は灰色</summary>
        public static Color SpeciesElementColor(int speciesNo)
        {
            var species = SpeciesTable.Get(speciesNo);
            return species != null ? ElementColor(species.Element) : new Color(0.55f, 0.57f, 0.62f);
        }

        public static string SpeciesName(int speciesNo)
        {
            var bigBoss = BigBossTable.Get(speciesNo);
            if (bigBoss != null)
            {
                return bigBoss.Name;
            }

            var species = SpeciesTable.Get(speciesNo);
            return species != null ? species.Name : "？";
        }
    }
}
