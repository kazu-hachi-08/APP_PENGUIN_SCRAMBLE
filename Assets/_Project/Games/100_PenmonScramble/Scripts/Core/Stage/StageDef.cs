using System.Collections.Generic;

namespace PenmonScramble.Core
{
    /// <summary>ボスの格。ペンギン（1〜20）は Normal（ステージのボス）か Giant（巨大ペンモン）、大ボス（101〜）は Normal か Serious（本気）</summary>
    public enum BossRank : byte
    {
        Normal,

        /// <summary>巨大ペンモン。ステージのボスより大きく硬く、属性ごとの赤い予告の大技も使う</summary>
        Giant,

        /// <summary>大ボスの本気。HP が半分を切ると吠えて、攻撃が速く・多くなる</summary>
        Serious,
    }

    /// <summary>波で一緒に出すボス1体</summary>
    public readonly struct BossSpawn
    {
        public BossSpawn(int speciesNo, BossRank rank)
        {
            SpeciesNo = speciesNo;
            Rank = rank;
        }

        public int SpeciesNo { get; }
        public BossRank Rank { get; }
    }

    /// <summary>1回分の敵の出方。種類は pool からばらばらに選ぶ</summary>
    public sealed class WaveDef
    {
        private static readonly BossSpawn[] NoBosses = new BossSpawn[0];

        /// <param name="bossSpeciesNo">0 ならボスなし。ペンギンならステージのボス、101〜 なら大ボス</param>
        public WaveDef(int count, int[] speciesPool, int bossSpeciesNo = 0)
            : this(count, speciesPool, bossSpeciesNo > 0 ? new[] { new BossSpawn(bossSpeciesNo, BossRank.Normal) } : NoBosses)
        {
        }

        public WaveDef(int count, int[] speciesPool, BossSpawn[] bosses)
        {
            Count = count;
            SpeciesPool = speciesPool;
            Bosses = bosses;
        }

        public int Count { get; }
        public IReadOnlyList<int> SpeciesPool { get; }

        /// <summary>この波で一緒に出すボス。巨大ペンモンの2体組のように、いくつも出せる</summary>
        public IReadOnlyList<BossSpawn> Bosses { get; }

        /// <summary>最初のボスの種類の番号。0 ならボスなし</summary>
        public int BossSpeciesNo => Bosses.Count > 0 ? Bosses[0].SpeciesNo : 0;
    }

    /// <summary>1エリア分。波を順に出し、全部倒すと出入口が開く</summary>
    public sealed class AreaDef
    {
        public AreaDef(params WaveDef[] waves)
            : this(false, waves)
        {
        }

        /// <param name="isArena">巨大ペンモンの戦いのように、大ボスがいなくても闘技場にするとき true</param>
        public AreaDef(bool isArena, params WaveDef[] waves)
        {
            Waves = waves;
            IsArena = isArena || HasBigBoss(waves);
        }

        /// <summary>闘技場のエリア（巨大ペンモン・大ボスの戦い）</summary>
        public static AreaDef Arena(params WaveDef[] waves) => new AreaDef(true, waves);

        public IReadOnlyList<WaveDef> Waves { get; }

        /// <summary>
        /// 広い楕円の闘技場にするか。狭い四角では大ボスの攻撃から逃げる先がすぐ壁になるため、大ボスがいれば印を付け忘れても必ず闘技場にする
        /// </summary>
        public bool IsArena { get; }

