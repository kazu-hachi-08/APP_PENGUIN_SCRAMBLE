using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PenmonScramble.Baseball.Core;

namespace PenmonScramble.Tests
{
    /// <summary>野球の走者・試合の流れ・CPU のバント・試合の確認</summary>
    public class BaseballGameTests
    {
        private const int Batter = 0;
        private const int MaxSteps = 1000;

        private static BaseballPitchOutcome Pitch(BaseballPitchOutcomeKind kind) => new BaseballPitchOutcome(kind, BaseballHitResult.Single, false);

        private static BaseballPitchOutcome InPlay(BaseballHitResult hit, bool bunt = false) => new BaseballPitchOutcome(BaseballPitchOutcomeKind.InPlay, hit, bunt);

        private static BaseballBases Bases(params int[] occupied)
        {
            var bases = new BaseballBases();
            foreach (var b in occupied)
            {
                bases.Place(b, 10 + b);
            }

            return bases;
        }

        private static BaseballBasesPlay Advance(BaseballBases bases, BaseballPlateResult plate, BaseballHitResult hit, int outsBefore, List<BaseballRunnerMove> moves = null)
        {
            return bases.Advance(plate, hit, Batter, outsBefore, moves ?? new List<BaseballRunnerMove>());
        }

        // ---- 走者 ----

        [Test]
        public void Bases_SingleMovesEveryoneOneBase()
        {
            var bases = Bases(1, 3);
            var play = Advance(bases, BaseballPlateResult.Hit, BaseballHitResult.Single, 0);
            Assert.That(play.Runs, Is.EqualTo(1));
            Assert.That(bases.RunnerOn(1), Is.EqualTo(Batter));
            Assert.That(bases.RunnerOn(2), Is.EqualTo(11));
            Assert.That(bases.IsOccupied(3), Is.False);
        }

        [Test]
        public void Bases_DoubleScoresFromSecondAndFirstGoesToThird()
        {
            var bases = Bases(1, 2);
            var play = Advance(bases, BaseballPlateResult.Hit, BaseballHitResult.Double, 1);
            Assert.That(play.Runs, Is.EqualTo(1));
            Assert.That(bases.RunnerOn(2), Is.EqualTo(Batter));
            Assert.That(bases.RunnerOn(3), Is.EqualTo(11));
            Assert.That(bases.IsOccupied(1), Is.False);
        }

        [Test]
        public void Bases_GrandSlamScoresFour()
        {
            var bases = Bases(1, 2, 3);
            var moves = new List<BaseballRunnerMove>();
            var play = Advance(bases, BaseballPlateResult.Hit, BaseballHitResult.HomeRun, 2, moves);
            Assert.That(play.Runs, Is.EqualTo(4));
            Assert.That(bases.RunnerCount, Is.EqualTo(0));
            Assert.That(moves.Count(m => m.Scored), Is.EqualTo(4));
        }

        [Test]
        public void Bases_WalkPushesOnlyForcedRunners()
        {
            var bases = Bases(2);
            var play = Advance(bases, BaseballPlateResult.Walk, BaseballHitResult.Single, 0);
            Assert.That(play.Runs, Is.EqualTo(0));
            Assert.That(bases.RunnerOn(1), Is.EqualTo(Batter));
            Assert.That(bases.RunnerOn(2), Is.EqualTo(12), "二塁の走者は押し出されない");

            var loaded = Bases(1, 2, 3);
            Assert.That(Advance(loaded, BaseballPlateResult.Walk, BaseballHitResult.Single, 0).Runs, Is.EqualTo(1), "満塁の四球は押し出しで1点");
            Assert.That(loaded.RunnerCount, Is.EqualTo(3));
        }

        [Test]
        public void Bases_GroundOutAdvancesRunners()
        {
            var bases = Bases(1, 3);
            var play = Advance(bases, BaseballPlateResult.Out, BaseballHitResult.GroundOut, 0);
            Assert.That(play.Outs, Is.EqualTo(1));
            Assert.That(play.Runs, Is.EqualTo(1));
            Assert.That(bases.RunnerOn(2), Is.EqualTo(11));
            Assert.That(bases.IsOccupied(1), Is.False);
        }

