using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PenmonScramble.Core;
using PenmonScramble.Soccer.Core;

namespace PenmonScramble.Tests
{
    /// <summary>サッカーの3対3（Docs/MINIGAME_SOCCER_PLAN.md フェーズ 3）: パス・スライディング・ダッシュ・動かす子の切り替え・AI</summary>
    public class SoccerTeamPlayTests
    {
        private const float Dt = SoccerRules.StepSeconds;

        private static int Steps(float seconds) => (int)MathF.Ceiling(seconds / Dt);

        private static List<SoccerEvent> Run(SoccerWorld world, int steps, params SoccerInput[] inputs)
        {
            var events = new List<SoccerEvent>();
            for (var i = 0; i < steps; i++)
            {
                world.Step(inputs, events);
            }

            return events;
        }

        /// <summary>赤 red 体・青 blue 体（キーパーなし）。子を置いてから、0 番の子の足元にボールを置いて持たせる</summary>
        private static SoccerWorld World(int red, int blue, params Vec2[] positions)
        {
            var world = new SoccerWorld(SoccerPitchTable.Standard(), red, blue);
            for (var i = 0; i < positions.Length; i++)
            {
                world.Players[i].Position = positions[i];
                world.Players[i].Velocity = Vec2.Zero;
            }

            return world;
        }

        private static void Give(SoccerWorld world, int index)
        {
            var player = world.Players[index];
            world.Ball.Place(player.Position + player.Facing * SoccerBalance.DribbleDistance);
            Run(world, 1);
            Assert.That(world.Ball.Holder, Is.EqualTo(index));
        }

        private static SoccerInput Press(Vec2 move, bool shoot = false, bool pass = false) => new SoccerInput(move, shoot, pass);

        // ---- パス ----

        [Test]
        public void Pass_RollsToTheTeammateInFront()
        {
            var world = World(2, 0, new Vec2(-4f, 0f), new Vec2(3f, 1f));
            Give(world, 0);

            var events = Run(world, 1, Press(Vec2.Zero, pass: true));
            var pass = events.Single(e => e.Kind == SoccerEventKind.Pass);
            Assert.That(pass.Other, Is.EqualTo(1));

            events = Run(world, Steps(2f));
            Assert.That(events.Any(e => e.Kind == SoccerEventKind.Possess && e.Player == 1), Is.True);
            Assert.That(world.Ball.Holder, Is.EqualTo(1));
        }

        /// <summary>扇の外（後ろ）の味方には出さず、扇の中の一番近い味方へ出す</summary>
        [Test]
        public void Pass_ChoosesNearestTeammateInsideTheCone()
        {
            var world = World(3, 0, new Vec2(0f, 0f), new Vec2(-2f, 0f), new Vec2(5f, 2f));
            Give(world, 0);

            var events = Run(world, 1, Press(new Vec2(1f, 0f), pass: true));
            Assert.That(events.Single(e => e.Kind == SoccerEventKind.Pass).Other, Is.EqualTo(2));
        }

        [Test]
        public void Pass_WithoutTeammateInFront_KicksGentlyForward()
        {
            var world = World(2, 0, new Vec2(0f, 0f), new Vec2(-5f, 0f));
            Give(world, 0);

            var events = Run(world, 1, Press(new Vec2(1f, 0f), pass: true));
            var pass = events.Single(e => e.Kind == SoccerEventKind.Pass);
            Assert.That(pass.Other, Is.EqualTo(SoccerBall.NoPlayer));
            Assert.That(world.Ball.Velocity.X, Is.GreaterThan(0f));
            Assert.That(pass.Strength, Is.EqualTo(SoccerBalance.PassFallbackSpeed).Within(1e-3f));
        }

