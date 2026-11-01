using System.Collections.Generic;
using NUnit.Framework;
using PenmonScramble.Core;
using static PenmonScramble.Tests.TestWorld;

namespace PenmonScramble.Tests
{
    /// <summary>巨大ペンモンの闘技場の大技（属性ごとの形・2体で交互）と、大ボスの本気（吠える・予告と休みが短くなる・攻撃が増える）</summary>
    public class GiantRageTests
    {
        private const int KingNo = 4;
        private const int GentooNo = 3;
        private const int FiordNo = 12;
        private const int ShuretaNo = 18;
        private const int GuardHp = 100000;

        private static StageWorld CreateArena()
        {
            var half = new Vec2(Balance.ArenaWidth, Balance.ArenaHeight) * 0.5f;
            return new StageWorld(AreaShape.Ellipse(-half, half), seed: 1);
        }

        /// <summary>倒れないほど硬く、動かないプレイヤー</summary>
        private static Unit AddPlayer(StageWorld world, Vec2 position)
        {
            var stats = Stats(Melee());
            stats.MaxHp = GuardHp;
            stats.MoveSpeed = 0f;
            return world.AddUnit(Team.Player, stats, position, 0);
        }

        private static Unit AddGiant(StageWorld world, int speciesNo, Vec2 position)
        {
            var stats = SpeciesTable.GiantStats(SpeciesTable.Get(speciesNo), playerCount: 1);
            // ふつうの技で動き回ると予告の場所が読みにくいので、テストでは動かない
            stats.MoveSpeed = 0f;
            return world.AddUnit(Team.Enemy, stats, position);
        }

        private static Unit AddBoss(StageWorld world, int no, Vec2 position, int attackIndex, bool serious)
        {
            var boss = world.AddUnit(Team.Enemy, BigBossTable.Stats(BigBossTable.Get(no), playerCount: 1, world: 1, serious), position);
            boss.Brain.NextAttackIndex = attackIndex;
            return boss;
        }

        private static void TickUntil(StageWorld world, System.Func<bool> done, float maxSeconds)
        {
            for (var i = 0; i < Seconds(maxSeconds) && !done(); i++)
            {
                world.Tick();
            }

            Assert.That(done(), Is.True);
        }

        [Test]
        public void Giant_IsBiggerAndTougherThanTheStageBoss()
        {
            var species = SpeciesTable.Get(KingNo);
            var boss = SpeciesTable.BossStats(species, playerCount: 1);
            var giant = SpeciesTable.GiantStats(species, playerCount: 1);

            Assert.That(giant.Radius, Is.GreaterThan(boss.Radius));
            Assert.That(giant.MaxHp, Is.GreaterThan(boss.MaxHp));
            Assert.That(giant.IsBoss, Is.True);
            Assert.That(giant.Skills.Length, Is.EqualTo(2));
            Assert.That(giant.GiantAttack, Is.SameAs(GiantAttacks.For(Element.Ice)));
        }

        [TestCase(Element.Ice, BossAttackKind.Rain)]
        [TestCase(Element.Water, BossAttackKind.Wave)]
        [TestCase(Element.Wind, BossAttackKind.Vortex)]
        [TestCase(Element.Thunder, BossAttackKind.Beam)]
        public void GiantAttack_MatchesTheElement(Element element, BossAttackKind kind)
        {
            Assert.That(GiantAttacks.For(element).Kind, Is.EqualTo(kind));
        }

        [Test]
        public void Giant_WarnsBeforeItsBigAttack()
        {
            var world = CreateArena();
            var giant = AddGiant(world, KingNo, new Vec2(6f, 0f));
            AddPlayer(world, new Vec2(-6f, 2f));

            TickUntil(world, () => world.Warnings.Count > 0, Balance.GiantFirstBigAttackDelay + 1f);

            Assert.That(giant.IsGiant, Is.True);
            Assert.That(giant.Brain.Current.Kind, Is.EqualTo(BossAttackKind.Rain));
            Assert.That(world.Warnings[0].TotalSeconds, Is.EqualTo(GiantAttacks.For(Element.Ice).TelegraphSeconds).Within(Tolerance));
        }

