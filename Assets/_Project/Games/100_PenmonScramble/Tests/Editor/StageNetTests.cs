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
                StageNo = 2,
                Tick = 120,
                Result = StageResult.Cleared,
                AreaMin = new Vec2(-8f, -4.5f),
                AreaMax = new Vec2(8f, 4.5f),
                AreaIndex = 2,
                AreaCount = 6,
                IsGateOpen = true,
                EnemiesDefeated = 37,
            };
            snapshot.Units.Add(new UnitState(1, Team.Player, 2, 5, new Vec2(1.5f, -2f), new Vec2(0f, 1f), 80, 120, 0.45f, UnitFlags.Dashing | UnitFlags.Invincible));
            snapshot.Units.Add(new UnitState(7, Team.Enemy, Unit.NoSeat, 20, new Vec2(3f, 4f), new Vec2(-1f, 0f), 5, 30, 0.45f, UnitFlags.Frozen));
            snapshot.Units.Add(new UnitState(12, Team.Player, 1, 3, Vec2.Zero, Vec2.Right, 0, 100, 0.45f, UnitFlags.Down, revive: 0.5f));
            snapshot.Units.Add(new UnitState(13, Team.Enemy, Unit.NoSeat, 4, Vec2.Zero, Vec2.Right, 300, 500, 0.9f, UnitFlags.Boss));
            snapshot.Projectiles.Add(new ProjectileState(9, Team.Enemy, Element.Water, new Vec2(0.25f, 0.5f), 0.2f));
            snapshot.Hits.Add(new HitEvent(1, 7, 14, new Vec2(3f, 4f)));
            snapshot.Defeats.Add(new DefeatedEvent(8, Team.Enemy, new Vec2(-1f, 2f)));
            snapshot.SkillsUsed.Add(new SkillUsedEvent(1, 1, SkillShape.Projectile));
            snapshot.Effects.Add(new EffectState(11, AreaEffectKind.Drop, Element.Thunder, new Vec2(-2f, 1f), 1.4f));
            var player = new PlayerState { Seat = 2, ActiveIndex = 1, SpecialGauge = 0.75f, SwitchCooldown = 1.5f };
            player.Members.Add(new MemberState(1, 0, 120));
            player.Members.Add(new MemberState(5, 80, 170));
            player.Members.Add(new MemberState(9, 135, 135));
            player.SkillCooldowns.Add(0f);
            player.SkillCooldowns.Add(0.6f);
            snapshot.Players.Add(player);
            snapshot.SpecialsUsed.Add(new SpecialUsedEvent(1, 5, Element.Ice));
            snapshot.Bursts.Add(new BurstEvent(BurstShape.Ring, Element.Wind, new Vec2(1f, 1f), 5f, true));
            snapshot.Combos.Add(new ComboEvent(ComboKind.Shock, new Vec2(-3f, 2f)));
            return snapshot;
        }

        [Test]
        public void Snapshot_RoundTripsThroughCodec()
        {
            var decoded = StageCodec.DecodeSnapshot(StageCodec.EncodeSnapshot(SampleSnapshot()));

            Assert.That(decoded, Is.Not.Null);
            Assert.That(decoded.Round, Is.EqualTo(3));
            Assert.That(decoded.StageNo, Is.EqualTo(2));
            Assert.That(decoded.Tick, Is.EqualTo(120));
            Assert.That(decoded.Result, Is.EqualTo(StageResult.Cleared));
            Assert.That(decoded.AreaMax, Is.EqualTo(new Vec2(8f, 4.5f)));

            Assert.That(decoded.Units.Count, Is.EqualTo(4));
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
        public void Snapshot_RoundTripsPhase3Fields()
        {
            var decoded = StageCodec.DecodeSnapshot(StageCodec.EncodeSnapshot(SampleSnapshot()));

            Assert.That(decoded.Units[0].SpeciesNo, Is.EqualTo(5));
            Assert.That(decoded.Units[0].Has(UnitFlags.Dashing), Is.True);
            Assert.That(decoded.Units[0].Has(UnitFlags.Frozen), Is.False);
            Assert.That(decoded.Units[1].Has(UnitFlags.Frozen), Is.True);
            Assert.That(decoded.Projectiles[0].Team, Is.EqualTo(Team.Enemy));
            Assert.That(decoded.Projectiles[0].Element, Is.EqualTo(Element.Water));

            var effect = decoded.Effects[0];
            Assert.That(effect.Kind, Is.EqualTo(AreaEffectKind.Drop));
            Assert.That(effect.Element, Is.EqualTo(Element.Thunder));
            Assert.That(effect.Radius, Is.EqualTo(1.4f));

            var player = decoded.FindPlayerState(2);
            Assert.That(player, Is.Not.Null);
            Assert.That(player.ActiveIndex, Is.EqualTo(1));
            Assert.That(player.SpecialGauge, Is.EqualTo(0.75f));
            Assert.That(player.SwitchCooldown, Is.EqualTo(1.5f));
            Assert.That(player.Members.Count, Is.EqualTo(3));
            Assert.That(player.Members[1].SpeciesNo, Is.EqualTo(5));
            Assert.That(player.Members[1].Hp, Is.EqualTo(80));
            Assert.That(player.Members[1].MaxHp, Is.EqualTo(170));
            // 待ちは 1 バイトに丸めて送る
            Assert.That(player.SkillCooldowns.Count, Is.EqualTo(2));
            Assert.That(player.SkillCooldowns[0], Is.EqualTo(0f));
            Assert.That(player.SkillCooldowns[1], Is.EqualTo(0.6f).Within(0.01f));

            Assert.That(decoded.SpecialsUsed[0].SpeciesNo, Is.EqualTo(5));
            Assert.That(decoded.SpecialsUsed[0].Element, Is.EqualTo(Element.Ice));
            Assert.That(decoded.Bursts[0].Shape, Is.EqualTo(BurstShape.Ring));
            Assert.That(decoded.Bursts[0].Radius, Is.EqualTo(5f));
            Assert.That(decoded.Bursts[0].IsSpecial, Is.True);
            Assert.That(decoded.Combos.Count, Is.EqualTo(1));
            Assert.That(decoded.Combos[0].Kind, Is.EqualTo(ComboKind.Shock));
            Assert.That(decoded.Combos[0].Position, Is.EqualTo(new Vec2(-3f, 2f)));
        }

        [Test]
        public void Snapshot_RoundTripsPhase4Fields()
        {
            var decoded = StageCodec.DecodeSnapshot(StageCodec.EncodeSnapshot(SampleSnapshot()));

            Assert.That(decoded.AreaIndex, Is.EqualTo(2));
            Assert.That(decoded.AreaCount, Is.EqualTo(6));
            Assert.That(decoded.IsGateOpen, Is.True);
            Assert.That(decoded.EnemiesDefeated, Is.EqualTo(37));
            Assert.That(decoded.Units[2].Has(UnitFlags.Down), Is.True);
            Assert.That(decoded.Units[2].Revive, Is.EqualTo(0.5f).Within(1f / byte.MaxValue));
            Assert.That(decoded.TryGetBoss(out var boss), Is.True);
            Assert.That(boss.Id, Is.EqualTo(13));
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
        public void Input_RoundTripsSpecialAndSwitch()
        {
            var input = new PlayerInput(Vec2.Zero, skill1: true, skill2: false, special: true, swap: true);

            Assert.That(StageCodec.TryDecodeInput(StageCodec.EncodeInput(input), out var decoded), Is.True);
            Assert.That(decoded.Skill1, Is.True);
            Assert.That(decoded.Special, Is.True);
            Assert.That(decoded.Switch, Is.True);
        }

        [Test]
        public void Input_RoundTripsInMenu()
        {
            Assert.That(StageCodec.TryDecodeInput(StageCodec.EncodeInput(PlayerInput.MenuOpen), out var decoded), Is.True);
            Assert.That(decoded.InMenu, Is.True);
            Assert.That(decoded.Move, Is.EqualTo(Vec2.Zero));
            Assert.That(decoded.Skill1 || decoded.Skill2 || decoded.Special || decoded.Switch, Is.False);
        }

        [Test]
        public void InputLatch_KeepsLatestInMenu()
        {
            var latch = new InputLatch();
            latch.Receive(PlayerInput.MenuOpen);

            Assert.That(latch.Consume().InMenu, Is.True);
            // メニューを開いたままなら、届いた最後の入力のまま「メニュー中」が続く
            Assert.That(latch.Consume().InMenu, Is.True);

            latch.Receive(new PlayerInput(Vec2.Zero, false, false));
            Assert.That(latch.Consume().InMenu, Is.False);
        }

        [Test]
        public void Recorder_MarksPlayerInMenu()
        {
            var players = TrialStage.Create(seed: 5, new[] { 0, 1 }, round: 1, out var world);
            var recorder = new StageRecorder(world, round: 1);
            world.SetInput(players[1].Id, PlayerInput.MenuOpen);
            world.Tick();

            var snapshot = StageCodec.DecodeSnapshot(StageCodec.EncodeSnapshot(recorder.Capture()));

            Assert.That(snapshot.TryGetPlayer(0, out var open), Is.True);
            Assert.That(open.Has(UnitFlags.InMenu), Is.False);
            Assert.That(snapshot.TryGetPlayer(1, out var inMenu), Is.True);
            Assert.That(inMenu.Has(UnitFlags.InMenu), Is.True);
        }

        [Test]
        public void InputLatch_KeepsSwitchTapUntilConsumed()
        {
            var latch = new InputLatch();
            latch.Receive(new PlayerInput(Vec2.Zero, false, false, special: false, swap: true));
            latch.Receive(new PlayerInput(Vec2.Zero, false, false));

            Assert.That(latch.Consume().Switch, Is.True);
            Assert.That(latch.Consume().Switch, Is.False);
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
            var players = TrialStage.Create(seed: 5, new[] { 0 }, round: 1, out var world);
            var recorder = new StageRecorder(world, round: 1);
            world.SetInput(players[0].Id, new PlayerInput(Vec2.Zero, skill1: true, skill2: false));
            world.Tick();

            var first = recorder.Capture();
            var second = recorder.Capture();

            Assert.That(first.SkillsUsed.Count, Is.EqualTo(1));
            Assert.That(second.SkillsUsed.Count, Is.EqualTo(0));
            Assert.That(first.Round, Is.EqualTo(1));
            Assert.That(first.StageNo, Is.EqualTo(StageTable.Stage1.No));
            Assert.That(first.Tick, Is.EqualTo(1));
            Assert.That(first.TryGetPlayer(0, out var player), Is.True);
            Assert.That(player.Id, Is.EqualTo(players[0].Id));
            // 最初の波は少し待ってから出る
            Assert.That(first.CountAlive(Team.Enemy), Is.EqualTo(0));
            Assert.That(first.AreaCount, Is.EqualTo(StageTable.Stage1.Areas.Count));
            Assert.That(first.Result, Is.EqualTo(StageResult.Playing));
            Assert.That(first.FindPlayerState(0).Members.Count, Is.EqualTo(Balance.PartySize));
            // 技1を出したばかりなので、技1の待ちはほぼ満タン・技2は出せる
            var skills = first.FindPlayerState(0).SkillCooldowns;
            Assert.That(skills.Count, Is.EqualTo(2));
            Assert.That(skills[0], Is.GreaterThan(0.9f));
            Assert.That(skills[1], Is.EqualTo(0f));
        }

        [Test]
        public void PlayerState_NextAliveIndex_SkipsFallen()
        {
            var state = new PlayerState { ActiveIndex = 0 };
            state.Members.Add(new MemberState(1, 50, 100));
            state.Members.Add(new MemberState(2, 0, 100));
            state.Members.Add(new MemberState(3, 70, 100));

            Assert.That(state.NextAliveIndex(), Is.EqualTo(2));
            state.ActiveIndex = 2;
            Assert.That(state.NextAliveIndex(), Is.EqualTo(0));
            state.Members[0] = new MemberState(1, 0, 100);
            Assert.That(state.NextAliveIndex(), Is.EqualTo(-1));
        }

        [Test]
        public void Recorder_ReportsFailedWhenNoPlayersLeft()
        {
            var players = TrialStage.Create(seed: 5, new[] { 0 }, round: 1, out var world);
            var recorder = new StageRecorder(world, round: 1);

            world.RemoveUnit(players[0].Id);

            Assert.That(recorder.Capture().Result, Is.EqualTo(StageResult.Failed));
        }

        [Test]
        public void Party_RoundTripsThroughCodec()
        {
            var party = new[] { new OwnedPenguin(10, 5, 150), new OwnedPenguin(11, 8, 240), new OwnedPenguin(12, 1, 90) };

            Assert.That(StageCodec.TryDecodeParty(StageCodec.EncodeParty(party), out var stats), Is.True);

            Assert.That(stats.Length, Is.EqualTo(3));
            Assert.That(stats[1].SpeciesNo, Is.EqualTo(8));
            Assert.That(stats[1].MaxHp, Is.EqualTo(SpeciesTable.PlayerStats(SpeciesTable.Get(8), 240).MaxHp));
        }

        [Test]
        public void Party_UnknownSpeciesOrBrokenData_IsRejected()
        {
            var unknown = StageCodec.EncodeParty(new[] { new OwnedPenguin(1, 99, 100) });

            Assert.That(StageCodec.TryDecodeParty(unknown, out _), Is.False);
            Assert.That(StageCodec.TryDecodeParty(new byte[] { StageCodec.FormatVersion, 3, 1 }, out _), Is.False);
        }

        [Test]
        public void TrialStage_UsesGivenStageAndParties()
        {
            var party = new[] { SpeciesTable.PlayerStats(SpeciesTable.Get(20), 300) };

            var players = TrialStage.Create(StageTable.Stage1, 1, new[] { 0, 1 }, new[] { party, party }, out var world);

            Assert.That(world.StageNo, Is.EqualTo(1));
            Assert.That(players.Length, Is.EqualTo(2));
            Assert.That(players[1].SpeciesNo, Is.EqualTo(20));
            Assert.That(players[1].MaxHp, Is.EqualTo(party[0].MaxHp));
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
