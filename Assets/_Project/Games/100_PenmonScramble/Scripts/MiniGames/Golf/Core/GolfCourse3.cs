using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// コース3「深海」（夜の海・氷の割れ目）。水流（押し流す帯）・ワープの泡・海藻の壁（柔らかく、跳ね返りが弱い）・氷の割れ目（池）。
    /// パーは 2・3・4（合計 9）。座標は上から見た地面の上（X が右・Y が奥、m）
    /// </summary>
    public static class GolfCourse3
    {
        private const float CurrentPush = 1.9f;

        public static readonly IReadOnlyList<GolfHoleDef> Holes = new[]
        {
            CrossCurrent(),
            WarpBubbles(),
            KelpMaze(),
        };

        private static GolfWall Kelp(Vec2 a, Vec2 b) => new GolfWall(a, b, GolfBalance.SoftWallBounce);

        /// <summary>
        /// ホール1「横切る海流」パー 2: まん中を右へ流れる海流を横切る。弱い球は右の割れ目へ流される。
        /// 流れを見越して左寄りに打つ。カップの手前は海藻の壁で絞る
        /// </summary>
        private static GolfHoleDef CrossCurrent()
        {
            var ground = GolfGeometry.Rect(0f, 0f, 7f, 17f);
            var walls = GolfHoleDef.WallsAround(ground);
            walls.Add(Kelp(new Vec2(0f, 12.2f), new Vec2(2.2f, 12.6f)));
            walls.Add(Kelp(new Vec2(7f, 12.2f), new Vec2(4.8f, 12.6f)));

            var areas = new List<GolfArea>
            {
                GolfArea.Flow(GolfGeometry.Rect(0f, 6.4f, 7f, 9.6f), new Vec2(CurrentPush, 0f)),
                // 流れの先の氷の割れ目
                GolfArea.Water(GolfGeometry.Rect(5.6f, 5.6f, 7f, 10.4f)),
            };

            return new GolfHoleDef("横切る海流", 2, new Vec2(3.5f, 1.6f), new Vec2(3.5f, 14.6f), ground, walls, areas);
        }

        /// <summary>
        /// ホール2「ワープの泡」パー 3: 手前と奥を、端から端までの氷の割れ目が分ける。渡るのはワープの泡（右の泡は奥へ、
        /// 左の泡は手前の右の隅へ戻される）か、水切りで跳び越える。泡を出た球は入ったときの向きのまま転がる
        /// </summary>
        private static GolfHoleDef WarpBubbles()
        {
            var ground = GolfGeometry.Rect(0f, 0f, 12f, 17f);
            var walls = GolfHoleDef.WallsAround(ground);
            // カップのまわりの海藻
            walls.Add(Kelp(new Vec2(6.4f, 12.6f), new Vec2(6.4f, 15.4f)));
            walls.Add(Kelp(new Vec2(10.6f, 12.6f), new Vec2(10.6f, 15.4f)));
            // 右の泡の手前の海藻（まっすぐは狙わせない）
            walls.Add(Kelp(new Vec2(7.6f, 3.6f), new Vec2(8.8f, 4.6f)));

            var areas = new List<GolfArea>
            {
                GolfArea.Water(GolfGeometry.Rect(0f, 7.4f, 12f, 9.2f)),
            };

            var warps = new[]
            {
                new GolfWarp(new Vec2(9.6f, 5.4f), new Vec2(2.6f, 11.2f)),
                new GolfWarp(new Vec2(2.4f, 5.4f), new Vec2(10.6f, 1.4f)),
            };

            return new GolfHoleDef("ワープの泡", 3, new Vec2(6f, 1.6f), new Vec2(8.5f, 14.4f), ground, walls, areas, warps: warps);
        }

        /// <summary>
        /// ホール3「海藻の迷路」パー 4: 海藻の壁で S の字に折れた迷路。まん中の通りは手前へ、右の通りは奥へ海流が流れる。
        /// 左の通りの奥の泡に入れれば、右の通りへ近道できる
        /// </summary>
        private static GolfHoleDef KelpMaze()
        {
            var ground = GolfGeometry.Rect(0f, 0f, 16f, 16f);
            var walls = GolfHoleDef.WallsAround(ground);
            walls.Add(Kelp(new Vec2(5f, 0f), new Vec2(5f, 11f)));
            walls.Add(Kelp(new Vec2(10.5f, 5f), new Vec2(10.5f, 16f)));

            var areas = new List<GolfArea>
            {
                // まん中の通りは手前へ、右の通りは奥へ流れる
                GolfArea.Flow(GolfGeometry.Rect(5f, 5.6f, 10.5f, 11f), new Vec2(0f, -CurrentPush)),
                GolfArea.Flow(GolfGeometry.Rect(10.5f, 3.4f, 16f, 10.4f), new Vec2(0f, CurrentPush)),
                // 手前のまん中の氷の割れ目
                GolfArea.Water(GolfGeometry.Rect(7f, 0.8f, 8.6f, 3.6f)),
            };

            var warps = new[]
            {
                new GolfWarp(new Vec2(1.4f, 14.4f), new Vec2(13.2f, 6f)),
            };

            return new GolfHoleDef("海藻の迷路", 4, new Vec2(2.5f, 2f), new Vec2(13.4f, 13.6f), ground, walls, areas, warps: warps);
        }
    }
}
