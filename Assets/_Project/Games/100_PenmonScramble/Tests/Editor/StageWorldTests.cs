using System.Collections.Generic;
using NUnit.Framework;
using PenmonScramble.Core;

namespace PenmonScramble.Tests
{
    public class StageWorldTests
    {
        private const float Tolerance = 0.001f;
        private const float AreaHalf = 10f;

        private static StageWorld CreateWorld()
        {
            return new StageWorld(new Vec2(-AreaHalf, -AreaHalf), new Vec2(AreaHalf, AreaHalf), seed: 1);
        }

        private static SkillDef Melee(float power = 10f, float knockback = 0f) => new SkillDef
        {
            Shape = SkillShape.Melee,
            Power = power,
            Cooldown = 1f,
            Range = 1f,
            HalfAngleDegrees = 60f,
            Knockback = knockback,
        };

        private static SkillDef Shot(bool pierce) => new SkillDef
        {
            Shape = SkillShape.Projectile,
            Power = 10f,
            Cooldown = 1f,
            Range = 5f,
            ProjectileSpeed = 10f,
            ProjectileRadius = 0.2f,
            Pierce = pierce,
        };

        private static UnitStats Stats(params SkillDef[] skills) => new UnitStats
        {
            Radius = 0.5f,
            MoveSpeed = 3f,
            MaxHp = 100,
            Attack = 1f,
            Skills = skills,
        };

        /// <summary>動かない的。待ち時間を長くして、テスト中に殴り返してこないようにする</summary>
        private static Unit AddDummyEnemy(StageWorld world, Vec2 position)
        {
            var idle = Melee();
            idle.Range = 0f;
            var stats = Stats(idle);
            stats.MoveSpeed = 0f;
            return world.AddUnit(Team.Enemy, stats, position);
        }

        private static void TickFor(StageWorld world, int ticks)
        {
            for (var i = 0; i < ticks; i++)
            {
                world.Tick();
            }
        }

        [Test]
        public void OverlappingUnits_ArePushedApart()
        {
            var world = CreateWorld();
            var a = AddDummyEnemy(world, new Vec2(0f, 0f));
            var b = AddDummyEnemy(world, new Vec2(0.4f, 0f));

            world.Tick();

            Assert.That(Vec2.Distance(a.Position, b.Position), Is.EqualTo(a.Radius + b.Radius).Within(Tolerance));
        }

        [Test]
        public void UnitsAtSamePosition_AreStillPushedApart()
        {
            var world = CreateWorld();
            var a = AddDummyEnemy(world, Vec2.Zero);
            var b = AddDummyEnemy(world, Vec2.Zero);

            world.Tick();

            Assert.That(Vec2.Distance(a.Position, b.Position), Is.EqualTo(a.Radius + b.Radius).Within(Tolerance));
        }

        [Test]
        public void Player_CannotWalkThroughWall()
        {
            var world = CreateWorld();
            var player = world.AddUnit(Team.Player, Stats(Melee()), new Vec2(AreaHalf - 1f, 0f));
            world.SetInput(player.Id, new PlayerInput(Vec2.Right, false, false));

            TickFor(world, 60);

            Assert.That(player.Position.X, Is.EqualTo(AreaHalf - player.Radius).Within(Tolerance));
        }

        [Test]
        public void Moving_ChangesFacing()
        {
            var world = CreateWorld();
            var player = world.AddUnit(Team.Player, Stats(Melee()), Vec2.Zero);
            world.SetInput(player.Id, new PlayerInput(new Vec2(0f, -1f), false, false));

            world.Tick();

            Assert.That(player.Facing.Y, Is.EqualTo(-1f).Within(Tolerance));
        }

        [Test]
        public void Melee_HitsEnemyInFront()
        {
            var world = CreateWorld();
            var player = world.AddUnit(Team.Player, Stats(Melee(power: 10f)), Vec2.Zero);
            var enemy = AddDummyEnemy(world, new Vec2(1.2f, 0f));
            var hits = new List<HitEvent>();
            world.Hit += hits.Add;

            world.SetInput(player.Id, new PlayerInput(Vec2.Zero, true, false));
            world.Tick();

            Assert.That(enemy.Hp, Is.EqualTo(enemy.MaxHp - 10));
            Assert.That(hits.Count, Is.EqualTo(1));
            Assert.That(hits[0].TargetId, Is.EqualTo(enemy.Id));
        }

        [Test]
        public void Melee_MissesEnemyBehind_WhileMoving()
        {
            var world = CreateWorld();
            var player = world.AddUnit(Team.Player, Stats(Melee()), Vec2.Zero);
            var enemy = AddDummyEnemy(world, new Vec2(-1.2f, 0f));

            // 動きながら出すと、移動の向き（右）へ出す。後ろの敵には当たらない
            world.SetInput(player.Id, new PlayerInput(Vec2.Right, true, false));
            world.Tick();

            Assert.That(enemy.Hp, Is.EqualTo(enemy.MaxHp));
        }