        [Test]
        public void PassSpeed_LeavesArriveSpeedAtTheTarget()
        {
            const float distance = 8f;
            var speed = SoccerWorld.PassSpeed(distance);
            var arrive = MathF.Sqrt(speed * speed - 2f * SoccerBalance.RollDecel * distance);
            Assert.That(arrive, Is.EqualTo(SoccerBalance.PassArriveSpeed).Within(0.05f));
        }

        // ---- スライディング ----

        [Test]
        public void Slide_StealsTheBallAndTheHolderFallsDown()
        {
            var world = World(1, 1, new Vec2(0f, 0f), new Vec2(2.5f, 0f));
            Give(world, 0);

            var events = Run(world, Steps(SoccerBalance.SlideSeconds), SoccerInput.None, Press(new Vec2(-1f, 0f), shoot: true));
            var tackle = events.Single(e => e.Kind == SoccerEventKind.Tackle);
            Assert.That(tackle.Player, Is.EqualTo(1));
            Assert.That(tackle.Other, Is.EqualTo(0));
            Assert.That(world.Ball.Holder, Is.EqualTo(1));
            Assert.That(world.Players[0].State, Is.EqualTo(SoccerPlayerState.Down));
            Assert.That(world.Players[1].State, Is.EqualTo(SoccerPlayerState.Normal), "奪ったら起き上がりの待ちは無い");

            Run(world, Steps(SoccerBalance.TackledDownSeconds));
            Assert.That(world.Players[0].State, Is.EqualTo(SoccerPlayerState.Normal));
        }

        /// <summary>外すと少しの間起き上がれず、スティックを倒しても動かない</summary>
        [Test]
        public void Slide_Miss_CannotMoveForAWhile()
        {
            var world = World(0, 1, new Vec2(-5f, 3f));
            var events = Run(world, 1, Press(new Vec2(1f, 0f), shoot: true));
            Assert.That(events.Any(e => e.Kind == SoccerEventKind.Slide), Is.True);

            events = Run(world, Steps(SoccerBalance.SlideSeconds), Press(new Vec2(0f, 1f)));
            Assert.That(events.Any(e => e.Kind == SoccerEventKind.SlideMiss), Is.True);
            Assert.That(world.Players[0].State, Is.EqualTo(SoccerPlayerState.Recover));
            Assert.That(world.Players[0].Position.Y, Is.EqualTo(3f).Within(1e-3f), "すべる間は向きを変えられない");

            var stopped = world.Players[0].Position;
            Run(world, Steps(SoccerBalance.SlideMissSeconds * 0.5f), Press(new Vec2(0f, 1f)));
            Assert.That(world.Players[0].Position.Y, Is.EqualTo(stopped.Y).Within(1e-3f));

            Run(world, Steps(SoccerBalance.SlideMissSeconds), Press(new Vec2(0f, 1f)));
            Assert.That(world.Players[0].State, Is.EqualTo(SoccerPlayerState.Normal));
            Assert.That(world.Players[0].Position.Y, Is.GreaterThan(stopped.Y));
        }

        [Test]
        public void Slide_HasCooldown()
        {
            var world = World(0, 1, new Vec2(-5f, 3f));
            var events = Run(world, 1, Press(new Vec2(1f, 0f), shoot: true));
            Run(world, Steps(SoccerBalance.SlideSeconds + SoccerBalance.SlideMissSeconds));
            events.AddRange(Run(world, 1, Press(new Vec2(1f, 0f), shoot: true)));
            Assert.That(events.Count(e => e.Kind == SoccerEventKind.Slide), Is.EqualTo(1), "待ちが明けるまでは出せない");

            Run(world, Steps(SoccerBalance.SlideCooldownSeconds));
            events = Run(world, 1, Press(new Vec2(1f, 0f), shoot: true));
            Assert.That(events.Count(e => e.Kind == SoccerEventKind.Slide), Is.EqualTo(1));
        }

