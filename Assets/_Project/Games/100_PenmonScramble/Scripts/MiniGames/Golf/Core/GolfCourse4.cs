using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// コース4「嵐の崖」（灰色の岩と波しぶき）。風の帯（押し流す帯）・回る風車（動く物）・崖（柵の無い縁。落ちると戻る）。
    /// パーは 3・3・4（合計 10）。座標は上から見た地面の上（X が右・Y が奥、m）
    /// </summary>
    public static class GolfCourse4
    {
        public static readonly IReadOnlyList<GolfHoleDef> Holes = new[]
        {
            WindyRidge(),
            WindmillHill(),
            StormCape(),
        };

        /// <summary>
        /// ホール1「風の尾根」パー 3: 右が崖（柵が無い）の細い尾根。まん中を崖の方へ風が吹き、弱い球は流される。
        /// まん中の岩をよけ、風を見越して左寄りに
        /// </summary>
        private static GolfHoleDef WindyRidge()
        {
            var ground = GolfGeometry.Rect(0f, 0f, 7f, 18f);
            // 右の縁は手前と奥だけ柵。あいだは崖
            var walls = GolfHoleDef.WallsAlong(new[] { new Vec2(7f, 15f), new Vec2(7f, 18f), new Vec2(0f, 18f), new Vec2(0f, 0f), new Vec2(7f, 0f), new Vec2(7f, 4f) });
            // まん中の岩
            walls.AddRange(GolfHoleDef.WallsAround(GolfGeometry.Rect(2.2f, 8.6f, 4.2f, 9.8f)));

            var areas = new List<GolfArea>
            {
                GolfArea.Flow(GolfGeometry.Rect(0f, 5.4f, 7f, 13.4f), new Vec2(1.25f, 0f)),
            };

            return new GolfHoleDef("風の尾根", 3, new Vec2(3.2f, 1.6f), new Vec2(3f, 16f), ground, walls, areas);
        }

        /// <summary>
        /// ホール2「風車の丘」パー 3: 柵のまん中の隙間で風車が回る。羽根の間を抜けるよう、打つ間合いを計る。
        /// 奥は左が崖で、崖の方へ風が吹く
        /// </summary>
        private static GolfHoleDef WindmillHill()
        {
            var ground = GolfGeometry.Rect(0f, 0f, 12f, 16f);
            // 左の縁の奥（y 10〜15）は崖
            var walls = GolfHoleDef.WallsAlong(new[] { new Vec2(0f, 15f), new Vec2(0f, 16f), new Vec2(12f, 16f), new Vec2(12f, 0f), new Vec2(0f, 0f), new Vec2(0f, 10f) });
            // 風車の柵（まん中に隙間）。羽根の先と柵の端の間は球がぎりぎり通れる
            walls.Add(new GolfWall(new Vec2(0f, 8f), new Vec2(3.9f, 8f)));
            walls.Add(new GolfWall(new Vec2(8.1f, 8f), new Vec2(12f, 8f)));

            var areas = new List<GolfArea>
            {
                GolfArea.Flow(GolfGeometry.Rect(0f, 10.4f, 12f, 12.4f), new Vec2(-1.2f, 0f)),
            };

            var movers = new[]
            {
                GolfMover.Windmill(new Vec2(6f, 8f), 1.5f, 4, 4.8f),
            };

            return new GolfHoleDef("風車の丘", 3, new Vec2(6f, 1.6f), new Vec2(7.4f, 13.8f), ground, walls, areas, movers);
        }

        /// <summary>
        /// ホール3「嵐の岬」パー 4: Z の字に折れた岬。手前の通りは崖の方へ風が吹き、右の曲がり角と奥の通りに風車。
        /// 手前と奥の縁のまん中は崖
        /// </summary>
        private static GolfHoleDef StormCape()
        {
            var ground = new[]
            {
                new Vec2(0f, 0f), new Vec2(16f, 0f), new Vec2(16f, 16f), new Vec2(4f, 16f),
                new Vec2(4f, 10f), new Vec2(10f, 10f), new Vec2(10f, 6f), new Vec2(0f, 6f),
            };
            var walls = GolfHoleDef.WallsAlong(new[]
            {
                new Vec2(12f, 0f), new Vec2(16f, 0f), new Vec2(16f, 16f), new Vec2(12f, 16f),
            });
            walls.AddRange(GolfHoleDef.WallsAlong(new[]
            {
                new Vec2(7f, 16f), new Vec2(4f, 16f), new Vec2(4f, 10f), new Vec2(10f, 10f),
                new Vec2(10f, 6f), new Vec2(0f, 6f), new Vec2(0f, 0f), new Vec2(4f, 0f),
            }));

            var areas = new List<GolfArea>
            {
                // 手前の通りのまん中は、手前の崖の方へ吹く
                GolfArea.Flow(GolfGeometry.Rect(4f, 0f, 12f, 6f), new Vec2(0f, -1.0f)),
            };

            var movers = new[]
            {
                GolfMover.Windmill(new Vec2(13f, 8f), 1.6f, 3, 4.2f),
                GolfMover.Windmill(new Vec2(9.5f, 13f), 1.6f, 4, 5.4f, 1),
            };

            return new GolfHoleDef("嵐の岬", 4, new Vec2(1.8f, 3f), new Vec2(5.8f, 13.6f), ground, walls, areas, movers);
        }
    }
}
