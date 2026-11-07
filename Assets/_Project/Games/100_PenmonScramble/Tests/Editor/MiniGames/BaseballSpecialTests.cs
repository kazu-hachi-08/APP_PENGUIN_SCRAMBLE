using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PenmonScramble.Baseball.Core;
using PenmonScramble.Core;

namespace PenmonScramble.Tests
{
    /// <summary>野球の魔球・必殺打・球場・報酬（Docs/MINIGAME_BASEBALL_PLAN.md フェーズ 4）</summary>
    public class BaseballSpecialTests
    {
        private const float Dt = BaseballRules.StepSeconds;
        private const int MaxSteps = 2000;

        private static BaseballFieldDef Field => BaseballFieldTable.Test();

        private static BaseballPitchOutcome InPlay(BaseballHitResult hit) => new BaseballPitchOutcome(BaseballPitchOutcomeKind.InPlay, hit, false);

        private static BaseballPitchMagic Magic(Element element, int stars, float freeze = 0.5f, float phase = 0f) =>
            new BaseballPitchMagic(new BaseballSpecial(element, stars), freeze, phase);

        /// <summary>魔球を投げて、ホームの線を越えるまでの秒と、毎歩の様子を返す（曲げる入力は bend）</summary>
        private static float TimeToPlate(BaseballPitchMagic magic, BaseballPitchType type, float bend, Action<BaseballPitch> each = null)
        {
            var pitch = new BaseballPitch(Field);
            pitch.Start(type, magic);
            var events = new List<BaseballEvent>();
            for (var step = 0; step < MaxSteps; step++)
            {
                pitch.Step(bend, Dt, events);
                each?.Invoke(pitch);
                if (pitch.Position.Y <= BaseballFieldDef.PlateY)
                {
                    return pitch.Elapsed;
                }
            }

            Assert.Fail("ホームに届かない");
            return 0f;
        }

        // ---- 魔球 ----

        [Test]
        public void Freeze_StopsTheBallForTheRolledSeconds()
        {
            var plain = TimeToPlate(BaseballPitchMagic.None, BaseballPitchType.Curve, 0f);
            var frozen = TimeToPlate(Magic(Element.Ice, 2, 0.5f), BaseballPitchType.Curve, 0f);
            Assert.That(frozen - plain, Is.EqualTo(0.5f).Within(Dt * 1.5f));
        }

        [Test]
        public void Freeze_HappensAfterTheBendableSection()
        {
            var frozenAt = -1f;
            TimeToPlate(Magic(Element.Ice, 1, 0.4f), BaseballPitchType.Straight, 0f, p =>
            {
                if (p.IsFrozen && frozenAt < 0f)
                {
                    frozenAt = p.Progress;
                    Assert.That(p.CanBend, Is.False);
                }
            });
            Assert.That(frozenAt, Is.GreaterThanOrEqualTo(BaseballBalance.FreezeAt));
        }

        [Test]
        public void FreezeSeconds_RollWithinTheStarRange()
        {
            var random = new Random(3);
            for (var stars = 1; stars <= 3; stars++)
            {
                for (var i = 0; i < 50; i++)
                {
                    var magic = BaseballPitchMagic.Roll(new BaseballSpecial(Element.Ice, stars), random);
                    Assert.That(magic.FreezeSeconds, Is.InRange(BaseballBalance.FreezeMinSeconds(stars), BaseballBalance.FreezeMaxSeconds(stars)));
                }
            }

            Assert.That(BaseballBalance.FreezeMaxSeconds(3) - BaseballBalance.FreezeMinSeconds(3),
                Is.GreaterThan(BaseballBalance.FreezeMaxSeconds(1) - BaseballBalance.FreezeMinSeconds(1)), "★が高いほど幅が広い");
        }

        [Test]
        public void Wobble_SwaysWithoutBending()
        {
            var widest = 0f;
            TimeToPlate(Magic(Element.Water, 3, phase: 0.7f), BaseballPitchType.Changeup, 0f, p =>
            {
                widest = Math.Max(widest, Math.Abs(p.Position.X));
                Assert.That(p.SteerX, Is.EqualTo(0f), "揺れは曲げで進んだ位置を変えない");
            });
            Assert.That(widest, Is.GreaterThan(BaseballBalance.WobbleAmplitude(3)));
        }

