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

        /// <summary>この波で一緒に出すボスの種類の番号。ペンギン（1〜20）ならステージのボス、101〜 なら大ボス（BigBossTable）。0 ならボスなし</summary>
        public int BossSpeciesNo { get; }
    }

    /// <summary>1エリア分。波を順に出し、全部倒すと出入口が開く</summary>
    public sealed class AreaDef
    {
        public AreaDef(params WaveDef[] waves)
        {
            Waves = waves;
            foreach (var wave in waves)
            {
                IsArena |= BigBossTable.Get(wave.BossSpeciesNo) != null;
            }
        }

        public IReadOnlyList<WaveDef> Waves { get; }

        /// <summary>
        /// 大ボスの出るエリアは、広い楕円の闘技場にする。狭い四角では大ボスの攻撃から逃げる先がすぐ壁になるため。
        /// 定義表に別の印を持たせると付け忘れるので、大ボスがいるかどうかから決める
        /// </summary>
        public bool IsArena { get; }
    }

    public sealed class StageDef
    {
        public StageDef(int no, string name, params AreaDef[] areas)
        {
            No = no;
            Name = name;
            Areas = areas;
        }

        /// <summary>1から数えたステージの番号。記録（クリア済み）と通信でこの番号を使うので、一度配ったら変えない</summary>
        public int No { get; }
        public string Name { get; }
        public IReadOnlyList<AreaDef> Areas { get; }
    }

    /// <summary>
    /// ステージは横に長いマップを、エリアに区切ったもの。エリア0の中心が原点で、右へすき間なく並ぶ。
    /// ふつうのエリアは画面1枚分（16×9）の四角。闘技場は横長の楕円で、左端の高さを前のエリアの真ん中にそろえる
    /// </summary>
    public static class StageLayout
    {
        /// <summary>stage が null（テスト用の1エリアだけの場）なら、ふつうのエリアが並んでいるとみなす</summary>
        public static AreaShape Area(StageDef stage, int index)
        {
            var left = -Balance.AreaWidth * 0.5f;
            for (var i = 0; i < index; i++)
            {
                left += WidthOf(stage, i);
            }

            if (IsArena(stage, index))
            {
                var half = new Vec2(0f, Balance.ArenaHeight * 0.5f);
                return AreaShape.Ellipse(new Vec2(left, 0f) - half, new Vec2(left + Balance.ArenaWidth, 0f) + half);
            }

            return AreaShape.Rect(new Vec2(left, -Balance.AreaHeight * 0.5f), new Vec2(left + Balance.AreaWidth, Balance.AreaHeight * 0.5f));
        }

        /// <summary>
        /// 闘技場へ入る通り道。前のエリアの右の壁と闘技場の左端の、つながる所だけを開ける。
        /// 楕円の左端は細くとがっているので、壁と楕円だけでは間を通り抜けられないため
        /// </summary>
        public static AreaShape Doorway(AreaShape arena)
        {
            var center = new Vec2(arena.Min.X, arena.Center.Y);
            var half = new Vec2(Balance.AreaEnterDepth, Balance.ArenaDoorHalfWidth);
            return AreaShape.Rect(center - half, center + half);
        }

        private static bool IsArena(StageDef stage, int index) => stage != null && index < stage.Areas.Count && stage.Areas[index].IsArena;

        private static float WidthOf(StageDef stage, int index) => IsArena(stage, index) ? Balance.ArenaWidth : Balance.AreaWidth;
    }
}
