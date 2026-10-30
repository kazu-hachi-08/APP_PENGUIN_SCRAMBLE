using NUnit.Framework;
using PenmonScramble.Core;
using static PenmonScramble.Tests.TestWorld;

namespace PenmonScramble.Tests
{
    /// <summary>ダウンと復活・全員ダウンで失敗（§4.5）</summary>
    public class ReviveTests
    {
        // 判定の境目（3秒ちょうど）の小数の誤差に左右されないよう、前後に余裕を持たせて確かめる
        private const int MarginTicks = 5;

        private static Unit AddPlayer(StageWorld world, Vec2 position, int seat)
        {
            var stats = Stats(Melee());
            return world.AddUnit(Team.Player, new[] { stats, Stats(Melee()), Stats(Melee()) }, position, seat);
        }

        /// <summary>3体とも倒す。次の更新でダウンになる</summary>
        private static void KnockDown(StageWorld world, Unit player)
        {
            foreach (var member in player.Members)
            {
                member.Hp = 0;
            }

            world.Tick();
        }

        private static (StageWorld world, Unit downed, Unit rescuer) CreateCoop()
        {
            var world = Create();
            var downed = AddPlayer(world, Vec2.Zero, seat: 0);
            var rescuer = AddPlayer(world, new Vec2(1.2f, 0f), seat: 1);
            // 敵が1体いないとクリアになって止まる。殴ってこない的を遠くに置く
            AddDummyEnemy(world, new Vec2(8f, 8f));
            return (world, downed, rescuer);
        }

        [Test]
        public void OnePlayerDown_IsNotFailed()
        {
            var (world, downed, _) = CreateCoop();

            KnockDown(world, downed);

            Assert.That(downed.IsDown, Is.True);
            Assert.That(world.FindUnit(downed.Id), Is.Not.Null);
            Assert.That(world.Result, Is.EqualTo(StageResult.Playing));
        }

        [Test]
        public void AllPlayersDown_IsFailed()
        {
            var (world, downed, rescuer) = CreateCoop();

            KnockDown(world, downed);
            KnockDown(world, rescuer);

            Assert.That(world.Result, Is.EqualTo(StageResult.Failed));
        }

        [Test]
        public void AllyNearby_RevivesFirstMemberAfterThreeSeconds()
        {
            var (world, downed, _) = CreateCoop();
            downed.SwitchToNextAlive();
            KnockDown(world, downed);

            TickFor(world, Seconds(Balance.ReviveSeconds) - MarginTicks);
            Assert.That(downed.IsDown, Is.True);

            TickFor(world, MarginTicks * 2);
            Assert.That(downed.IsDown, Is.False);
            Assert.That(downed.ActiveIndex, Is.EqualTo(0));
            Assert.That(downed.Hp, Is.EqualTo((int)System.MathF.Round(downed.MaxHp * Balance.ReviveHpRatio)));
            Assert.That(downed.IsInvincible, Is.True);
        }

        [Test]
        public void ReviveTime_AddsUpEvenAfterAllyLeaves()
        {
            var (world, downed, rescuer) = CreateCoop();
            KnockDown(world, downed);
            var half = Seconds(Balance.ReviveSeconds) / 2;

            TickFor(world, half);
            rescuer.Position = new Vec2(6f, 0f);
            TickFor(world, Seconds(Balance.ReviveSeconds) * 2);
            Assert.That(downed.IsDown, Is.True);

            rescuer.Position = new Vec2(1.2f, 0f);
            TickFor(world, Seconds(Balance.ReviveSeconds) - half + MarginTicks);
            Assert.That(downed.IsDown, Is.False);
        }

        [Test]
        public void NoAllyNearby_StaysDown()
        {
            var (world, downed, rescuer) = CreateCoop();
            rescuer.Position = new Vec2(6f, 0f);
            KnockDown(world, downed);

            TickFor(world, Seconds(Balance.ReviveSeconds) * 2);

            Assert.That(downed.IsDown, Is.True);
        }

        [Test]
        public void Recorder_MarksDownAndRevive()
        {
            var (world, downed, _) = CreateCoop();
            var recorder = new StageRecorder(world, round: 1);
            KnockDown(world, downed);
            TickFor(world, Seconds(Balance.ReviveSeconds) / 2);

            var snapshot = recorder.Capture();

            Assert.That(snapshot.TryGetPlayer(0, out var state), Is.True);
            Assert.That(state.Has(UnitFlags.Down), Is.True);
            Assert.That(state.Revive, Is.GreaterThan(0.3f).And.LessThan(0.7f));
        }
    }
}