        [Test]
        public void Wind_BendsHarderAndReversesAtTheEnd()
        {
            float plainEndX = 0f, windEndX = 0f, windLateral = 0f;
            var plain = new BaseballPitch(Field);
            var wind = new BaseballPitch(Field);
            plain.Start(BaseballPitchType.Curve);
            wind.Start(BaseballPitchType.Curve, Magic(Element.Wind, 2));
            for (var step = 0; step < MaxSteps && wind.Position.Y > 0f; step++)
            {
                var plainEnded = plain.Step(1f, Dt);
                if (wind.Step(1f, Dt))
                {
                    windEndX = wind.Position.X;
                    windLateral = wind.LateralSpeed;
                }

                if (plainEnded)
                {
                    plainEndX = plain.Position.X;
                }
            }

            Assert.That(windEndX, Is.GreaterThan(plainEndX), "曲げる入力が大きく効く");
            Assert.That(windLateral, Is.EqualTo(-BaseballBalance.WindReverseSpeed(2)), "区間の後は逆へ曲がる");
            Assert.That(wind.Position.X, Is.LessThan(windEndX));
        }

        [Test]
        public void Vanish_HidesOnlyInTheMiddle()
        {
            var hiddenProgress = new List<float>();
            var pitch = new BaseballPitch(Field);
            pitch.Start(BaseballPitchType.Curve, Magic(Element.Thunder, 2));
            var events = new List<BaseballEvent>();
            for (var step = 0; step < MaxSteps && pitch.Position.Y > 0f; step++)
            {
                pitch.Step(0f, Dt, events);
                if (pitch.IsHidden)
                {
                    hiddenProgress.Add(pitch.Progress);
                }
            }

            Assert.That(hiddenProgress, Is.Not.Empty);
            Assert.That(hiddenProgress.Min(), Is.GreaterThanOrEqualTo(BaseballBalance.VanishAt));
            Assert.That(hiddenProgress.Max(), Is.LessThan(BaseballBalance.AppearAt(2)));
            Assert.That(pitch.IsHidden, Is.False, "ホームの手前では見える");
            Assert.That(events.Count(e => e.Kind == BaseballEventKind.MagicVanish), Is.EqualTo(1));
            Assert.That(events.Count(e => e.Kind == BaseballEventKind.MagicAppear), Is.EqualTo(1));
        }

        // ---- 必殺打の打球 ----

        /// <summary>穴の手前から、穴へ向けて遅く転がす</summary>
        private static BaseballBallSim RollInto(BaseballHole hole, float speed, BaseballSpecial special, out List<BaseballEvent> events)
        {
            var direction = hole.Center.Normalized;
            var from = hole.Center - direction * 0.8f;
            var sim = new BaseballBallSim(Field);
            sim.Launch(from, new BaseballHit(direction, speed, 0f, 0.3f, false, false), special);
            events = new List<BaseballEvent>();
            for (var step = 0; step < MaxSteps && sim.IsActive; step++)
            {
                sim.Step(Dt, events);
            }

            return sim;
        }

        private static int HoleIndex(BaseballHitResult result) => Field.Holes.ToList().FindIndex(h => h.Result == result);

        [Test]
        public void IceRoad_SlidesOverOutHoles()
        {
            var index = HoleIndex(BaseballHitResult.GroundOut);
            var hole = Field.Holes[index];
            Assert.That(RollInto(hole, 3f, BaseballSpecial.None, out _).HoleIndex, Is.EqualTo(index), "ふつうは落ちる");
            Assert.That(RollInto(hole, 3f, new BaseballSpecial(Element.Ice, 1), out _).HoleIndex, Is.Not.EqualTo(index));
        }

        [Test]
        public void IceRoad_StillDropsIntoHitHoles()
        {
            var index = HoleIndex(BaseballHitResult.Single);
            Assert.That(RollInto(Field.Holes[index], 3f, new BaseballSpecial(Element.Ice, 1), out _).HoleIndex, Is.EqualTo(index));
        }

