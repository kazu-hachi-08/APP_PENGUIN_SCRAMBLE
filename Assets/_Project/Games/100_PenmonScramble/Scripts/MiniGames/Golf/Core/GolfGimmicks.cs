using System;
using System.Collections.Generic;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;

namespace PenmonScramble.Golf.Core
{
    /// <summary>
    /// 決まった間隔のくり返し（Period 秒ごとに、はじめの Active 秒だけ「動いている」）。Offset 秒ずらして始める。
    /// 時間だけで決まるので、ホストとゲストで同じ時刻なら同じ様子になる
    /// </summary>
    public readonly struct GolfCycle
    {
        public readonly float Period;
        public readonly float Active;
        public readonly float Offset;

        public GolfCycle(float period, float active, float offset = 0f)
        {
            Period = period;
            Active = active;
            Offset = offset;
        }

        /// <summary>1回りの中のどこか（0〜Period）</summary>
        public float Local(float time)
        {
            if (Period <= 0f)
            {
                return 0f;
            }

            var t = (time + Offset) % Period;
            return t < 0f ? t + Period : t;
        }

        public bool IsActive(float time) => Period > 0f && Local(time) < Active;

        /// <summary>次に動き出すまでの秒（動いている間は 0）。見た目の「もうすぐ噴く」に使う</summary>
        public float UntilActive(float time) => IsActive(time) ? 0f : Period - Local(time);
    }

    /// <summary>動く物の見た目の種類（当たり方は同じ。見た目だけ変える）</summary>
    public enum GolfMoverLook
    {
        /// <summary>行ったり来たりする壁（流氷・岩などの塊）</summary>
        Block,
        /// <summary>回る風車の羽根</summary>
        Windmill,
    }

    /// <summary>
    /// 動く物（動く壁・回る風車・動く岩）。形は Pivot を原点にした壁の並び（LocalWalls）で持ち、
    /// 時刻から位置（行ったり来たり）と向き（回る）が決まる。球は動く壁の速さも受けて弾かれる
    /// </summary>
    public sealed class GolfMover
    {
        private readonly Vec2[] _sweep;
        private readonly float _sweepRadius;

        private GolfMover(GolfMoverLook look, Vec2 pivot, IReadOnlyList<GolfWall> localWalls, Vec2 travel, int spin, float period, float phase)
        {
            Look = look;
            Pivot = pivot;
            LocalWalls = localWalls;
            Travel = travel;
            Spin = spin;
            Period = period;
            Phase = phase;
            if (spin != 0)
            {
                _sweepRadius = MaxReach(localWalls);
            }
            else
            {
                _sweep = SlideHull(pivot, localWalls, travel);
            }
        }

        public GolfMoverLook Look { get; }

        /// <summary>回る中心（行ったり来たりするなら、動き始めの位置）</summary>
        public Vec2 Pivot { get; }

        /// <summary>Pivot を原点にした形（壁の並び）</summary>
        public IReadOnlyList<GolfWall> LocalWalls { get; }

        /// <summary>行ったり来たりする幅（Pivot から Pivot + Travel まで）。回る物は 0</summary>
        public Vec2 Travel { get; }

        /// <summary>回る向き（+1 で上から見て左回り、−1 で右回り、0 で回らない）</summary>
        public int Spin { get; }

        /// <summary>1往復（1回転）の秒</summary>
        public float Period { get; }

        /// <summary>動き始めのずれ（0〜1 回り）</summary>
        public float Phase { get; }

        /// <summary>
        /// 行ったり来たりする塊。center を中心にした幅 width・奥行き depth の箱が、center から center + travel まで動く
        /// （端でゆっくり止まって折り返す）
        /// </summary>
        public static GolfMover Block(Vec2 center, float width, float depth, Vec2 travel, float period, float phase = 0f,
            float bounce = GolfBalance.WallBounce, float height = GolfBalance.TallWallHeight)
        {
            var walls = GolfHoleDef.WallsAround(MiniGameGeometry.Rect(-width * 0.5f, -depth * 0.5f, width * 0.5f, depth * 0.5f), bounce, height);
            return new GolfMover(GolfMoverLook.Block, center, walls, travel, 0, period, phase);
        }

        /// <summary>回る風車の羽根。center から armLength の羽根が arms 本、period 秒で1回転する</summary>
        /// <param name="spin">+1 で上から見て左回り、−1 で右回り</param>
        public static GolfMover Windmill(Vec2 center, float armLength, int arms, float period, int spin = -1, float phase = 0f)
        {
            var walls = new List<GolfWall>();
            for (var i = 0; i < arms; i++)
            {
                var direction = GolfHoleWorld.Direction(MathF.PI * 2f * i / arms);
                walls.Add(new GolfWall(Vec2.Zero, direction * armLength, GolfBalance.WallBounce, GolfBalance.TallWallHeight));
            }

            return new GolfMover(GolfMoverLook.Windmill, center, walls, Vec2.Zero, spin < 0 ? -1 : 1, period, phase);
        }

        /// <summary>この時刻の1回りの中の位置（0〜1）</summary>
        private float Turn(float time) => Period > 0f ? time / Period + Phase : 0f;

        /// <summary>行ったり来たりの進み具合（0〜1。端でゆっくりになる）</summary>
        public float SlideAmount(float time) => (1f - MathF.Cos(Turn(time) * MathF.PI * 2f)) * 0.5f;

