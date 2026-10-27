using NUnit.Framework;
using PenmonScramble.Core;

namespace PenmonScramble.Tests
{
    /// <summary>オンラインで送る中身（スナップショット・入力）と、その作り方</summary>
    public class StageNetTests
    {
        private static StageSnapshot SampleSnapshot()
        {
            var snapshot = new StageSnapshot
            {
                Round = 3,
                Tick = 120,
                Result = StageResult.Cleared,
                AreaMin = new Vec2(-8f, -4.5f),
                AreaMax = new Vec2(8f, 4.5f),
            };
            snapshot.Units.Add(new UnitState(1, Team.Player, 2, new Vec2(1.5f, -2f), new Vec2(0f, 1f), 80, 120, 0.45f));
            snapshot.Units.Add(new UnitState(7, Team.Enemy, Unit.NoSeat, new Vec2(3f, 4f), new Vec2(-1f, 0f), 5, 30, 0.45f));
            snapshot.Projectiles.Add(new ProjectileState(9, new Vec2(0.25f, 0.5f), 0.2f));
            snapshot.Hits.Add(new HitEvent(1, 7, 14, new Vec2(3f, 4f)));
            snapshot.Defeats.Add(new DefeatedEvent(8, Team.Enemy, new Vec2(-1f, 2f)));
            snapshot.SkillsUsed.Add(new SkillUsedEvent(1, 1, SkillShape.Projectile));
            return snapshot;
        }

        [Test]
        public void Snapshot_RoundTripsThroughCodec()
        {
            var decoded = StageCodec.DecodeSnapshot(StageCodec.EncodeSnapshot(SampleSnapshot()));

            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded.Round, Is.EqualTo(3));
            Assert.That(decoded.Tick, Is.EqualTo(120));
            Assert.That(decoded.Result, Is.EqualTo(StageResult.Cleared));
            Assert.That(decoded.AreaMax, Is.EqualTo(new Vec2(8f, 4.5f)));

            Assert.That(decoded.Units.Count, Is.EqualTo(2));
            var player = decoded.Units[0];
            Assert.That(player.Id, Is.EqualTo(1));
            Assert.That(player.Seat, Is.EqualTo(2));
            Assert.That(player.Position, Is.EqualTo(new Vec2(1.5f, -2f)));
            Assert.That(player.Hp, Is.EqualTo(80));
            Assert.That(player.MaxHp, Is.EqualTo(120));
            Assert.That(decoded.Units[1].Seat, Is.EqualTo(Unit.NoSeat));
            Assert.That(decoded.Units[1].Team, Is.EqualTo(Team.Enemy));

            Assert.That(decoded.Projectiles[0].Id, Is.EqualTo(9));
            Assert.That(decoded.Hits[0].Damage, Is.EqualTo(14));
            Assert.That(decoded.Defeats[0].UnitId, Is.EqualTo(8));
            Assert.That(decoded.SkillsUsed[0].Shape, Is.EqualTo(SkillShape.Projectile));
        }

        [Test]
        public void Snapshot_WithOtherVersion_IsIgnored()
        {
            var data = StageCodec.EncodeSnapshot(SampleSnapshot());
            data[0] = (byte)(StageCodec.FormatVersion + 1);

            Assert.That(StageCodec.DecodeSnapshot(data), Is.Null);
        }

        [Test]
        public void Snapshot_Truncated_IsIgnored()
        {
            var data = StageCodec.EncodeSnapshot(SampleSnapshot());
            System.Array.Resize(ref data, data.Length / 2);

            Assert.That(StageCodec.DecodeSnapshot(data), Is.Null);
        }

        [Test]
        public void Input_RoundTripsThroughCodec()
        {
            var input = new PlayerInput(new Vec2(-0.5f, 0.75f), skill1: false, skill2: true);

            Assert.That(StageCodec.TryDecodeInput(StageCodec.EncodeInput(input), out var decoded), Is.True);
            Assert.That(decoded.Move, Is.EqualTo(input.Move));
            Assert.That(decoded.Skill1, Is.False);
            Assert.That(decoded.Skill2, Is.True);
        }

        [Test]
        public void PrependEvents_PutsOlderEventsFirst()
        {
            var older = new StageSnapshot();
            older.Hits.Add(new HitEvent(1, 2, 10, Vec2.Zero));
            var newer = new StageSnapshot();
            newer.Hits.Add(new HitEvent(1, 2, 20, Vec2.Zero));

            newer.PrependEvents(older);

            Assert.That(newer.Hits.Count, Is.EqualTo(2));
            Assert.That(newer.Hits[0].Damage, Is.EqualTo(10));
            Assert.That(newer.Hits[1].Damage, Is.EqualTo(20));
        }

        [Test]
        public void Recorder_CapturesEventsOnce()
        {
            var players = TrialStage.Create(seed: 5, new[] { 0 }, out var world);
            var recorder = new StageRecorder(world, round: 1);
            world.SetInput(players[0].Id, new PlayerInput(Vec2.Zero, skill1: true, skill2: false));
            world.Tick();

            var first = recorder.Capture();
            var second = recorder.Capture();

            Assert.That(first.SkillsUsed.Count, Is.EqualTo(1));
            Assert.That(second.SkillsUsed.Count, Is.EqualTo(0));
            Assert.That(first.Round, Is.EqualTo(1));
            Assert.That(first.Tick, Is.EqualTo(1));
            Assert.That(first.TryGetPlayer(0, out var player), Is.True);
            Assert.That(player.Id, Is.EqualTo(players[0].Id));
            Assert.That(first.CountAlive(Team.Enemy), Is.EqualTo(TrialStage.EnemyCount));
            Assert.That(first.Result, Is.EqualTo(StageResult.Playing));
        }

        [Test]
        public void Recorder_ReportsFailedWhenNoPlayersLeft()
        {
            var players = TrialStage.Create(seed: 5, new[] { 0 }, out var world);
            var recorder = new StageRecorder(world, round: 1);

            world.RemoveUnit(players[0].Id);

            Assert.That(recorder.Capture().Result, Is.EqualTo(StageResult.Failed));
        }

        [Test]
        public void InputLatch_KeepsTapUntilConsumed()
        {
            var latch = new InputLatch();
            latch.Receive(new PlayerInput(Vec2.Zero, skill1: true, skill2: false));
            latch.Receive(new PlayerInput(Vec2.Right, skill1: false, skill2: false));

            var first = latch.Consume();
            var second = latch.Consume();

            Assert.That(first.Skill1, Is.True);
            Assert.That(first.Move, Is.EqualTo(Vec2.Right));
            Assert.That(second.Skill1, Is.False);
            Assert.That(second.Move, Is.EqualTo(Vec2.Right));
        }
    }
}
