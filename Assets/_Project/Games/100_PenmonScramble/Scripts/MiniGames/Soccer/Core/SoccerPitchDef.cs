using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>
    /// ピッチ1面の形（コードの定義表 SoccerPitchTable から作る）。真ん中が原点、X が横（ゴールの向き）、Y が縦。
    /// 横長の長方形の四隅を斜めに切り（ボールが角に詰まらないように）、左右の壁の真ん中にゴールの口を開けた外周を、
    /// ボールが外へ出ない壁にする。ゴールの口の奥には網の箱（SoccerWallKind.Net）を置く
    /// </summary>
    public sealed class SoccerPitchDef
    {
        private readonly SoccerGoal[] _goals;
        private readonly Vec2[] _kickingSpots;
        private readonly Vec2[] _waitingSpots;

        /// <param name="halfLength">横の長さの半分（ゴールの線の X）</param>
        /// <param name="halfWidth">縦の長さの半分</param>
        /// <param name="cornerCut">四隅を斜めに切る長さ</param>
        /// <param name="kickingSpots">キックオフで蹴る側の赤の並び（先頭がボールのすぐ後ろ）。青は左右を反対にして使う</param>
        /// <param name="waitingSpots">キックオフで蹴らない側の赤の並び</param>
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
            IReadOnlyList<SoccerGroundZone> grounds = null)
        {
            Name = name;
            HalfLength = halfLength;
            HalfWidth = halfWidth;
            CornerCut = cornerCut;
            Grounds = grounds ?? new SoccerGroundZone[0];
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

        /// <summary>地面の種類の範囲（仕掛けはフェーズ 6）。どれにも入らない所は芝</summary>
        public IReadOnlyList<SoccerGroundZone> Grounds { get; }

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

        /// <summary>外周を順に壁にする。ゴールの両端の点の間だけゴールの口にし、その奥に網の箱を足す</summary>
        private SoccerWall[] BuildWalls()
        {
            var walls = new List<SoccerWall>();
            for (var i = 0; i < Outline.Count; i++)
            {
                var a = Outline[i];
                var b = Outline[(i + 1) % Outline.Count];
                var goal = MouthGoal(a, b);
                walls.Add(goal != null
                    ? new SoccerWall(a, b, SoccerWallKind.GoalMouth, goal.Team)
                    : new SoccerWall(a, b, SoccerWallKind.Board));
            }

            foreach (var goal in _goals)
            {
                AddNet(walls, goal);
            }

            return walls.ToArray();
        }

        /// <summary>ゴールの両端の点どうしを結ぶ辺なら、そのゴール（ゴールの線の上の辺はゴールの口だけ）</summary>
        private SoccerGoal MouthGoal(Vec2 a, Vec2 b)
        {
            foreach (var goal in _goals)
            {
                if (a.X == goal.LineX && b.X == goal.LineX && System.Math.Abs(a.Y) == goal.HalfWidth && System.Math.Abs(b.Y) == goal.HalfWidth)
                {
                    return goal;
                }
            }

            return null;
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