        [Test]
        public void Bases_ThirdOutScoresNoRun()
        {
            var bases = Bases(3);
            var play = Advance(bases, BaseballPlateResult.Out, BaseballHitResult.GroundOut, 2);
            Assert.That(play.Runs, Is.EqualTo(0));
            var fly = Bases(3);
            Assert.That(Advance(fly, BaseballPlateResult.Out, BaseballHitResult.FlyOut, 2).Runs, Is.EqualTo(0), "3アウト目のフライではタッチアップしない");
        }

        [Test]
        public void Bases_FlyOutTagsUpFromThirdOnly()
        {
            var bases = Bases(1, 2, 3);
            var play = Advance(bases, BaseballPlateResult.Out, BaseballHitResult.FlyOut, 1);
            Assert.That(play.Runs, Is.EqualTo(1));
            Assert.That(bases.RunnerOn(1), Is.EqualTo(11));
            Assert.That(bases.RunnerOn(2), Is.EqualTo(12));
            Assert.That(bases.IsOccupied(3), Is.False);
        }

        [Test]
        public void Bases_DoublePlayTakesRunnerOnFirst()
        {
            var bases = Bases(1, 3);
            var moves = new List<BaseballRunnerMove>();
            var play = Advance(bases, BaseballPlateResult.Out, BaseballHitResult.DoublePlay, 0, moves);
            Assert.That(play.Outs, Is.EqualTo(2));
            Assert.That(play.Runs, Is.EqualTo(1), "0アウトのゲッツーなら三塁の走者は還る");
            Assert.That(bases.RunnerCount, Is.EqualTo(0));
            Assert.That(moves.Single(m => m.Out).Runner, Is.EqualTo(11));
        }

        [Test]
        public void Bases_DoublePlayWithOneOutEndsInningWithoutRuns()
        {
            var bases = Bases(1, 3);
            var play = Advance(bases, BaseballPlateResult.Out, BaseballHitResult.DoublePlay, 1);
            Assert.That(play.Outs, Is.EqualTo(2));
            Assert.That(play.Runs, Is.EqualTo(0));
        }

        [Test]
        public void Bases_DoublePlayHoleWithoutRunnerOnFirstIsGroundOut()
        {
            var bases = Bases(2);
            var play = Advance(bases, BaseballPlateResult.Out, BaseballHitResult.DoublePlay, 0);
            Assert.That(play.Outs, Is.EqualTo(1));
            Assert.That(bases.RunnerOn(3), Is.EqualTo(12));
        }

        // ---- 試合の流れ ----

        private static void Strikeout(BaseballGame game)
        {
            for (var i = 0; i < BaseballRules.StrikesForOut; i++)
            {
                game.Apply(Pitch(BaseballPitchOutcomeKind.SwingingStrike));
            }
        }

        private static void Inning(BaseballGame game, params BaseballHitResult[] hits)
        {
            foreach (var hit in hits)
            {
                game.Apply(InPlay(hit));
            }

            while (!game.IsHalfOver && !game.IsOver)
            {
                Strikeout(game);
            }
        }

        [Test]
        public void Game_ThreeOutsSwitchesSidesAndKeepsBattingOrder()
        {
            var game = new BaseballGame(3, 3);
            game.Apply(InPlay(BaseballHitResult.Single));
            Strikeout(game);
            Strikeout(game);
            Assert.That(game.IsHalfOver, Is.False);
            Strikeout(game);
            Assert.That(game.IsHalfOver, Is.True);
            Assert.That(game.Bases.RunnerCount, Is.EqualTo(1), "見せ終わるまでは塁に残す");
            game.StartNextHalf();
            Assert.That(game.IsTop, Is.False);
            Assert.That(game.Count.Outs, Is.EqualTo(0));
            Assert.That(game.Bases.RunnerCount, Is.EqualTo(0));
            Assert.That(game.BatterSlot(BaseballRules.Away), Is.EqualTo(1), "4人打って 1→2→3→1 の次は 2 番目");
        }

