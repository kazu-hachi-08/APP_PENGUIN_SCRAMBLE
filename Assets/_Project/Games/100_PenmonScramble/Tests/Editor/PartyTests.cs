using System.Collections.Generic;
using NUnit.Framework;
using PenmonScramble.Core;
using static PenmonScramble.Tests.TestWorld;

namespace PenmonScramble.Tests
{
    /// <summary>3体の交代（§4.1）と必殺技のゲージ・必殺技（§4.3）</summary>
    public class PartyTests
    {
        private static readonly PlayerInput SwitchPress = new PlayerInput(Vec2.Zero, false, false, special: false, swap: true);
        private static readonly PlayerInput SpecialPress = new PlayerInput(Vec2.Zero, false, false, special: true);

        private static UnitStats Member(int maxHp, float power = 10f)
        {
            var stats = Stats(Melee(power));
            stats.MaxHp = maxHp;
            return stats;
        }

        private static Unit AddParty(StageWorld world, params UnitStats[] party)
        {
            return world.AddUnit(Team.Player, party, Vec2.Zero, seat: 0);
        }

        private static Unit AddSpecialist(StageWorld world, Element element)
        {
            var stats = SpeciesTable.PlayerStats(FirstOf(element), Balance.TrialPower);
            var unit = world.AddUnit(Team.Player, new[] { stats }, Vec2.Zero, seat: 0);
            unit.SpecialGauge = 1f;
            return unit;
        }

        private static SpeciesDef FirstOf(Element element)
        {
            foreach (var species in SpeciesTable.Species)
            {
                if (species.Element == element)
                {
                    return species;
                }
            }

            return null;
        }

        [Test]
        public void Switch_ChangesActiveMember_ThenWaitsCooldown()
        {
            var world = Create();
            var player = AddParty(world, Member(100), Member(120), Member(140));
            AddDummyEnemy(world, new Vec2(8f, 8f));

            Press(world, player, SwitchPress);
            Assert.That(player.ActiveIndex, Is.EqualTo(1));
            Assert.That(player.MaxHp, Is.EqualTo(120));

            // 2秒たつ前の交代は効かない
            Press(world, player, SwitchPress);
            Assert.That(player.ActiveIndex, Is.EqualTo(1));

            TickFor(world, Seconds(Balance.SwitchCooldownSeconds));
            Press(world, player, SwitchPress);
            Assert.That(player.ActiveIndex, Is.EqualTo(2));
        }

        [Test]
        public void Switch_HeldDown_SwitchesOnlyOnce()
        {
            var world = Create();
            var player = AddParty(world, Member(100), Member(100), Member(100));
            AddDummyEnemy(world, new Vec2(8f, 8f));

            world.SetInput(player.Id, SwitchPress);
            TickFor(world, Seconds(Balance.SwitchCooldownSeconds * 2f));

            Assert.That(player.ActiveIndex, Is.EqualTo(1));
        }

        [Test]
        public void Switch_SkipsFallenMember_AndWrapsAround()
        {
            var world = Create();
            var player = AddParty(world, Member(100), Member(100), Member(100));
            AddDummyEnemy(world, new Vec2(8f, 8f));
            player.Members[1].Hp = 0;

            Press(world, player, SwitchPress);
            Assert.That(player.ActiveIndex, Is.EqualTo(2));

            TickFor(world, Seconds(Balance.SwitchCooldownSeconds));
            Press(world, player, SwitchPress);
            Assert.That(player.ActiveIndex, Is.EqualTo(0));
        }

        [Test]
        public void Switch_KeepsEachMembersHp()
        {
            var world = Create();
            var player = AddParty(world, Member(100), Member(100));
            AddDummyEnemy(world, new Vec2(8f, 8f));
            player.Hp = 40;

            Press(world, player, SwitchPress);

            Assert.That(player.Hp, Is.EqualTo(100));
            Assert.That(player.Members[0].Hp, Is.EqualTo(40));
        }

        [Test]
        public void FallenMember_IsReplacedByNext_WithoutDefeatedEvent()
        {
            var world = Create();
            var player = AddParty(world, Member(1), Member(100));
            world.AddUnit(Team.Enemy, Stats(Melee(power: 5f)), new Vec2(1f, 0f));
            var defeated = new List<DefeatedEvent>();
            world.Defeated += defeated.Add;

            TickFor(world, Seconds(2f));

            Assert.That(player.ActiveIndex, Is.EqualTo(1));
            Assert.That(world.FindUnit(player.Id), Is.Not.Null);
            Assert.That(defeated.Exists(d => d.UnitId == player.Id), Is.False);
        }

        [Test]
        public void AllMembersFallen_PlayerIsDown_AndSoloFails()
        {
            var world = Create();
            var player = AddParty(world, Member(1), Member(1), Member(1));
            world.AddUnit(Team.Enemy, Stats(Melee(power: 5f)), new Vec2(1f, 0f));

            TickFor(world, Seconds(8f));

            // ダウンしてもその場に残る（§4.5）
            Assert.That(world.FindUnit(player.Id), Is.Not.Null);
            Assert.That(player.IsDown, Is.True);
            Assert.That(world.IsFailed, Is.True);
        }

        [Test]
        public void BenchedMembersCooldown_KeepsCounting()
        {
            var world = Create();
            var player = AddParty(world, Member(100), Member(100));
            var enemy = AddDummyEnemy(world, new Vec2(1.2f, 0f));
            Press(world, player, new PlayerInput(Vec2.Zero, true, false));

            Press(world, player, SwitchPress);
            TickFor(world, Seconds(Balance.SwitchCooldownSeconds));
            Press(world, player, SwitchPress);

            // 控えにいる間に待ち時間（1秒）が明けているので、戻ってすぐ出せる
            Assert.That(player.GetCooldown(0), Is.EqualTo(0f));
            Assert.That(enemy.Hp, Is.LessThan(enemy.MaxHp));
        }

