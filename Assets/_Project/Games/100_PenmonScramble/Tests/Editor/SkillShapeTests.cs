using System.Collections.Generic;
using NUnit.Framework;
using PenmonScramble.Core;
using static PenmonScramble.Tests.TestWorld;

namespace PenmonScramble.Tests
{
    /// <summary>技の形（突進・拡散・周囲・設置・落下）ごとの当たり方（§4.2）</summary>
    public class SkillShapeTests
    {
        private static readonly PlayerInput UseSkill1 = new PlayerInput(Vec2.Zero, true, false);

        private static Unit AddPlayer(StageWorld world, SkillDef skill, Vec2 position)
        {
            return world.AddUnit(Team.Player, Stats(skill), position);
        }

        [Test]
        public void Dash_SlidesForward_AndHitsEachEnemyOnce()
        {
            var world = Create();
            var dash = new SkillDef { Shape = SkillShape.Dash, Power = 10f, Cooldown = 5f, Range = 4f, DashSpeed = 12f, Knockback = 8f };
            var player = AddPlayer(world, dash, Vec2.Zero);
            player.Facing = Vec2.Right;
            var enemy = AddDummyEnemy(world, new Vec2(2f, 0f));

            world.SetInput(player.Id, new PlayerInput(Vec2.Right, true, false));
            world.Tick();
            world.SetInput(player.Id, default);
            TickFor(world, Seconds(1f));

            Assert.That(player.Position.X, Is.GreaterThan(3f), "すべった距離");
            Assert.That(player.IsDashing, Is.False, "すべり終わったら止まる");
            Assert.That(enemy.Hp, Is.EqualTo(enemy.MaxHp - 10), "1回の突進で1回だけ当たる");
            Assert.That(enemy.Position.X, Is.GreaterThan(2f), "進む向きへ飛ばす");
        }

