using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ワールドごとの色と、地面の絵（CreatureArt.Ground）に掛ける色。
    /// ワールドらしさは絵が受け持つので、絵に掛ける色はエリアの区別だけにする
    /// </summary>
    public sealed class StageTheme
    {
        // 1つおきに少し暗く、ボスエリアは赤っぽく
        private static readonly Color AreaTint = Color.white;
        private static readonly Color AreaAltTint = new Color(0.92f, 0.92f, 0.92f);
        private static readonly Color BossAreaTint = new Color(1f, 0.85f, 0.85f);

        private StageTheme(Color background, Color gate, FenceColors fence)
        {
            Background = background;
            Gate = gate;
            FencePost = fence.Post;
            FenceRail = fence.Rail;
            FenceCap = fence.Cap;
            FenceAltCap = fence.AltCap;
        }

        /// <summary>カメラの背景。エリアの外の床より遠くがわずかに映るときの色</summary>
        public Color Background { get; }
        public Color Gate { get; }

        /// <summary>大ボスの闘技場の柵: 柱・横木・柱の上の玉（1本おきに2色）。床の絵の上で目立つ、おもちゃらしい色</summary>
        public Color FencePost { get; }
        public Color FenceRail { get; }
        public Color FenceCap { get; }
        public Color FenceAltCap { get; }

        // エリアの外は暗くして、動ける範囲との境目を見せる
        /// <summary>エリアの外に敷く床（同じ絵を暗くする色）</summary>
        public static Color OutsideTint { get; } = new Color(0.55f, 0.55f, 0.6f);

        private static readonly StageTheme[] Worlds =
        {
            // 1 海岸: 砂浜と流氷。闘技場は砂の上に白い柱と赤・黄の玉
            new StageTheme(new Color(0.35f, 0.65f, 0.85f), new Color(0.50f, 0.38f, 0.26f),
                new FenceColors(new Color(0.97f, 0.95f, 0.90f), new Color(0.90f, 0.42f, 0.30f), new Color(0.95f, 0.30f, 0.30f), new Color(1f, 0.80f, 0.25f))),
            // 2 氷山: 氷の洞くつ。闘技場は氷のリンクを囲む白と青の柵
            new StageTheme(new Color(0.22f, 0.32f, 0.52f), new Color(0.35f, 0.45f, 0.62f),
                new FenceColors(new Color(0.96f, 0.98f, 1f), new Color(0.30f, 0.50f, 0.85f), new Color(0.35f, 0.65f, 1f), new Color(0.95f, 0.45f, 0.65f))),
            // 3 深海: 夜の海・氷の割れ目。暗い海でも柵が見えるよう、明るい柱に金と水色の玉
            new StageTheme(new Color(0.04f, 0.06f, 0.14f), new Color(0.55f, 0.75f, 0.90f),
                new FenceColors(new Color(0.80f, 0.90f, 1f), new Color(0.25f, 0.40f, 0.75f), new Color(1f, 0.80f, 0.30f), new Color(0.45f, 0.90f, 1f))),
        };

        private readonly struct FenceColors
        {
            public readonly Color Post;
            public readonly Color Rail;
            public readonly Color Cap;
            public readonly Color AltCap;

            public FenceColors(Color post, Color rail, Color cap, Color altCap)
            {
                Post = post;
                Rail = rail;
                Cap = cap;
                AltCap = altCap;
            }
        }

        public static StageTheme ForWorld(int world)
        {
            return Worlds[Mathf.Clamp(world, 1, Worlds.Length) - 1];
        }

        /// <summary>エリアの地面の絵に掛ける色</summary>
        public static Color AreaTintOf(int index, bool isBossArea)
        {
            return isBossArea ? BossAreaTint : index % 2 == 0 ? AreaTint : AreaAltTint;
        }
    }
}
