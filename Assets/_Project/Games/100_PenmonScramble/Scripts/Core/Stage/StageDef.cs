using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>1回分の敵の出方。種類は pool からばらばらに選ぶ</summary>
    public sealed class WaveDef
    {
        public WaveDef(int count, int[] speciesPool, int bossSpeciesNo = 0)
        {
            Count = count;
            SpeciesPool = speciesPool;
            BossSpeciesNo = bossSpeciesNo;
        }

        public int Count { get; }
        public IReadOnlyList<int> SpeciesPool { get; }

        /// <summary>この波で一緒に出すボスの種類の番号。0 ならボスなし</summary>
        public int BossSpeciesNo { get; }
    }

    /// <summary>1エリア分。波を順に出し、全部倒すと出入口が開く（§6.1）</summary>
    public sealed class AreaDef
    {
        public AreaDef(params WaveDef[] waves)
        {
            Waves = waves;
        }

        public IReadOnlyList<WaveDef> Waves { get; }
    }

    public sealed class StageDef
    {
        public StageDef(string name, params AreaDef[] areas)
        {
            Name = name;
            Areas = areas;
        }

        public string Name { get; }
        public IReadOnlyList<AreaDef> Areas { get; }
    }

    /// <summary>
    /// ステージは横に長いマップを、画面1枚分のエリアに区切ったもの（§6.1）。エリア0の中心が原点で、右へ並ぶ
    /// </summary>
    public static class StageLayout
    {
        public static Vec2 AreaMin(int index) => new Vec2((index - 0.5f) * Balance.AreaWidth, -Balance.AreaHeight * 0.5f);
        public static Vec2 AreaMax(int index) => new Vec2((index + 0.5f) * Balance.AreaWidth, Balance.AreaHeight * 0.5f);
    }
}
