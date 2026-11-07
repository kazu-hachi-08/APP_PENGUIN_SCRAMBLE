using System;
using System.Collections.Generic;

namespace PenmonScramble.Baseball.Core
{
    /// <summary>CPU 同士の1試合の数え上げ（BaseballMatchCheck）。打撃の数はチームごと（0 = 先攻・1 = 後攻）</summary>
    public sealed class BaseballMatchStats
    {
        public readonly int[] Runs = new int[BaseballRules.TeamCount];
        public readonly int[] PlateAppearances = new int[BaseballRules.TeamCount];
        public readonly int[] AtBats = new int[BaseballRules.TeamCount];
        public readonly int[] Hits = new int[BaseballRules.TeamCount];
        public readonly int[] HomeRuns = new int[BaseballRules.TeamCount];
        public readonly int[] Strikeouts = new int[BaseballRules.TeamCount];
        public readonly int[] Walks = new int[BaseballRules.TeamCount];

        /// <summary>打球の結果ごとの数（穴・止まった場所・フェンス越えをまとめて。ファウルは入れない）</summary>
        public readonly Dictionary<BaseballHitResult, int> Results = new Dictionary<BaseballHitResult, int>();

        /// <summary>穴ごとに入った数（BaseballFieldDef.Holes の番号）と、穴に入らずに止まった結果ごとの数</summary>
        public int[] HoleIns = Array.Empty<int>();
        public readonly Dictionary<BaseballHitResult, int> Stops = new Dictionary<BaseballHitResult, int>();

        public int Pitches;
        public int Swings;
        public int Contacts;
        public int Fouls;

        /// <summary>浮いた打球（ジャスト＆芯）の数</summary>
        public int Lofted;

        /// <summary>バントの構えのまま球が来た数・当てた数・それで走者が進んだ（犠打）数</summary>
        public int BuntAttempts;
        public int BuntContacts;
        public int Sacrifices;

        /// <summary>本当に2つアウトを取ったゲッツーの数</summary>
        public int DoublePlays;

        public int Innings;
        public bool Mercy;
        public int Winner = -1;

        /// <summary>1試合のおよその秒（CPU 同士の間で。人が投げる番は球種を選ぶ分だけ長くなる）</summary>
        public float Seconds;

        public int TotalRuns => Runs[0] + Runs[1];

        public void AddResult(BaseballHitResult result)
        {
            Results.TryGetValue(result, out var n);
            Results[result] = n + 1;
        }

        public void AddStop(BaseballHitResult result)
        {
            Stops.TryGetValue(result, out var n);
            Stops[result] = n + 1;
        }
    }

    /// <summary>
    /// CPU 同士の試合の確認（Docs/MINIGAME_BASEBALL_PLAN.md フェーズ 3）。描かずに計算だけで1試合を回し、
    /// 点・打率・ホームラン・三振・四球・穴ごとの入った割合・1試合の長さ・強さの差での勝ち負けを数える（エディタのメニューとテストから使う）。
    /// 1球の流れはゲームの BaseballMatch と同じ（打席の始めに投げる子を選ぶ → 球種と曲げ方を決めて投げる → 打つ側が読んで振る・バント）
    /// </summary>
    public static class BaseballMatchCheck
    {
        /// <summary>CPU 同士のときの1球の間（構え・振りかぶり・結果を見せる秒。BaseballMatch の既定と同じ）</summary>
        public const float ReadySeconds = 1f;
        public const float WindUpSeconds = 0.55f;
        public const float ResultSeconds = 1.5f;
        public const float PlateEndSeconds = 2.2f;
        public const float ChangeSeconds = 2.5f;

        public const int LineupSize = 3;

        // 終わらない試合が無いかの見張り（1試合でこれより多く投げたら打ち切る）
        private const int MaxPitches = 2000;
        private const int MaxStepsPerPitch = 1000;