        private static bool HasBigBoss(WaveDef[] waves)
        {
            foreach (var wave in waves)
            {
                foreach (var boss in wave.Bosses)
                {
                    if (BigBossTable.Get(boss.SpeciesNo) != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    public sealed class StageDef
    {
        public StageDef(int no, string name, params AreaDef[] areas)
            : this(no, name, StageTable.WorldOf(no), areas)
        {
        }

        /// <param name="world">敵の強さ・床の絵・色に使うワールド。ストーリーは番号から決まる</param>
        public StageDef(int no, string name, int world, params AreaDef[] areas)
        {
            No = no;
            Name = name;
            World = world;
            Areas = areas;
            ArenaCount = CountArenas(areas, areas.Length);
        }

        /// <summary>1から数えたステージの番号。記録（クリア済み）と通信でこの番号を使うので、一度配ったら変えない</summary>
        public int No { get; }
        public string Name { get; }

        /// <summary>何番目のワールドか（1〜）。先のワールドほど敵が強い</summary>
        public int World { get; }
        public IReadOnlyList<AreaDef> Areas { get; }

        /// <summary>闘技場のエリアの数（ボスラッシュは3）</summary>
        public int ArenaCount { get; }

        /// <summary>エリア index が何戦目の闘技場か（1〜）。闘技場でなければ 0</summary>
        public int ArenaRoundOf(int index)
        {
            if (index < 0 || index >= Areas.Count || !Areas[index].IsArena)
            {
                return 0;
            }

            return CountArenas(Areas, index + 1);
        }

        private static int CountArenas(IReadOnlyList<AreaDef> areas, int count)
        {
            var arenas = 0;
            for (var i = 0; i < count; i++)
            {
                if (areas[i].IsArena)
                {
                    arenas++;
                }
            }

            return arenas;
        }
    }

    /// <summary>
    /// ステージは横に長いマップを、エリアに区切ったもの。エリア0の中心が原点で、右へ並ぶ。
    /// ふつうのエリアは画面1枚分（16×9）の四角で、すき間なく並ぶ。闘技場は横長の楕円で、左端の高さを前のエリアの真ん中にそろえる。
    /// 闘技場が続くときは、前の闘技場の右端と次の闘技場の左端を、短い通り道（ArenaCorridorLength）でつなぐ
    /// </summary>
    public static class StageLayout
    {
        /// <summary>stage が null（テスト用の1エリアだけの場）なら、ふつうのエリアが並んでいるとみなす</summary>
        public static AreaShape Area(StageDef stage, int index)
        {
            var left = -WidthOf(stage, 0) * 0.5f;
            for (var i = 0; i < index; i++)
            {
                left += WidthOf(stage, i) + GapAfter(stage, i);
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

        /// <summary>
        /// エリア index（闘技場）へ入る通り道。前がふつうのエリアなら Doorway、前も闘技場なら、
        /// 前の右端から次の左端までを幅3マスでつなぐ（両はしは楕円の中へ少し入れて、とがった先のすき間を埋める）
        /// </summary>
        public static AreaShape Passage(StageDef stage, int index)
        {
            var arena = Area(stage, index);
            if (index <= 0 || !IsArena(stage, index - 1))
            {
                return Doorway(arena);
            }

            var previous = Area(stage, index - 1);
            var halfWidth = Balance.ArenaDoorHalfWidth;
            return AreaShape.Rect(
                new Vec2(previous.Max.X - Balance.AreaEnterDepth, arena.Center.Y - halfWidth),
                new Vec2(arena.Min.X + Balance.AreaEnterDepth, arena.Center.Y + halfWidth));
        }

        /// <summary>エリア index が闘技場で、前にエリアがある（通り道で入ってくる）か。ボスラッシュの最初の闘技場には入口が無い</summary>
        public static bool HasPassage(StageDef stage, int index) => index > 0 && IsArena(stage, index);

        /// <summary>闘技場から次の闘技場へ出る通り道があるか（右端の柵に出口を開ける）</summary>
        public static bool HasExitPassage(StageDef stage, int index) => IsArena(stage, index) && IsArena(stage, index + 1);

        private static bool IsArena(StageDef stage, int index) => stage != null && index >= 0 && index < stage.Areas.Count && stage.Areas[index].IsArena;

        private static float WidthOf(StageDef stage, int index) => IsArena(stage, index) ? Balance.ArenaWidth : Balance.AreaWidth;

        /// <summary>エリア index と次のエリアの間のすき間。闘技場が続くときだけ通り道の長さを空ける</summary>
        private static float GapAfter(StageDef stage, int index) => HasExitPassage(stage, index) ? Balance.ArenaCorridorLength : 0f;
    }
}