        [Test]
        public void DownPlayer_CannotPickUpTheBall()
        {
            var world = World(1, 1, new Vec2(0f, 0f), new Vec2(2.5f, 0f));
            Give(world, 0);
            Run(world, Steps(SoccerBalance.SlideSeconds), SoccerInput.None, Press(new Vec2(-1f, 0f), shoot: true));
            Assert.That(world.Players[0].IsDown, Is.True);

            // 奪った子が離れたボールを、転んだ子の足元に置いても拾えない
            Run(world, 1, SoccerInput.None, Press(new Vec2(1f, 0f), pass: true));
            world.Ball.Place(world.Players[0].Position + new Vec2(0.3f, 0f));
            world.Players[1].Position = new Vec2(10f, 5f);
            Run(world, 2);
            Assert.That(world.Ball.Holder, Is.Not.EqualTo(0));
        }

        /// <summary>
        /// 持つ前から押しっぱなしのシュートのボタンでは、ためも蹴りも起きない
        /// （スライディングで拾ったあと、押したままのボタンを離した瞬間に勝手に蹴らない）
        /// </summary>
        [Test]
        public void Shoot_HeldBeforePossession_DoesNotCharge()
        {
            var world = World(1, 0, new Vec2(-5f, 3f));
            world.Ball.Place(new Vec2(-3.5f, 3f));
            var events = Run(world, Steps(SoccerBalance.SlideSeconds), Press(new Vec2(1f, 0f), shoot: true));
            Assert.That(events.Any(e => e.Kind == SoccerEventKind.Slide), Is.True);
            Assert.That(world.Ball.Holder, Is.EqualTo(0));
            Assert.That(world.Players[0].State, Is.EqualTo(SoccerPlayerState.Normal), "離れたボールを拾ったら、すべりは成功で終わる");
            Assert.That(world.Players[0].IsCharging, Is.False);

            events = Run(world, 1, SoccerInput.None);
            Assert.That(events.Any(e => e.Kind == SoccerEventKind.Kick), Is.False);
        }

        // ---- ダッシュ・体 ----

        [Test]
        public void Dash_IsFasterForAWhileThenWaits()
        {
            var world = World(1, 0, new Vec2(-13f, 3f));
            Run(world, Steps(0.5f), Press(new Vec2(1f, 0f)));
            var events = Run(world, 1, Press(new Vec2(1f, 0f), pass: true));
            Assert.That(events.Any(e => e.Kind == SoccerEventKind.Dash), Is.True);

            Run(world, Steps(0.3f), Press(new Vec2(1f, 0f)));
            Assert.That(world.Players[0].Velocity.X, Is.GreaterThan(SoccerBalance.RunSpeed * 1.2f));

            Run(world, Steps(SoccerBalance.DashSeconds), Press(new Vec2(1f, 0f)));
            Assert.That(world.Players[0].Velocity.X, Is.EqualTo(SoccerBalance.RunSpeed).Within(0.01f));
            events = Run(world, 1, Press(new Vec2(1f, 0f), pass: true));
            Assert.That(events.Any(e => e.Kind == SoccerEventKind.Dash), Is.False, "待ちの間は出せない");
        }

        [Test]
        public void Players_PushEachOtherApart()
        {
            var world = World(1, 1, new Vec2(0f, 0f), new Vec2(0.1f, 0f));
            Run(world, 1);
            var gap = Vec2.Distance(world.Players[0].Position, world.Players[1].Position);
            Assert.That(gap, Is.GreaterThanOrEqualTo(SoccerBalance.PlayerRadius * 2f - 1e-3f));
        }