        [Test]
        public void Game_ScoreAddsToInning()
        {
            var game = new BaseballGame(3, 3);
            game.Apply(InPlay(BaseballHitResult.HomeRun));
            Assert.That(game.Score(BaseballRules.Away), Is.EqualTo(1));
            Assert.That(game.InningRuns(BaseballRules.Away, 1), Is.EqualTo(1));
            Assert.That(game.InningRuns(BaseballRules.Away, 2), Is.EqualTo(-1));
        }

        [Test]
        public void Game_WalkOffEndsAtOnce()
        {
            var game = new BaseballGame(3, 3);
            for (var half = 0; half < BaseballRules.Innings * 2 - 1; half++)
            {
                Inning(game);
                game.StartNextHalf();
            }

            Assert.That(game.Inning, Is.EqualTo(BaseballRules.Innings));
            Assert.That(game.IsTop, Is.False);
            var play = game.Apply(InPlay(BaseballHitResult.HomeRun));
            Assert.That(play.GameOver, Is.True);
            Assert.That(game.Winner, Is.EqualTo(BaseballRules.Home));
        }

        [Test]
        public void Game_HomeLeadingAfterTopOfLastSkipsBottom()
        {
            var game = new BaseballGame(3, 3);
            Inning(game);
            game.StartNextHalf();
            Inning(game, BaseballHitResult.HomeRun);
            for (var half = 0; half < 3; half++)
            {
                game.StartNextHalf();
                Inning(game);
            }

            Assert.That(game.Inning, Is.EqualTo(BaseballRules.Innings));
            Assert.That(game.IsTop, Is.True);
            Assert.That(game.IsOver, Is.True);
            Assert.That(game.Winner, Is.EqualTo(BaseballRules.Home));
        }

        [Test]
        public void Game_TieAfterLastInningIsDraw()
        {
            var game = new BaseballGame(3, 3);
            for (var half = 0; half < BaseballRules.Innings * 2; half++)
            {
                Inning(game);
                if (!game.IsOver)
                {
                    game.StartNextHalf();
                }
            }

            Assert.That(game.IsOver, Is.True);
            Assert.That(game.Winner, Is.EqualTo(-1));
            Assert.That(game.EndedByMercy, Is.False);
        }

        [Test]
        public void Game_BigLeadEndsEarly()
        {
            var game = new BaseballGame(3, 3);
            var homers = Enumerable.Repeat(BaseballHitResult.HomeRun, BaseballRules.MercyRuns).ToArray();
            Inning(game, homers);
            game.StartNextHalf();
            Inning(game);
            Assert.That(game.IsOver, Is.False, "1回では早く終わらない");
            game.StartNextHalf();
            Inning(game);
            game.StartNextHalf();
            Inning(game);
            Assert.That(game.IsOver, Is.True);
            Assert.That(game.EndedByMercy, Is.True);
            Assert.That(game.Winner, Is.EqualTo(BaseballRules.Away));
        }

        [Test]
        public void Game_DoublePlayCountsTwoOuts()
        {
            var game = new BaseballGame(3, 3);
            game.Apply(InPlay(BaseballHitResult.Single));
            var play = game.Apply(InPlay(BaseballHitResult.DoublePlay));
            Assert.That(game.Count.Outs, Is.EqualTo(2));
            Assert.That(play.HalfOver, Is.False);
            Assert.That(game.LastMoves.Count(m => m.Out), Is.EqualTo(1));
        }

        [Test]
        public void Game_PitcherOnlyChosenAtPlateStart()
        {
            var game = new BaseballGame(3, 3);
            game.ChoosePitcher(2);
            Assert.That(game.CurrentPitcher, Is.EqualTo(2));
            game.Apply(Pitch(BaseballPitchOutcomeKind.Ball));
            Assert.That(game.IsPlateStart, Is.False);
            Assert.Throws<System.InvalidOperationException>(() => game.ChoosePitcher(1));
        }

        [Test]
        public void Game_PracticeNeverEndsAndKeepsBatting()
        {
            var game = BaseballGame.Practice(3, 3, BaseballRules.Home);
            for (var i = 0; i < 10; i++)
            {
                Inning(game, BaseballHitResult.HomeRun);
                Assert.That(game.IsOver, Is.False);
                game.StartNextHalf();
                Assert.That(game.BattingTeam, Is.EqualTo(BaseballRules.Home));
            }
        }