        [Test]
        public void Thunder_DoesNotDropWhileFast()
        {
            var index = HoleIndex(BaseballHitResult.GroundOut);
            Assert.That(RollInto(Field.Holes[index], 5f, BaseballSpecial.None, out _).HoleIndex, Is.EqualTo(index), "ふつうは落ちる");
            var sim = RollInto(Field.Holes[index], 5f, new BaseballSpecial(Element.Thunder, 1), out _);
            Assert.That(sim.HoleIndex, Is.Not.EqualTo(index));
        }

        [Test]
        public void WaterSkip_HopsStarsTimes()
        {
            for (var stars = 1; stars <= 3; stars++)
            {
                var sim = new BaseballBallSim(Field);
                sim.Launch(new Vec2(0f, 0.3f), new BaseballHit(new Vec2(0f, 1f), 8f, 0f, 0.6f, false, false), new BaseballSpecial(Element.Water, stars));
                Assert.That(sim.IsFlying, Is.True, "転がる打球も最初から跳ぶ");
                var events = new List<BaseballEvent>();
                for (var step = 0; step < MaxSteps && sim.IsActive; step++)
                {
                    sim.Step(Dt, events);
                }

                Assert.That(events.Count(e => e.Kind == BaseballEventKind.Skip), Is.EqualTo(stars - 1), $"★{stars}");
            }
        }

        [Test]
        public void WindSteer_TurnsTheBallTowardTheInput()
        {
            float EndX(float steer)
            {
                var sim = new BaseballBallSim(Field);
                sim.Launch(new Vec2(0f, 0.3f), new BaseballHit(new Vec2(0f, 1f), 9f, 0f, 0.6f, false, false), new BaseballSpecial(Element.Wind, 3));
                var events = new List<BaseballEvent>();
                for (var step = 0; step < 15; step++)
                {
                    sim.SetSteer(steer);
                    sim.Step(Dt, events);
                }

                return sim.Position.X;
            }

            Assert.That(EndX(1f), Is.GreaterThan(0.3f));
            Assert.That(EndX(-1f), Is.LessThan(-0.3f));
        }

        // ---- 盤: 必殺打を使った扱い ----

        [Test]
        public void SpecialHit_IsUsedOnlyWhenSwingMakesContact()
        {
            var special = new BaseballSpecial(Element.Thunder, 1);
            var used = 0;
            var whiffs = 0;
            for (var swingStep = 0; swingStep < 60; swingStep++)
            {
                var board = new BaseballBoard(Field);
                var events = new List<BaseballEvent>();
                board.Throw(BaseballPitchType.Changeup, BaseballPitchMagic.None, special, events);
                for (var step = 0; step < MaxSteps && board.Phase != BaseballBoardPhase.Done; step++)
                {
                    board.Step(new BaseballInput(0f, step == swingStep, false), events);
                }

                if (board.LastHit.HasValue)
                {
                    Assert.That(board.SpecialHitUsed, Is.True);
                    Assert.That(events.Any(e => e.Kind == BaseballEventKind.SpecialHit), Is.True);
                    used++;
                }
                else
                {
                    Assert.That(board.SpecialHitUsed, Is.False, "空振り・見送りでは残る");
                    whiffs++;
                }
            }

            Assert.That(used, Is.GreaterThan(0));
            Assert.That(whiffs, Is.GreaterThan(0));
        }

        // ---- 試合: 各子1試合1回 ----

        [Test]
        public void Game_TracksMagicAndSpecialPerChild()
        {
            var game = new BaseballGame(3, 3);
            Assert.That(game.CanUseMagic, Is.True);
            Assert.That(game.CanUseSpecialHit, Is.True);
            game.UseMagic();
            game.UseSpecialHit();
            Assert.That(game.CanUseMagic, Is.False);
            Assert.That(game.IsMagicUsed(BaseballRules.Home, 0), Is.True);
            Assert.That(game.IsSpecialHitUsed(BaseballRules.Away, 0), Is.True);
            Assert.That(game.IsSpecialHitUsed(BaseballRules.Away, 1), Is.False, "必殺打と魔球は子ごと・別に数える");
            game.Apply(InPlay(BaseballHitResult.Single));
            Assert.That(game.CanUseSpecialHit, Is.True, "次の打者はまだ出せる");
        }

