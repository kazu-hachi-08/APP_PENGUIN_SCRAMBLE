using UnityEngine;

namespace PenmonScramble.Game
{
    /// <summary>
    /// ワールドごとの地面の色（§6.2）。地面の絵（CreatureArt.Ground）が無いときはこの色で敷き、
    /// 絵があるときは Area / AreaAlt / BossArea を絵に掛ける色として使う
    /// </summary>
    public sealed class StageTheme
    {
        private StageTheme(Color area, Color areaAlt, Color bossArea, Color outside, Color gate)
        {
            Area = area;
            AreaAlt = areaAlt;
            BossArea = bossArea;
            Outside = outside;
            Gate = gate;
        }

        public Color Area { get; }
        /// <summary>1つおきのエリア。境目が分かるように少しだけ変える</summary>
        public Color AreaAlt { get; }
        public Color BossArea { get; }
        /// <summary>エリアの外（カメラの背景）</summary>
        public Color Outside { get; }
        public Color Gate { get; }

        // 絵の上に掛ける色。1つおきに少し暗く、ボスエリアは赤っぽく
        private static readonly StageTheme ArtTint = new StageTheme(Color.white, new Color(0.92f, 0.92f, 0.92f), new Color(1f, 0.85f, 0.85f), Color.black, Color.black);

        private static readonly StageTheme[] Worlds =
        {
            // 1 海岸: 砂浜と流氷
            new StageTheme(new Color(0.96f, 0.90f, 0.74f), new Color(0.92f, 0.85f, 0.68f), new Color(0.98f, 0.80f, 0.72f),
                new Color(0.35f, 0.65f, 0.85f), new Color(0.50f, 0.38f, 0.26f)),
            // 2 氷山: 氷の洞くつ
            new StageTheme(new Color(0.82f, 0.91f, 0.97f), new Color(0.74f, 0.85f, 0.94f), new Color(0.84f, 0.80f, 0.95f),
                new Color(0.22f, 0.32f, 0.52f), new Color(0.35f, 0.45f, 0.62f)),
            // 3 深海: 夜の海・氷の割れ目
            new StageTheme(new Color(0.24f, 0.32f, 0.48f), new Color(0.20f, 0.28f, 0.43f), new Color(0.34f, 0.20f, 0.32f),
                new Color(0.04f, 0.06f, 0.14f), new Color(0.55f, 0.75f, 0.90f)),
        };

        public static StageTheme ForWorld(int world)
        {
            return Worlds[Mathf.Clamp(world, 1, Worlds.Length) - 1];
        }

        /// <summary>地面の絵に掛ける色（エリアの区別だけ。ワールドらしさは絵が受け持つ）</summary>
        public Color AreaColor(int index, bool isBossArea, bool hasArt)
        {
            var theme = hasArt ? ArtTint : this;
            return isBossArea ? theme.BossArea : index % 2 == 0 ? theme.Area : theme.AreaAlt;
        }
    }
}