        [Test]
        public void ThunderGiant_BeamRunsFromItsFeetTowardThePlayer()
        {
            var world = CreateArena();
            var giant = AddGiant(world, ShuretaNo, new Vec2(6f, 0f));
            var player = AddPlayer(world, new Vec2(-6f, 0f));

            TickUntil(world, () => world.Warnings.Count > 0, Balance.GiantFirstBigAttackDelay + 1f);
            var band = world.Warnings[0];
            Assert.That(band.From, Is.EqualTo(giant.Position));
            Assert.That(band.To.X, Is.LessThan(player.Position.X), "柵まで届く");

            TickUntil(world, () => world.Warnings.Count == 0, 2f);
            Assert.That(player.Hp, Is.LessThan(player.MaxHp));
        }

        [Test]
        public void WindGiant_TornadoPullsThePlayerIn()
        {
            var world = CreateArena();
            AddGiant(world, FiordNo, new Vec2(10f, 0f));
            var player = AddPlayer(world, new Vec2(-6f, 0f));

            TickUntil(world, () => world.Warnings.Count > 0, Balance.GiantFirstBigAttackDelay + 1f);
            Assert.That(world.Warnings.Count, Is.EqualTo(GiantAttacks.For(Element.Wind).Skill.Count));
            var center = world.Warnings[0].From;
            TickUntil(world, () => world.AreaEffects.Count > 0, 2f);
            Assert.That(world.AreaEffects[0].Kind, Is.EqualTo(AreaEffectKind.Vortex));

            // 竜巻の縁へ動かすと、中心へ寄せられる
            var radius = GiantAttacks.For(Element.Wind).Skill.AreaRadius;
            player.Position = center + new Vec2(radius * 1.2f, 0f);
            var before = Vec2.Distance(player.Position, center);
            TickFor(world, Seconds(0.5f));
            Assert.That(Vec2.Distance(player.Position, center), Is.LessThan(before));
        }

        [Test]
        public void TwoGiants_TakeTurnsWithTheirBigAttacks()
        {
            var world = CreateArena();
            var first = AddGiant(world, KingNo, new Vec2(6f, 3f));
            var second = AddGiant(world, GentooNo, new Vec2(6f, -3f));
            AddPlayer(world, new Vec2(-8f, 0f));
            var attackers = new List<int>();
            var wasAttacking = new Dictionary<int, bool> { [first.Id] = false, [second.Id] = false };

            for (var i = 0; i < Seconds(Balance.GiantFirstBigAttackDelay + Balance.GiantBigAttackInterval * 2f + 4f); i++)
            {
                world.Tick();
                var telegraphing = (first.Brain.Current != null ? 1 : 0) + (second.Brain.Current != null ? 1 : 0);
                Assert.That(telegraphing, Is.LessThanOrEqualTo(1), "2体の大技は重ならない");
                foreach (var giant in new[] { first, second })
                {
                    // 予告を出し始めた瞬間を数える
                    var attacking = giant.Brain.Current != null;
                    if (attacking && !wasAttacking[giant.Id])
                    {
                        attackers.Add(giant.Id);
                    }

                    wasAttacking[giant.Id] = attacking;
                }
            }

            Assert.That(attackers.Count, Is.GreaterThanOrEqualTo(3));
            for (var i = 1; i < attackers.Count; i++)
            {
                Assert.That(attackers[i], Is.Not.EqualTo(attackers[i - 1]), "交互に出す");
            }
        }

        [Test]
        public void SeriousBoss_RoarsBelowHalfHp_AndStopsEverything()
        {
            var world = CreateArena();
            var boss = AddBoss(world, BigBossTable.OrcaNo, new Vec2(6f, 0f), attackIndex: 0, serious: true);
            AddPlayer(world, new Vec2(-6f, 2f));
            world.Tick();
            Assert.That(boss.IsSerious, Is.True);
            Assert.That(boss.IsEnraged, Is.False);

            boss.Hp = boss.MaxHp / 2 - 1;
            world.Tick();
            Assert.That(boss.IsEnraged, Is.True);

            var position = boss.Position;
            var rest = boss.Brain.RestSeconds;
            TickFor(world, Balance.RageRoarTicks);
            Assert.That(boss.Position, Is.EqualTo(position));
            Assert.That(boss.Brain.RestSeconds, Is.EqualTo(rest), "吠えている間は全体が止まる");
        }

