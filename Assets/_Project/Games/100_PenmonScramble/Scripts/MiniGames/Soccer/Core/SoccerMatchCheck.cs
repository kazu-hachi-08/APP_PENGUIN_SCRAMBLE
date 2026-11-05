using System;
using System.Collections.Generic;
using PenmonScramble.Core;

namespace PenmonScramble.Soccer.Core
{
    /// <summary>CPU 同士の1試合の数え上げ（SoccerMatchCheck）</summary>
    public sealed class SoccerMatchStats
    {
        public readonly int[] Goals = new int[SoccerRules.TeamCount];
        public int Shots;
        public int Passes;
        public int Tackles;
        public int SlideMisses;
        public int KeeperCatches;
        public int Nudges;

        /// <summary>赤の人が動かす子が替わった回数と、そのうち FlipSeconds 以内に1つ前の子へ戻った回数（ガタつき）</summary>
        public int ControlSwitches;
        public int FlipSwitches;

        /// <summary>誰も触らないまま続いた、いちばん長い秒</summary>
        public float LongestIdleSeconds;

        /// <summary>離れたボールが壁ぎわでほとんど止まっていた秒の合計（詰まり）</summary>
        public float WallStuckSeconds;

        /// <summary>ボールのまわりにいた子の数の平均と、CrowdLimit 体以上が群がっていた割合（団子）</summary>
        public float CrowdAverage;
        public float CrowdRate;

        public int Winner => Goals[0] == Goals[1] ? -1 : Goals[0] > Goals[1] ? SoccerRules.Red : SoccerRules.Blue;
    }

    /// <summary>
    /// CPU 同士の試合の確認（Docs/MINIGAME_SOCCER_PLAN.md フェーズ 3）。描かずに計算だけで1試合を回し、
    /// 点の数・誰も触らない時間・壁ぎわの詰まり・団子・強さの差での勝ち負けを数える（エディタのメニューとテストから使う）。
    /// 赤には人の切り替え（SoccerControl）を横で動かし、動かす子がガタガタ替わらないかも数える（子は AI が動かしたまま）。
    /// 点が入ったら少し転がしてから、取られた側のキックオフで並び直す（演出の間は時間に数えない）
    /// </summary>
    public static class SoccerMatchCheck
    {
        /// <summary>ボールのまわりとみなす距離と、団子とみなす数</summary>
        public const float CrowdRadius = 2.5f;
        public const int CrowdLimit = 4;

        /// <summary>替わってからこれより短い間に1つ前の子へ戻ったら、ガタつきとみなす</summary>
        public const float FlipSeconds = 1f;

        // 点が入ってから並び直すまでの秒（ボールが網へ入るのを待つ）
        private const float GoalPauseSeconds = 1f;

        // 壁ぎわの詰まりとみなす、壁までの距離と、ボールの速さの上限
        private const float StuckWallDistance = 1f;
        private const float StuckSpeed = 1f;

        public static SoccerMatchStats Play(SoccerAiLevel red, SoccerAiLevel blue, int seed, float seconds = SoccerRules.MatchSeconds)
        {
            var world = new SoccerWorld(SoccerPitchTable.Standard(), SoccerRules.PlayersPerTeam, SoccerRules.PlayersPerTeam, keepers: true, seed: seed);
            var ai = new SoccerAi(world, seed + 1);
            ai.SetLevel(SoccerRules.Red, red);
            ai.SetLevel(SoccerRules.Blue, blue);

            var stats = new SoccerMatchStats();
            var inputs = new SoccerInput[world.Players.Count];
            var events = new List<SoccerEvent>();
            var control = new SoccerControl(SoccerRules.Red, 0);
            var sinceSwitch = float.MaxValue;
            var before = SoccerBall.NoPlayer;
            var steps = (int)(seconds / SoccerRules.StepSeconds);
            var crowdTotal = 0;
            var crowdSteps = 0;
            for (var step = 0; step < steps; step++)
            {
                var controlled = control.Controlled;
                control.Update(world, SoccerInput.None, false, events);
                sinceSwitch += SoccerRules.StepSeconds;
                if (control.Controlled != controlled)
                {
                    stats.ControlSwitches++;
                    stats.FlipSwitches += control.Controlled == before && sinceSwitch < FlipSeconds ? 1 : 0;
                    before = controlled;
                    sinceSwitch = 0f;
                }

                events.Clear();
                ai.Think(world, inputs, _ => false);
                world.Step(inputs, events);
                Count(stats, events);
                var near = CountNear(world);
                crowdTotal += near;
                crowdSteps += near >= CrowdLimit ? 1 : 0;
                Watch(stats, world);
                if (world.IsGoalScored)
                {
                    PauseAndKickoff(world, events);
                }
            }

            stats.CrowdAverage = (float)crowdTotal / steps;
            stats.CrowdRate = (float)crowdSteps / steps;
            stats.Goals[SoccerRules.Red] = world.Score(SoccerRules.Red);
            stats.Goals[SoccerRules.Blue] = world.Score(SoccerRules.Blue);
            return stats;
        }

        private static void Count(SoccerMatchStats stats, List<SoccerEvent> events)
        {
            foreach (var e in events)
            {
                switch (e.Kind)
                {
                    case SoccerEventKind.Kick:
                        stats.Shots++;
                        break;
                    case SoccerEventKind.Pass:
                        stats.Passes++;
                        break;
                    case SoccerEventKind.Tackle:
                        stats.Tackles++;
                        break;
                    case SoccerEventKind.SlideMiss:
                        stats.SlideMisses++;
                        break;
                    case SoccerEventKind.KeeperCatch:
                        stats.KeeperCatches++;
                        break;
                    case SoccerEventKind.Nudge:
                        stats.Nudges++;
                        break;
                }
            }
        }

        private static int CountNear(SoccerWorld world)
        {
            var near = 0;
            foreach (var player in world.Players)
            {
                near += Vec2.Distance(player.Position, world.Ball.Position) <= CrowdRadius ? 1 : 0;
            }

            return near;
        }

        private static void Watch(SoccerMatchStats stats, SoccerWorld world)
        {
            var ball = world.Ball;
            stats.LongestIdleSeconds = Math.Max(stats.LongestIdleSeconds, ball.IdleSeconds);
            if (ball.IsHeld || ball.Velocity.Length > StuckSpeed)
            {
                return;
            }

            var pitch = world.Pitch;
            var nearSide = pitch.HalfWidth - Math.Abs(ball.Position.Y) < StuckWallDistance;
            var nearEnd = pitch.HalfLength - Math.Abs(ball.Position.X) < StuckWallDistance;
            if (nearSide || nearEnd)
            {
                stats.WallStuckSeconds += SoccerRules.StepSeconds;
            }
        }

        private static void PauseAndKickoff(SoccerWorld world, List<SoccerEvent> events)
        {
            var conceding = SoccerRules.Opponent(events.Find(e => e.Kind == SoccerEventKind.Goal).Team);
            for (var t = 0f; t < GoalPauseSeconds; t += SoccerRules.StepSeconds)
            {
                world.Step(null, events);
            }

            world.Kickoff(conceding, events);
        }
    }
}
