namespace PenmonScramble.Core
{
    /// <summary>ペンギン1種類分の値（§5.1）。個体ごとの違いはパワーだけ</summary>
    public sealed class SpeciesDef
    {
        public SpeciesDef(int no, string name, Element element, int stars, float hpRate, float attackRate, float moveSpeed, SkillDef skill1, SkillDef skill2)
        {
            No = no;
            Name = name;
            Element = element;
            Stars = stars;
            HpRate = hpRate;
            AttackRate = attackRate;
            MoveSpeed = moveSpeed;
            Skill1 = skill1;
            Skill2 = skill2;
        }

        /// <summary>図鑑の番号（1〜20）。通信でもこの番号で種類を送る</summary>
        public int No { get; }
        public string Name { get; }
        public Element Element { get; }
        public int Stars { get; }

        /// <summary>HP ＝ HP の倍率 × パワー</summary>
        public float HpRate { get; }

        /// <summary>技のダメージ ＝ 威力 × 攻撃の倍率 × パワー / 100</summary>
        public float AttackRate { get; }

        public float MoveSpeed { get; }
        public SkillDef Skill1 { get; }
        public SkillDef Skill2 { get; }

        public SkillDef GetSkill(int slot) => slot == 0 ? Skill1 : Skill2;
    }
}
