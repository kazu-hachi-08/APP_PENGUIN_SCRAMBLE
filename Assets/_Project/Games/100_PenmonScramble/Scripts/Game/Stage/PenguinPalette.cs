using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 絵ができるまでの、種類ごとの仮の色。属性の色を少しずつずらして、同じ属性でも見分けられるようにする。
    /// 絵に差し替えたら、属性の色（技・範囲の色）だけ残す
    /// </summary>
    public static class PenguinPalette
    {
        // 同じ属性の5種類を、色相をこれだけずつずらして見分ける
        private const float HueStep = 0.035f;
        private const int SpeciesPerElement = 5;

        // 敵は同じ絵を暗くして使う（§9）
        private const float EnemyDarkness = 0.55f;

        private static readonly Color Unknown = new Color(0.7f, 0.7f, 0.7f);

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

        public static Color SpeciesColor(int speciesNo)
        {
            var species = SpeciesTable.Get(speciesNo);
            if (species == null)
            {
                return Unknown;
            }

            Color.RGBToHSV(ElementColor(species.Element), out var h, out var s, out var v);
            var offset = (species.No - 1) % SpeciesPerElement - SpeciesPerElement / 2;
            return Color.HSVToRGB(Mathf.Repeat(h + offset * HueStep, 1f), s, v);
        }

        public static Color BodyColor(UnitState unit)
        {
            var color = SpeciesColor(unit.SpeciesNo);
            return unit.Team == Team.Enemy ? Color.Lerp(color, Color.black, EnemyDarkness) : color;
        }

        public static string SpeciesName(int speciesNo)
        {
            var species = SpeciesTable.Get(speciesNo);
            return species != null ? species.Name : "？";
        }
    }
}