        /// <summary>この時刻の中心の位置</summary>
        public Vec2 Center(float time) => Pivot + Travel * SlideAmount(time);

        /// <summary>この時刻の向き（ラジアン。左回りが正）</summary>
        public float Angle(float time) => Spin * Turn(time) * MathF.PI * 2f;

        public Vec2 ToWorld(Vec2 local, float time) => Center(time) + local.Rotated(Angle(time));

        /// <summary>この時刻の壁（地面の上の位置）</summary>
        public GolfWall WallAt(int index, float time)
        {
            var wall = LocalWalls[index];
            return new GolfWall(ToWorld(wall.A, time), ToWorld(wall.B, time), wall.Bounce, wall.Height);
        }

        /// <summary>この時刻に、地面の上のこの点が動いている速さ（行ったり来たり ＋ 回る分）</summary>
        public Vec2 VelocityAt(Vec2 point, float time)
        {
            if (Period <= 0f)
            {
                return Vec2.Zero;
            }

            var slide = Travel * (MathF.PI / Period * MathF.Sin(Turn(time) * MathF.PI * 2f));
            if (Spin == 0)
            {
                return slide;
            }

            var omega = Spin * MathF.PI * 2f / Period;
            var r = point - Center(time);
            return slide + new Vec2(-omega * r.Y, omega * r.X);
        }

        /// <summary>
        /// 動く物が通る範囲に、球（中心が point）が掛かっているか。掛かっていれば、範囲の外の一番近い位置を away に入れる。
        /// 止まった球はこの範囲の外へよける（狙っている間は球を動かさないので、動く物が球をすり抜けないように）
        /// </summary>
        public bool TryPushOutOfSweep(Vec2 point, out Vec2 away)
        {
            var clearance = GolfBalance.BallRadius + GolfBalance.SweepClearance;
            return Spin != 0 ? PushOutOfDisk(point, clearance, out away) : PushOutOfHull(point, clearance, out away);
        }

        private bool PushOutOfDisk(Vec2 point, float clearance, out Vec2 away)
        {
            var offset = point - Pivot;
            var reach = _sweepRadius + clearance;
            if (offset.Length >= reach)
            {
                away = point;
                return false;
            }

            var direction = offset.IsNearlyZero ? Vec2.Right : offset.Normalized;
            away = Pivot + direction * (reach + 0.01f);
            return true;
        }

        /// <summary>凸な範囲（左回り）の縁からの距離で判定し、一番近い辺の外へ出す</summary>
        private bool PushOutOfHull(Vec2 point, float clearance, out Vec2 away)
        {
            var inside = true;
            var nearest = float.MaxValue;
            var best = point;
            for (var i = 0; i < _sweep.Length; i++)
            {
                var a = _sweep[i];
                var b = _sweep[(i + 1) % _sweep.Length];
                var closest = MiniGameGeometry.ClosestOnSegment(a, b, point);
                var edge = b - a;
                var outward = new Vec2(edge.Y, -edge.X).Normalized;
                var side = Vec2.Dot(point - a, outward);
                inside &= side <= 0f;
                var distance = Vec2.Distance(point, closest);
                if (distance < nearest)
                {
                    nearest = distance;
                    best = closest + outward * (clearance + 0.01f);
                }
            }

            if (!inside && nearest >= clearance)
            {
                away = point;
                return false;
            }

            if (!inside)
            {
                // 外にいて近すぎるだけなら、いま離れている向きへそのまま押し出す
                var closest = ClosestOnHull(point);
                best = closest + (point - closest).Normalized * (clearance + 0.01f);
            }

            away = best;
            return true;
        }

        private Vec2 ClosestOnHull(Vec2 point)
        {
            var nearest = float.MaxValue;
            var best = point;
            for (var i = 0; i < _sweep.Length; i++)
            {
                var closest = MiniGameGeometry.ClosestOnSegment(_sweep[i], _sweep[(i + 1) % _sweep.Length], point);
                var distance = Vec2.Distance(point, closest);
                if (distance < nearest)
                {
                    nearest = distance;
                    best = closest;
                }
            }

            return best;
        }

        private static float MaxReach(IReadOnlyList<GolfWall> walls)
        {
            var reach = 0f;
            foreach (var wall in walls)
            {
                reach = Math.Max(reach, Math.Max(wall.A.Length, wall.B.Length));
            }

            return reach;
        }

        /// <summary>行ったり来たりする物が通る範囲（両端の形を囲む凸な多角形）</summary>
        private static Vec2[] SlideHull(Vec2 pivot, IReadOnlyList<GolfWall> walls, Vec2 travel)
        {
            var points = new List<Vec2>();
            foreach (var wall in walls)
            {
                points.Add(pivot + wall.A);
                points.Add(pivot + wall.B);
                points.Add(pivot + travel + wall.A);
                points.Add(pivot + travel + wall.B);
            }

            return MiniGameGeometry.ConvexHull(points);
        }
    }

    /// <summary>ワープの泡。入口（Entry）に地面を転がる球が入ると、速さと向きはそのままで出口（Exit）へ飛ぶ。片道だけ</summary>
    public readonly struct GolfWarp
    {
        public readonly Vec2 Entry;
        public readonly Vec2 Exit;

        public GolfWarp(Vec2 entry, Vec2 exit)
        {
            Entry = entry;
            Exit = exit;
        }

        public bool Catches(Vec2 point) => Vec2.Distance(point, Entry) < GolfBalance.WarpRadius;
    }
}
