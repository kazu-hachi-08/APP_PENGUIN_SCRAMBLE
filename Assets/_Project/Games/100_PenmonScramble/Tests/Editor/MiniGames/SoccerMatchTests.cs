using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PenmonScramble.Core;
using PenmonScramble.MiniGames.Core;
using PenmonScramble.Soccer.Core;

namespace PenmonScramble.Tests
{
    /// <summary>
    /// サッカーのひとりで1試合（Docs/MINIGAME_SOCCER_PLAN.md フェーズ 4）: 試合の流れ・PK戦・報酬・スタジアムの定義表
    /// </summary>
    public class SoccerMatchTests
    {
        private const float Dt = SoccerRules.StepSeconds;

        private static int Steps(float seconds) => (int)MathF.Ceiling(seconds / Dt);

        /// <summary>キーパーなしの 3対3（ボールの動きを読みやすく）で、seconds 秒の試合を始める</summary>
        private static SoccerMatch Match(float seconds, List<SoccerEvent> events)
        {
            var world = new SoccerWorld(SoccerPitchTable.Standard(), SoccerRules.PlayersPerTeam, SoccerRules.PlayersPerTeam);
            var match = new SoccerMatch(world, 1, seconds);
            match.Begin(events);
            return match;
        }

        private static List<SoccerEvent> Run(SoccerMatch match, int steps, SoccerInput[] inputs = null)
        {
            var events = new List<SoccerEvent>();
            for (var i = 0; i < steps; i++)
            {
                match.Step(inputs, events);
            }

            return events;
        }

        /// <summary>キックオフの合図が終わるまで進める（刻みの足し算の誤差で1歩ずれても良いように、変わるまで進める）</summary>
        private static void SkipKickoff(SoccerMatch match)
        {
            for (var i = 0; i < Steps(SoccerRules.KickoffWaitSeconds) + 3 && match.Phase == SoccerMatchPhase.Kickoff; i++)
            {
                Run(match, 1);
            }

            Assert.That(match.Phase, Is.EqualTo(SoccerMatchPhase.Playing));
        }

        /// <summary>青のゴール（右）の前から、ゴールへ速く転がす（赤の点になる）</summary>
        private static void ShootIntoBlueGoal(SoccerMatch match)
        {
            var goal = match.World.Pitch.GoalOf(SoccerRules.Blue);
            match.World.Ball.Place(new Vec2(goal.LineX - 2f, 0f));
            match.World.Ball.Velocity = new Vec2(20f, 0f);
        }

        // ---- 試合の流れ ----

        [Test]
        public void Match_WaitsForKickoffBeforePlaying_AndTheClockDoesNotRunMeanwhile()
        {
            var events = new List<SoccerEvent>();
            var match = Match(SoccerRules.MatchSeconds, events);

            Assert.That(events.Single().Kind, Is.EqualTo(SoccerEventKind.Kickoff));
            Run(match, Steps(SoccerRules.KickoffWaitSeconds) - 1);
            Assert.That(match.Phase, Is.EqualTo(SoccerMatchPhase.Kickoff));
            Assert.That(match.TimeLeft, Is.EqualTo(SoccerRules.MatchSeconds));

            var started = false;
            for (var i = 0; i < 3 && !started; i++)
            {
                started = Run(match, 1).Any(e => e.Kind == SoccerEventKind.PlayStart);
            }

            Assert.That(started, Is.True);
            Assert.That(match.TimeLeft, Is.EqualTo(SoccerRules.MatchSeconds));
            Run(match, Steps(1f));
            Assert.That(match.TimeLeft, Is.EqualTo(SoccerRules.MatchSeconds - 1f).Within(0.05f));
        }

        [Test]
        public void Goal_StopsTheClock_ThenTheConcedingTeamKicksOff()
        {
            var match = Match(SoccerRules.MatchSeconds, new List<SoccerEvent>());
            SkipKickoff(match);
            ShootIntoBlueGoal(match);

            var events = Run(match, Steps(0.3f));
            Assert.That(events.Any(e => e.Kind == SoccerEventKind.Goal && e.Team == SoccerRules.Red), Is.True);
            Assert.That(match.Phase, Is.EqualTo(SoccerMatchPhase.GoalPause));
            var timeAtGoal = match.TimeLeft;

            var after = Run(match, Steps(SoccerRules.GoalPauseSeconds));
            Assert.That(match.TimeLeft, Is.EqualTo(timeAtGoal), "ゴールの間は時間が止まる");
            var kickoff = after.Single(e => e.Kind == SoccerEventKind.Kickoff);
            Assert.That(kickoff.Team, Is.EqualTo(SoccerRules.Blue), "点を取られた青が次のキックオフ");
            Assert.That(match.Phase, Is.EqualTo(SoccerMatchPhase.Kickoff));
            Assert.That(match.World.Score(SoccerRules.Red), Is.EqualTo(1));
        }

