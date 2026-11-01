using System.Linq;
using NUnit.Framework;
using PenmonScramble.Core;
using static PenmonScramble.Tests.TestWorld;

namespace PenmonScramble.Tests
{
    /// <summary>
    /// オオフルマカモメ（突風・動く竜巻・急降下）とサメ（背びれ・食いつきと溶岩のしぶき・突進）。
    /// 攻撃の順番・予告の形・もぐっている間の扱い・本気で増える攻撃（竜巻が増える・背びれが2本）
    /// </summary>
    public class NewBigBossTests
    {
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

        /// <summary>予告の場所が読みやすいよう、休みの間も寄ってこない大ボス</summary>
        private static Unit AddBoss(StageWorld world, int no, Vec2 position, int attackIndex, bool serious = false)
        {
            var stats = BigBossTable.Stats(BigBossTable.Get(no), playerCount: 1, world: 1, serious);
            stats.MoveSpeed = 0f;
            var boss = world.AddUnit(Team.Enemy, stats, position);
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

        private static void WaitForWarning(StageWorld world) => TickUntil(world, () => world.Warnings.Count > 0, Balance.BigBossFirstAttackDelay + 0.5f);

        private static float TelegraphOf(int no, int attackIndex) => BigBossTable.Get(no).Attacks[attackIndex].TelegraphSeconds;

        [Test]
        public void Table_HasFiveBigBosses_WithTheirAttacksInOrder()
        {
            Assert.That(BigBossTable.Count, Is.EqualTo(5));
            Assert.That(BigBossTable.Get(BigBossTable.FulmarNo).Attacks.Select(a => a.Kind),
                Is.EqualTo(new[] { BossAttackKind.Gust, BossAttackKind.Vortex, BossAttackKind.Charge }));
            Assert.That(BigBossTable.Get(BigBossTable.SharkNo).Attacks.Select(a => a.Kind),
                Is.EqualTo(new[] { BossAttackKind.Fin, BossAttackKind.Ambush, BossAttackKind.Charge }));
            Assert.That(BigBossTable.Get(BigBossTable.SharkNo + 1), Is.Null);
        }

        [Test]
        public void FanWarning_CoversOnlyTheAngleAndRadius()
        {
            var fan = new BossWarning(1, BossAttackKind.Gust, Vec2.Zero, new Vec2(9f, 0f), 0f, 35f, 1f);

            Assert.That(fan.IsFan, Is.True);
            Assert.That(fan.Overlaps(new Vec2(5f, 0f), 0.5f), Is.True, "真ん中");
            Assert.That(fan.Overlaps(new Vec2(5f, 3.6f), 0.5f), Is.True, "縁に体がかかる");
            Assert.That(fan.Overlaps(new Vec2(5f, 5f), 0.5f), Is.False, "扇の外の角度");
            Assert.That(fan.Overlaps(new Vec2(10f, 0f), 0.5f), Is.False, "半径の外");
            Assert.That(fan.Overlaps(new Vec2(-5f, 0f), 0.5f), Is.False, "後ろ");
        }

        [Test]
        public void Gust_WarnsWithAFan_ThenPushesThePlayerIntoTheFence_WhichHurts()
        {
            var world = CreateArena();
            AddBoss(world, BigBossTable.FulmarNo, new Vec2(6f, 0f), attackIndex: 0);
            var target = AddPlayer(world, new Vec2(10f, 0f));
            var aside = AddPlayer(world, new Vec2(6f, -6f));

            WaitForWarning(world);
            Assert.That(world.Warnings[0].Kind, Is.EqualTo(BossAttackKind.Gust));
            Assert.That(world.Warnings[0].IsFan, Is.True);

            TickFor(world, Seconds(TelegraphOf(BigBossTable.FulmarNo, 0)));
            Assert.That(target.IsBlownByGust, Is.True, "扇の中は押し流される");
            Assert.That(aside.IsBlownByGust, Is.False, "扇の外は押されない");

            TickUntil(world, () => !target.IsBlownByGust, 2f);
            Assert.That(target.Hp, Is.LessThan(target.MaxHp), "柵に当たって痛い");
            Assert.That(aside.Hp, Is.EqualTo(aside.MaxHp));
        }

        [Test]
        public void Gust_DoesNotHurt_WhenThePushEndsBeforeTheFence()
        {
            var world = CreateArena();
            AddBoss(world, BigBossTable.FulmarNo, new Vec2(-13f, 0f), attackIndex: 0);
            var target = AddPlayer(world, new Vec2(-9f, 0f));

            WaitForWarning(world);
            TickFor(world, Seconds(TelegraphOf(BigBossTable.FulmarNo, 0)));
            TickUntil(world, () => !target.IsBlownByGust, 2f);

            Assert.That(target.Position.X, Is.GreaterThan(-9f + 5f), "大きく押し流される");
            Assert.That(target.Hp, Is.EqualTo(target.MaxHp), "押されただけでは痛くない");
        }

        [Test]
        public void Vortex_DriftsTowardThePlayer()
        {
            var world = CreateArena();
            AddBoss(world, BigBossTable.FulmarNo, new Vec2(-8f, 0f), attackIndex: 1);
            var player = AddPlayer(world, new Vec2(8f, 0f));

            WaitForWarning(world);
            Assert.That(world.Warnings.Count, Is.EqualTo(BigBossTable.Get(BigBossTable.FulmarNo).Attacks[1].Skill.Count));
            TickFor(world, Seconds(TelegraphOf(BigBossTable.FulmarNo, 1)));
            var vortexes = world.AreaEffects.Where(e => e.Kind == AreaEffectKind.Vortex).ToArray();
            Assert.That(vortexes.Length, Is.EqualTo(3));
            var before = vortexes.Sum(v => Vec2.Distance(v.Position, player.Position));

            TickFor(world, Seconds(1f));
            var after = vortexes.Sum(v => Vec2.Distance(v.Position, player.Position));
            Assert.That(after, Is.LessThan(before - 1f), "ゆっくり寄ってくる");
        }

        [Test]
        public void SeriousFulmar_AddsVortexesAfterRoaring()
        {
            var world = CreateArena();
            var boss = AddBoss(world, BigBossTable.FulmarNo, new Vec2(-8f, 0f), attackIndex: 1, serious: true);
            boss.Brain.IsEnraged = true;
            AddPlayer(world, new Vec2(8f, 0f));

            WaitForWarning(world);

            var def = BigBossTable.Get(BigBossTable.FulmarNo);
            Assert.That(world.Warnings.Count, Is.EqualTo(def.Attacks[1].Skill.Count + def.Rage.Extra));
        }

        [Test]
        public void Fin_IsInvincibleWhileRunning_HitsOnThePath_AndRunsSeveralLegsBeforeSurfacing()
        {
            var world = CreateArena();
            var boss = AddBoss(world, BigBossTable.SharkNo, new Vec2(0f, 0f), attackIndex: 0);
            var player = AddPlayer(world, new Vec2(5f, 0f));

            WaitForWarning(world);
            Assert.That(world.Warnings[0].Kind, Is.EqualTo(BossAttackKind.Fin));
            Assert.That(boss.IsSubmerged, Is.True);
            Assert.That(boss.IsInvincible, Is.True, "もぐっている間は攻撃が効かない");
            Assert.That(world.Warnings[0].HalfWidth, Is.LessThan(boss.Radius), "当たるのは背びれだけ");

            var legs = 1;
            var warned = true;
            var hitWhileSubmerged = false;
            for (var i = 0; i < Seconds(10f) && boss.IsSubmerged; i++)
            {
                world.Tick();
                hitWhileSubmerged |= boss.IsSubmerged && player.Hp < player.MaxHp;
                var warning = world.Warnings.Count > 0;
                legs += warning && !warned ? 1 : 0;
                warned = warning;
            }

            Assert.That(boss.IsSubmerged, Is.False, "走り終えたら浮き上がる");
            Assert.That(legs, Is.EqualTo(1 + BigBossTable.Get(BigBossTable.SharkNo).Attacks[0].Skill.Count), "向きを変えて走り直す");
            Assert.That(hitWhileSubmerged, Is.True, "背びれに触れると当たる");
        }

        [Test]
        public void SeriousShark_RunsASecondFinAfterRoaring()
        {
            var world = CreateArena();
            var boss = AddBoss(world, BigBossTable.SharkNo, new Vec2(0f, 0f), attackIndex: 0, serious: true);
            boss.Brain.IsEnraged = true;
            AddPlayer(world, new Vec2(3f, 0f));
            // 2本目は1本目（一番近いプレイヤーへの道）から左回りに開く
            var spread = Balance.RageFinSpreadDegrees * System.MathF.PI / 180f;
            var second = AddPlayer(world, new Vec2(1f, 0f).Rotated(spread) * 5f);

            WaitForWarning(world);
            Assert.That(world.Warnings.Count, Is.EqualTo(2));
            Assert.That(world.Warnings.All(w => w.Kind == BossAttackKind.Fin), Is.True);

            TickUntil(world, () => world.AreaEffects.Any(e => e.Kind == AreaEffectKind.Fin), TelegraphOf(BigBossTable.SharkNo, 0) + 0.1f);
            TickFor(world, Seconds(1f));
            Assert.That(second.Hp, Is.LessThan(second.MaxHp), "2本目の背びれが当たる");
        }

        [Test]
        public void SharkBite_LeavesLavaSplashesAroundTheSpot_WhichFade()
        {
            var world = CreateArena();
            AddBoss(world, BigBossTable.SharkNo, new Vec2(-6f, 0f), attackIndex: 1);
            AddPlayer(world, new Vec2(5f, 0f));

            WaitForWarning(world);
            Assert.That(world.Warnings[0].Kind, Is.EqualTo(BossAttackKind.Ambush));
            var spot = world.Warnings[0].From;
            TickFor(world, Seconds(TelegraphOf(BigBossTable.SharkNo, 1)));

            var bite = BigBossTable.Get(BigBossTable.SharkNo).Attacks[1];
            var lava = world.AreaEffects.Where(e => e.Kind == AreaEffectKind.Lava).ToArray();
            Assert.That(lava.Length, Is.EqualTo(bite.Leftover.Count));
            foreach (var splash in lava)
            {
                Assert.That(Vec2.Distance(splash.Position, spot), Is.EqualTo(bite.Skill.AreaRadius * Balance.LeftoverSpreadRatio).Within(0.5f));
            }

            TickFor(world, Seconds(bite.Leftover.Duration + 0.1f));
            Assert.That(world.AreaEffects.Any(e => e.Kind == AreaEffectKind.Lava), Is.False);
        }

        [Test]
        public void Snapshot_SendsTheWarningKindAndFanAngle()
        {
            var world = CreateArena();
            AddBoss(world, BigBossTable.FulmarNo, new Vec2(0f, 0f), attackIndex: 0);
            AddPlayer(world, new Vec2(4f, 0f));
            var recorder = new StageRecorder(world, round: 1);
            WaitForWarning(world);

            var decoded = StageCodec.DecodeSnapshot(StageCodec.EncodeSnapshot(recorder.Capture()));

            Assert.That(decoded.Warnings[0].Kind, Is.EqualTo(BossAttackKind.Gust));
            Assert.That(decoded.Warnings[0].IsFan, Is.True);
            Assert.That(decoded.Warnings[0].HalfAngleDegrees, Is.EqualTo(world.Warnings[0].HalfAngleDegrees));
        }

        [Test]
        public void DevStage_FightsBothNewBigBosses_ThenTheirSeriousForms()
        {
            var stage = Stages.Get(DevStageTable.NewBossesNo);

            Assert.That(stage, Is.SameAs(DevStageTable.NewBosses));
            Assert.That(Stages.IsDev(DevStageTable.NewBossesNo), Is.True);
            var bosses = stage.Areas.Select(a => a.Waves[0].Bosses[0]).ToArray();
            Assert.That(bosses.Select(b => b.SpeciesNo), Is.EqualTo(new[] { BigBossTable.FulmarNo, BigBossTable.SharkNo, BigBossTable.FulmarNo, BigBossTable.SharkNo }));
            Assert.That(bosses.Select(b => b.Rank), Is.EqualTo(new[] { BossRank.Normal, BossRank.Normal, BossRank.Serious, BossRank.Serious }));
            Assert.That(stage.Areas.All(a => a.IsArena), Is.True);
        }
    }
}
