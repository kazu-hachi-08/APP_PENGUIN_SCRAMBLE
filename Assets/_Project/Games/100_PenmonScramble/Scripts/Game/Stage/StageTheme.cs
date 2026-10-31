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

        private StageTheme(Color background, Color gate)
        {
            Background = background;
            Gate = gate;
        }

        /// <summary>カメラの背景。エリアの外の床より遠くがわずかに映るときの色</summary>
        public Color Background { get; }
        public Color Gate { get; }

        // エリアの外は暗くして、動ける範囲との境目を見せる
        /// <summary>エリアの外に敷く床（同じ絵を暗くする色）</summary>
        public static Color OutsideTint { get; } = new Color(0.55f, 0.55f, 0.6f);

        private static readonly StageTheme[] Worlds =
        {
            // 1 海岸: 砂浜と流氷
            new StageTheme(new Color(0.35f, 0.65f, 0.85f), new Color(0.50f, 0.38f, 0.26f)),
            // 2 氷山: 氷の洞くつ
            new StageTheme(new Color(0.22f, 0.32f, 0.52f), new Color(0.35f, 0.45f, 0.62f)),
            // 3 深海: 夜の海・氷の割れ目
            new StageTheme(new Color(0.04f, 0.06f, 0.14f), new Color(0.55f, 0.75f, 0.90f)),
        };

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
