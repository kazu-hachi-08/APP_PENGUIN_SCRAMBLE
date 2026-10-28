using PenmonScramble.Core;
using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 絵が無い種類の仮の色と、種類の名前。属性の色を少しずつずらして、同じ属性でも見分けられるようにする。
    /// 大ボスは天敵の動物らしい色にする。属性の色（技・範囲の色）は絵があっても使う
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
            if (BigBossTable.Get(speciesNo) != null)
            {
                return BigBossColor(speciesNo);
            }

            var species = SpeciesTable.Get(speciesNo);
            if (species == null)
            {
                return Unknown;
            }

            Color.RGBToHSV(ElementColor(species.Element), out var h, out var s, out var v);
            var offset = (species.No - 1) % SpeciesPerElement - SpeciesPerElement / 2;
            return Color.HSVToRGB(Mathf.Repeat(h + offset * HueStep, 1f), s, v);
        }

        private static Color BigBossColor(int no)
        {
            switch (no)
            {
                case BigBossTable.SkuaNo: return new Color(0.55f, 0.42f, 0.30f);
                case BigBossTable.LeopardSealNo: return new Color(0.55f, 0.58f, 0.66f);
                default: return new Color(0.12f, 0.13f, 0.18f);
            }
        }

        /// <summary>
        /// 絵が無いときの体の色。絵があるときは絵に掛ける色（敵は暗くするだけ）。
        /// 大ボスは野生のペンギンではないので暗くしない
        /// </summary>
        public static Color BodyColor(UnitState unit, bool hasArt)
        {
            var color = hasArt ? Color.white : SpeciesColor(unit.SpeciesNo);
            var darken = unit.Team == Team.Enemy && BigBossTable.Get(unit.SpeciesNo) == null;
            return darken ? Color.Lerp(color, Color.black, EnemyDarkness) : color;
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