        [Test]
        public void Melee_WhileIdle_TurnsToNearestEnemy()
        {
            var world = CreateWorld();
            var player = world.AddUnit(Team.Player, Stats(Melee()), Vec2.Zero);
            var enemy = AddDummyEnemy(world, new Vec2(-1.2f, 0f));

            world.SetInput(player.Id, new PlayerInput(Vec2.Zero, true, false));
            world.Tick();

            Assert.That(enemy.Hp, Is.LessThan(enemy.MaxHp));
            Assert.That(player.Facing.X, Is.EqualTo(-1f).Within(Tolerance));
        }

        [Test]
        public void Melee_OutOfRange_DoesNotHit()
        {
            var world = CreateWorld();
            var player = world.AddUnit(Team.Player, Stats(Melee()), Vec2.Zero);
            var enemy = AddDummyEnemy(world, new Vec2(3f, 0f));

            world.SetInput(player.Id, new PlayerInput(Vec2.Zero, true, false));
            world.Tick();

            Assert.That(enemy.Hp, Is.EqualTo(enemy.MaxHp));
        }

        [Test]
        public void HoldingSkill_WaitsForCooldown()
        {
            var world = CreateWorld();
            var player = world.AddUnit(Team.Player, Stats(Melee(power: 10f)), Vec2.Zero);
            var enemy = AddDummyEnemy(world, new Vec2(1.2f, 0f));
            world.SetInput(player.Id, new PlayerInput(Vec2.Zero, true, false));

            // 待ち時間1秒 = 30回の更新。その間は2回目が出ない
            TickFor(world, 29);

            Assert.That(enemy.Hp, Is.EqualTo(enemy.MaxHp - 10));
        }

        [Test]
        public void Projectile_HitsDistantEnemy_AndDisappears()
        {
            var world = CreateWorld();
            var player = world.AddUnit(Team.Player, Stats(Melee(), Shot(pierce: false)), Vec2.Zero);
            var enemy = AddDummyEnemy(world, new Vec2(4f, 0f));

            world.SetInput(player.Id, new PlayerInput(Vec2.Zero, false, true));
            world.Tick();
            world.SetInput(player.Id, new PlayerInput(Vec2.Zero, false, false));
            TickFor(world, 15);

            Assert.That(enemy.Hp, Is.EqualTo(enemy.MaxHp - 10));
            Assert.That(world.Projectiles.Count, Is.EqualTo(0));
        }

        [Test]
        public void PiercingProjectile_HitsEachEnemyOnce()
        {
            var world = CreateWorld();
            var player = world.AddUnit(Team.Player, Stats(Melee(), Shot(pierce: true)), Vec2.Zero);
            var first = AddDummyEnemy(world, new Vec2(2f, 0f));
            var second = AddDummyEnemy(world, new Vec2(3.5f, 0f));
            world.SetInput(player.Id, new PlayerInput(Vec2.Right, false, true));
            world.Tick();
            world.SetInput(player.Id, new PlayerInput(Vec2.Zero, false, false));

            TickFor(world, 30);

            Assert.That(first.Hp, Is.EqualTo(first.MaxHp - 10));
            Assert.That(second.Hp, Is.EqualTo(second.MaxHp - 10));
        }

        [Test]
        public void Projectile_DisappearsAfterRange()
        {
            var world = CreateWorld();
            var player = world.AddUnit(Team.Player, Stats(Melee(), Shot(pierce: true)), Vec2.Zero);
            world.SetInput(player.Id, new PlayerInput(Vec2.Right, false, true));
            world.Tick();
            world.SetInput(player.Id, new PlayerInput(Vec2.Zero, false, false));

            // 5マスを秒速10で = 0.5秒（15回）で消える
            TickFor(world, 16);

            Assert.That(world.Projectiles.Count, Is.EqualTo(0));
        }

        [Test]
        public void Knockback_PushesAwayAndSlowsDown()
        {
            var world = CreateWorld();
            var player = world.AddUnit(Team.Player, Stats(Melee(knockback: 6f)), Vec2.Zero);
            var enemy = AddDummyEnemy(world, new Vec2(1.2f, 0f));
            world.SetInput(player.Id, new PlayerInput(Vec2.Zero, true, false));
            world.Tick();
            world.SetInput(player.Id, new PlayerInput(Vec2.Zero, false, false));

            Assert.That(enemy.IsStunned, Is.True);

            TickFor(world, 30);

            Assert.That(enemy.Position.X, Is.GreaterThan(1.5f));
            Assert.That(enemy.KnockbackVelocity.IsNearlyZero, Is.True);
        }

