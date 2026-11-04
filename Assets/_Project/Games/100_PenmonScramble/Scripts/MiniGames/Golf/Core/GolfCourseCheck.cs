using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PenmonScramble.Core;

namespace PenmonScramble.Golf.Core
{
    /// <summary>1ホールの確かめの結果</summary>
    public sealed class GolfHoleCheck
    {
        public string HoleName;
        public int Par;

        /// <summary>試しの打ち手が入れられた一番少ない打数（パー ＋ 1 打まで探して入らなければ 0）</summary>
        public int BestStrokes;

        /// <summary>ティーから1打で入る打ち方の割合（0〜1。簡単すぎないかの目安）</summary>
        public float HoleInOneRate;

        /// <summary>止まった所の数（マス目で数える）</summary>
        public int CellsReached;

        /// <summary>どう打っても同じ所から出られない（詰んだ）所の数と、その1つ</summary>
        public int StuckCells;

        public Vec2 StuckExample;

        /// <summary>止まらずに転がる時間の上限まで動き続けた打ち方の数（坂・帯の作りの誤り）</summary>
        public int EndlessShots;

        /// <summary>置き方の誤り（ティー・カップが池や壁や動く物に掛かっているなど）</summary>
        public List<string> Problems = new List<string>();

        public bool Passed => BestStrokes > 0 && BestStrokes <= Par && StuckCells == 0 && EndlessShots == 0 && Problems.Count == 0;

        public override string ToString()
        {
            var best = BestStrokes > 0 ? $"{BestStrokes}打" : "入らない";
            var stuck = StuckCells > 0 ? $" 詰む所 {StuckCells}（{StuckExample}）" : string.Empty;
            var endless = EndlessShots > 0 ? $" 止まらない打ち方 {EndlessShots}" : string.Empty;
            var problems = Problems.Count > 0 ? " " + string.Join(" / ", Problems) : string.Empty;
            return $"{(Passed ? "OK " : "NG ")}{HoleName} パー{Par}: 最少 {best}・1打で入る {HoleInOneRate:P1}・止まる所 {CellsReached}{stuck}{endless}{problems}";
        }
    }

    /// <summary>
    /// コースの確かめ（Tools/PenmonScramble/Golf/コースの確認）。試しの打ち手に向きと強さを総当たりさせ、
    /// 「パー以内で入る打ち方がある」「詰んで出られない所が無い」「止まらない所が無い」を確かめる。
    /// 動く物・間欠泉のあるホールは、打つ時刻もずらして試す（待ってから打つのと同じ）。球の計算は本番と同じ GolfHoleWorld
    /// </summary>
    public static class GolfCourseCheck
    {
        public const int AngleSteps = 96;
        public const int PowerSteps = 12;

        /// <summary>時間で様子が変わるホールで、1回りの間に打つ時刻を何通り試すか</summary>
        public const int PhaseSteps = 4;

        /// <summary>止まった所をまとめるマス目の大きさ（m）。同じマスは同じ所として1回だけ調べる</summary>
        public const float CellSize = 0.6f;

        /// <summary>一番弱い打ち方の強さ（0 だとほとんど動かず、詰んでいるか調べる役に立たない）</summary>
        private const float MinPower = 0.04f;

        public static GolfHoleCheck CheckHole(GolfHoleDef hole)
        {
            var result = new GolfHoleCheck { HoleName = hole.Name, Par = hole.Par };
            CheckPlacement(hole, result.Problems);
            var shots = ShotList(hole);
            var depthOf = new Dictionary<(int, int), int>();
            var frontier = new List<Vec2> { hole.Tee };
            depthOf[CellOf(hole.Tee)] = 0;
            var maxDepth = hole.Par + 1;
            for (var depth = 0; depth < maxDepth && frontier.Count > 0; depth++)
            {
                var outcomes = new Outcome[frontier.Count][];
                Parallel.For(0, frontier.Count, i => outcomes[i] = TryAll(hole, frontier[i], shots));
                var next = new List<Vec2>();
                for (var i = 0; i < frontier.Count; i++)
                {
                    Collect(result, frontier[i], outcomes[i], depth, depthOf, next);
                }

                frontier = next;
            }

            result.CellsReached = depthOf.Count;
            return result;
        }