        [Test]
        public void TimeUp_WithMoreGoals_FinishesWithTheWinner()
        {
            var match = Match(1f, new List<SoccerEvent>());
            SkipKickoff(match);
            ShootIntoBlueGoal(match);

            var events = Run(match, Steps(10f));
            Assert.That(events.Count(e => e.Kind == SoccerEventKind.TimeUp), Is.EqualTo(1));
            Assert.That(match.IsFinished, Is.True);
            Assert.That(match.Winner, Is.EqualTo(SoccerRules.Red));
            Assert.That(match.WonByShootout, Is.False);
            Assert.That(match.Shootout, Is.Null);
            var end = events.Single(e => e.Kind == SoccerEventKind.MatchEnd);
            Assert.That(end.Team, Is.EqualTo(SoccerRules.Red));
        }

        [Test]
        public void TimeUp_WithATie_GoesToTheShootout()
        {
            var match = Match(0.5f, new List<SoccerEvent>());
            SkipKickoff(match);

            var events = Run(match, Steps(0.5f + SoccerRules.TimeUpSeconds) + 5);
            Assert.That(events.Any(e => e.Kind == SoccerEventKind.TimeUp), Is.True);
            Assert.That(events.Any(e => e.Kind == SoccerEventKind.ShootoutStart), Is.True);
            Assert.That(match.Phase, Is.EqualTo(SoccerMatchPhase.Shootout));
            Assert.That(match.Shootout, Is.Not.Null);
            Assert.That(events.Single(e => e.Kind == SoccerEventKind.PenaltyReady).Team, Is.EqualTo(SoccerRules.Red), "先に蹴るのは赤");
        }

        [Test]
        public void Countdown_TicksEachSecondFromTen()
        {
            var match = Match(SoccerRules.CountdownFrom + 2f, new List<SoccerEvent>());
            SkipKickoff(match);

            var counts = Run(match, Steps(SoccerRules.CountdownFrom + 2f)).Where(e => e.Kind == SoccerEventKind.Countdown).Select(e => (int)e.Strength);
            Assert.That(counts, Is.EqualTo(Enumerable.Range(1, SoccerRules.CountdownFrom).Reverse()));
        }

        [Test]
        public void CutIn_StopsTheClockAndThePitch()
        {
            var match = Match(SoccerRules.MatchSeconds, new List<SoccerEvent>());
            SkipKickoff(match);
            var world = match.World;
            var shooter = world.Players[0];
            world.Ball.Place(shooter.Position + shooter.Facing * SoccerBalance.DribbleDistance);
            Run(match, 1);
            Assert.That(world.Ball.Holder, Is.EqualTo(0));
            world.SetGauge(SoccerRules.Red, 1f);

            var inputs = new SoccerInput[world.Players.Count];
            inputs[0] = new SoccerInput(Vec2.Zero, false, false, true);
            Run(match, 1, inputs);
            Assert.That(world.IsCutIn, Is.True);
            var time = match.TimeLeft;
            var worldTime = world.Time;

            Run(match, Steps(SoccerBalance.SpecialCutInSeconds) - 1, inputs);
            Assert.That(match.TimeLeft, Is.EqualTo(time));
            Assert.That(world.Time, Is.EqualTo(worldTime));
        }

        // ---- PK戦 ----

        /// <summary>試合を飛ばして PK戦から始める（キーパーのいる 3対3）</summary>
        private static SoccerMatch ShootoutMatch(int seed = 1)
        {
            var world = new SoccerWorld(SoccerPitchTable.Standard(), SoccerRules.PlayersPerTeam, SoccerRules.PlayersPerTeam, keepers: true, seed: seed);
            var match = new SoccerMatch(world, seed);
            var events = new List<SoccerEvent>();
            match.Begin(events);
            match.StartShootoutNow(events);
            return match;
        }

