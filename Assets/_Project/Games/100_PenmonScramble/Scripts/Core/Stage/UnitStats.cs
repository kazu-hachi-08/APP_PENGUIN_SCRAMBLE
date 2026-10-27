namespace PenmonScramble.Core
{
    /// <summary>
    /// ユニットを作るときの値。フェーズ3でペンギンの種類とパワーから計算するようになる
    /// </summary>
    public sealed class UnitStats
    {
        public float Radius { get; set; }
        public float MoveSpeed { get; set; }
        public int MaxHp { get; set; }

        /// <summary>技の威力に掛ける倍率</summary>
        public float Attack { get; set; }

        /// <summary>技1・技2（敵は技1だけ）</summary>
        public SkillDef[] Skills { get; set; }
    }
}
