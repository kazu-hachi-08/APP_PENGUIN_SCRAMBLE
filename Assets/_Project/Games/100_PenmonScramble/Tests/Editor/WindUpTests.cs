using System.Collections.Generic;
using NUnit.Framework;
using PenmonScramble.Core;
using static PenmonScramble.Tests.TestWorld;

namespace PenmonScramble.Tests
{
    /// <summary>雑魚・ステージのボスが技を出す前の構え（§4.7）</summary>
    public class WindUpTests
    {
        // 技が届く距離（体の半径 0.5 + 届く距離 1 × 0.8）の内側
        private static readonly Vec2 EnemyStart = new Vec2(1.2f, 0f);
        private static readonly PlayerInput Skill1Press = new PlayerInput(Vec2.Zero, skill1: true, skill2: false);

        // 構えの最後の更新は、秒数の丸めで前後に1回ずれることがあるので、その分の余裕を取る
        private static int WindUpTicks => Seconds(Balance.EnemyWindUpSeconds);

        private static (StageWorld world, Unit player, Unit enemy) CreateDuel()
        {
            var world = Create();
            var player = world.AddUnit(Team.Player, Stats(Melee()), Vec2.Zero);
            var enemy = world.AddUnit(Team.Enemy, Stats(Melee(power: 5f)), EnemyStart);
            // 最初の待ち時間はばらけさせてあるので、すぐ構えるようにそろえる
            enemy.Cooldowns[0] = 0f;
            return (world, player, enemy);
        }

        private static List<SkillUsedEvent> RecordEnemySkills(StageWorld world, Unit enemy)
        {
            var used = new List<SkillUsedEvent>();
            world.SkillUsed += e =>
            {
                if (e.UnitId == enemy.Id)
                {
                    used.Add(e);
                }
            };
            return used;
        }

        [Test]
        public void InRange_WindsUpFirst_ThenHits()
        {
            var (world, player, enemy) = CreateDuel();

            world.Tick();
            Assert.That(enemy.IsWindingUp, Is.True);

            TickFor(world, WindUpTicks - 2);
            Assert.That(player.Hp, Is.EqualTo(player.MaxHp), "構えの間は技が出ない");

            TickFor(world, 3);
            Assert.That(player.Hp, Is.LessThan(player.MaxHp));
            Assert.That(enemy.IsWindingUp, Is.False);
        }

        [Test]
        public void WindingUp_DoesNotMoveOrTurn()
        {
            var (world, player, enemy) = CreateDuel();
            world.Tick();
            var facing = enemy.Facing;
            var position = enemy.Position;

            // 後ろへ回り込んでも、向きを変えずに構えたときの向きへ出す
            player.Position = new Vec2(2.4f, 0f);
            var used = RecordEnemySkills(world, enemy);
            TickFor(world, WindUpTicks + 1);

            Assert.That(used.Count, Is.EqualTo(1));
            Assert.That(enemy.Facing, Is.EqualTo(facing));
            Assert.That(enemy.Position.X, Is.EqualTo(position.X).Within(Tolerance));
            Assert.That(player.Hp, Is.EqualTo(player.MaxHp));
        }

        [Test]
        public void SidestepDuringWindUp_Dodges()
        {
            var (world, player, enemy) = CreateDuel();
            world.Tick();
            var used = RecordEnemySkills(world, enemy);

            world.SetInput(player.Id, new PlayerInput(new Vec2(0f, 1f), skill1: false, skill2: false));
            TickFor(world, WindUpTicks + 1);

            Assert.That(used.Count, Is.EqualTo(1), "技は出ている");
            Assert.That(player.Hp, Is.EqualTo(player.MaxHp), "横へ一歩ずれれば当たらない");
        }

