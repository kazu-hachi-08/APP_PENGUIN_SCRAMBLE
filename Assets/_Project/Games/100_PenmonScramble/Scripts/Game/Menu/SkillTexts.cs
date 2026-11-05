using PenmonScramble.Core;

namespace PenmonScramble.Game
{
    /// <summary>
    /// 技の見せ方の文（見出し・名前・［形・特徴］・一言の説明・威力／出しやすさ／範囲の3段階）。
    /// ガチャの結果の技のカード（SkillCardView）と、ペンモン画面の技のタブ・詳しい欄（SkillTabView / SkillDetailView）が同じ文を使う。
    /// エディタの UiLayoutCheck も、全種類の文が枠に収まるかをこれで測る
    /// </summary>
    public static class SkillTexts
    {
        private const string SkillRatingLabels = "威力\n出しやすさ\n範囲";
        private const string SpecialRatingLabels = "威力\n範囲";
        private const string SpecialHeader = "必殺";
        private const string SpecialShape = "［ゲージ満タンで1回］";

        public readonly struct Lines
        {
            /// <summary>「技1」「技2」「必殺」</summary>
            public readonly string Header;
            public readonly string Name;
            /// <summary>「［突進・はね飛ばし］」のような形と特徴（必殺技は［ゲージ満タンで1回］）</summary>
            public readonly string Shape;
            public readonly string Description;
            public readonly string RatingLabels;
            public readonly string Ratings;

            public Lines(string header, string name, string shape, string description, string ratingLabels, string ratings)
            {
                Header = header;
                Name = name;
                Shape = shape;
                Description = description;
                RatingLabels = ratingLabels;
                Ratings = ratings;
            }
        }

        public static bool IsSpecial(int slot) => slot == SkillPractice.SpecialSlot;

        /// <param name="slot">0 が技1、SkillPractice.SpecialSlot が必殺技</param>
        public static SkillDef SkillOf(SpeciesDef species, int slot) =>
            IsSpecial(slot) ? SpecialMoves.For(species.Element) : species.GetSkill(slot);

        public static Lines For(SpeciesDef species, int slot)
        {
            if (IsSpecial(slot))
            {
                return Special(species.Element);
            }

            var skill = species.GetSkill(slot);
            return new Lines(
                $"技{slot + 1}",
                skill.Name,
                $"［{SkillProfile.ShapeLabel(skill)}］",
                SpeciesTexts.Skill(species.No, slot),
                SkillRatingLabels,
                $"{PenguinLabels.Pips(SkillProfile.PowerRating(skill), SkillProfile.MaxRating)}\n" +
                $"{PenguinLabels.Pips(SkillProfile.QuicknessRating(skill), SkillProfile.MaxRating)}\n" +
                $"{PenguinLabels.Pips(SkillProfile.AreaRating(skill), SkillProfile.MaxRating)}");
        }

        private static Lines Special(Element element)
        {
            var special = SpecialMoves.For(element);
            // 必殺技はゲージで出すので「出しやすさ」は比べられない。範囲は画面全体に効くものがあるので言葉で出す
            return new Lines(
                SpecialHeader,
                special.Name,
                SpecialShape,
                SpeciesTexts.Special(element),
                SpecialRatingLabels,
                $"{PenguinLabels.Pips(SkillProfile.PowerRating(special), SkillProfile.MaxRating)}\n" +
                SkillProfile.SpecialReach(element));
        }
    }
}