        /// <summary>持ったまま角へ押し込んでも、ボールは壁の外へ出ない（足元から置き直すので、壁の線の向こうへ置かない）</summary>
        [Test]
        public void Ball_StaysInsideWhenCarriedIntoACorner()
        {
            var pitch = SoccerPitchTable.Standard();
            var world = World(1, 0, new Vec2(-pitch.HalfLength + 2f, pitch.HalfWidth - 2f));
            Give(world, 0);
            Run(world, Steps(2f), Press(new Vec2(-1f, 1f)));
            Assert.That(world.Ball.Holder, Is.EqualTo(0));
            Assert.That(pitch.IsInside(world.Ball.Position), Is.True);

            var events = Run(world, 1, Press(new Vec2(-1f, 1f), pass: true));
            Run(world, Steps(1f));
            Assert.That(events.Any(e => e.Kind == SoccerEventKind.Pass), Is.True);
            Assert.That(pitch.IsInside(world.Ball.Position), Is.True);
        }

        // ---- 動かす子の切り替え ----

        [Test]
        public void Control_MovesToTheReceiverWhenPassing()
        {
            var world = World(2, 0, new Vec2(-4f, 0f), new Vec2(3f, 0f));
            var control = new SoccerControl(SoccerRules.Red, 0);
            Give(world, 0);

            var events = new List<SoccerEvent>();
            world.Step(new[] { control.Update(world, Press(Vec2.Zero, pass: true), false, events), SoccerInput.None }, events);
            control.Update(world, Press(Vec2.Zero, pass: true), false, events);
            Assert.That(control.Controlled, Is.EqualTo(1));
        }

        /// <summary>替わった瞬間に押していたボタンは、離すまで新しい子へ届けない</summary>
        [Test]
        public void Control_MasksButtonHeldThroughTheSwitch()
        {
            var world = World(2, 0, new Vec2(-4f, 0f), new Vec2(3f, 0f));
            var control = new SoccerControl(SoccerRules.Red, 0);
            Give(world, 0);

            var events = new List<SoccerEvent>();
            world.Step(new[] { control.Update(world, Press(Vec2.Zero, pass: true), false, events), SoccerInput.None }, events);
            var held = control.Update(world, Press(Vec2.Zero, pass: true), false, events);
            Assert.That(held.Pass, Is.False);

            control.Update(world, SoccerInput.None, false, null);
            Assert.That(control.Update(world, Press(Vec2.Zero, pass: true), false, null).Pass, Is.True);
        }

        [Test]
        public void Control_SwitchButtonPicksTheOtherMateNearestTheBall()
        {
            var world = World(3, 0, new Vec2(-1f, 0f), new Vec2(-8f, 5f), new Vec2(-3f, -1f));
            world.Ball.Place(new Vec2(4f, 0f));
            var control = new SoccerControl(SoccerRules.Red, 0);

            Assert.That(control.NextSwitchTarget(world), Is.EqualTo(2));
            control.Update(world, SoccerInput.None, true, null);
            Assert.That(control.Controlled, Is.EqualTo(2));
        }

        /// <summary>ボールに早く着ける味方へ自動で替わるが、替わってすぐ・少しの差では替わらない（ガタつかない）</summary>
        [Test]
        public void Control_AutoSwitchWaitsAndNeedsAClearMargin()
        {
            var world = World(2, 0, new Vec2(-8f, 0f), new Vec2(2f, 0f));
            world.Ball.Place(new Vec2(4f, 0f));
            var control = new SoccerControl(SoccerRules.Red, 0);

            control.Update(world, SoccerInput.None, false, null);
            Assert.That(control.Controlled, Is.EqualTo(0), "替わってから SwitchHoldSeconds は替わらない（作った直後も同じ）");

            for (var i = 0; i < Steps(SoccerBalance.SwitchHoldSeconds); i++)
            {
                control.Update(world, SoccerInput.None, false, null);
            }

            Assert.That(control.Controlled, Is.EqualTo(1));

            // ほとんど同じ近さなら、戻らない
            world.Players[0].Position = new Vec2(5.5f, 0.5f);
            for (var i = 0; i < Steps(SoccerBalance.SwitchHoldSeconds * 2f); i++)
            {
                control.Update(world, SoccerInput.None, false, null);
            }

            Assert.That(control.Controlled, Is.EqualTo(1));
        }

