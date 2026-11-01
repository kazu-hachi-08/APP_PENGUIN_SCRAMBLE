using System.Linq;
using NUnit.Framework;
using PenmonScramble.Core;
using static PenmonScramble.Tests.TestWorld;

namespace PenmonScramble.Tests
{
    /// <summary>バランスの試験場（§11）: ボットのよけ・交代・次のエリアへ進む、1回の試験が種で決まる、条件と目標の表</summary>
    public class BalanceLabTests
    {
        private static StageWorld CreateArena()
        {
            var half = new Vec2(Balance.ArenaWidth, Balance.ArenaHeight) * 0.5f;
            return new StageWorld(AreaShape.Ellipse(-half, half), seed: 1);
        }

        private static Unit AddPlayer(StageWorld world, Vec2 position)
        {
            var stats = SpeciesTable.PlayerStats(SpeciesTable.Get(1), Balance.TrialPower);
            stats.MaxHp = 100000;
            return world.AddUnit(Team.Player, stats, position, 0);
        }

        /// <summary>ボットに操作させながら seconds 秒進める</summary>
        private static void Drive(StageWorld world, Unit player, StageBot bot, float seconds)
        {
            for (var i = 0; i < Seconds(seconds); i++)
            {
                world.SetInput(player.Id, bot.Next(world, player, world.TickCount * Balance.TickSeconds));
                world.Tick();
            }
        }

        [Test]
        public void Bot_StepsOutOfABossWarning()
        {
            var world = CreateArena();
            var player = AddPlayer(world, new Vec2(4f, 0f));
            // 足元に赤い円を出す、ヒョウアザラシの食いつき
            var stats = BigBossTable.Stats(BigBossTable.Get(BigBossTable.LeopardSealNo), playerCount: 1, world: 1);
            stats.MoveSpeed = 0f;
            var boss = world.AddUnit(Team.Enemy, stats, new Vec2(-6f, 0f));
            boss.Brain.NextAttackIndex = 1;
            var bot = new StageBot(BotSkill.Expert);
            for (var i = 0; i < Seconds(Balance.BigBossFirstAttackDelay + 0.5f) && world.Warnings.Count == 0; i++)
            {
                world.Tick();
            }

            var warning = world.Warnings.Single();
            Assert.That(warning.Overlaps(player.Position, player.Radius), Is.True);

            Drive(world, player, bot, warning.TotalSeconds * 0.8f);
            Assert.That(warning.Overlaps(player.Position, player.Radius), Is.False);
        }

        [Test]
        public void Bot_SwitchesToAHealthierMember_WhenLow()
        {
            var world = Create();
            var party = new[] { Stats(Melee()), Stats(Melee()) };
            var player = world.AddUnit(Team.Player, party, Vec2.Zero, 0);
            player.Hp = 10;
            var bot = new StageBot(BotSkill.Casual);

            Assert.That(bot.Next(world, player, 0f).Switch, Is.True);
            // 交代は押した瞬間だけ。押しっぱなしにしない
            Assert.That(bot.Next(world, player, Balance.TickSeconds).Switch, Is.False);
        }

        [Test]
        public void Bot_WalksIntoTheNextArea_WhenTheGateOpens()
        {
            var stage = new StageDef(1, "test", new AreaDef(new WaveDef(1, new[] { 1 })), new AreaDef(new WaveDef(1, new[] { 1 })));
            var world = new StageWorld(stage, playerCount: 1, seed: 1);
            var player = AddPlayer(world, new Vec2(-5f, 0f));
            TickFor(world, Seconds(Balance.FirstWaveDelaySeconds) + 1);
            foreach (var unit in world.Units.Where(u => u.Team == Team.Enemy))
            {
                unit.Hp = 0;
            }

            world.Tick();
            world.Tick();
            Assert.That(world.IsGateOpen, Is.True);

            Drive(world, player, new StageBot(BotSkill.Expert), 8f);
            Assert.That(world.AreaIndex, Is.EqualTo(1));
        }

        [Test]
        public void Run_IsDecidedBySeed()
        {
            var party = BalanceParty.Starter;
            var a = new BalanceRun(StageTable.Stage1, Difficulty.Normal, 1, BotSkill.Casual, party, seed: 5).Run();
            var b = new BalanceRun(StageTable.Stage1, Difficulty.Normal, 1, BotSkill.Casual, party, seed: 5).Run();

            Assert.That(a.Cleared, Is.EqualTo(b.Cleared));
            Assert.That(a.Seconds, Is.EqualTo(b.Seconds));
            Assert.That(a.DamageRatio, Is.EqualTo(b.DamageRatio));
            Assert.That(a.Seconds, Is.GreaterThan(0f));
        }

        [Test]
        public void Party_GrowsTowardTheCap()
        {
            var random = new System.Random(1);
            var fresh = BalanceParty.Starter.PowerOf(1, random);
            Assert.That(fresh, Is.InRange(Balance.PowerMin(1), Balance.PowerMax(1)));

            // 上限まで育てた ★3 は、引いたときのパワーの上限（×2.5）まで
            var maxed = BalanceParty.Maxed(Balance.WorldCount).PowerOf(3, new System.Random(1));
            var basePower = Balance.PowerMin(Balance.WorldCount) * Balance.StarPowerScale(3);
            Assert.That(maxed, Is.GreaterThanOrEqualTo((int)(Balance.PowerCap((int)basePower, Balance.MaxLimitBreak) * 0.99f)));
        }

        [Test]
        public void Cases_CoverEveryStoryStage_AndEveryRushCondition()
        {
            var story = BalanceLab.CreateCases(new BalanceLabOptions { Rush = false });
            var rush = BalanceLab.CreateCases(new BalanceLabOptions { Story = false });

            // 15 ステージ × 2 段階のうまさ ＋ 1-1 の無料3回の子・5-3 の強化を進めた子
            Assert.That(story.Count, Is.EqualTo((StageTable.Count + 2) * 2));
            Assert.That(story.All(c => c.Players == 1 && c.Difficulty == Difficulty.Normal), Is.True);
            // 5 ステージ × 3 難易度 × 1〜4 人 × 2 段階のうまさ
            Assert.That(rush.Count, Is.EqualTo(RushTable.Count * Balance.DifficultyCount * Balance.MaxPlayers * 2));
        }

        [Test]
        public void Report_ListsTheGoals()
        {
            var cases = BalanceLab.Run(new BalanceLabOptions { Seeds = 1, Rush = false, Parallel = false });
            var checks = BalanceLab.Check(cases);
            var report = BalanceLab.Report(cases, 1);

            // 1-1 の2つ ＋ ワールド1〜4 の3ステージ目の2つずつ ＋ 5-3
            Assert.That(checks.Count, Is.EqualTo(2 + (Balance.WorldCount - 1) * 2 + 1));
            Assert.That(report, Does.Contain("## 目標との比べ"));
            Assert.That(report, Does.Contain("## コインの流れ"));
        }
    }
}
