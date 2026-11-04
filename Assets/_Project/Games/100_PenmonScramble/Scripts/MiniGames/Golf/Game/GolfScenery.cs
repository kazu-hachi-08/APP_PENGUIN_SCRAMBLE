using UnityEngine;

namespace PenmonScramble.Golf.Game
{
    /// <summary>
    /// コースの景色ごとの、地面の種類と動く物の色（当たり方は Core のまま。見た目だけ景色に合わせる）。
    /// 例: 砂地は氷山では雪だまり・火山では灰、場外は火山では溶岩、柔らかい壁は深海では海藻、押し流す帯は嵐の崖では風
    /// </summary>
    public sealed class GolfScenery
    {
        public Color Water = new Color(0.30f, 0.62f, 0.95f);
        public Color Foam = new Color(0.88f, 0.96f, 1f);
        public Color Sand = new Color(0.98f, 0.87f, 0.58f);
        public Color SandRim = new Color(0.86f, 0.72f, 0.45f);
        public Color Out = new Color(0.20f, 0.17f, 0.18f);

        /// <summary>場外の縁（溶岩なら光る縁。無ければ縁を描かない）</summary>
        public Color? OutRim;

        public Color IceFloor = new Color(0.78f, 0.93f, 1f);
        public Color IceRim = new Color(0.95f, 0.99f, 1f);
        public Color SoftWall = new Color(0.60f, 0.82f, 0.98f);

        /// <summary>柔らかい壁の上に帯を付けるか（スポンジは付けない・海藻は葉の色の帯）</summary>
        public Color? SoftWallCap;

        public Color Flow = new Color(0.36f, 0.66f, 0.96f);
        public Color FlowMark = new Color(0.86f, 0.95f, 1f);

        /// <summary>押し流す帯が風か（風なら帯の床を薄くし、印を細い筋にする）</summary>
        public bool WindFlow;

        public Color Rock = new Color(0.55f, 0.50f, 0.48f);
        public Color RockCap = new Color(0.70f, 0.66f, 0.62f);
        public Color Geyser = new Color(0.85f, 0.95f, 1f);
        public Color Vent = new Color(0.30f, 0.26f, 0.26f);

        public static GolfScenery For(int world)
        {
            var scenery = new GolfScenery();
            switch (world)
            {
                case 2:
                    // 氷山: 雪だまり・冷たい水・氷の塊
                    scenery.Sand = new Color(0.96f, 0.98f, 1f);
                    scenery.SandRim = new Color(0.80f, 0.87f, 0.96f);
                    scenery.Water = new Color(0.40f, 0.70f, 0.92f);
                    scenery.Rock = new Color(0.70f, 0.88f, 0.98f);
                    scenery.RockCap = new Color(0.92f, 0.98f, 1f);
                    break;
                case 3:
                    // 深海: 暗い割れ目・海藻の壁・青い海流
                    scenery.Water = new Color(0.10f, 0.26f, 0.55f);
                    scenery.Foam = new Color(0.45f, 0.78f, 0.98f);
                    scenery.SoftWall = new Color(0.22f, 0.62f, 0.38f);
                    scenery.SoftWallCap = new Color(0.45f, 0.82f, 0.45f);
                    scenery.Flow = new Color(0.22f, 0.48f, 0.86f);
                    break;
                case 4:
                    // 嵐の崖: 風の帯・灰色の岩
                    scenery.Flow = new Color(0.86f, 0.90f, 0.94f);
                    scenery.FlowMark = Color.white;
                    scenery.WindFlow = true;
                    scenery.Rock = new Color(0.50f, 0.52f, 0.56f);
                    scenery.RockCap = new Color(0.68f, 0.70f, 0.74f);
                    break;
                case 5:
                    // 火山の入り江: 溶岩・灰・黒い岩
                    scenery.Out = new Color(1f, 0.40f, 0.10f);
                    scenery.OutRim = new Color(1f, 0.80f, 0.30f);
                    scenery.Sand = new Color(0.50f, 0.46f, 0.46f);
                    scenery.SandRim = new Color(0.38f, 0.34f, 0.34f);
                    scenery.Rock = new Color(0.28f, 0.24f, 0.24f);
                    scenery.RockCap = new Color(0.95f, 0.45f, 0.15f);
                    scenery.Geyser = new Color(0.95f, 0.92f, 0.88f);
                    scenery.Vent = new Color(0.20f, 0.16f, 0.16f);
                    break;
            }

            return scenery;
        }
    }
}