        [Test]
        public void Control_NeverTakesAPlayerMovedByAnotherPerson()
        {
            var world = World(2, 0, new Vec2(-8f, 0f), new Vec2(3f, 0f));
            world.Ball.Place(new Vec2(4f, 0f));
            var control = new SoccerControl(SoccerRules.Red, 0, index => index == 1);

            for (var i = 0; i < Steps(2f); i++)
            {
                control.Update(world, SoccerInput.None, i % 10 == 0, null);
            }

            Assert.That(control.Controlled, Is.EqualTo(0));
            Assert.That(control.NextSwitchTarget(world), Is.EqualTo(SoccerBall.NoPlayer));
        }

        // ---- AI ----

        private static (int red, int blue, bool escaped) PlayAiMatch(int redStars, int blueStars, int seed, float seconds)
        {
            var world = new SoccerWorld(SoccerPitchTable.Standard(), SoccerRules.PlayersPerTeam, SoccerRules.PlayersPerTeam, keepers: true, seed: seed);
            var ai = new SoccerAi(world, seed);
            ai.SetLevel(SoccerRules.Red, SoccerBalance.AiLevel(redStars));
            ai.SetLevel(SoccerRules.Blue, SoccerBalance.AiLevel(blueStars));
            var inputs = new SoccerInput[world.Players.Count];
            var events = new List<SoccerEvent>();
            var pitch = world.Pitch;
            var goalDepth = pitch.GoalOf(SoccerRules.Red).Depth;
            var escaped = false;
            for (var step = 0; step < Steps(seconds); step++)
            {
                events.Clear();
                ai.Think(world, inputs, _ => false);
                world.Step(inputs, events);
                var ball = world.Ball.Position;
                escaped |= Math.Abs(ball.X) > pitch.HalfLength + goalDepth || Math.Abs(ball.Y) > pitch.HalfWidth;
                if (world.IsGoalScored)
                {
                    var scorer = events.First(e => e.Kind == SoccerEventKind.Goal).Team;
                    world.Kickoff(SoccerRules.Opponent(scorer), events);
                }
            }

            return (world.Score(SoccerRules.Red), world.Score(SoccerRules.Blue), escaped);
        }

        [Test]
        public void Ai_SameSeedPlaysTheSameMatch()
        {
            Assert.That(PlayAiMatch(3, 3, 7, 30f), Is.EqualTo(PlayAiMatch(3, 3, 7, 30f)));
        }

        /// <summary>CPU 同士でも点が入り、ボールがピッチの外へ出ない</summary>
        [Test]
        public void Ai_MatchScoresAndKeepsTheBallInside()
        {
            var (red, blue, escaped) = PlayAiMatch(3, 3, 11, SoccerRules.MatchSeconds);
            Assert.That(red + blue, Is.GreaterThan(0));
            Assert.That(escaped, Is.False);
        }

        [Test]
        public void Ai_StrongerTeamScoresMore()
        {
            var strong = 0;
            var weak = 0;
            for (var seed = 0; seed < 3; seed++)
            {
                var (red, blue, _) = PlayAiMatch(SoccerBalance.AiMaxStars, SoccerBalance.AiMinStars, 100 + seed, 60f);
                strong += red;
                weak += blue;
            }

            Assert.That(strong, Is.GreaterThan(weak));
        }

        [Test]
        public void MatchCheck_CountsAMatch()
        {
            var stats = SoccerMatchCheck.Play(SoccerBalance.AiLevel(3), SoccerBalance.AiLevel(3), 5, 60f);
            Assert.That(stats.Shots + stats.Passes, Is.GreaterThan(0));
            Assert.That(stats.CrowdAverage, Is.GreaterThan(0f));
            Assert.That(stats.LongestIdleSeconds, Is.LessThan(SoccerBalance.IdleNudgeSeconds + 1f));
        }
    }
}