        /// <summary>
        /// 1本を蹴る: 構えの間は離しておき、狙い（kickSide・high）で power までためて離す。守る側は keeperSide へ飛ぶ。結果の後、次の1本の構えまで進める
        /// </summary>
        private static SoccerPenaltyResult Kick(SoccerMatch match, int kickSide, bool high, float power, int keeperSide)
        {
            var shootout = match.Shootout;
            var inputs = new SoccerInput[match.World.Players.Count];
            var kicks = shootout.TotalKicks;
            for (var guard = 0; guard < Steps(20f) && shootout.TotalKicks == kicks && !shootout.IsFinished; guard++)
            {
                var aiming = shootout.Phase == SoccerShootoutPhase.Aim;
                var hold = aiming && shootout.PhaseSeconds > 0.2f && shootout.Charge01 < power;
                inputs[shootout.Kicker] = new SoccerInput(new Vec2(kickSide, high ? 1f : 0f), hold);
                inputs[shootout.KeeperInputIndex(shootout.KeeperTeam)] = new SoccerInput(new Vec2(keeperSide, 0f), false);
                match.Step(inputs, new List<SoccerEvent>());
            }

            var result = shootout.LastResult;
            Run(match, Steps(SoccerBalance.PenaltyResultSeconds) + 1);
            return result;
        }

        [Test]
        public void Shootout_KeeperOnTheWrongSide_AlwaysConcedesALowShot()
        {
            for (var seed = 0; seed < 10; seed++)
            {
                var match = ShootoutMatch(seed);
                Assert.That(Kick(match, 1, false, 0.7f, -1), Is.EqualTo(SoccerPenaltyResult.Goal));
            }
        }

        [Test]
        public void Shootout_KeeperOnTheRightSide_StopsMostWeakShots()
        {
            var saved = 0;
            const int tries = 40;
            for (var seed = 0; seed < tries; seed++)
            {
                saved += Kick(ShootoutMatch(seed), -1, false, 0.4f, -1) == SoccerPenaltyResult.Saved ? 1 : 0;
            }

            Assert.That(saved, Is.GreaterThan(tries * 0.6f));
        }

        [Test]
        public void SaveChance_IsLowerForStrongerAndHigherShots()
        {
            Assert.That(SoccerShootout.SaveChance(0.3f, false), Is.GreaterThan(SoccerShootout.SaveChance(0.8f, false)));
            Assert.That(SoccerShootout.SaveChance(0.8f, false), Is.GreaterThan(SoccerShootout.SaveChance(0.8f, true)));
            Assert.That(SoccerShootout.SaveChance(1f, true), Is.GreaterThanOrEqualTo(SoccerBalance.PenaltyMinSave));
        }

        [Test]
        public void Shootout_AllGoals_GoesIntoSuddenDeathAfterThreeEach_AndKickersTakeTurns()
        {
            var match = ShootoutMatch();
            var shootout = match.Shootout;
            var kickers = new List<int>();
            for (var i = 0; i < SoccerRules.PenaltyRounds * 2 + 2; i++)
            {
                kickers.Add(shootout.Kicker);
                Assert.That(Kick(match, 1, false, 0.7f, -1), Is.EqualTo(SoccerPenaltyResult.Goal));
            }

            Assert.That(shootout.IsFinished, Is.False);
            Assert.That(shootout.IsSuddenDeath, Is.True);
            Assert.That(shootout.Score(SoccerRules.Red), Is.EqualTo(SoccerRules.PenaltyRounds + 1));
            // 赤・青の交互で、チームの子の番号の順にくり返す（赤 0,1,2,0 / 青 3,4,5,3）
            Assert.That(kickers, Is.EqualTo(new[] { 0, 3, 1, 4, 2, 5, 0, 3 }));
        }

        [TestCase(2, 2, 2, 0, true, SoccerRules.Red)] // 赤 2/2・青 0/2: 青は残り1本を決めても追いつけない
        [TestCase(2, 2, 1, 0, false, -1)] // 青は残り2本を決めれば追いつける
        [TestCase(2, 1, 1, 1, false, -1)]
        [TestCase(3, 2, 3, 2, false, -1)] // 3本ずつで同点 → サドンデス
        [TestCase(4, 3, 3, 2, false, -1)] // サドンデスは同じ本数を蹴るまで決めない
        [TestCase(4, 3, 4, 2, true, SoccerRules.Red)]
        [TestCase(4, 2, 4, 3, true, SoccerRules.Blue)]
        [TestCase(3, 0, 2, 2, true, SoccerRules.Blue)] // 赤は蹴り終わっていて、青がもう上
        public void IsDecided_FollowsThreeEachThenSuddenDeath(int redKicks, int redGoals, int blueKicks, int blueGoals, bool decided, int winner)
        {
            Assert.That(SoccerShootout.IsDecided(redKicks, redGoals, blueKicks, blueGoals, out var actual), Is.EqualTo(decided));
            Assert.That(actual, Is.EqualTo(winner));
        }