        private static void Collect(GolfHoleCheck result, Vec2 from, Outcome[] outcomes, int depth, Dictionary<(int, int), int> depthOf, List<Vec2> next)
        {
            var home = CellOf(from);
            var leaves = false;
            var holeInOnes = 0;
            foreach (var outcome in outcomes)
            {
                result.EndlessShots += outcome.Endless ? 1 : 0;
                if (outcome.InCup)
                {
                    leaves = true;
                    holeInOnes++;
                    if (result.BestStrokes == 0 || depth + outcome.Strokes < result.BestStrokes)
                    {
                        result.BestStrokes = depth + outcome.Strokes;
                    }

                    continue;
                }

                var cell = CellOf(outcome.Position);
                leaves |= cell != home;
                if (!depthOf.ContainsKey(cell))
                {
                    depthOf[cell] = depth + 1;
                    next.Add(outcome.Position);
                }
            }

            if (depth == 0)
            {
                result.HoleInOneRate = (float)holeInOnes / outcomes.Length;
            }

            if (!leaves)
            {
                result.StuckCells++;
                result.StuckExample = from;
            }
        }

        /// <summary>4人分のティーの位置とカップが、地面の上の空いた所にあるか</summary>
        private static void CheckPlacement(GolfHoleDef hole, List<string> problems)
        {
            var sim = new GolfBallSim(hole);
            for (var player = 0; player < GolfRules.MaxPlayers; player++)
            {
                var tee = GolfHoleWorld.TeePosition(hole, player, GolfRules.MaxPlayers);
                if (!IsClear(hole, sim, tee))
                {
                    problems.Add($"ティー{player + 1}が空いていない {tee}");
                }
            }

            if (sim.IsHazard(hole.Cup))
            {
                problems.Add("カップが池・場外");
            }
        }

        private static bool IsClear(GolfHoleDef hole, GolfBallSim sim, Vec2 point)
        {
            if (sim.IsHazard(point))
            {
                return false;
            }

            foreach (var wall in hole.Walls)
            {
                if (Vec2.Distance(GolfGeometry.ClosestOnSegment(wall.A, wall.B, point), point) < GolfBalance.BallRadius)
                {
                    return false;
                }
            }

            foreach (var mover in hole.Movers)
            {
                if (mover.TryPushOutOfSweep(point, out _))
                {
                    return false;
                }
            }

            foreach (var warp in hole.Warps)
            {
                if (warp.Catches(point))
                {
                    return false;
                }
            }

            return true;
        }

        private static (int, int) CellOf(Vec2 point) => ((int)MathF.Floor(point.X / CellSize), (int)MathF.Floor(point.Y / CellSize));

        /// <summary>試す打ち方の一覧（打つ時刻・向き・強さ）</summary>
        private static List<(float time, float angle, float power)> ShotList(GolfHoleDef hole)
        {
            var period = hole.GimmickPeriod;
            var phases = period > 0f ? PhaseSteps : 1;
            var shots = new List<(float, float, float)>();
            for (var phase = 0; phase < phases; phase++)
            {
                for (var a = 0; a < AngleSteps; a++)
                {
                    for (var p = 0; p < PowerSteps; p++)
                    {
                        var power = MinPower + (1f - MinPower) * p / (PowerSteps - 1);
                        shots.Add((period * phase / phases, MathF.PI * 2f * a / AngleSteps, power));
                    }
                }
            }

            return shots;
        }

        private static Outcome[] TryAll(GolfHoleDef hole, Vec2 from, List<(float time, float angle, float power)> shots)
        {
            var outcomes = new Outcome[shots.Count];
            for (var i = 0; i < shots.Count; i++)
            {
                outcomes[i] = Simulate(hole, from, shots[i].time, shots[i].angle, shots[i].power);
            }

            return outcomes;
        }

        /// <summary>球を1つだけ置いたホールで1打を打ち、止まるまで進める（本番と同じ進め方）</summary>
        internal static Outcome Simulate(GolfHoleDef hole, Vec2 from, float time, float angle, float power)
        {
            var world = GolfHoleWorld.ForCheck(hole, from, time);
            world.Shoot(0, angle, power);
            var guard = (int)(GolfBalance.MaxRollSeconds / GolfBalance.StepSeconds) + 2;
            while (world.Phase == GolfHolePhase.Rolling && guard-- > 0)
            {
                world.Tick(GolfBalance.StepSeconds);
            }

            var ball = world.Balls[0];
            return new Outcome
            {
                InCup = ball.InCup,
                Position = ball.Position,
                Strokes = world.Strokes(0),
                Endless = world.LastRollSeconds >= GolfBalance.MaxRollSeconds,
            };
        }

        internal struct Outcome
        {
            public bool InCup;
            public Vec2 Position;

            /// <summary>この1打で増えた打数（池に落ちれば 2）</summary>
            public int Strokes;

            public bool Endless;
        }
    }
}
