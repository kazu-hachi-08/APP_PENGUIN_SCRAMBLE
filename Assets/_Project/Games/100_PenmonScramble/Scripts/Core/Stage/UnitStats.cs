namespace PenmonScramble.Core
{
    /// <summary>
    /// ユニット（の1体分）を作るときの値。ペンギンは SpeciesTable で種類とパワーから作る
    /// </summary>
    public sealed class UnitStats
    {
        /// <summary>種類の番号（1〜20）。0 は種類の無い仮のユニット（テスト用）</summary>
        public int SpeciesNo { get; set; }
        public Element Element { get; set; }
        public float Radius { get; set; }
        public float MoveSpeed { get; set; }
        public int MaxHp { get; set; }

        /// <summary>技の威力に掛ける倍率</summary>
        public float Attack { get; set; }

        /// <summary>技1・技2（敵は技1だけ）</summary>
        public SkillDef[] Skills { get; set; }

        /// <summary>技の待ち時間に掛ける倍率。敵は同じ技でも間をあけて使う（§4.7）</summary>
        public float CooldownScale { get; set; } = 1f;

        /// <summary>ステージのボス。大きく描き、HUD に HP を出す</summary>
        public bool IsBoss { get; set; }

        /// <summary>飛ばされる強さに掛ける倍率。ボスは飛ばされにくい</summary>
        public float KnockbackScale { get; set; } = 1f;
    }
}