        [Test]
        public void Practice_RefillsSpecialsAfterThreeOuts()
        {
            var game = BaseballGame.Practice(3, 3, BaseballRules.Away);
            game.UseSpecialHit();
            for (var i = 0; i < BaseballRules.OutsPerInning; i++)
            {
                game.Apply(InPlay(BaseballHitResult.GroundOut));
            }

            game.StartNextHalf();
            Assert.That(game.IsSpecialHitUsed(BaseballRules.Away, 0), Is.False);
        }

        // ---- CPU ----

        [Test]
        public void Ai_NeverUsesSpentMagic()
        {
            var game = new BaseballGame(3, 3);
            game.UseMagic();
            var ai = new BaseballAi(1, BaseballBalance.AiSkill(3));
            for (var i = 0; i < 100; i++)
            {
                Assert.That(ai.WantsMagic(game), Is.False);
            }
        }

        [Test]
        public void Ai_SwitchesToPitcherWithMagicInAPinch()
        {
            var game = new BaseballGame(3, 3);
            game.UseMagic();
            game.Apply(InPlay(BaseballHitResult.Double));
            Assert.That(BaseballAi.ChoosePitcher(game), Is.EqualTo(1), "得点圏に走者がいて、順番の子の魔球が残っていない");
        }

        [Test]
        public void MatchCheck_UsesSpecials()
        {
            var skill = BaseballBalance.AiSkill(3);
            var thrown = 0;
            var used = 0;
            for (var seed = 0; seed < 10; seed++)
            {
                var stats = BaseballMatchCheck.Play(skill, skill, seed);
                thrown += stats.MagicThrown.Sum();
                used += stats.SpecialContact.Sum();
            }

            Assert.That(thrown, Is.GreaterThan(10));
            Assert.That(used, Is.GreaterThan(10));
        }

        // ---- 球場と報酬 ----

        [Test]
        public void Stadium_BeachIsFirstAndPicksDistinctWild()
        {
            var beach = BaseballStadiumTable.Get(1);
            Assert.That(beach, Is.Not.Null);
            Assert.That(beach.CpuStars, Is.EqualTo(1));
            Assert.That(BaseballStadiumTable.Get(6), Is.Null);
            var picked = beach.PickWild(BaseballStadiumTable.CpuLineup, 5);
            Assert.That(picked.Distinct().Count(), Is.EqualTo(picked.Length));
            Assert.That(picked.All(s => beach.WildSpecies.Contains(s)), Is.True);
            Assert.That(beach.CreateField().Holes.Count, Is.EqualTo(Field.Holes.Count));
        }

        [Test]
        public void Reward_WinDrawLossAndFirstWin()
        {
            var win = BaseballReward.Compute(7, BaseballOutcome.Win, true);
            Assert.That(win.Total, Is.EqualTo(BaseballReward.BaseCoins + BaseballReward.WinCoins + BaseballReward.MaxPaidRuns * BaseballReward.CoinsPerRun + BaseballReward.FirstWinBonus));
            var draw = BaseballReward.Compute(2, BaseballOutcome.Draw, true);
            Assert.That(draw.FirstWinBonus, Is.EqualTo(0), "引き分けは初勝利にならない");
            Assert.That(draw.WinCoins, Is.EqualTo(BaseballReward.DrawCoins));
            Assert.That(BaseballReward.Compute(0, BaseballOutcome.Loss, false).Total, Is.EqualTo(BaseballReward.BaseCoins));
            Assert.That(BaseballReward.OutcomeOf(3, 3), Is.EqualTo(BaseballOutcome.Draw));
            Assert.That(BaseballReward.OutcomeOf(1, 3), Is.EqualTo(BaseballOutcome.Loss));
        }

        [Test]
        public void Special_OfSpeciesUsesElementAndStars()
        {
            var special = BaseballSpecial.Of(5);
            Assert.That(special.Element, Is.EqualTo(Element.Ice));
            Assert.That(special.Stars, Is.EqualTo(3));
            Assert.That(BaseballSpecial.Of(99).IsActive, Is.False);
        }
    }
}