        // ---- CPU ----

        [Test]
        public void Ai_WantsBuntOnlyWithRunnerAndFewOuts()
        {
            var game = new BaseballGame(3, 3);
            Assert.That(BaseballAi.WantsBunt(game), Is.False, "走者がいない");
            game.Apply(InPlay(BaseballHitResult.Single));
            Assert.That(BaseballAi.WantsBunt(game), Is.True);
            Strikeout(game);
            Strikeout(game);
            Assert.That(BaseballAi.WantsBunt(game), Is.False, "2アウト");
        }

        [Test]
        public void Ai_BuntingCpuSquaresAndPutsBallInPlay()
        {
            // 走者がいる場面で何度か試せば、バントの打席が出て、ゾーンの球に当てて弱く転がす
            var field = BaseballFieldTable.Test();
            var bunted = false;
            for (var seed = 0; seed < 40 && !bunted; seed++)
            {
                var game = new BaseballGame(3, 3);
                game.Apply(InPlay(BaseballHitResult.Single));
                var ai = new BaseballAi(seed, BaseballBalance.AiSkill(3));
                var board = new BaseballBoard(field);
                var events = new List<BaseballEvent>();
                ai.BeginPitch(game);
                board.Throw(BaseballPitchType.Curve, events);
                for (var step = 0; step < MaxSteps && board.Phase != BaseballBoardPhase.Done; step++)
                {
                    var bat = ai.BatInput(board);
                    board.Step(new BaseballInput(0f, bat.Swing, bat.Bunt, bat.SwingTime), events);
                }

                bunted = board.LastHit is { Bunt: true };
            }

            Assert.That(bunted, Is.True);
        }

        [Test]
        public void Ai_ChoosesPitcherByInning()
        {
            var game = new BaseballGame(3, 2);
            Assert.That(BaseballAi.ChoosePitcher(game), Is.EqualTo(0));
            Inning(game);
            game.StartNextHalf();
            Inning(game);
            game.StartNextHalf();
            Inning(game);
            game.StartNextHalf();
            Assert.That(game.Inning, Is.EqualTo(2));
            Assert.That(BaseballAi.ChoosePitcher(game), Is.EqualTo(1), "2回の裏は先攻（3体）の2番目の子が投げる");
        }

        [Test]
        public void Ai_StarsGetSharper()
        {
            for (var stars = BaseballBalance.AiMinStars; stars < BaseballBalance.AiMaxStars; stars++)
            {
                Assert.That(BaseballBalance.AiSkill(stars + 1).TimingErrorSeconds, Is.LessThan(BaseballBalance.AiSkill(stars).TimingErrorSeconds));
                Assert.That(BaseballBalance.AiSkill(stars + 1).TakeBallChance, Is.GreaterThan(BaseballBalance.AiSkill(stars).TakeBallChance));
            }
        }

        // ---- 試合の確認 ----

        [Test]
        public void MatchCheck_FinishesAndIsRepeatable()
        {
            var skill = BaseballBalance.AiSkill(3);
            var a = BaseballMatchCheck.Play(skill, skill, 7);
            var b = BaseballMatchCheck.Play(skill, skill, 7);
            Assert.That(a.Innings, Is.InRange(BaseballRules.MercyInning, BaseballRules.Innings));
            Assert.That(a.Runs, Is.EqualTo(b.Runs), "同じ乱数の種なら同じ試合になる");
            Assert.That(a.Pitches, Is.EqualTo(b.Pitches));
            Assert.That(a.PlateAppearances[0], Is.GreaterThanOrEqualTo(BaseballRules.OutsPerInning * BaseballRules.MercyInning));
        }

        [Test]
        public void MatchCheck_StrongerCpuWinsMore()
        {
            int strong = 0, weak = 0;
            for (var seed = 0; seed < 40; seed++)
            {
                var stats = BaseballMatchCheck.Play(BaseballBalance.AiSkill(5), BaseballBalance.AiSkill(1), seed);
                strong += stats.Winner == BaseballRules.Away ? 1 : 0;
                weak += stats.Winner == BaseballRules.Home ? 1 : 0;
            }

            Assert.That(strong, Is.GreaterThan(weak * 2));
        }
    }
}