        [Test]
        public void DamagingEnemies_FillsGauge_ByShareOfEnemyHp()
        {
            var world = Create();
            var player = AddParty(world, Member(100, power: 10f));
            AddDummyEnemy(world, new Vec2(1.2f, 0f), maxHp: 100);

            Press(world, player, new PlayerInput(Vec2.Zero, true, false));

            Assert.That(player.SpecialGauge, Is.EqualTo(0.1f * Balance.SpecialGainPerEnemy).Within(Tolerance));
        }

        [Test]
        public void Gauge_IsKeptWhenSwitching()
        {
            var world = Create();
            var player = AddParty(world, Member(100), Member(100));
            AddDummyEnemy(world, new Vec2(8f, 8f));
            player.SpecialGauge = 0.5f;

            Press(world, player, SwitchPress);

            Assert.That(player.SpecialGauge, Is.EqualTo(0.5f));
        }

        [Test]
        public void Special_NeedsFullGauge()
        {
            var world = Create();
            var player = AddSpecialist(world, Element.Wind);
            player.SpecialGauge = 0.9f;
            AddDummyEnemy(world, new Vec2(2f, 0f));
            var used = 0;
            world.SpecialUsed += _ => used++;

            Press(world, player, SpecialPress);

            Assert.That(used, Is.EqualTo(0));
            Assert.That(player.SpecialGauge, Is.EqualTo(0.9f));
        }

        [Test]
        public void Special_EmptiesGauge_StopsWorldForCutIn_ThenHits()
        {
            var world = Create();
            var player = AddSpecialist(world, Element.Wind);
            var enemy = AddDummyEnemy(world, new Vec2(2f, 0f), maxHp: 1000);
            var used = new List<SpecialUsedEvent>();
            world.SpecialUsed += used.Add;

            Press(world, player, SpecialPress);
            Assert.That(used.Count, Is.EqualTo(1));
            Assert.That(used[0].Element, Is.EqualTo(Element.Wind));
            Assert.That(player.SpecialGauge, Is.EqualTo(0f));
            Assert.That(player.IsInvincible, Is.True);

            // カットインの間は効かない
            TickFor(world, Balance.SpecialCutInTicks);
            Assert.That(enemy.Hp, Is.EqualTo(enemy.MaxHp));

            world.Tick();
            Assert.That(enemy.Hp, Is.LessThan(enemy.MaxHp));
        }

        [Test]
        public void Special_DamageDoesNotRefillGauge()
        {
            var world = Create();
            var player = AddSpecialist(world, Element.Wind);
            AddDummyEnemy(world, new Vec2(2f, 0f), maxHp: 1000);

            Press(world, player, SpecialPress);
            TickFor(world, Balance.SpecialCutInTicks + 1);

            Assert.That(player.SpecialGauge, Is.EqualTo(0f));
        }

        [Test]
        public void Special_MakesUserInvincible()
        {
            var world = Create();
            var player = AddSpecialist(world, Element.Wind);
            // 大竜巻で飛ばされないよう、遠くから弾で撃ってくる敵
            var shot = new SkillDef { Shape = SkillShape.Projectile, Power = 50f, Cooldown = 0.1f, Range = 20f, ProjectileSpeed = 20f, ProjectileRadius = 0.3f };
            world.AddUnit(Team.Enemy, Stats(shot), new Vec2(9f, 0f));

            Press(world, player, SpecialPress);
            TickFor(world, Balance.SpecialCutInTicks + Seconds(1f));

            Assert.That(player.Hp, Is.EqualTo(player.MaxHp));
        }

        [TestCase(Element.Ice)]
        [TestCase(Element.Water)]
        [TestCase(Element.Wind)]
        [TestCase(Element.Thunder)]
        public void EachElementSpecial_DamagesEnemies(Element element)
        {
            var world = Create();
            var player = AddSpecialist(world, element);
            player.Facing = Vec2.Right;
            var near = AddDummyEnemy(world, new Vec2(2f, 1f), maxHp: 1000);
            var far = AddDummyEnemy(world, new Vec2(-4f, -3f), maxHp: 1000);

            Press(world, player, SpecialPress);
            TickFor(world, Balance.SpecialCutInTicks + Seconds(SpecialMoves.IceFreezeSeconds + 1.5f));

            Assert.That(near.Hp, Is.LessThan(near.MaxHp));
            if (element != Element.Wind)
            {
                // 風（自分の周り）以外は画面全体に届く
                Assert.That(far.Hp, Is.LessThan(far.MaxHp));
            }
        }

        [Test]
        public void IceSpecial_FreezesEnemies()
        {
            var world = Create();
            var player = AddSpecialist(world, Element.Ice);
            var enemy = world.AddUnit(Team.Enemy, Stats(Melee(power: 1f)), new Vec2(6f, 0f));

            Press(world, player, SpecialPress);
            TickFor(world, Balance.SpecialCutInTicks + 1);
            var frozenAt = enemy.Position;
            TickFor(world, Seconds(SpecialMoves.IceFreezeSeconds) - 3);

            Assert.That(enemy.IsFrozen, Is.True);
            Assert.That(enemy.Position.X, Is.EqualTo(frozenAt.X).Within(Tolerance), "凍っている間は歩かない");
        }
    }
}
