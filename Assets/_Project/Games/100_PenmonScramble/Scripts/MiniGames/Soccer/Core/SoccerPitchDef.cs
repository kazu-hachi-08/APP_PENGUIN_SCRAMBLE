using System;
using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// ピッチ1面の形（コードの定義表 SoccerPitchTable から作る）。真ん中が原点、X が横（ゴールの向き）、Y が縦。
    /// 横長の長方形の四隅を斜めに切り（ボールが角に詰まらないように）、左右の壁の真ん中にゴールの口を開けた外周を、
    /// ボールが外へ出ない壁にする。ゴールの口の奥には網の箱（SoccerWallKind.Net）を置く。
    /// スタジアムの仕掛け: 地面の種類（氷の床）・押し流す帯（水流・風）・間欠泉・壁のバンパー（外周の板の一部を置き換える）
    /// </summary>
    public sealed class SoccerPitchDef
    {
        private readonly SoccerGoal[] _goals;
        private readonly Vec2[] _kickingSpots;
        private readonly Vec2[] _waitingSpots;
        private readonly IReadOnlyList<SoccerBumper> _bumpers;

        // 点が辺の上にあるとみなす距離（バンパーの両端を外周の辺に合わせるときの誤差）
        private const float OnEdgeTolerance = 1e-3f;

        /// <param name="halfLength">横の長さの半分（ゴールの線の X）</param>
        /// <param name="halfWidth">縦の長さの半分</param>
        /// <param name="cornerCut">四隅を斜めに切る長さ</param>
        /// <param name="kickingSpots">キックオフで蹴る側の赤の並び（先頭がボールのすぐ後ろ）。青は左右を反対にして使う</param>
        /// <param name="waitingSpots">キックオフで蹴らない側の赤の並び</param>
        /// <param name="bumpers">外周の上に置くバンパー（外周の辺の上にない物は使わない）</param>
        /// <param name="posts">ピッチの中に立てるバンパーの柱</param>
        public SoccerPitchDef(
            string name,
            float halfLength,
            float halfWidth,
            float cornerCut,
            float goalHalfWidth,
            float goalBarHeight,
            float goalDepth,
            IReadOnlyList<Vec2> kickingSpots,
            IReadOnlyList<Vec2> waitingSpots,
            IReadOnlyList<SoccerGroundZone> grounds = null,
            IReadOnlyList<SoccerFlowZone> flows = null,
            IReadOnlyList<SoccerGeyser> geysers = null,
            IReadOnlyList<SoccerBumper> bumpers = null,
            IReadOnlyList<SoccerPost> posts = null)
        {
            Name = name;
            HalfLength = halfLength;
            HalfWidth = halfWidth;
            CornerCut = cornerCut;
            Grounds = grounds ?? new SoccerGroundZone[0];
            Flows = flows ?? new SoccerFlowZone[0];
            Geysers = geysers ?? new SoccerGeyser[0];
            _bumpers = bumpers ?? new SoccerBumper[0];
            Posts = posts ?? new SoccerPost[0];
            _kickingSpots = new List<Vec2>(kickingSpots).ToArray();
            _waitingSpots = new List<Vec2>(waitingSpots).ToArray();
            _goals = new[]
            {
                new SoccerGoal(SoccerRules.Red, -halfLength, -1f, goalHalfWidth, goalBarHeight, goalDepth),
                new SoccerGoal(SoccerRules.Blue, halfLength, 1f, goalHalfWidth, goalBarHeight, goalDepth),
            };
            Outline = BuildOutline();
            Walls = BuildWalls();
        }

        public string Name { get; }

        public float HalfLength { get; }

        public float HalfWidth { get; }

        public float CornerCut { get; }

        /// <summary>ピッチの外周（左回り。ゴールの口の両端も角として入る）。地面を描くのに使う</summary>
        public IReadOnlyList<Vec2> Outline { get; }

        /// <summary>外周の壁・ゴールの口・網。ボールとペンモンはこれで跳ね返る / 止まる</summary>
        public IReadOnlyList<SoccerWall> Walls { get; }

        /// <summary>地面の種類の範囲（氷の床）。どれにも入らない所は芝</summary>
        public IReadOnlyList<SoccerGroundZone> Grounds { get; }

        /// <summary>ボールを押し流す帯（水流・風）</summary>
        public IReadOnlyList<SoccerFlowZone> Flows { get; }

        public IReadOnlyList<SoccerGeyser> Geysers { get; }

        /// <summary>ピッチの中のバンパーの柱（当たりは Walls に多角形で入っている。これは見た目用）</summary>
        public IReadOnlyList<SoccerPost> Posts { get; }

        public IReadOnlyList<SoccerGoal> Goals => _goals;

        /// <summary>team が守るゴール</summary>
        public SoccerGoal GoalOf(int team) => _goals[team];

        /// <summary>team が攻める向き（X の正負）。赤は右（+1）、青は左（−1）</summary>
        public static float AttackDirection(int team) => team == SoccerRules.Red ? 1f : -1f;

        /// <summary>後に足した範囲ほど上に重なる（後のものが勝つ）</summary>
        public SoccerGround GroundAt(Vec2 position)
        {
            for (var i = Grounds.Count - 1; i >= 0; i--)
            {
                if (Grounds[i].Contains(position))
                {
                    return Grounds[i].Ground;
                }
            }

            return SoccerGround.Grass;
        }

        /// <summary>キックオフの位置。並びが足りないときは最後の位置から縦にずらす（試しで人数を増やしたとき用）</summary>
        public Vec2 KickoffSpot(int team, int index, bool kicking)
        {
            var spots = kicking ? _kickingSpots : _waitingSpots;
            var last = spots.Length - 1;
            var spot = index <= last ? spots[index] : spots[last] + new Vec2(0f, index - last);
            return team == SoccerRules.Red ? spot : new Vec2(-spot.X, spot.Y);
        }

        public bool IsInside(Vec2 position) => MiniGameGeometry.Contains(Outline, position);

        private Vec2[] BuildOutline()
        {
            float x = HalfLength, y = HalfWidth, c = CornerCut, g = _goals[SoccerRules.Blue].HalfWidth;
            return new[]
            {
                new Vec2(-x + c, -y), new Vec2(x - c, -y), new Vec2(x, -y + c),
                new Vec2(x, -g), new Vec2(x, g),
                new Vec2(x, y - c), new Vec2(x - c, y), new Vec2(-x + c, y), new Vec2(-x, y - c),
                new Vec2(-x, g), new Vec2(-x, -g),
                new Vec2(-x, -y + c),
            };
        }

        /// <summary>
        /// 外周を順に壁にする。ゴールの両端の点の間だけゴールの口にし、その奥に網の箱を足す。
        /// バンパーが乗る辺は、板・バンパー・板に分ける
        /// </summary>
        private SoccerWall[] BuildWalls()
        {
            var walls = new List<SoccerWall>();
            for (var i = 0; i < Outline.Count; i++)
            {
                var a = Outline[i];
                var b = Outline[(i + 1) % Outline.Count];
                var goal = MouthGoal(a, b);
                if (goal != null)
                {
                    walls.Add(new SoccerWall(a, b, SoccerWallKind.GoalMouth, goal.Team));
                }
                else
                {
                    AddBoardEdge(walls, a, b);
                }
            }

            foreach (var goal in _goals)
            {
                AddNet(walls, goal);
            }

            foreach (var post in Posts)
            {
                AddPost(walls, post);
            }

            return walls.ToArray();
        }

        /// <summary>ゴールの両端の点どうしを結ぶ辺なら、そのゴール（ゴールの線の上の辺はゴールの口だけ）</summary>
        private SoccerGoal MouthGoal(Vec2 a, Vec2 b)
        {
            foreach (var goal in _goals)
            {
                if (a.X == goal.LineX && b.X == goal.LineX && Math.Abs(a.Y) == goal.HalfWidth && Math.Abs(b.Y) == goal.HalfWidth)
                {
                    return goal;
                }
            }

            return null;
        }

        /// <summary>
        /// 外周の辺 a → b を板にする。辺の上にバンパーがあれば、その所だけバンパーにする（向きは辺と同じ左回りにそろえる。
        /// 見た目が辺の右手を外として置くため）
        /// </summary>
        private void AddBoardEdge(List<SoccerWall> walls, Vec2 a, Vec2 b)
        {
            var edge = b - a;
            var length = edge.Length;
            var along = edge / length;
            var spans = new List<(float from, float to)>();
            foreach (var bumper in _bumpers)
            {
                if (TryProject(a, along, length, bumper.A, out var ta) && TryProject(a, along, length, bumper.B, out var tb) && Math.Abs(ta - tb) > OnEdgeTolerance)
                {
                    spans.Add((Math.Min(ta, tb), Math.Max(ta, tb)));
                }
            }

            spans.Sort((x, y) => x.from.CompareTo(y.from));
            var at = 0f;
            foreach (var (from, to) in spans)
            {
                if (from > at + OnEdgeTolerance)
                {
                    walls.Add(new SoccerWall(a + along * at, a + along * from, SoccerWallKind.Board));
                }

                walls.Add(new SoccerWall(a + along * Math.Max(at, from), a + along * to, SoccerWallKind.Bumper));
                at = Math.Max(at, to);
            }

            if (length > at + OnEdgeTolerance)
            {
                walls.Add(new SoccerWall(a + along * at, b, SoccerWallKind.Board));
            }
        }

        /// <summary>点 p が辺（a から along へ length）の上にあれば、a からの距離</summary>
        private static bool TryProject(Vec2 a, Vec2 along, float length, Vec2 p, out float t)
        {
            t = Vec2.Dot(p - a, along);
            var off = Vec2.Distance(a + along * t, p);
            return off <= OnEdgeTolerance && t >= -OnEdgeTolerance && t <= length + OnEdgeTolerance;
        }

        /// <summary>
        /// 柱を、半径の外に接する多角形の壁にする（円の当たりを別に作らず、壁の跳ね返り・子の押し出しをそのまま使うため）。
        /// 外へ向かって押し出すよう、柱は右回りに並べる（線の真上のときに押す「左手」が柱の外になる）
        /// </summary>
        private static void AddPost(List<SoccerWall> walls, SoccerPost post)
        {
            var sides = SoccerPost.Sides;
            // 辺の真ん中が半径に来るよう、角は少し外へ出す
            var corner = post.Radius / (float)Math.Cos(Math.PI / sides);
            for (var i = 0; i < sides; i++)
            {
                var a = post.Center + Corner(-i, sides) * corner;
                var b = post.Center + Corner(-(i + 1), sides) * corner;
                walls.Add(new SoccerWall(a, b, SoccerWallKind.Post));
            }
        }

        private static Vec2 Corner(int index, int sides)
        {
            var angle = Math.PI * 2.0 * index / sides;
            return new Vec2((float)Math.Cos(angle), (float)Math.Sin(angle));
        }

        /// <summary>網の箱の横2枚と奥1枚（ゴールの口の両端から外へ）</summary>
        private static void AddNet(List<SoccerWall> walls, SoccerGoal goal)
        {
            var backX = goal.LineX + goal.Outward * goal.Depth;
            var lowPost = new Vec2(goal.LineX, -goal.HalfWidth);
            var highPost = new Vec2(goal.LineX, goal.HalfWidth);
            var lowBack = new Vec2(backX, -goal.HalfWidth);
            var highBack = new Vec2(backX, goal.HalfWidth);
            walls.Add(new SoccerWall(lowPost, lowBack, SoccerWallKind.Net, goal.Team));
            walls.Add(new SoccerWall(lowBack, highBack, SoccerWallKind.Net, goal.Team));
            walls.Add(new SoccerWall(highBack, highPost, SoccerWallKind.Net, goal.Team));
        }
    }

    public enum SoccerWallKind
    {
        /// <summary>ピッチを囲む低い板。ボールはいつも跳ね返る（外へ出ない）</summary>
        Board,
        /// <summary>ゴールの口。バーより低いボールは通り抜け、高いボールはバーに当たって跳ね返る。ペンモンは通れない</summary>
        GoalMouth,
        /// <summary>ゴールの網。勢いを吸う</summary>
        Net,
        /// <summary>壁のバンパー（嵐の崖）。ボールを強く跳ね返す。ペンモンには板と同じ</summary>
        Bumper,
        /// <summary>ピッチの中のバンパーの柱（嵐の崖）。跳ね返り方はバンパーと同じ。ペンモンは回り込む</summary>
        Post,
    }

    /// <summary>壁1枚（線分）</summary>
    public readonly struct SoccerWall
    {
        public readonly Vec2 A;
        public readonly Vec2 B;
        public readonly SoccerWallKind Kind;

        /// <summary>ゴールの口・網なら、そのゴールを守るチーム。板は −1</summary>
        public readonly int Team;

        public SoccerWall(Vec2 a, Vec2 b, SoccerWallKind kind, int team = -1)
        {
            A = a;
            B = b;
            Kind = kind;
            Team = team;
        }

        /// <summary>ボールを強く跳ね返す（壁のバンパー・柱）</summary>
        public bool IsBumper => Kind == SoccerWallKind.Bumper || Kind == SoccerWallKind.Post;
    }

    /// <summary>ゴール1つ。口は左右の壁の真ん中（Y が −HalfWidth〜HalfWidth）</summary>
    public sealed class SoccerGoal
    {
        public SoccerGoal(int team, float lineX, float outward, float halfWidth, float barHeight, float depth)
        {
            Team = team;
            LineX = lineX;
            Outward = outward;
            HalfWidth = halfWidth;
            BarHeight = barHeight;
            Depth = depth;
        }

        /// <summary>このゴールを守るチーム（ここに入ると相手の点）</summary>
        public int Team { get; }

        /// <summary>ゴールの線の X</summary>
        public float LineX { get; }

        /// <summary>ピッチの外へ向かう X の向き（左のゴールは −1）</summary>
        public float Outward { get; }

        public float HalfWidth { get; }

        public float BarHeight { get; }

        public float Depth { get; }

        public Vec2 Center => new Vec2(LineX, 0f);

        /// <summary>ボールの中心がゴールの線をどれだけ越えたか（ピッチの中なら負）</summary>
        public float DepthPastLine(Vec2 position) => (position.X - LineX) * Outward;

        /// <summary>この高さ（ボールの下の端）のボールがバーの下を通れるか（ボールの上の端がバーより低い）</summary>
        public bool PassesUnderBar(float height) => height + SoccerBalance.BallRadius * 2f <= BarHeight;
    }

    public enum SoccerGround
    {
        Grass,
        /// <summary>氷の床（氷山）。ボールがよく転がり、子は歩き出し・止まりがすべる</summary>
        Ice,
    }

    /// <summary>地面の種類の範囲（左下 Min・右上 Max の長方形）</summary>
    public readonly struct SoccerGroundZone
    {
        public readonly SoccerGround Ground;
        public readonly Vec2 Min;
        public readonly Vec2 Max;

        public SoccerGroundZone(SoccerGround ground, Vec2 min, Vec2 max)
        {
            Ground = ground;
            Min = min;
            Max = max;
        }

        public bool Contains(Vec2 p) => p.X >= Min.X && p.X <= Max.X && p.Y >= Min.Y && p.Y <= Max.Y;
    }
}
