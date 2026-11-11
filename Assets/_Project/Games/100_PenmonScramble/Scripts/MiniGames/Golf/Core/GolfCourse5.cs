using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// コース5「火山の入り江」（黒い岩と溶岩の筋）。溶岩（場外。落ちると戻る）・間欠泉（噴いている間だけ跳ぶ）・動く岩（行ったり来たりする塊）。
    /// パーは 2・3・4（合計 9）。座標は上から見た地面の上（X が右・Y が奥、m）
    /// </summary>
    public static class GolfCourse5
    {
        private const float GeyserLaunch = 3.6f;

        public static readonly IReadOnlyList<GolfHoleDef> Holes = new[]
        {
            LavaRiver(),
            RollingRocks(),
            VolcanoCove(),
        };

        /// <summary>
        /// ホール1「溶岩の川」パー 2: 通りを横切る溶岩の川。渡るのは右の細い岩の橋か、川の手前の間欠泉が噴いた瞬間に跳び越えるか
        /// </summary>
        private static GolfHoleDef LavaRiver()
        {
            var ground = MiniGameGeometry.Rect(0f, 0f, 7f, 17f);
            var walls = GolfHoleDef.WallsAround(ground);
            var areas = new List<GolfArea>
            {
                GolfArea.OutOfBounds(MiniGameGeometry.Rect(0f, 7.4f, 5.8f, 9f)),
                GolfArea.OutOfBounds(MiniGameGeometry.Rect(6.5f, 7.4f, 7f, 9f)),
                GolfArea.Geyser(MiniGameGeometry.Rect(2.3f, 5.4f, 4.5f, 6.8f), GeyserLaunch, new MiniGameCycle(3f, 1.2f)),
            };

            return new GolfHoleDef("溶岩の川", 2, new Vec2(3.4f, 1.6f), new Vec2(3.4f, 14f), ground, walls, areas);
        }

        /// <summary>
        /// ホール2「動く岩の通り」パー 3: 通りを横切って行ったり来たりする岩が2つ。間合いを計って抜ける。
        /// カップの左右は溶岩
        /// </summary>
        private static GolfHoleDef RollingRocks()
        {
            var ground = MiniGameGeometry.Rect(0f, 0f, 8f, 19f);
            var walls = GolfHoleDef.WallsAround(ground);
            var areas = new List<GolfArea>
            {
                GolfArea.OutOfBounds(MiniGameGeometry.Rect(0f, 14f, 2.4f, 17.4f)),
                GolfArea.OutOfBounds(MiniGameGeometry.Rect(5.6f, 14f, 8f, 17.4f)),
            };

            var movers = new[]
            {
                GolfMover.Block(new Vec2(2f, 6.4f), 2.4f, 1.2f, new Vec2(4f, 0f), 4f),
                GolfMover.Block(new Vec2(6f, 10.4f), 2.4f, 1.2f, new Vec2(-4f, 0f), 5f, 0.25f),
            };

            return new GolfHoleDef("動く岩の通り", 3, new Vec2(4f, 1.6f), new Vec2(4f, 16f), ground, walls, areas, movers);
        }

        /// <summary>
        /// ホール3「間欠泉の渡し」パー 4: まん中を溶岩の川が流れ、奥の岩場だけが向こう岸へつながる。
        /// 近道は、川の手前の間欠泉で跳び越えること。向こう岸ではカップの手前を動く岩が上り下りする
        /// </summary>
        private static GolfHoleDef VolcanoCove()
        {
            var ground = MiniGameGeometry.Rect(0f, 0f, 16f, 16f);
            var walls = GolfHoleDef.WallsAround(ground);
            var areas = new List<GolfArea>
            {
                // まん中の溶岩の川（奥の y 12 から先は岩場で渡れる）
                GolfArea.OutOfBounds(new[] { new Vec2(7f, 0f), new Vec2(8.8f, 0f), new Vec2(9.2f, 6f), new Vec2(8.6f, 12f), new Vec2(6.8f, 12f), new Vec2(7.4f, 6f) }),
                // 川の手前の間欠泉
                GolfArea.Geyser(MiniGameGeometry.Rect(4.6f, 3.6f, 6.4f, 5.6f), GeyserLaunch, new MiniGameCycle(3.5f, 1.4f)),
                // カップの右の溶岩だまり
                GolfArea.OutOfBounds(MiniGameGeometry.Rect(14.2f, 0.8f, 16f, 4.6f)),
            };

            var movers = new[]
            {
                GolfMover.Block(new Vec2(12.4f, 7.2f), 2.6f, 1.1f, new Vec2(0f, 4.2f), 4.6f),
            };

            return new GolfHoleDef("間欠泉の渡し", 4, new Vec2(2f, 2.4f), new Vec2(12.4f, 3f), ground, walls, areas, movers);
        }
    }
}