        /// <param name="field">試合をする盤（null なら試しの盤）</param>
        public static BaseballMatchStats Play(BaseballAiSkill away, BaseballAiSkill home, int seed, BaseballFieldDef field = null)
        {
            field ??= BaseballFieldTable.Test();
            var game = new BaseballGame(LineupSize, LineupSize);
            var board = new BaseballBoard(field);
            var ais = new[] { new BaseballAi(seed * 2 + 1, away), new BaseballAi(seed * 2 + 2, home) };
            var stats = new BaseballMatchStats { HoleIns = new int[field.Holes.Count] };
            var events = new List<BaseballEvent>();
            while (!game.IsOver && stats.Pitches < MaxPitches)
            {
                if (game.IsHalfOver)
                {
                    game.StartNextHalf();
                    stats.Seconds += ChangeSeconds;
                    continue;
                }

                if (game.IsPlateStart)
                {
                    game.ChoosePitcher(BaseballAi.ChoosePitcher(game));
                }

                var outcome = PlayPitch(game, board, ais[game.FieldingTeam], ais[game.BattingTeam], events, stats);
                var team = game.BattingTeam;
                var play = game.Apply(outcome);
                Count(stats, team, play, game);
            }

            stats.Runs[0] = game.Score(BaseballRules.Away);
            stats.Runs[1] = game.Score(BaseballRules.Home);
            stats.Innings = game.Inning;
            stats.Mercy = game.EndedByMercy;
            stats.Winner = game.Winner;
            return stats;
        }

        /// <summary>1球を投げて結果が出るまで回す</summary>
        private static BaseballPitchOutcome PlayPitch(BaseballGame game, BaseballBoard board, BaseballAi pitcher, BaseballAi batter, List<BaseballEvent> events, BaseballMatchStats stats)
        {
            var field = board.Field;
            var plan = pitcher.PlanPitch(field, game.Count);
            batter.BeginPitch(game);
            board.Reset();
            events.Clear();
            board.Throw(plan.Type, events);
            var steps = 0;
            for (; steps < MaxStepsPerPitch && board.Phase != BaseballBoardPhase.Done; steps++)
            {
                var bat = batter.BatInput(board);
                board.Step(new BaseballInput(BaseballAi.Bend(board.Pitch, plan, field), bat.Swing, bat.Bunt, bat.SwingTime), events);
            }

            foreach (var e in events)
            {
                CountEvent(stats, e);
            }

            stats.Pitches++;
            // バントの構えのまま球が来た（当てた・空振り）
            var buntAttempt = board.LastHit is { Bunt: true } || board.Bat.State == BaseballBatState.Bunting;
            stats.BuntAttempts += buntAttempt ? 1 : 0;
            stats.Seconds += ReadySeconds + WindUpSeconds + steps * BaseballRules.StepSeconds;
            return board.Outcome;
        }

        private static void CountEvent(BaseballMatchStats stats, BaseballEvent e)
        {
            switch (e.Kind)
            {
                case BaseballEventKind.Swing:
                    stats.Swings++;
                    break;
                case BaseballEventKind.Contact:
                    stats.Contacts++;
                    stats.Lofted += e.Flag ? 1 : 0;
                    break;
                case BaseballEventKind.HoleIn:
                    stats.HoleIns[e.Index]++;
                    break;
                case BaseballEventKind.Stop:
                    stats.AddStop(e.Result);
                    break;
            }
        }

        private static void Count(BaseballMatchStats stats, int team, BaseballGamePlay play, BaseballGame game)
        {
            var outcome = play.Outcome;
            stats.Fouls += outcome.Kind == BaseballPitchOutcomeKind.Foul ? 1 : 0;
            stats.BuntContacts += outcome.Bunt ? 1 : 0;
            stats.Seconds += play.PlateEnded ? PlateEndSeconds : ResultSeconds;
            if (!play.PlateEnded)
            {
                return;
            }

            stats.PlateAppearances[team]++;
            switch (play.Plate)
            {
                case BaseballPlateResult.Strikeout:
                    stats.AtBats[team]++;
                    stats.Strikeouts[team]++;
                    break;
                case BaseballPlateResult.Walk:
                    stats.Walks[team]++;
                    break;
                case BaseballPlateResult.Hit:
                    stats.AtBats[team]++;
                    stats.Hits[team]++;
                    stats.HomeRuns[team] += outcome.Hit == BaseballHitResult.HomeRun ? 1 : 0;
                    stats.AddResult(outcome.Hit);
                    break;
                case BaseballPlateResult.Out:
                    CountOut(stats, team, outcome, game);
                    break;
            }
        }

        /// <summary>アウト（犠打は打数に数えない。ゲッツーは本当に2つ取ったときだけ数える）</summary>
        private static void CountOut(BaseballMatchStats stats, int team, BaseballPitchOutcome outcome, BaseballGame game)
        {
            stats.AddResult(outcome.Hit);
            var advanced = false;
            foreach (var move in game.LastMoves)
            {
                stats.DoublePlays += move.Out ? 1 : 0;
                advanced |= !move.Out;
            }

            var sacrifice = outcome.Bunt && advanced;
            stats.Sacrifices += sacrifice ? 1 : 0;
            stats.AtBats[team] += sacrifice ? 0 : 1;
        }
    }
}