        [Test]
        public void Knockback_BouncesOffWall()
        {
            var world = CreateWorld();
            var player = world.AddUnit(Team.Player, Stats(Melee(knockback: 20f)), new Vec2(AreaHalf - 2f, 0f));
            var enemy = AddDummyEnemy(world, new Vec2(AreaHalf - 0.8f, 0f));
            world.SetInput(player.Id, new PlayerInput(Vec2.Zero, true, false));
            world.Tick();
            world.SetInput(player.Id, new PlayerInput(Vec2.Zero, false, false));

            world.Tick();

            Assert.That(enemy.Position.X, Is.LessThanOrEqualTo(AreaHalf - enemy.Radius + Tolerance));
            Assert.That(enemy.KnockbackVelocity.X, Is.LessThan(0f));
        }

        [Test]
        public void Defeated_IsRaisedOnce_AndUnitRemoved()
        {
            var world = CreateWorld();
            var player = world.AddUnit(Team.Player, Stats(Melee(power: 1000f)), Vec2.Zero);
            var enemy = AddDummyEnemy(world, new Vec2(1.2f, 0f));
            var defeated = new List<DefeatedEvent>();
            world.Defeated += defeated.Add;

            world.SetInput(player.Id, new PlayerInput(Vec2.Zero, true, false));
            TickFor(world, 3);

            Assert.That(defeated.Count, Is.EqualTo(1));
            Assert.That(defeated[0].UnitId, Is.EqualTo(enemy.Id));
            Assert.That(world.FindUnit(enemy.Id), Is.Null);
            Assert.That(world.IsCleared, Is.True);
        }

        [Test]
        public void Enemy_ApproachesAndAttacksPlayer()
        {
            var world = CreateWorld();
            var player = world.AddUnit(Team.Player, Stats(Melee()), Vec2.Zero);
            world.AddUnit(Team.Enemy, Stats(Melee(power: 5f)), new Vec2(5f, 0f));

            TickFor(world, 120);

            Assert.That(player.Hp, Is.LessThan(player.MaxHp));
        }

        [Test]
        public void AllPlayersDefeated_IsFailed()
        {
            var world = CreateWorld();
            var stats = Stats(Melee());
            stats.MaxHp = 1;
            world.AddUnit(Team.Player, stats, Vec2.Zero);
            world.AddUnit(Team.Enemy, Stats(Melee()), new Vec2(1f, 0f));

            TickFor(world, 60);

            Assert.That(world.IsFailed, Is.True);
        }

        [Test]
        public void TrialStage_StartsWithPlayerAndEnemies()
        {
            var players = TrialStage.Create(seed: 42, new[] { 0 }, round: 1, out var world);
            world.Tick();

            Assert.That(world.Units.Count, Is.EqualTo(1 + TrialStage.EnemyCount));
            Assert.That(players[0].Team, Is.EqualTo(Team.Player));
            Assert.That(players[0].Seat, Is.EqualTo(0));
            Assert.That(players[0].Hp, Is.EqualTo(players[0].MaxHp));
        }

        [Test]
        public void TrialStage_PlacesOnePlayerPerSeatWithoutOverlap()
        {
            var players = TrialStage.Create(seed: 42, new[] { 0, 2, 3 }, round: 1, out var world);
            var startPositions = new[] { players[0].Position, players[1].Position, players[2].Position };
            world.Tick();

            Assert.That(players[1].Seat, Is.EqualTo(2));
            Assert.That(players[2].Seat, Is.EqualTo(3));
            for (var i = 0; i < players.Length; i++)
            {
                // 重なっていれば押し合いで動く
                Assert.That(Vec2.Distance(players[i].Position, startPositions[i]), Is.LessThan(Tolerance));
            }
        }

        [Test]
        public void TrialStage_EnemyHpScalesWithPlayers()
        {
            TrialStage.Create(seed: 1, new[] { 0, 1, 2, 3 }, round: 1, out var world);

            var enemy = FirstEnemy(world);
            var species = SpeciesTable.Get(enemy.SpeciesNo);
            Assert.That(enemy.MaxHp, Is.EqualTo((int)System.MathF.Round(species.HpRate * Balance.EnemyPower * 2.8f)));
        }

        [Test]
        public void RemoveUnit_RemovesWithoutDefeatedEvent()
        {
            var world = CreateWorld();
            var player = world.AddUnit(Team.Player, Stats(Melee()), Vec2.Zero, seat: 1);
            AddDummyEnemy(world, new Vec2(5f, 0f));
            var defeated = 0;
            world.Defeated += _ => defeated++;

            world.RemoveUnit(player.Id);
            world.Tick();

            Assert.That(world.FindUnit(player.Id), Is.Null);
            Assert.That(defeated, Is.EqualTo(0));
            Assert.That(world.IsFailed, Is.True);
        }

        private static Unit FirstEnemy(StageWorld world)
        {
            foreach (var unit in world.Units)
            {
                if (unit.Team == Team.Enemy)
                {
                    return unit;
                }
            }

            return null;
        }
    }
}