        [Test]
        public void Dash_IgnoresMoveInputWhileSliding()
        {
            var world = Create();
            var dash = new SkillDef { Shape = SkillShape.Dash, Power = 1f, Cooldown = 5f, Range = 3f, DashSpeed = 9f };
            var player = AddPlayer(world, dash, Vec2.Zero);
            // 止まったまま出すと一番近い敵へ向くので、真横に置いて向きを右にする
            AddDummyEnemy(world, new Vec2(9f, 0f));

            Press(world, player, UseSkill1);
            world.SetInput(player.Id, new PlayerInput(new Vec2(0f, 1f), false, false));
            TickFor(world, 5);

            Assert.That(player.Position.Y, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Spread_FiresCountProjectilesInFan()
        {
            var world = Create();
            var spread = new SkillDef
            {
                Shape = SkillShape.Spread, Power = 5f, Cooldown = 5f, Range = 5f,
                ProjectileSpeed = 10f, ProjectileRadius = 0.2f, Count = 3, SpreadDegrees = 60f,
            };
            var player = AddPlayer(world, spread, Vec2.Zero);
            player.Facing = Vec2.Right;
            AddDummyEnemy(world, new Vec2(8f, 8f));

            world.SetInput(player.Id, new PlayerInput(Vec2.Right, true, false));
            world.Tick();

            Assert.That(world.Projectiles.Count, Is.EqualTo(3));
            var ys = new List<float>();
            foreach (var projectile in world.Projectiles)
            {
                ys.Add(projectile.Velocity.Y);
            }

            ys.Sort();
            Assert.That(ys[0], Is.LessThan(0f));
            Assert.That(ys[1], Is.EqualTo(0f).Within(Tolerance));
            Assert.That(ys[2], Is.GreaterThan(0f));
        }

        [Test]
        public void Nova_HitsAllAroundIncludingBehind()
        {
            var world = Create();
            var nova = new SkillDef { Shape = SkillShape.Nova, Power = 10f, Cooldown = 5f, Range = 1.5f, Knockback = 5f };
            var player = AddPlayer(world, nova, Vec2.Zero);
            player.Facing = Vec2.Right;
            var front = AddDummyEnemy(world, new Vec2(1.5f, 0f));
            var behind = AddDummyEnemy(world, new Vec2(-1.5f, 0f));
            var far = AddDummyEnemy(world, new Vec2(5f, 0f));

            world.SetInput(player.Id, new PlayerInput(Vec2.Right, true, false));
            world.Tick();

            Assert.That(front.Hp, Is.LessThan(front.MaxHp));
            Assert.That(behind.Hp, Is.LessThan(behind.MaxHp));
            Assert.That(far.Hp, Is.EqualTo(far.MaxHp));
            Assert.That(behind.KnockbackVelocity.X, Is.LessThan(0f), "外向きに飛ばす");
        }

        [Test]
        public void Zone_DamagesRepeatedly_ThenDisappears()
        {
            var world = Create();
            var zone = new SkillDef
            {
                Shape = SkillShape.Zone, Power = 3f, Cooldown = 10f, Range = 2f,
                AreaRadius = 1f, Duration = 0.9f, Interval = 0.25f,
            };
            var player = AddPlayer(world, zone, Vec2.Zero);
            player.Facing = Vec2.Right;
            var enemy = AddDummyEnemy(world, new Vec2(2f, 0f));
            var hits = 0;
            world.Hit += _ => hits++;

            world.SetInput(player.Id, new PlayerInput(Vec2.Right, true, false));
            world.Tick();
            world.SetInput(player.Id, default);
            Assert.That(world.AreaEffects.Count, Is.EqualTo(1));

            TickFor(world, Seconds(1.2f));

            // 置いた瞬間 + 0.25秒ごと = 0.9秒で4回
            Assert.That(hits, Is.EqualTo(4));
            Assert.That(enemy.Hp, Is.EqualTo(enemy.MaxHp - 12));
            Assert.That(world.AreaEffects.Count, Is.EqualTo(0));
        }

        [Test]
        public void Zone_WithPull_DrawsEnemyTowardCenter()
        {
            var world = Create();
            var whirl = new SkillDef
            {
                Shape = SkillShape.Zone, Power = 1f, Cooldown = 10f, Range = 3f,
                AreaRadius = 2f, Duration = 2f, Interval = 0.3f, Pull = 3f,
            };
            var player = AddPlayer(world, whirl, Vec2.Zero);
            player.Facing = Vec2.Right;
            var enemy = AddDummyEnemy(world, new Vec2(4.5f, 0f));

            Press(world, player, new PlayerInput(Vec2.Right, true, false));
            TickFor(world, Seconds(1f));

            Assert.That(enemy.Position.X, Is.LessThan(4.5f));
        }

        [Test]
        public void Drop_LandsAfterDelay_AtNearestEnemy()
        {
            var world = Create();
            var drop = new SkillDef
            {
                Shape = SkillShape.Drop, Power = 20f, Cooldown = 10f, Range = 8f,
                AreaRadius = 1f, Delay = 0.5f, Count = 1,
            };
            var player = AddPlayer(world, drop, Vec2.Zero);
            var near = AddDummyEnemy(world, new Vec2(3f, 0f));
            var farther = AddDummyEnemy(world, new Vec2(-6f, 0f));
            var bursts = new List<BurstEvent>();
            world.Burst += bursts.Add;

            Press(world, player, UseSkill1);
            TickFor(world, Seconds(0.4f) - 1);
            Assert.That(near.Hp, Is.EqualTo(near.MaxHp), "落ちるまでは当たらない");
            Assert.That(world.AreaEffects[0].Kind, Is.EqualTo(AreaEffectKind.Drop));

            TickFor(world, Seconds(0.2f));

            Assert.That(near.Hp, Is.EqualTo(near.MaxHp - 20));
            Assert.That(farther.Hp, Is.EqualTo(farther.MaxHp));
            Assert.That(world.AreaEffects.Count, Is.EqualTo(0));
            Assert.That(bursts.Exists(b => b.Shape == BurstShape.Blast), Is.True);
        }

        [Test]
        public void Drop_CanBeDodged()
        {
            var world = Create();
            var drop = new SkillDef { Shape = SkillShape.Drop, Power = 20f, Cooldown = 10f, Range = 8f, AreaRadius = 0.5f, Delay = 0.5f };
            var player = AddPlayer(world, drop, Vec2.Zero);
            var enemy = AddDummyEnemy(world, new Vec2(3f, 0f));

            Press(world, player, UseSkill1);
            enemy.Position = new Vec2(3f, 3f);
            TickFor(world, Seconds(1f));

            Assert.That(enemy.Hp, Is.EqualTo(enemy.MaxHp));
        }

        [Test]
        public void Drop_WithManyTargets_HitsUpToCount()
        {
            var world = Create();
            var drop = new SkillDef { Shape = SkillShape.Drop, Power = 5f, Cooldown = 10f, Range = 20f, AreaRadius = 0.5f, Delay = 0.1f, Count = 2 };
            var player = AddPlayer(world, drop, Vec2.Zero);
            AddDummyEnemy(world, new Vec2(3f, 3f));
            AddDummyEnemy(world, new Vec2(-3f, 3f));
            AddDummyEnemy(world, new Vec2(6f, -6f));

            Press(world, player, UseSkill1);

            Assert.That(world.AreaEffects.Count, Is.EqualTo(2));
        }
    }
}
