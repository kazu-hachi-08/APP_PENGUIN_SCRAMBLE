using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// 壁（上から見た線分）。跳ね返りの強さと高さを持つ。浮いた球は高さを越えれば当たらない
    /// </summary>
    public readonly struct GolfWall
    {
        public readonly Vec2 A;
        public readonly Vec2 B;
        public readonly float Bounce;
        public readonly float Height;

        public GolfWall(Vec2 a, Vec2 b, float bounce = GolfBalance.WallBounce, float height = GolfBalance.TallWallHeight)
        {
            A = a;
            B = b;
            Bounce = bounce;
            Height = height;
        }

        /// <summary>低い壁（ジャンプや水切りで越えられる）</summary>
        public static GolfWall Low(Vec2 a, Vec2 b) => new GolfWall(a, b, GolfBalance.WallBounce, GolfBalance.LowWallHeight);
    }

    public enum GolfAreaKind
    {
        /// <summary>池。落ちたら 1 打足して打つ前の位置へ</summary>
        Water,
        /// <summary>場外（溶岩・崖など）。池と同じ扱い</summary>
        OutOfBounds,
        /// <summary>砂地。止まりやすい</summary>
        Sand,
        /// <summary>坂。地面の上で球を押す力の場（見た目は傾いた床）</summary>
        Slope,
        /// <summary>ジャンプ台。上を通ると跳ぶ</summary>
        Jump,
    }

    /// <summary>地面の上の範囲（多角形）。種類ごとに球への効き目が違う</summary>
    public sealed class GolfArea
    {
        public GolfAreaKind Kind { get; }
        public IReadOnlyList<Vec2> Shape { get; }

        /// <summary>坂が押す力（m/s²。坂の下る向き）。坂のときだけ使う</summary>
        public Vec2 Push { get; }

        /// <summary>ジャンプ台で上へ跳ぶ速さ（m/s）。ジャンプ台のときだけ使う</summary>
        public float Launch { get; }

        private GolfArea(GolfAreaKind kind, IReadOnlyList<Vec2> shape, Vec2 push, float launch)
        {
            Kind = kind;
            Shape = shape;
            Push = push;
            Launch = launch;
        }

        public static GolfArea Water(IReadOnlyList<Vec2> shape) => new GolfArea(GolfAreaKind.Water, shape, Vec2.Zero, 0f);
        public static GolfArea OutOfBounds(IReadOnlyList<Vec2> shape) => new GolfArea(GolfAreaKind.OutOfBounds, shape, Vec2.Zero, 0f);
        public static GolfArea Sand(IReadOnlyList<Vec2> shape) => new GolfArea(GolfAreaKind.Sand, shape, Vec2.Zero, 0f);
        public static GolfArea Slope(IReadOnlyList<Vec2> shape, Vec2 push) => new GolfArea(GolfAreaKind.Slope, shape, push, 0f);
        public static GolfArea Jump(IReadOnlyList<Vec2> shape, float launch) => new GolfArea(GolfAreaKind.Jump, shape, Vec2.Zero, launch);

        public bool Contains(Vec2 point) => GolfGeometry.Contains(Shape, point);

        /// <summary>落ちると打つ前の位置へ戻る範囲か</summary>
        public bool IsHazard => Kind == GolfAreaKind.Water || Kind == GolfAreaKind.OutOfBounds;
    }

    /// <summary>
    /// 1ホールの定義（コードの定義表から作る）。座標は上から見た地面の上（X が右・Y が奥、m）。
    /// 地面の多角形の外へ出た球は場外として戻す
    /// </summary>
    public sealed class GolfHoleDef
    {
        public string Name { get; }
        public int Par { get; }
        public Vec2 Tee { get; }
        public Vec2 Cup { get; }
        public IReadOnlyList<Vec2> Ground { get; }
        public IReadOnlyList<GolfWall> Walls { get; }
        public IReadOnlyList<GolfArea> Areas { get; }

        public GolfHoleDef(string name, int par, Vec2 tee, Vec2 cup, IReadOnlyList<Vec2> ground, IReadOnlyList<GolfWall> walls, IReadOnlyList<GolfArea> areas)
        {
            Name = name;
            Par = par;
            Tee = tee;
            Cup = cup;
            Ground = ground;
            Walls = walls;
            Areas = areas;
        }

        /// <summary>打数の上限（これまで打っても入らなければ「上限 ＋ 1」で終わり）</summary>
        public int StrokeLimit => Par + GolfRules.StrokeLimitOverPar;

        public bool OnGround(Vec2 point) => GolfGeometry.Contains(Ground, point);

        /// <summary>多角形の縁にぐるりと壁を立てる（コースの外周の柵）</summary>
        public static List<GolfWall> WallsAround(IReadOnlyList<Vec2> polygon, float bounce = GolfBalance.WallBounce, float height = GolfBalance.TallWallHeight)
        {
            var walls = new List<GolfWall>();
            for (var i = 0; i < polygon.Count; i++)
            {
                walls.Add(new GolfWall(polygon[i], polygon[(i + 1) % polygon.Count], bounce, height));
            }

            return walls;
        }
    }
}