        [Test]
        public void Shootout_WhenTheTimeRunsOut_KicksWeakly()
        {
            var match = ShootoutMatch();
            var events = Run(match, Steps(SoccerBalance.PenaltyReadySeconds + SoccerBalance.PenaltyAimSeconds) + 5);

            var kick = events.Single(e => e.Kind == SoccerEventKind.PenaltyKick);
            Assert.That(kick.Strength, Is.EqualTo(SoccerBalance.PenaltyTimeoutPower));
        }

        [Test]
        public void Shootout_ButtonHeldFromBefore_DoesNotCharge()
        {
            var match = ShootoutMatch();
            var shootout = match.Shootout;
            var inputs = new SoccerInput[match.World.Players.Count];
            inputs[shootout.Kicker] = new SoccerInput(Vec2.Zero, true);

            Run(match, Steps(SoccerBalance.PenaltyReadySeconds + 1f), inputs);
            Assert.That(shootout.Phase, Is.EqualTo(SoccerShootoutPhase.Aim));
            Assert.That(shootout.IsCharging, Is.False);
        }

        // ---- 報酬・スタジアム ----

        [Test]
        public void Reward_WinPaysMoreThanShootoutWin_AndGoalsAreCapped()
        {
            var win = SoccerReward.Compute(3, SoccerOutcome.Win, false);
            var shootout = SoccerReward.Compute(3, SoccerOutcome.ShootoutWin, false);
            var loss = SoccerReward.Compute(9, SoccerOutcome.Loss, true);

            Assert.That(win.Total, Is.EqualTo(SoccerReward.BaseCoins + SoccerReward.WinCoins + 3 * SoccerReward.CoinsPerGoal));
            Assert.That(shootout.WinCoins, Is.EqualTo(SoccerReward.ShootoutWinCoins));
            Assert.That(win.Total, Is.GreaterThan(shootout.Total));
            Assert.That(loss.Goals, Is.EqualTo(SoccerReward.MaxPaidGoals));
            Assert.That(loss.FirstWinBonus, Is.EqualTo(0), "負けた試合には初めて勝ったボーナスは付かない");
        }

        [Test]
        public void Reward_FirstWinBonusOnlyWhenFirst()
        {
            Assert.That(SoccerReward.Compute(1, SoccerOutcome.Win, true).FirstWinBonus, Is.EqualTo(SoccerReward.FirstWinBonus));
            Assert.That(SoccerReward.Compute(1, SoccerOutcome.ShootoutWin, true).FirstWinBonus, Is.EqualTo(SoccerReward.FirstWinBonus));
            Assert.That(SoccerReward.Compute(1, SoccerOutcome.Win, false).FirstWinBonus, Is.EqualTo(0));
        }

        [Test]
        public void Records_SoccerRowKeepsTheBestGoalDifferenceAndCountsWins()
        {
            var data = new SaveData();
            var game = MiniGameTable.Get(MiniGameTable.SoccerId);

            MiniGameReward.Apply(data, game, 1, -1, 0, 20);
            var played = MiniGameReward.Apply(data, game, 1, 2, 1, 120);
            MiniGameReward.Apply(data, game, 1, 0, 1, 50);

            var row = new MiniGameRecords(data).Find(MiniGameTable.SoccerId, 1);
            Assert.That(played.NewBest, Is.True);
            Assert.That(row.best, Is.EqualTo(2));
            Assert.That(row.special, Is.EqualTo(2));
            Assert.That(row.plays, Is.EqualTo(3));
            Assert.That(data.coins, Is.EqualTo(190));
        }

        [Test]
        public void Stadiums_MatchTheMiniGameTable_AndOnlyBuiltOnesAreReady()
        {
            var soccer = MiniGameTable.Get(MiniGameTable.SoccerId);
            var guide = new SoccerCourseGuide();
            foreach (var course in soccer.Courses)
            {
                var stadium = SoccerStadiumTable.Get(course.No);
                Assert.That(guide.IsReady(course.No), Is.EqualTo(stadium != null));
                if (stadium != null)
                {
                    Assert.That(stadium.Name, Is.EqualTo(course.Name));
                    Assert.That(stadium.World, Is.EqualTo(course.World));
                }
            }

            Assert.That(guide.IsReady(1), Is.True);
            Assert.That(guide.Layout(1), Does.Contain("★☆☆☆☆"));
        }

        [Test]
        public void Stadium_PicksDifferentWildPenmonWhileThePoolLasts()
        {
            var picked = SoccerStadiumTable.Get(1).PickWild(SoccerRules.PlayersPerTeam, 7);

            Assert.That(picked.Distinct().Count(), Is.EqualTo(SoccerRules.PlayersPerTeam));
            Assert.That(picked.All(no => SoccerStadiumTable.Get(1).WildSpecies.Contains(no)), Is.True);
        }
    }
}