        [Test]
        public void OrdinaryBoss_NeverRoars()
        {
            var world = CreateArena();
            var boss = AddBoss(world, BigBossTable.OrcaNo, new Vec2(6f, 0f), attackIndex: 0, serious: false);
            AddPlayer(world, new Vec2(-6f, 2f));
            boss.Hp = 1;
            world.Tick();

            Assert.That(boss.IsEnraged, Is.False);
        }

        [Test]
        public void EnragedOrca_SendsTwoWavesAtOnce_WithShorterTelegraph()
        {
            var world = CreateArena();
            var boss = AddBoss(world, BigBossTable.OrcaNo, new Vec2(6f, 0f), attackIndex: 0, serious: true);
            AddPlayer(world, new Vec2(-6f, 2f));
            boss.Hp = boss.MaxHp / 3;

            TickUntil(world, () => world.Warnings.Count > 0, Balance.BigBossFirstAttackDelay + 2f);

            var wave = BigBossTable.Get(BigBossTable.OrcaNo).Attacks[0];
            Assert.That(world.Warnings.Count, Is.EqualTo(2));
            Assert.That(world.Warnings[1].From.Y, Is.Not.EqualTo(world.Warnings[0].From.Y));
            Assert.That(world.Warnings[0].TotalSeconds, Is.EqualTo(wave.TelegraphSeconds * Balance.RageTelegraphScale).Within(Tolerance));
        }

        [Test]
        public void EnragedSkua_RainsTwiceInARow()
        {
            var world = CreateArena();
            var boss = AddBoss(world, BigBossTable.SkuaNo, new Vec2(6f, 0f), attackIndex: 1, serious: true);
            AddPlayer(world, new Vec2(-6f, 2f));
            boss.Hp = boss.MaxHp / 3;

            TickUntil(world, () => world.Warnings.Count > 0, Balance.BigBossFirstAttackDelay + 2f);
            var firstIds = new HashSet<int>();
            foreach (var warning in world.Warnings)
            {
                firstIds.Add(warning.Id);
            }

            // 1回目が降った次の更新で、休まずに2回目の予告が出ている
            TickUntil(world, () => world.Warnings.Count == 0 || !firstIds.Contains(world.Warnings[0].Id), 2f);
            Assert.That(world.Warnings.Count, Is.GreaterThan(0));
            Assert.That(boss.Brain.Current.Kind, Is.EqualTo(BossAttackKind.Rain));

            TickUntil(world, () => boss.Brain.Current == null, 2f);
            Assert.That(boss.Brain.RestSeconds, Is.EqualTo(BigBossTable.Get(BigBossTable.SkuaNo).RestSeconds * Balance.RageRestScale).Within(Tolerance));
        }

        [Test]
        public void HardDifficulty_ShortensBigBossTelegraph()
        {
            var stage = new StageDef(DevStageTable.ArenaGauntletNo, "test", 1, AreaDef.Arena(new WaveDef(0, new[] { 1 }, BigBossTable.SkuaNo)));
            var world = new StageWorld(stage, playerCount: 1, seed: 1, Difficulty.Hard);
            AddPlayer(world, new Vec2(-6f, 2f));

            TickUntil(world, () => world.Warnings.Count > 0, Balance.FirstWaveDelaySeconds + Balance.BigBossFirstAttackDelay + 2f);

            var charge = BigBossTable.Get(BigBossTable.SkuaNo).Attacks[0];
            Assert.That(world.Warnings[0].TotalSeconds, Is.EqualTo(charge.TelegraphSeconds * Balance.DifficultyTelegraphScale(Difficulty.Hard)).Within(Tolerance));
        }
    }
}