        [Test]
        public void KnockedBackDuringWindUp_Interrupts_AndUsesCooldown()
        {
            var world = Create();
            var hard = Melee();
            hard.Knockback = 8f;
            var player = world.AddUnit(Team.Player, Stats(hard), Vec2.Zero);
            var enemy = world.AddUnit(Team.Enemy, Stats(Melee(power: 5f)), EnemyStart);
            enemy.Cooldowns[0] = 0f;
            world.Tick();
            Assert.That(enemy.IsWindingUp, Is.True);
            var used = RecordEnemySkills(world, enemy);

            Press(world, player, Skill1Press);

            Assert.That(enemy.IsWindingUp, Is.False);
            Assert.That(enemy.Cooldowns[0], Is.GreaterThan(0f), "すぐ構え直さない");
            TickFor(world, WindUpTicks + 1);
            Assert.That(used.Count, Is.EqualTo(0));
        }

        [Test]
        public void FrozenDuringWindUp_Interrupts()
        {
            var (world, _, enemy) = CreateDuel();
            world.Tick();

            enemy.FrozenSeconds = 1f;
            world.Tick();

            Assert.That(enemy.IsWindingUp, Is.False);
            Assert.That(enemy.Cooldowns[0], Is.GreaterThan(0f));
        }

        [Test]
        public void Boss_WindsUpLongerForSkill2()
        {
            var world = Create();
            world.AddUnit(Team.Player, Stats(Melee()), Vec2.Zero);
            var boss = world.AddUnit(Team.Enemy, SpeciesTable.BossStats(SpeciesTable.Get(1), 1), new Vec2(1.2f, 0f));
            boss.Cooldowns[0] = 0f;
            boss.Cooldowns[1] = 0f;

            world.Tick();

            Assert.That(boss.WindUp, Is.Not.Null);
            Assert.That(boss.WindUp.Slot, Is.EqualTo(1));
            Assert.That(boss.WindUp.TotalSeconds, Is.EqualTo(Balance.BossSkill2WindUpSeconds));
        }

        [Test]
        public void EnemyWithoutSkills_NeverWindsUp()
        {
            var world = Create();
            world.AddUnit(Team.Player, Stats(Melee()), Vec2.Zero);
            var dummy = world.AddUnit(Team.Enemy, Stats(), EnemyStart);

            TickFor(world, 10);

            Assert.That(dummy.IsWindingUp, Is.False);
        }

        [Test]
        public void Recorder_SendsWindUpFlagAndTelegraph()
        {
            var (world, _, enemy) = CreateDuel();
            var recorder = new StageRecorder(world, round: 1);
            world.Tick();

            var snapshot = StageCodec.DecodeSnapshot(StageCodec.EncodeSnapshot(recorder.Capture()));

            Assert.That(snapshot.Units.Find(u => u.Id == enemy.Id).Has(UnitFlags.WindingUp), Is.True);
            Assert.That(snapshot.Telegraphs.Count, Is.EqualTo(1));
            var telegraph = snapshot.Telegraphs[0];
            Assert.That(telegraph.UnitId, Is.EqualTo(enemy.Id));
            Assert.That(telegraph.Shape, Is.EqualTo(SkillShape.Melee));
            Assert.That(telegraph.Facing, Is.EqualTo(enemy.Facing));
            Assert.That(telegraph.Reach, Is.EqualTo(1f));
            Assert.That(telegraph.HalfAngleDegrees, Is.EqualTo(60f));
            Assert.That(telegraph.Seconds, Is.EqualTo(Balance.EnemyWindUpSeconds));
        }

        [Test]
        public void Recorder_ClearsTelegraphAfterSkillFires()
        {
            var (world, _, enemy) = CreateDuel();
            var recorder = new StageRecorder(world, round: 1);
            TickFor(world, WindUpTicks + 2);

            var snapshot = recorder.Capture();

            Assert.That(snapshot.Units.Find(u => u.Id == enemy.Id).Has(UnitFlags.WindingUp), Is.False);
            Assert.That(snapshot.Telegraphs.Count, Is.EqualTo(0));
        }
    }
}
